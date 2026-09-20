using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using ImGuiNET;
using TerrainSystem;
using TerrainSystem.Lods;
using Trading;
using UI.ImGuiUi;
using UnityEngine;

namespace Assets.Scripts;

[DefaultExecutionOrder(int.MaxValue)]
public class CinematicCamera : MonoBehaviour, ILodRequester, IReferencable, IEvaluable, IThreadable
{
	[Header("Movement (m/s)")]
	public float BaseSpeed = 8f;

	public float MinSpeed = 0.25f;

	public float MaxSpeed = 200f;

	[Tooltip("Multiplicative change per scroll-wheel tick. 1.3 = +30% / -23% per notch.")]
	public float ScrollSpeedFactor = 1.3f;

	[Tooltip("Time-constant smoothing of the base-speed setting itself, so scroll-wheel ticks don't snap movement instantly. 0 = raw (jumpy).")]
	public float SpeedSmoothing = 8f;

	[Tooltip("Multiplier when the QuantityModifier (Shift) key is held.")]
	public float FastMultiplier = 5f;

	[Tooltip("Slow / fine-tune modifier. Defaults to LeftAlt to avoid clashing with the Descend keybind, which is LeftControl by default.")]
	public KeyCode SlowModifier = KeyCode.LeftAlt;

	public float SlowMultiplier = 0.2f;

	[Header("Smoothing (higher = snappier; lower = silkier for trailers)")]
	[Tooltip("Time-constant smoothing of translation velocity. 0 = no smoothing (raw, twitchy).")]
	public float MoveSmoothing = 8f;

	[Tooltip("Time-constant smoothing of mouse look + roll. Lower = more cinematic glide.")]
	public float LookSmoothing = 12f;

	[Header("Rotation")]
	public float LookSensitivity = 2f;

	public float RollSpeed = 60f;

	[Header("Screenshot")]
	public int ScreenshotWidth = 3840;

	public int ScreenshotHeight = 2160;

	[Tooltip("MSAA samples for the offscreen render. 8 is the Unity max; 4 is a good speed/quality balance.")]
	[Range(1f, 8f)]
	public int ScreenshotMsaa = 8;

	[Header("Time-lapse")]
	[Tooltip("Capture rate in frames per second. 0.5 = one frame every 2 seconds. Reuses Screenshot W/H/MSAA. PNG encoding at 4K hitches the main thread, so keep the rate sane (≤2 fps at 4K).")]
	public float TimelapseFps = 0.5f;

	[Tooltip("Wall-clock duration in minutes. Time-lapse auto-stops when this elapses, or sooner if the cinematic camera is deactivated.")]
	public float TimelapseDurationMinutes = 5f;

	[Tooltip("Subfolder under <save>/screenshots to write into. Empty = auto-named 'timelapse_<timestamp>'. Frames are named frame_00000.png upward, ready for `ffmpeg -i frame_%05d.png`.")]
	public string TimelapseSubfolder = "";

	[Tooltip("If true, drive the camera along the keyframe path during time-lapse instead of holding a static anchor. Path runs at its natural speed (per-keyframe Speed); set 'Loop path during lapse' to repeat it for the full lapse duration.")]
	public bool TimelapseUsePath;

	[Tooltip("Loop the path during the time-lapse. Camera transitions Catmull-Rom-smoothly from the last keyframe back to the first instead of teleporting.")]
	public bool TimelapseLoop;

	[Header("Path / Keyframes")]
	[Tooltip("Render the path lines + frustum gizmos in the world view.")]
	public bool ShowPathOverlay = true;

	[Tooltip("Keep the overlay visible even when cinematic mode is off (e.g., while playing the game) so you can see where your shot is set up.")]
	public bool ShowOverlayOutsideCinematic;

	[Tooltip("Playback speed multiplier for path preview. 1.0 = real-time per the per-keyframe Speed values.")]
	public float PathPlaybackSpeed = 1f;

	public bool PathPlaybackLoop;

	[Tooltip("Hotkey to capture the current camera pose as a new keyframe (cinematic only).")]
	public KeyCode AddKeyframeKey = KeyCode.K;

	[Header("Start sequence")]
	[Tooltip("Seconds of countdown before time-lapse / path actually starts. 0 = no countdown.")]
	public float CountdownSeconds;

	[Tooltip("If checked, exit cinematic mode automatically the moment a time-lapse or path playback starts (after the countdown). Lets you go play while the lapse captures.")]
	public bool AutoReturnOnStart;

	[Header("Tether")]
	[Tooltip("Soft-clamp distance from player (m). 0 = unlimited. Terrain LODs are keyed off the player's body, so flying past loaded terrain shows low-LOD meshes.")]
	[SerializeField]
	private float _tetherRadius;

	[Header("Settings Window")]
	[Tooltip("Key that toggles the ImGui settings panel while cinematic is active.")]
	public KeyCode SettingsToggleKey = KeyCode.Tab;

	private const string InputStateKey = "CinematicCamera";

	private const long SyntheticReferenceId = -9223372036854775807L;

	private const float LodMoveThreshold = 8f;

	private bool _active;

	private Vector3 _velocity;

	private float _baseSpeedSmoothed;

	private float _targetYaw;

	private float _targetPitch;

	private float _targetRoll;

	private float _yaw;

	private float _pitch;

	private float _roll;

	private float _entryFov;

	private bool _entryUiVisible;

	private bool _entryThirdPerson;

	private CursorLockMode _entryCursorLock;

	private bool _entryCursorVisible;

	private bool _entryCursorRaycastBlocked;

	private Vector3 _lastLodRequestPosition;

	private Vector3 _centerPosition;

	private bool _registeredWithLod;

	private bool _timelapseRunning;

	private int _timelapseFrameCount;

	private float _timelapseStartTimeReal;

	private string _timelapseDir;

	private Vector3 _timelapseAnchorPos;

	private Quaternion _timelapseAnchorRot = Quaternion.identity;

	private float _timelapseAnchorFov = 70f;

	private bool _hasLastCinematicPose;

	private Vector3 _lastCinematicPos;

	private Quaternion _lastCinematicRot;

	private float _lastCinematicFov;

	public int SelectedKeyframeIndex = -1;

	private bool _pathPlaying;

	private bool _pathPaused;

	private float _pathTime;

	private float _countdownRemaining;

	private string _countdownLabel;

	private bool _capturing;

	private const int PathSegmentSubdivisions = 24;

	private static readonly uint PathColorNormal = ImGuiColor.Integer.White;

	private static readonly uint PathColorLoop = ImGuiColor.Integer.LightBlue;

	private static readonly uint PathColorPlayhead = ImGuiColor.Integer.LightBlue;

	private static readonly uint FrustumColorNormal = ImGuiColor.Integer.Yellow;

	private static readonly uint FrustumColorSelected = ImGuiColor.Integer.LightBlue;

	public static CinematicCamera Instance { get; private set; }

	public static bool IsActive
	{
		get
		{
			if (Instance != null)
			{
				return Instance._active;
			}
			return false;
		}
	}

	public float TetherRadius
	{
		get
		{
			return _tetherRadius;
		}
		set
		{
			_tetherRadius = Mathf.Max(0f, value);
		}
	}

	public CinematicPath CurrentPath { get; private set; } = new CinematicPath();

	public float CurrentSpeed => _velocity.magnitude;

	public bool TimelapseRunning => _timelapseRunning;

	public int TimelapseFrameCount => _timelapseFrameCount;

	public string TimelapseDirectory => _timelapseDir;

	public float TimelapseElapsedSeconds
	{
		get
		{
			if (!_timelapseRunning)
			{
				return 0f;
			}
			return Time.realtimeSinceStartup - _timelapseStartTimeReal;
		}
	}

	public bool PathPlaying => _pathPlaying;

	public bool PathPaused => _pathPaused;

	public float PathTime => _pathTime;

	public float CountdownRemaining => _countdownRemaining;

	public string CountdownLabel => _countdownLabel;

	public bool ShouldRender => _active;

	public HashSet<Vector3Int>[] RequestedLods { get; set; } = LodHelper.InitArray(6);

	public LodInfo LodInfo => LodManager.PlayerLodInfo;

	public Vector3 CenterPosition => _centerPosition;

	public string DisplayName => "CinematicCamera";

	public ushort NetworkUpdateFlags { get; set; }

	public long ReferenceId { get; set; } = -9223372036854775807L;

	public bool BeingDestroyed { get; set; }

	public int ThreadCost => 1;

	public static CinematicCamera GetOrCreate()
	{
		if (Instance != null)
		{
			return Instance;
		}
		CinematicCamera cinematicCamera = UnityEngine.Object.FindObjectOfType<CinematicCamera>(includeInactive: true);
		if (cinematicCamera != null)
		{
			Instance = cinematicCamera;
			return Instance;
		}
		Instance = new GameObject("CinematicCamera").AddComponent<CinematicCamera>();
		return Instance;
	}

	public static bool Toggle()
	{
		CinematicCamera orCreate = GetOrCreate();
		if (orCreate._active)
		{
			orCreate.Deactivate();
		}
		else
		{
			orCreate.Activate();
		}
		return orCreate._active;
	}

	public static void SetActive(bool on)
	{
		CinematicCamera orCreate = GetOrCreate();
		if (on && !orCreate._active)
		{
			orCreate.Activate();
		}
		else if (!on && orCreate._active)
		{
			orCreate.Deactivate();
		}
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			Instance = this;
		}
	}

	private void OnDestroy()
	{
		_timelapseRunning = false;
		_pathPlaying = false;
		_pathPaused = false;
		if (_active)
		{
			RestoreState();
		}
		if (_registeredWithLod && LodManager.Instance != null)
		{
			BeingDestroyed = true;
			LodManager.EnqueueRequesterToRemove(this);
			_registeredWithLod = false;
		}
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void Activate()
	{
		if (_active || GameManager.IsBatchMode || GameManager.GameState != GameState.Running)
		{
			return;
		}
		Camera currentCamera = CameraController.CurrentCamera;
		if (!(currentCamera == null))
		{
			Transform transform = currentCamera.transform;
			_entryFov = currentCamera.fieldOfView;
			_entryUiVisible = InventoryManager.ShowUi;
			_entryThirdPerson = CameraController.IsThirdPerson;
			_entryCursorLock = Cursor.lockState;
			_entryCursorVisible = Cursor.visible;
			_entryCursorRaycastBlocked = CursorManager.Instance.BlockCursorRaycast;
			CameraController.CinematicMode = true;
			KeyManager.SetInputState("CinematicCamera", KeyInputState.Cinematic);
			InventoryManager.SetUiState(showUi: false);
			if (CameraController.Instance != null)
			{
				CameraController.Instance.SetThirdPersonCamera(show: true);
			}
			if (_hasLastCinematicPose)
			{
				transform.SetPositionAndRotation(_lastCinematicPos, _lastCinematicRot);
				CameraController.SetFieldOfView(_lastCinematicFov);
			}
			else
			{
				CameraController.SetFieldOfView(Settings.CurrentData.FieldOfView);
			}
			Vector3 eulerAngles = transform.rotation.eulerAngles;
			_yaw = (_targetYaw = eulerAngles.y);
			_pitch = (_targetPitch = NormalisePitch(eulerAngles.x));
			_roll = (_targetRoll = 0f);
			_velocity = Vector3.zero;
			_baseSpeedSmoothed = BaseSpeed;
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			CursorManager.Instance.BlockCursorRaycast = true;
			_centerPosition = transform.position;
			_lastLodRequestPosition = _centerPosition;
			BeingDestroyed = false;
			for (int i = 0; i < RequestedLods.Length; i++)
			{
				RequestedLods[i].Clear();
			}
			if (LodManager.Instance != null)
			{
				LodManager.EnqueueRequesterToUpdate(this);
				_registeredWithLod = true;
			}
			_active = true;
		}
	}

	public void Deactivate()
	{
		if (_active)
		{
			RestoreState();
			_active = false;
		}
	}

	private void RestoreState()
	{
		Camera currentCamera = CameraController.CurrentCamera;
		if (currentCamera != null)
		{
			_lastCinematicPos = currentCamera.transform.position;
			_lastCinematicRot = currentCamera.transform.rotation;
			_lastCinematicFov = currentCamera.fieldOfView;
			_hasLastCinematicPose = true;
		}
		bool timelapseRunning = _timelapseRunning;
		if (CinematicSettingsWindow.IsOpen)
		{
			CinematicSettingsWindow.Close();
		}
		if (_registeredWithLod && LodManager.Instance != null && !timelapseRunning)
		{
			BeingDestroyed = true;
			LodManager.EnqueueRequesterToRemove(this);
			_registeredWithLod = false;
		}
		CameraController.CinematicMode = false;
		KeyManager.RemoveInputState("CinematicCamera");
		CameraController.SetFieldOfView(_entryFov);
		if (CameraController.Instance != null)
		{
			CameraController.Instance.SetThirdPersonCamera(_entryThirdPerson);
		}
		InventoryManager.SetUiState(_entryUiVisible);
		Cursor.lockState = _entryCursorLock;
		Cursor.visible = _entryCursorVisible;
		CursorManager.Instance.BlockCursorRaycast = _entryCursorRaycastBlocked;
	}

	private void LateUpdate()
	{
		if (!_active)
		{
			return;
		}
		if (GameManager.GameState != GameState.Running)
		{
			Deactivate();
			return;
		}
		if (KeyManager.GetButtonDown(KeyMap.Cancel))
		{
			if (CinematicSettingsWindow.IsOpen)
			{
				CinematicSettingsWindow.Close();
			}
			else
			{
				Deactivate();
			}
			return;
		}
		Camera currentCamera = CameraController.CurrentCamera;
		if (currentCamera == null)
		{
			return;
		}
		Transform transform = currentCamera.transform;
		float unscaledDeltaTime = Time.unscaledDeltaTime;
		bool flag = false;
		if (Input.GetKeyDown(SettingsToggleKey) && !ImGui.GetIO().WantCaptureKeyboard)
		{
			ToggleSettingsWindow();
		}
		flag = CinematicSettingsWindow.IsOpen;
		if (KeyManager.GetButtonDown(KeyMap.ScreenShot) && !_capturing)
		{
			StartCoroutine(CaptureScreenshot(currentCamera));
		}
		bool flag2 = _timelapseRunning || _pathPlaying;
		if (_pathPlaying && CurrentPath != null && CurrentPath.Count > 0)
		{
			bool flag3 = ((_timelapseRunning && TimelapseUsePath) ? TimelapseLoop : PathPlaybackLoop);
			if (_timelapseRunning && TimelapseUsePath)
			{
				_pathTime = Time.realtimeSinceStartup - _timelapseStartTimeReal;
			}
			else if (!_pathPaused)
			{
				_pathTime += unscaledDeltaTime * Mathf.Max(0f, PathPlaybackSpeed);
				if (!flag3)
				{
					float totalDuration = CurrentPath.TotalDuration;
					if (totalDuration > 0f && _pathTime >= totalDuration)
					{
						_pathTime = totalDuration;
						StopPath();
					}
				}
			}
			if (CurrentPath.Evaluate(_pathTime, flag3, out var position, out var rotation, out var fov))
			{
				transform.SetPositionAndRotation(position, rotation);
				currentCamera.fieldOfView = fov;
				PublishCameraState(position, rotation);
				MaybeRequestLodUpdate(position);
			}
		}
		else if (!flag2)
		{
			if (!flag)
			{
				AccumulateLookInput();
				AccumulateRollInput(unscaledDeltaTime);
				ApplyScrollSpeed();
			}
			SmoothRotationToTargets(unscaledDeltaTime);
			SmoothBaseSpeed(unscaledDeltaTime);
			Quaternion rotation2 = Quaternion.Euler(_pitch, _yaw, _roll);
			Vector3 vector = ComputeMoveTarget(rotation2);
			_velocity = ((MoveSmoothing > 0f) ? Vector3.Lerp(_velocity, vector, 1f - Mathf.Exp((0f - MoveSmoothing) * unscaledDeltaTime)) : vector);
			Vector3 candidate = transform.position + _velocity * unscaledDeltaTime;
			candidate = ApplyTether(candidate);
			transform.SetPositionAndRotation(candidate, rotation2);
			PublishCameraState(candidate, rotation2);
			MaybeRequestLodUpdate(candidate);
		}
		if (flag)
		{
			if (Cursor.lockState != CursorLockMode.None)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}
		else if (Cursor.lockState != CursorLockMode.Locked)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}

	private void Update()
	{
		if (!GameManager.IsBatchMode && _active && !_timelapseRunning && !_pathPlaying && Input.GetKeyDown(AddKeyframeKey) && !ImGui.GetIO().WantCaptureKeyboard)
		{
			AddKeyframeFromCurrent();
		}
	}

	public static void DrawOverlay()
	{
		CinematicCamera instance = Instance;
		if (!(instance == null))
		{
			if (instance.ShouldDrawOverlay())
			{
				instance.DrawPathOverlay();
			}
			instance.DrawCountdownOverlay();
		}
	}

	private bool ShouldDrawOverlay()
	{
		if (!ShowPathOverlay)
		{
			return false;
		}
		if (CurrentPath == null || CurrentPath.Count == 0)
		{
			return false;
		}
		if (!_active)
		{
			return ShowOverlayOutsideCinematic;
		}
		return true;
	}

	public static void ToggleSettingsWindow()
	{
		if (CinematicSettingsWindow.IsOpen)
		{
			CinematicSettingsWindow.Close();
		}
		else
		{
			CinematicSettingsWindow.Open();
		}
	}

	public void ResetRoll()
	{
		_targetRoll = 0f;
		_roll = 0f;
	}

	private void AccumulateLookInput()
	{
		InputManager instance = Singleton<InputManager>.Instance;
		if (!(instance == null))
		{
			float axis = instance.GetAxis("LookY");
			float axis2 = instance.GetAxis("LookX");
			float num = (Settings.CurrentData.InvertMouse ? 1f : (-1f));
			_targetPitch = Mathf.Clamp(_targetPitch + axis * LookSensitivity * num, -89.9f, 89.9f);
			_targetYaw = Mathf.Repeat(_targetYaw + axis2 * LookSensitivity, 360f);
		}
	}

	private void AccumulateRollInput(float dt)
	{
		float num = 0f;
		if (Input.GetKey(KeyMap.RotateRollLeft))
		{
			num -= 1f;
		}
		if (Input.GetKey(KeyMap.RotateRollRight))
		{
			num += 1f;
		}
		_targetRoll = Mathf.Repeat(_targetRoll + num * RollSpeed * dt, 360f);
	}

	private void SmoothRotationToTargets(float dt)
	{
		if (LookSmoothing <= 0f)
		{
			_yaw = _targetYaw;
			_pitch = _targetPitch;
			_roll = _targetRoll;
		}
		else
		{
			float t = 1f - Mathf.Exp((0f - LookSmoothing) * dt);
			_yaw = Mathf.LerpAngle(_yaw, _targetYaw, t);
			_pitch = Mathf.Lerp(_pitch, _targetPitch, t);
			_roll = Mathf.LerpAngle(_roll, _targetRoll, t);
		}
	}

	private void ApplyScrollSpeed()
	{
		float y = Input.mouseScrollDelta.y;
		if (!(Mathf.Abs(y) < float.Epsilon))
		{
			BaseSpeed = Mathf.Clamp(BaseSpeed * Mathf.Pow(ScrollSpeedFactor, y), MinSpeed, MaxSpeed);
		}
	}

	private void SmoothBaseSpeed(float dt)
	{
		if (SpeedSmoothing <= 0f)
		{
			_baseSpeedSmoothed = BaseSpeed;
		}
		else
		{
			_baseSpeedSmoothed = Mathf.Lerp(_baseSpeedSmoothed, BaseSpeed, 1f - Mathf.Exp((0f - SpeedSmoothing) * dt));
		}
	}

	private IEnumerator CaptureScreenshot(Camera cam)
	{
		_capturing = true;
		yield return Yielders.EndOfFrame;
		int num = Mathf.Max(1, ScreenshotWidth);
		int num2 = Mathf.Max(1, ScreenshotHeight);
		int msaa = Mathf.Clamp(ScreenshotMsaa, 1, 8);
		try
		{
			string arg = WriteScreenshot(RenderCameraToPng(cam, num, num2, msaa));
			ConsoleWindow.PrintAction($"Screenshot saved: {arg} ({num}x{num2})");
		}
		catch (Exception ex)
		{
			ConsoleWindow.PrintError("Screenshot failed: " + ex.Message);
		}
		finally
		{
			_capturing = false;
		}
	}

	private static byte[] RenderCameraToPng(Camera cam, int width, int height, int msaa)
	{
		RenderTexture renderTexture = null;
		Texture2D texture2D = null;
		RenderTexture targetTexture = cam.targetTexture;
		RenderTexture active = RenderTexture.active;
		try
		{
			renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
			{
				antiAliasing = msaa
			};
			renderTexture.Create();
			cam.targetTexture = renderTexture;
			cam.Render();
			RenderTexture.active = renderTexture;
			texture2D = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false);
			texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, recalculateMipMaps: false);
			texture2D.Apply(updateMipmaps: false);
			return texture2D.EncodeToPNG();
		}
		finally
		{
			cam.targetTexture = targetTexture;
			RenderTexture.active = active;
			if (texture2D != null)
			{
				UnityEngine.Object.Destroy(texture2D);
			}
			if (renderTexture != null)
			{
				renderTexture.Release();
				UnityEngine.Object.Destroy(renderTexture);
			}
		}
	}

	private static string WriteScreenshot(byte[] pngBytes)
	{
		string text = Path.Combine(StationSaveUtils.GetSavePath(), "screenshots");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		string path = $"stationeers_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.png";
		string text2 = Path.Combine(text, path);
		File.WriteAllBytes(text2, pngBytes);
		return text2;
	}

	public void StartTimelapse()
	{
		if (_timelapseRunning)
		{
			return;
		}
		if (!_active)
		{
			ConsoleWindow.PrintError("Time-lapse: cinematic camera must be active.");
			return;
		}
		Camera currentCamera = CameraController.CurrentCamera;
		if (currentCamera != null)
		{
			_timelapseAnchorPos = currentCamera.transform.position;
			_timelapseAnchorRot = currentCamera.transform.rotation;
			_timelapseAnchorFov = currentCamera.fieldOfView;
		}
		_velocity = Vector3.zero;
		StartCoroutine(StartTimelapseSequence());
	}

	public void StopTimelapse()
	{
		_timelapseRunning = false;
		if (TimelapseUsePath)
		{
			_pathPlaying = false;
		}
	}

	private IEnumerator StartTimelapseSequence()
	{
		yield return Countdown("Time-lapse");
		if (TimelapseUsePath && CurrentPath != null && CurrentPath.Count > 0)
		{
			_pathTime = 0f;
			_pathPlaying = true;
			_pathPaused = false;
		}
		if (AutoReturnOnStart && _active)
		{
			Deactivate();
		}
		yield return RunTimelapse();
	}

	private IEnumerator RunTimelapse()
	{
		_timelapseRunning = true;
		_timelapseFrameCount = 0;
		_timelapseStartTimeReal = Time.realtimeSinceStartup;
		string path = (string.IsNullOrWhiteSpace(TimelapseSubfolder) ? $"timelapse_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}" : TimelapseSubfolder.Trim());
		_timelapseDir = Path.Combine(StationSaveUtils.GetSavePath(), "screenshots", path);
		Directory.CreateDirectory(_timelapseDir);
		float num = Mathf.Max(0.01f, TimelapseFps);
		float interval = 1f / num;
		float num2 = Mathf.Max(0f, TimelapseDurationMinutes) * 60f;
		float deadline = Time.realtimeSinceStartup + num2;
		ConsoleWindow.PrintAction($"Time-lapse started: {_timelapseDir} (interval {interval:0.##}s, " + $"duration {TimelapseDurationMinutes:0.##}min, {ScreenshotWidth}x{ScreenshotHeight})");
		while (_timelapseRunning && Time.realtimeSinceStartup < deadline)
		{
			yield return Yielders.EndOfFrame;
			if (!_timelapseRunning)
			{
				break;
			}
			Camera currentCamera = CameraController.CurrentCamera;
			if (currentCamera == null)
			{
				break;
			}
			try
			{
				CaptureTimelapseFrame(currentCamera);
			}
			catch (Exception ex)
			{
				ConsoleWindow.PrintError("Time-lapse frame failed: " + ex.Message);
				break;
			}
			yield return new WaitForSecondsRealtime(interval);
		}
		float num3 = Time.realtimeSinceStartup - _timelapseStartTimeReal;
		ConsoleWindow.PrintAction($"Time-lapse finished: {_timelapseFrameCount} frames over {num3:0.0}s -> {_timelapseDir}");
		_timelapseRunning = false;
		if (!_active && _registeredWithLod && LodManager.Instance != null)
		{
			BeingDestroyed = true;
			LodManager.EnqueueRequesterToRemove(this);
			_registeredWithLod = false;
		}
	}

	private void CaptureTimelapseFrame(Camera cam)
	{
		int width = Mathf.Max(1, ScreenshotWidth);
		int height = Mathf.Max(1, ScreenshotHeight);
		int msaa = Mathf.Clamp(ScreenshotMsaa, 1, 8);
		Vector3 position = _timelapseAnchorPos;
		Quaternion rotation = _timelapseAnchorRot;
		float fov = _timelapseAnchorFov;
		if (TimelapseUsePath && CurrentPath != null && CurrentPath.Count > 0)
		{
			float time = Time.realtimeSinceStartup - _timelapseStartTimeReal;
			CurrentPath.Evaluate(time, TimelapseLoop, out position, out rotation, out fov);
		}
		byte[] bytes;
		if (_active)
		{
			bytes = RenderCameraToPng(cam, width, height, msaa);
		}
		else
		{
			Transform transform = cam.transform;
			Vector3 position2 = transform.position;
			Quaternion rotation2 = transform.rotation;
			float fieldOfView = cam.fieldOfView;
			try
			{
				transform.SetPositionAndRotation(position, rotation);
				cam.fieldOfView = fov;
				bytes = RenderCameraToPng(cam, width, height, msaa);
			}
			finally
			{
				transform.SetPositionAndRotation(position2, rotation2);
				cam.fieldOfView = fieldOfView;
			}
		}
		string path = $"frame_{_timelapseFrameCount:D5}.png";
		File.WriteAllBytes(Path.Combine(_timelapseDir, path), bytes);
		_timelapseFrameCount++;
	}

	public CinematicKeyframe AddKeyframeFromCurrent()
	{
		Camera currentCamera = CameraController.CurrentCamera;
		if (currentCamera == null)
		{
			ConsoleWindow.PrintError("Add keyframe: no active camera.");
			return null;
		}
		if (CurrentPath == null)
		{
			CinematicPath cinematicPath = (CurrentPath = new CinematicPath());
		}
		CinematicKeyframe result = CurrentPath.Add(currentCamera.transform.position, currentCamera.transform.rotation, currentCamera.fieldOfView);
		SelectedKeyframeIndex = CurrentPath.Count - 1;
		return result;
	}

	public void RemoveKeyframe(int index)
	{
		if (CurrentPath != null)
		{
			CurrentPath.RemoveAt(index);
			if (SelectedKeyframeIndex >= CurrentPath.Count)
			{
				SelectedKeyframeIndex = CurrentPath.Count - 1;
			}
		}
	}

	public void MoveKeyframe(int from, int to)
	{
		CurrentPath?.Move(from, to);
		if (SelectedKeyframeIndex == from)
		{
			SelectedKeyframeIndex = to;
		}
	}

	public void JumpToKeyframe(int index)
	{
		if (_active && CurrentPath != null && index >= 0 && index < CurrentPath.Count)
		{
			Camera currentCamera = CameraController.CurrentCamera;
			if (!(currentCamera == null))
			{
				CinematicKeyframe cinematicKeyframe = CurrentPath.Keyframes[index];
				currentCamera.transform.SetPositionAndRotation(cinematicKeyframe.Position, cinematicKeyframe.Rotation);
				CameraController.SetFieldOfView(cinematicKeyframe.Fov);
				Vector3 eulerAngles = cinematicKeyframe.Rotation.eulerAngles;
				_yaw = (_targetYaw = eulerAngles.y);
				_pitch = (_targetPitch = NormalisePitch(eulerAngles.x));
				_roll = (_targetRoll = ((eulerAngles.z > 180f) ? (eulerAngles.z - 360f) : eulerAngles.z));
				_velocity = Vector3.zero;
				SelectedKeyframeIndex = index;
			}
		}
	}

	public void RecaptureKeyframe(int index)
	{
		if (CurrentPath != null && index >= 0 && index < CurrentPath.Count)
		{
			Camera currentCamera = CameraController.CurrentCamera;
			if (!(currentCamera == null))
			{
				CinematicKeyframe cinematicKeyframe = CurrentPath.Keyframes[index];
				cinematicKeyframe.Position = currentCamera.transform.position;
				cinematicKeyframe.Rotation = currentCamera.transform.rotation;
				cinematicKeyframe.Fov = currentCamera.fieldOfView;
			}
		}
	}

	public void StartPathPlayback()
	{
		if (!_pathPlaying)
		{
			if (CurrentPath == null || CurrentPath.Count < 2)
			{
				ConsoleWindow.PrintError("Path playback: needs at least 2 keyframes.");
			}
			else if (!_active)
			{
				ConsoleWindow.PrintError("Path playback: cinematic camera must be active to preview.");
			}
			else
			{
				StartCoroutine(StartPathSequence());
			}
		}
	}

	public void StopPath()
	{
		_pathPlaying = false;
		_pathPaused = false;
	}

	public void TogglePathPause()
	{
		if (_pathPlaying)
		{
			_pathPaused = !_pathPaused;
		}
	}

	private IEnumerator StartPathSequence()
	{
		yield return Countdown("Path");
		_pathTime = 0f;
		_pathPlaying = true;
		_pathPaused = false;
	}

	private IEnumerator Countdown(string label)
	{
		float num = Mathf.Max(0f, CountdownSeconds);
		if (num <= 0f)
		{
			_countdownRemaining = 0f;
			_countdownLabel = null;
			yield break;
		}
		for (_countdownRemaining = num; _countdownRemaining > 0f; _countdownRemaining -= 0.05f)
		{
			_countdownLabel = $"{label} in {Mathf.CeilToInt(_countdownRemaining)}…";
			yield return new WaitForSecondsRealtime(0.05f);
		}
		_countdownLabel = "GO!";
		yield return new WaitForSecondsRealtime(0.4f);
		_countdownLabel = null;
		_countdownRemaining = 0f;
	}

	private Vector3 ComputeMoveTarget(Quaternion rotation)
	{
		float forwardAxis = KeyManager.GetForwardAxis();
		float rightAxis = KeyManager.GetRightAxis();
		float y = KeyManager.GetAscend() - KeyManager.GetDescend();
		Vector3 vector = new Vector3(rightAxis, y, forwardAxis);
		if (vector.sqrMagnitude < 1E-06f)
		{
			return Vector3.zero;
		}
		vector = Vector3.ClampMagnitude(vector, 1f);
		float num = _baseSpeedSmoothed;
		if (KeyManager.GetButton(KeyMap.QuantityModifier))
		{
			num *= FastMultiplier;
		}
		if (Input.GetKey(SlowModifier))
		{
			num *= SlowMultiplier;
		}
		return rotation * vector * num;
	}

	private Vector3 ApplyTether(Vector3 candidate)
	{
		if (_tetherRadius <= 0f)
		{
			return candidate;
		}
		Vector3 parentPosition = InventoryManager.ParentPosition;
		Vector3 vector = candidate - parentPosition;
		float sqrMagnitude = vector.sqrMagnitude;
		if (sqrMagnitude <= _tetherRadius * _tetherRadius)
		{
			return candidate;
		}
		return parentPosition + vector * (_tetherRadius / Mathf.Sqrt(sqrMagnitude));
	}

	private static void PublishCameraState(Vector3 position, Quaternion rotation)
	{
		CameraController.CameraPosition = position;
		CameraController.CameraRotation = rotation;
		CameraController.EffectiveCameraPosition = position;
		if (CameraController.Instance != null)
		{
			CameraController.Instance.MainCameraForward = rotation * Vector3.forward;
			CameraController.Instance.MainCameraPosition = position;
		}
	}

	private static float NormalisePitch(float eulerX)
	{
		return Mathf.Clamp((eulerX > 180f) ? (eulerX - 360f) : eulerX, -89.9f, 89.9f);
	}

	private void MaybeRequestLodUpdate(Vector3 position)
	{
		_centerPosition = position;
		if (_registeredWithLod && !(LodManager.Instance == null) && !(RocketMath.SquareDistanceComparison(position, _lastLodRequestPosition, 8f) <= 0f))
		{
			_lastLodRequestPosition = position;
			LodManager.EnqueueRequesterToUpdate(this);
		}
	}

	public void OnAssignedReference()
	{
	}

	public void PrintDebugInfo(bool verbose = false)
	{
	}

	public bool CanThread()
	{
		if (_active)
		{
			return !BeingDestroyed;
		}
		return false;
	}

	public string DebugName()
	{
		return "CinematicCamera";
	}

	private void DrawPathOverlay()
	{
		Camera currentCamera = CameraController.CurrentCamera;
		if (currentCamera == null)
		{
			return;
		}
		ImDrawListPtr foregroundDrawList = ImGui.GetForegroundDrawList();
		bool flag = ((_pathPlaying && _timelapseRunning && TimelapseUsePath) ? TimelapseLoop : PathPlaybackLoop);
		if (CurrentPath.Count >= 2)
		{
			for (int i = 1; i < CurrentPath.Count; i++)
			{
				DrawPathSegment(currentCamera, foregroundDrawList, i, flag, PathColorNormal, 1.5f);
			}
			if (flag)
			{
				DrawPathSegment(currentCamera, foregroundDrawList, CurrentPath.Count, loop: true, PathColorLoop, 1.5f);
			}
		}
		for (int j = 0; j < CurrentPath.Count; j++)
		{
			CinematicKeyframe cinematicKeyframe = CurrentPath.Keyframes[j];
			bool flag2 = j == SelectedKeyframeIndex;
			DrawKeyframeFrustum(currentCamera, foregroundDrawList, cinematicKeyframe, flag2 ? FrustumColorSelected : FrustumColorNormal, flag2 ? 2.5f : 1.5f);
			if (TryWorldToScreen(currentCamera, cinematicKeyframe.Position, out var screen))
			{
				foregroundDrawList.AddText(screen + new Vector2(8f, -8f), ImGuiColor.Integer.White, $"#{j} {cinematicKeyframe.Label}");
			}
		}
		if (_pathPlaying && CurrentPath.Count >= 2 && CurrentPath.Evaluate(_pathTime, flag, out var position, out var rotation, out var _))
		{
			DrawWireRing(currentCamera, foregroundDrawList, position, rotation * Vector3.up, 0.3f, PathColorPlayhead, 2.5f);
			DrawWorldLine(currentCamera, foregroundDrawList, position, position + rotation * Vector3.forward * 1.5f, PathColorPlayhead, 2.5f);
		}
	}

	private void DrawPathSegment(Camera cam, ImDrawListPtr draw, int i, bool loop, uint color, float thickness)
	{
		int count = CurrentPath.Count;
		Vector3 position;
		Vector3 position2;
		Vector3 p;
		Vector3 p2;
		if (i == count)
		{
			position = CurrentPath.Keyframes[count - 1].Position;
			position2 = CurrentPath.Keyframes[0].Position;
			p = CurrentPath.Keyframes[count - 2].Position;
			p2 = ((count >= 2) ? CurrentPath.Keyframes[1].Position : (position2 + (position2 - position)));
		}
		else
		{
			position = CurrentPath.Keyframes[i - 1].Position;
			position2 = CurrentPath.Keyframes[i].Position;
			p = ((i >= 2) ? CurrentPath.Keyframes[i - 2].Position : ((!loop || count < 2) ? (position - (position2 - position)) : CurrentPath.Keyframes[count - 1].Position));
			p2 = ((i + 1 < count) ? CurrentPath.Keyframes[i + 1].Position : ((!loop || count < 2) ? (position2 + (position2 - position)) : CurrentPath.Keyframes[0].Position));
		}
		Vector3 a = position;
		for (int j = 1; j <= 24; j++)
		{
			float t = (float)j / 24f;
			Vector3 vector = CinematicPath.CatmullRom(p, position, position2, p2, t);
			DrawWorldLine(cam, draw, a, vector, color, thickness);
			a = vector;
		}
	}

	private static void DrawKeyframeFrustum(Camera cam, ImDrawListPtr draw, CinematicKeyframe kf, uint color, float thickness)
	{
		float num = 1f * Mathf.Tan(kf.Fov * 0.5f * (MathF.PI / 180f));
		float num2 = num * 1.7777778f;
		Vector3 vector = kf.Rotation * Vector3.forward;
		Vector3 vector2 = kf.Rotation * Vector3.up;
		Vector3 vector3 = kf.Rotation * Vector3.right;
		Vector3 position = kf.Position;
		Vector3 vector4 = position + vector * 1f;
		Vector3 vector5 = vector4 + vector2 * num - vector3 * num2;
		Vector3 vector6 = vector4 + vector2 * num + vector3 * num2;
		Vector3 vector7 = vector4 - vector2 * num + vector3 * num2;
		Vector3 vector8 = vector4 - vector2 * num - vector3 * num2;
		DrawWorldLine(cam, draw, position, vector5, color, thickness);
		DrawWorldLine(cam, draw, position, vector6, color, thickness);
		DrawWorldLine(cam, draw, position, vector7, color, thickness);
		DrawWorldLine(cam, draw, position, vector8, color, thickness);
		DrawWorldLine(cam, draw, vector5, vector6, color, thickness);
		DrawWorldLine(cam, draw, vector6, vector7, color, thickness);
		DrawWorldLine(cam, draw, vector7, vector8, color, thickness);
		DrawWorldLine(cam, draw, vector8, vector5, color, thickness);
		Vector3 b = (vector5 + vector6) * 0.5f + vector2 * (num * 0.5f);
		DrawWorldLine(cam, draw, (vector5 + vector6) * 0.5f, b, color, thickness);
	}

	private static void DrawWireRing(Camera cam, ImDrawListPtr draw, Vector3 center, Vector3 axis, float radius, uint color, float thickness)
	{
		Quaternion quaternion = Quaternion.LookRotation((axis.sqrMagnitude > 0f) ? axis : Vector3.up);
		for (int i = 0; i < 3; i++)
		{
			Vector3 a = Vector3.zero;
			_ = Vector3.zero;
			for (int j = 0; j <= 16; j++)
			{
				float f = (float)j / 16f * MathF.PI * 2f;
				float num = Mathf.Cos(f) * radius;
				float num2 = Mathf.Sin(f) * radius;
				Vector3 vector = center + quaternion * i switch
				{
					0 => new Vector3(num, num2, 0f), 
					1 => new Vector3(num, 0f, num2), 
					_ => new Vector3(0f, num, num2), 
				};
				if (j != 0)
				{
					DrawWorldLine(cam, draw, a, vector, color, thickness);
				}
				a = vector;
			}
		}
	}

	private static void DrawWorldLine(Camera cam, ImDrawListPtr draw, Vector3 a, Vector3 b, uint color, float thickness)
	{
		if (TryWorldToScreen(cam, a, out var screen) && TryWorldToScreen(cam, b, out var screen2))
		{
			draw.AddLine(screen, screen2, color, thickness);
		}
	}

	private static bool TryWorldToScreen(Camera cam, Vector3 worldPos, out Vector2 screen)
	{
		Vector3 vector = cam.WorldToScreenPoint(worldPos);
		if (vector.z <= 0f)
		{
			screen = default(Vector2);
			return false;
		}
		screen = new Vector2(vector.x, (float)Screen.height - vector.y);
		return true;
	}

	private void DrawCountdownOverlay()
	{
		if (!string.IsNullOrEmpty(_countdownLabel))
		{
			ImDrawListPtr foregroundDrawList = ImGui.GetForegroundDrawList();
			Vector2 vector = new Vector2((float)Screen.width * 0.5f, (float)Screen.height * 0.18f);
			Vector2 vector2 = ImGui.CalcTextSize(_countdownLabel);
			foregroundDrawList.AddText(new Vector2(vector.x - vector2.x * 0.5f + 2f, vector.y + 2f), ImGuiColor.Integer.Black, _countdownLabel);
			foregroundDrawList.AddText(new Vector2(vector.x - vector2.x * 0.5f, vector.y), ImGuiColor.Integer.White, _countdownLabel);
		}
	}
}
