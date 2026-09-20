using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using CharacterCustomisation;
using TerrainSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts;

public class CursorManager : ManagerBase
{
	public Camera TextCamera;

	public TextMeshProUGUI LabelTextField;

	private Vector3 _cursorPlanePosition;

	private Vector3 _cursorPositionForward;

	private bool _updatedThisFramePosition;

	private bool _updatedThisFrameFowardPosition;

	public static float MaxInteractDistance = 3f;

	[Header("Visualizers")]
	[Tooltip("A prefab that is stretched to size of bounds of target to indicate what is being selected")]
	public GameBase CursorSelectionHighlighter;

	[Tooltip("A prefab that is used as the cursor")]
	public GameObject CursorHighlighter;

	public Shader CursorShader;

	public Material CursorMaterial;

	public static CursorVoxel CursorVoxel;

	[Header("Cursor/Selection State")]
	public LayerMask CursorHitMask;

	public LayerMask TerrainHitMask;

	public LayerMask WeaponHitMask;

	[ReadOnly]
	public Thing FoundThing;

	[ReadOnly]
	public Collider CursorTargetCollider;

	[Header("Environment")]
	public bool SunMovement = true;

	[ReadOnly]
	public Vector3 SunRotationAxis;

	public LayerMask SunMask;

	private static readonly float _cursorForwardDistance = 2f;

	private static Transform _raycastTransform;

	private static RaycastHit _raycastHit;

	public static CursorManager Instance;

	public GameObject LavaComponentObject;

	public Light LavaLight;

	public ParticleSystem LavaParticles;

	public AudioSource LavaAudio;

	public AudioHighPassFilter LavaAudioHighPassFilter;

	public AudioLowPassFilter LavaAudioLowPassFilter;

	public float LavaAudioHighPassFilterTarget = 20f;

	public float LavaAudioLowPassFilterTarget = 22000f;

	public float LavaAudioVolumeTarget = 1f;

	public AnimationCurve LavaAudioHighPassFilterCurve;

	public AnimationCurve LavaAudioLowPassFilterCurve;

	public GameObject LavaPlane;

	public Material[] PlaneMats;

	private int DistanceToStartLava = 25;

	public LayerMask PlanetarySunMask;

	public static bool DefaultEasyFlareEnabled = true;

	public static Vector2 ScrollbarData;

	public StandaloneInputModuleOverride InputModule;

	public AtmosphericScattering AtmosphericScattering;

	public AtmosphericScatteringDeferred AtmosphericScatteringCamera;

	public AnimationCurve FlareBrightnessDistanceCurve;

	public static Transform CursorSelectionTransform { get; set; }

	public static Transform CursorTransform { get; set; }

	public static SelectionInstance LastSelectionInstance { get; private set; }

	public CursorTerrain FoundTerrain { get; set; }

	public static Renderer CursorSelectionRenderer { get; set; }

	public static Renderer CursorRenderer { get; set; }

	public static RaycastHit CursorHit { get; set; }

	public static Vector2 CursorCenter { get; set; }

	public bool BlockCursorRaycast { get; set; }

	public static Thing CursorThing => Instance.FoundThing;

	public static CursorTerrain CursorTerrain => Instance.FoundTerrain;

	public static Vector3 CursorPlanePosition
	{
		get
		{
			if (Instance._updatedThisFramePosition)
			{
				return Instance._cursorPlanePosition;
			}
			CursorPlanePosition = InputHelpers.GetPlaneCameraCentrePosition();
			Instance._updatedThisFramePosition = true;
			return Instance._cursorPlanePosition;
		}
		private set
		{
			Instance._cursorPlanePosition = value;
		}
	}

	public static Vector3 CursorPositionForward
	{
		get
		{
			if (Instance._updatedThisFrameFowardPosition)
			{
				return Instance._cursorPositionForward;
			}
			CursorPositionForward = InputHelpers.GetCameraRay().GetPoint(_cursorForwardDistance);
			Instance._updatedThisFrameFowardPosition = true;
			return Instance._cursorPositionForward;
		}
		private set
		{
			Instance._cursorPositionForward = value;
		}
	}

	public static bool IsLocked => Cursor.visible;

	public static void SetSelection(SelectionInstance selection, int selectionSoundHash)
	{
		if (selection == null)
		{
			CursorSelectionRenderer.gameObject.SetActive(value: false);
			ClearLastSelectionId();
			return;
		}
		CursorSelectionTransform.localScale = selection.GetScale();
		CursorSelectionTransform.position = selection.GetPosition();
		CursorSelectionTransform.rotation = selection.GetRotation();
		Instance.CursorSelectionHighlighter.SetVisible(isVisble: true);
		if (LastSelectionInstance == null || selection.ParentThingRefernceId != LastSelectionInstance.ParentThingRefernceId || (selection.ParentThingRefernceId == LastSelectionInstance.ParentThingRefernceId && selection.InteractableId != LastSelectionInstance.InteractableId))
		{
			UIAudioManager.Play(selectionSoundHash);
			LastSelectionInstance = selection;
		}
	}

	public static void SetSelection(SelectionInstance selection, Color color)
	{
		SetSelectionColor(color);
		int selectionSoundHash = UIAudioManager.HighlightInWorldCanInteractHash;
		if (color.CompareRGB(Color.red))
		{
			selectionSoundHash = UIAudioManager.HighlightInWorldCantInteractHash;
		}
		else if (color.CompareRGB(Color.blue))
		{
			selectionSoundHash = UIAudioManager.HighlightInWorldItemHash;
		}
		SetSelection(selection, selectionSoundHash);
	}

	public static void SetSelectionColor(Color color)
	{
		CursorSelectionRenderer.material.color = color.SetAlpha(InventoryManager.Instance.CursorAlphaInteractable);
	}

	public static void SetSelectionVisibility(bool isVisible)
	{
		Instance.CursorSelectionHighlighter.SetVisible(isVisible);
	}

	public static void ClearLastSelectionId()
	{
		LastSelectionInstance = null;
	}

	public static LayerMask GetPlanetarySunMask()
	{
		return Instance.PlanetarySunMask;
	}

	public static Vector3 GetCurrentVoxelWorld()
	{
		if (!CursorTerrain.IsValid)
		{
			return Vector3.zero;
		}
		return CursorVoxel.Transform.position + ((BoxCollider)Instance.CursorTargetCollider).center;
	}

	public void SetCursorTarget()
	{
		Human parentHuman = InventoryManager.ParentHuman;
		bool flag = (object)parentHuman != null && parentHuman.MovementController?.ControlMode == MovementController.Mode.Seated && !InputMouse.IsMouseControl && (!(InventoryManager.ParentHuman?.ParentSlot?.Parent is IExitable exitable) || !exitable.FreeLook);
		if (ConsoleWindow.IsOpen || Stationpedia.IsOpenAndLocked || BlockCursorRaycast || flag)
		{
			CursorTargetCollider = null;
			FoundThing = null;
			FoundTerrain = CursorTerrain.Invalid;
			if (MyGraphicsRaycaster.CameraInfo.CameraTransform != null)
			{
				MyGraphicsRaycaster.CameraInfo.CameraTransform.SetParent(null);
				MyGraphicsRaycaster.ShouldCast = false;
			}
			return;
		}
		Ray cameraRay = InputHelpers.GetCameraRay();
		CursorVoxel.UpdatePosition(cameraRay);
		bool num = Physics.Raycast(cameraRay, out _raycastHit, MaxInteractDistance, CursorHitMask);
		Debug.DrawRay(cameraRay.origin, cameraRay.direction * MaxInteractDistance);
		_raycastTransform = _raycastHit.transform;
		CursorHit = _raycastHit;
		if (num)
		{
			if (_raycastTransform == CursorVoxel.Transform)
			{
				Vector3 vector = CursorVoxel.Transform.position + ((BoxCollider)_raycastHit.collider).center;
				bool flag2 = VoxelTerrain.GetDensityWorldSpace(vector) > 0f;
				Vein veinAtPosition = Vein.GetVeinAtPosition(vector);
				byte activeIndex = 0;
				veinAtPosition?.HasActivePosition(vector.FloorToInt(), out activeIndex);
				if (veinAtPosition != null || flag2)
				{
					FoundTerrain = new CursorTerrain(veinAtPosition, activeIndex, flag2, new Vector3Int((int)vector.x, (int)vector.y, (int)vector.z));
				}
				CursorTargetCollider = _raycastHit.collider;
				FoundThing = null;
				if (MyGraphicsRaycaster.CameraInfo.CameraTransform != null)
				{
					MyGraphicsRaycaster.CameraInfo.CameraTransform.SetParent(null);
					MyGraphicsRaycaster.ShouldCast = false;
				}
			}
			else
			{
				if (CameraController.IsThirdPerson && Physics.Raycast(CameraController.CameraPosition, _raycastHit.point - CameraController.CameraPosition, out var hitInfo, MaxInteractDistance, CursorHitMask) && hitInfo.transform != _raycastTransform)
				{
					return;
				}
				CursorTargetCollider = _raycastHit.collider;
				FoundTerrain = CursorTerrain.Invalid;
				FoundThing = _raycastTransform.GetComponentInParent<Thing>();
				if (FoundThing is IComputer)
				{
					if (CursorTargetCollider.GetComponent<InWorldUIScreen>() != null && MyGraphicsRaycaster.CameraInfo.CameraTransform != null)
					{
						MyGraphicsRaycaster.CameraInfo.CameraTransform.SetParent(FoundThing.ThingTransform);
						MyGraphicsRaycaster.ShouldCast = true;
					}
					else
					{
						MyGraphicsRaycaster.CameraInfo.CameraTransform?.SetParent(null);
						MyGraphicsRaycaster.ShouldCast = false;
					}
				}
				else
				{
					MyGraphicsRaycaster.CameraInfo.CameraTransform.SetParent(null);
					MyGraphicsRaycaster.ShouldCast = false;
				}
			}
		}
		else
		{
			CursorTargetCollider = null;
			FoundThing = null;
			FoundTerrain = CursorTerrain.Invalid;
			if (MyGraphicsRaycaster.CameraInfo.CameraTransform != null)
			{
				MyGraphicsRaycaster.CameraInfo.CameraTransform.SetParent(null);
				MyGraphicsRaycaster.ShouldCast = false;
			}
		}
	}

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if (Instance == null)
		{
			Instance = this;
		}
		Initialize();
	}

	private void Initialize()
	{
		if (!GameManager.IsBatchMode)
		{
			CursorTransform = CursorHighlighter.transform;
			CursorSelectionTransform = CursorSelectionHighlighter.transform;
			if (OrbitalSimulation.WorldSun != null)
			{
				OrbitalSimulation.InitializeSimulation();
			}
			CursorSelectionRenderer = CursorSelectionHighlighter.GetComponent<Renderer>();
			CursorRenderer = CursorHighlighter.GetComponent<Renderer>();
			if (CursorVoxel == null)
			{
				CursorVoxel = new CursorVoxel();
			}
		}
	}

	public void HandleLavaBehaviour(Human human)
	{
		Vector3 thingTransformPosition = human.ThingTransformPosition;
		float y = thingTransformPosition.y;
		float num = LavaLight?.range ?? 0f;
		float lavaHeight = WorldSetting.Current.Data.TerrainSettings.LavaData.GetLavaHeight(thingTransformPosition);
		LavaComponentObject.transform.position = new Vector3(thingTransformPosition.x, lavaHeight, thingTransformPosition.z);
		if ((bool)LavaLight && LavaLight.enabled)
		{
			LavaLight.intensity = Mathf.Lerp(5f, 0f, RocketMath.MapToScaleClamp(lavaHeight, lavaHeight + num, 0f, 1f, y));
		}
		if (CameraController.AlreadyLavaCam && WorldManager.HasLava && y > lavaHeight)
		{
			CameraController.SetUnderLava(show: false);
		}
		if (WorldManager.HasLava && human.IsInLava)
		{
			if (human.IsInLava && !CameraController.AlreadyLavaCam)
			{
				CameraController.SetUnderLava(show: true);
			}
			LavaLight.enabled = true;
			LavaParticles.Play();
		}
		else
		{
			LavaParticles.Stop();
		}
		LavaAudioVolumeTarget = 1f;
		if (human.Room != null)
		{
			LavaAudioVolumeTarget = 0.2f;
		}
		if (!LavaAudio.isPlaying)
		{
			LavaAudio.Play();
		}
		float time = Mathf.Clamp(y - LavaAudio.transform.position.y, -2f, 30f);
		LavaAudioHighPassFilterTarget = LavaAudioHighPassFilterCurve.Evaluate(time);
		LavaAudioLowPassFilterTarget = LavaAudioLowPassFilterCurve.Evaluate(time);
	}

	public static void SetAtmosphericScattering(bool isOn)
	{
		if ((bool)Instance && (bool)Instance.AtmosphericScattering)
		{
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D9 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
			{
				Debug.Log("Atmospheric scattering disabled: not supported on dx9 or vulkan");
				Instance.AtmosphericScattering.enabled = false;
				Instance.AtmosphericScatteringCamera.enabled = false;
			}
			else
			{
				Instance.AtmosphericScattering.enabled = isOn;
				Instance.AtmosphericScatteringCamera.enabled = isOn;
			}
		}
	}

	public static void UpdateAtmosphericScattering()
	{
		if (!GameManager.IsBatchMode && (bool)Instance && (bool)Instance.AtmosphericScattering)
		{
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D9 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
			{
				Instance.AtmosphericScattering.enabled = false;
				Instance.AtmosphericScatteringCamera.enabled = false;
			}
			else
			{
				Instance.AtmosphericScattering.enabled = Settings.CurrentData.AtmosphericScattering && WorldManager.AtmosphericScattering;
				Instance.AtmosphericScatteringCamera.enabled = Settings.CurrentData.AtmosphericScattering && WorldManager.AtmosphericScattering;
			}
		}
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		MouseModeController.Check();
		if (GameManager.GameState != GameState.Running || WorldManager.IsGamePaused)
		{
			return;
		}
		ScrollbarData = Input.mouseScrollDelta;
		if (!GameManager.IsBatchMode && WorldManager.HasLava)
		{
			LavaAudioHighPassFilter.cutoffFrequency = Mathf.Lerp(LavaAudioHighPassFilter.cutoffFrequency, LavaAudioHighPassFilterTarget, Time.deltaTime * 5f);
			LavaAudioLowPassFilter.cutoffFrequency = Mathf.Lerp(LavaAudioLowPassFilter.cutoffFrequency, LavaAudioLowPassFilterTarget, Time.deltaTime * 5f);
			LavaAudio.volume = Mathf.Lerp(LavaAudio.volume, LavaAudioVolumeTarget, Time.deltaTime * 5f);
		}
		if (!GameManager.IsBatchMode && !(CameraController.CurrentCamera == null))
		{
			SetCursorTarget();
			if (FoundThing is IComputer computer && computer.Screen.activeSelf)
			{
				MyGraphicsRaycaster.CameraInfo.CameraTransform.SetPositionAndRotation(Camera.main.transform.position, Camera.main.transform.rotation);
				InputModule.UpdateModule();
			}
		}
	}

	public void OnApplicationFocus(bool focus)
	{
		if (!GameManager.IsBatchMode && GameManager.IsInitialized && focus && !InputWindowBase.IsInputWindow && !Stationpedia.IsOpenAndLocked)
		{
			if (!InventoryManager.Instance.transform.gameObject.activeSelf)
			{
				InventoryManager.Instance.transform.gameObject.SetActive(value: true);
			}
			MouseModeController.Reset();
		}
	}

	public static void SetCursor(bool isLocked)
	{
		if (isLocked && !CharacterCustomisationManager.IsSceneLoaded && GameManager.GameState == GameState.Running && !PromptPanel.Instance.IsActive)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		else
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}

	private void LateUpdate()
	{
		_updatedThisFramePosition = false;
		_updatedThisFrameFowardPosition = false;
	}

	public static void ClearAll()
	{
	}

	public static void DebugCaptureGridDataForBugReports()
	{
		Debug.Log("DebugCaptureGridDataForBugReports");
		if ((bool)CursorThing)
		{
			string text = $"Cursor Thing: {CursorThing.DisplayName} at grid coords: {CursorThing.GridPosition}";
			if (CursorThing is SmallGrid { SmallCell: not null } smallGrid)
			{
				text = $"{text} small: {smallGrid.SmallCell.SmallGrid}";
			}
			Debug.Log(text);
			ConsoleWindow.Print(text);
		}
	}
}
