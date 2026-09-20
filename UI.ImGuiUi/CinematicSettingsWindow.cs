using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI.ImGuiUi;
using ImGuiNET;
using UI.ImGuiUi.ImGuiWindows;
using UnityEngine;

namespace UI.ImGuiUi;

public class CinematicSettingsWindow : UI.ImGuiUi.ImGuiWindows.ImGuiWindow
{
	private static readonly CinematicSettingsWindow _instance = new CinematicSettingsWindow();

	private static readonly string[] _slowModifierLabels = new string[6] { "Left Alt", "Right Alt", "Left Ctrl", "Right Ctrl", "Left Shift", "Right Shift" };

	private static readonly KeyCode[] _slowModifierValues = new KeyCode[6]
	{
		KeyCode.LeftAlt,
		KeyCode.RightAlt,
		KeyCode.LeftControl,
		KeyCode.RightControl,
		KeyCode.LeftShift,
		KeyCode.RightShift
	};

	private static readonly string[] _msaaLabels = new string[4] { "1 (off)", "2x", "4x", "8x" };

	private static readonly int[] _msaaValues = new int[4] { 1, 2, 4, 8 };

	private static string _saveAsName = "";

	private static List<string> _savedPathNames;

	private static bool _savedListDirty = true;

	private static readonly string[] _easeLabels = new string[5] { "Linear", "SmoothStep", "EaseIn", "EaseOut", "EaseInOut" };

	public static bool IsOpen => _instance.IsShowing;

	public static void Open()
	{
		if (!_instance.IsShowing)
		{
			ImGuiWindowManager.Open(_instance);
		}
	}

	public static void Close()
	{
		if (_instance.IsShowing)
		{
			ImGuiWindowManager.Close(_instance);
		}
	}

	public CinematicSettingsWindow()
		: base("Cinematic Camera", new Vector2(420f, 720f))
	{
	}

	public override void OnOpen()
	{
	}

	public override void OnClose()
	{
	}

	public override void DrawContent()
	{
		CinematicCamera instance = CinematicCamera.Instance;
		if (instance == null)
		{
			ImGui.TextColored(ImGuiColor.Float4.Red, "CinematicCamera instance not found.");
			return;
		}
		DrawMovement(instance);
		ImGui.Spacing();
		DrawSmoothing(instance);
		ImGui.Spacing();
		DrawRotation(instance);
		ImGui.Spacing();
		DrawFov();
		ImGui.Spacing();
		DrawScreenshot(instance);
		ImGui.Spacing();
		DrawTimelapse(instance);
		ImGui.Spacing();
		DrawPath(instance);
		ImGui.Spacing();
		DrawTether(instance);
		ImGui.Spacing();
		DrawStatus(instance);
		ImGui.Spacing();
		DrawActions();
	}

	private static void DrawMovement(CinematicCamera cam)
	{
		if (ImGui.CollapsingHeader("Movement", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.SliderFloat("Base Speed (m/s)", ref cam.BaseSpeed, cam.MinSpeed, cam.MaxSpeed);
			ImGui.DragFloat("Min Speed", ref cam.MinSpeed, 0.05f, 0.01f, cam.MaxSpeed);
			ImGui.DragFloat("Max Speed", ref cam.MaxSpeed, 1f, cam.MinSpeed, 5000f);
			cam.BaseSpeed = Mathf.Clamp(cam.BaseSpeed, cam.MinSpeed, cam.MaxSpeed);
			ImGui.SliderFloat("Scroll Speed Factor", ref cam.ScrollSpeedFactor, 1.01f, 3f);
			ImGui.SliderFloat("Speed Smoothing", ref cam.SpeedSmoothing, 0f, 30f);
			ImGui.SliderFloat("Fast Multiplier (Shift)", ref cam.FastMultiplier, 1f, 25f);
			ImGui.SliderFloat("Slow Multiplier", ref cam.SlowMultiplier, 0.01f, 1f);
			int current_item = IndexOf(_slowModifierValues, cam.SlowModifier);
			if (current_item < 0)
			{
				current_item = 0;
			}
			if (ImGui.Combo("Slow Modifier", ref current_item, _slowModifierLabels, _slowModifierLabels.Length))
			{
				cam.SlowModifier = _slowModifierValues[current_item];
			}
		}
	}

	private static void DrawSmoothing(CinematicCamera cam)
	{
		if (ImGui.CollapsingHeader("Smoothing", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.TextWrapped("Higher = snappier. Lower = silkier glide for trailers. 0 = raw input.");
			ImGui.SliderFloat("Move Smoothing", ref cam.MoveSmoothing, 0f, 30f);
			ImGui.SliderFloat("Look Smoothing", ref cam.LookSmoothing, 0f, 30f);
			if (ImGui.Button("Trailer preset (silky)"))
			{
				cam.MoveSmoothing = 4f;
				cam.LookSmoothing = 5f;
			}
			ImGui.SameLine();
			if (ImGui.Button("Default"))
			{
				cam.MoveSmoothing = 8f;
				cam.LookSmoothing = 12f;
			}
			ImGui.SameLine();
			if (ImGui.Button("Snappy"))
			{
				cam.MoveSmoothing = 20f;
				cam.LookSmoothing = 25f;
			}
		}
	}

	private static void DrawRotation(CinematicCamera cam)
	{
		if (ImGui.CollapsingHeader("Rotation", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.SliderFloat("Look Sensitivity", ref cam.LookSensitivity, 0.1f, 10f);
			ImGui.SliderFloat("Roll Speed (deg/s)", ref cam.RollSpeed, 0f, 180f);
			if (ImGui.Button("Reset Roll to 0"))
			{
				cam.ResetRoll();
			}
		}
	}

	private static void DrawFov()
	{
		Camera currentCamera = CameraController.CurrentCamera;
		if (!(currentCamera == null) && ImGui.CollapsingHeader("Field of View", ImGuiTreeNodeFlags.DefaultOpen))
		{
			float v = currentCamera.fieldOfView;
			if (ImGui.SliderFloat("FOV (deg)", ref v, 5f, 130f))
			{
				CameraController.SetFieldOfView(v);
			}
			if (ImGui.Button("Reset to settings FOV"))
			{
				CameraController.SetFieldOfView(Settings.CurrentData.FieldOfView);
			}
		}
	}

	private static void DrawScreenshot(CinematicCamera cam)
	{
		if (ImGui.CollapsingHeader("Screenshot", ImGuiTreeNodeFlags.DefaultOpen))
		{
			ImGui.InputInt("Width", ref cam.ScreenshotWidth, 16, 256);
			ImGui.InputInt("Height", ref cam.ScreenshotHeight, 16, 256);
			cam.ScreenshotWidth = Mathf.Clamp(cam.ScreenshotWidth, 16, 16384);
			cam.ScreenshotHeight = Mathf.Clamp(cam.ScreenshotHeight, 16, 16384);
			int current_item = IndexOf(_msaaValues, cam.ScreenshotMsaa);
			if (current_item < 0)
			{
				current_item = _msaaValues.Length - 1;
			}
			if (ImGui.Combo("MSAA", ref current_item, _msaaLabels, _msaaLabels.Length))
			{
				cam.ScreenshotMsaa = _msaaValues[current_item];
			}
			ImGui.Spacing();
			if (ImGui.Button("1080p"))
			{
				cam.ScreenshotWidth = 1920;
				cam.ScreenshotHeight = 1080;
			}
			ImGui.SameLine();
			if (ImGui.Button("1440p"))
			{
				cam.ScreenshotWidth = 2560;
				cam.ScreenshotHeight = 1440;
			}
			ImGui.SameLine();
			if (ImGui.Button("4K"))
			{
				cam.ScreenshotWidth = 3840;
				cam.ScreenshotHeight = 2160;
			}
			ImGui.SameLine();
			if (ImGui.Button("8K"))
			{
				cam.ScreenshotWidth = 7680;
				cam.ScreenshotHeight = 4320;
			}
			ImGui.SameLine();
			if (ImGui.Button("21:9 4K"))
			{
				cam.ScreenshotWidth = 5120;
				cam.ScreenshotHeight = 2160;
			}
		}
	}

	private static void DrawTimelapse(CinematicCamera cam)
	{
		if (!ImGui.CollapsingHeader("Time-lapse"))
		{
			return;
		}
		ImGui.TextWrapped("Captures sequential PNG frames at the chosen rate using the Screenshot W/H/MSAA above. Frames are named frame_00000.png onward — assemble with ffmpeg, e.g.\n    ffmpeg -framerate 30 -i frame_%05d.png -c:v libx264 -pix_fmt yuv420p out.mp4\n\nFrame your shot first, then hit Start — the camera locks in place and movement is disabled until you Stop. You can exit cinematic (Esc) and play normally; the lapse keeps capturing from the locked anchor.");
		bool timelapseRunning = cam.TimelapseRunning;
		ImGui.BeginDisabled(timelapseRunning);
		ImGui.SliderFloat("Capture rate (fps)", ref cam.TimelapseFps, 0.05f, 10f);
		ImGui.SliderFloat("Duration (min)", ref cam.TimelapseDurationMinutes, 0.1f, 240f);
		string input = cam.TimelapseSubfolder ?? string.Empty;
		if (ImGui.InputText("Subfolder (blank = auto)", ref input, 128u))
		{
			cam.TimelapseSubfolder = input;
		}
		ImGui.Checkbox("Use keyframe path (dolly shot)", ref cam.TimelapseUsePath);
		if (cam.TimelapseUsePath)
		{
			int num = cam.CurrentPath?.Count ?? 0;
			if (num < 2)
			{
				ImGui.TextColored(ImGuiColor.Float4.Yellow, "Path needs at least 2 keyframes (currently " + num + ").");
			}
			else
			{
				ImGui.Checkbox("Loop path during lapse", ref cam.TimelapseLoop);
				float totalDuration = cam.CurrentPath.GetTotalDuration(cam.TimelapseLoop);
				ImGui.TextDisabled($"Path runs at its natural speed ({totalDuration:0.0}s for one " + (cam.TimelapseLoop ? "loop" : "pass") + "). Lapse stops after Duration.");
			}
		}
		ImGui.InputFloat("Countdown (s)", ref cam.CountdownSeconds, 0.5f, 1f, "%.1f");
		cam.CountdownSeconds = Mathf.Max(0f, cam.CountdownSeconds);
		ImGui.Checkbox("Auto-return to player on Start", ref cam.AutoReturnOnStart);
		ImGui.EndDisabled();
		float num2 = Mathf.Max(0.01f, cam.TimelapseFps);
		int num3 = Mathf.RoundToInt(cam.TimelapseDurationMinutes * 60f * num2);
		float num4 = (float)num3 / 30f;
		ImGui.TextDisabled($"~{num3} frames -> {num4:0.0}s of footage at 30 fps playback");
		ImGui.Spacing();
		if (!timelapseRunning)
		{
			if (ImGui.Button("Start"))
			{
				cam.StartTimelapse();
			}
			return;
		}
		if (ImGui.Button("Stop"))
		{
			cam.StopTimelapse();
		}
		ImGui.Spacing();
		float timelapseElapsedSeconds = cam.TimelapseElapsedSeconds;
		float num5 = cam.TimelapseDurationMinutes * 60f;
		ImGui.ProgressBar((num5 > 0f) ? Mathf.Clamp01(timelapseElapsedSeconds / num5) : 0f, new Vector2(-1f, 0f), $"{cam.TimelapseFrameCount} frames  |  {timelapseElapsedSeconds:0.0}s / {num5:0.0}s");
		if (!string.IsNullOrEmpty(cam.TimelapseDirectory))
		{
			ImGui.TextDisabled(cam.TimelapseDirectory);
		}
	}

	private static void DrawPath(CinematicCamera cam)
	{
		if (!ImGui.CollapsingHeader("Path / Keyframes"))
		{
			return;
		}
		ImGui.TextWrapped("Compose a dolly path: fly to a pose, click 'Add from current'. Path interpolates with Catmull-Rom (smooth curves through your points) + slerp on rotation. While playback is running, camera input is locked. Frame the shot, then Stop to release.");
		CinematicPath currentPath = cam.CurrentPath;
		bool num = currentPath != null && currentPath.Count >= 2;
		ImGui.Checkbox("Show path overlay", ref cam.ShowPathOverlay);
		ImGui.SameLine();
		ImGui.Checkbox("...also outside cinematic", ref cam.ShowOverlayOutsideCinematic);
		if (ImGui.Button($"Add keyframe from current ({cam.AddKeyframeKey})"))
		{
			cam.AddKeyframeFromCurrent();
		}
		ImGui.SameLine();
		ImGui.TextDisabled($"{currentPath?.Count ?? 0} keyframes  |  total {currentPath?.TotalDuration ?? 0f:0.0}s");
		ImGui.Spacing();
		ImGui.BeginDisabled(!num || cam.TimelapseRunning);
		if (cam.PathPlaying)
		{
			if (ImGui.Button("Stop"))
			{
				cam.StopPath();
			}
			ImGui.SameLine();
			if (ImGui.Button(cam.PathPaused ? "Resume" : "Pause"))
			{
				cam.TogglePathPause();
			}
		}
		else if (ImGui.Button("Play"))
		{
			cam.StartPathPlayback();
		}
		ImGui.SameLine();
		ImGui.Checkbox("Loop", ref cam.PathPlaybackLoop);
		ImGui.EndDisabled();
		ImGui.SliderFloat("Playback speed", ref cam.PathPlaybackSpeed, 0.1f, 5f);
		if (currentPath != null)
		{
			ImGui.Checkbox("Constant speed (ignore per-keyframe Speed)", ref currentPath.ConstantSpeed);
			ImGui.BeginDisabled(!currentPath.ConstantSpeed);
			ImGui.DragFloat("Constant speed (m/s)", ref currentPath.ConstantSpeedMps, 0.1f, 0.05f, 200f);
			ImGui.EndDisabled();
		}
		if (cam.PathPlaying && currentPath != null)
		{
			float num2 = Mathf.Max(0.001f, currentPath.TotalDuration);
			ImGui.ProgressBar(Mathf.Clamp01(cam.PathTime / num2), new Vector2(-1f, 0f), $"{cam.PathTime:0.0} / {num2:0.0} s");
		}
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Text("Keyframes:");
		DrawKeyframeList(cam);
		ImGui.Spacing();
		ImGui.Separator();
		DrawPathSaveLoad(cam);
	}

	private static void DrawKeyframeList(CinematicCamera cam)
	{
		CinematicPath currentPath = cam.CurrentPath;
		if (currentPath == null || currentPath.Count == 0)
		{
			ImGui.TextDisabled("(empty — fly somewhere and click 'Add keyframe from current')");
			return;
		}
		int num = -1;
		int to = -1;
		int num2 = -1;
		int selectedKeyframeIndex = -1;
		for (int i = 0; i < currentPath.Count; i++)
		{
			CinematicKeyframe cinematicKeyframe = currentPath.Keyframes[i];
			ImGui.PushID(i);
			Vector3 position = cinematicKeyframe.Position;
			bool num3 = ImGui.TreeNodeEx($"#{i} {cinematicKeyframe.Label}  ({position.x:0.0}, {position.y:0.0}, {position.z:0.0})");
			ImGui.SameLine();
			if (ImGui.SmallButton("Up") && i > 0)
			{
				num = i;
				to = i - 1;
			}
			ImGui.SameLine();
			if (ImGui.SmallButton("Down") && i < currentPath.Count - 1)
			{
				num = i;
				to = i + 1;
			}
			ImGui.SameLine();
			if (ImGui.SmallButton("Jump"))
			{
				cam.JumpToKeyframe(i);
			}
			ImGui.SameLine();
			if (ImGui.SmallButton("Recap"))
			{
				cam.RecaptureKeyframe(i);
			}
			ImGui.SameLine();
			if (ImGui.SmallButton("X"))
			{
				num2 = i;
			}
			if (num3)
			{
				selectedKeyframeIndex = i;
				string input = cinematicKeyframe.Label ?? "";
				if (ImGui.InputText("Label", ref input, 64u))
				{
					cinematicKeyframe.Label = input;
				}
				Vector3 v = cinematicKeyframe.Position;
				if (ImGui.DragFloat3("Position", ref v, 0.1f))
				{
					cinematicKeyframe.Position = v;
				}
				Vector3 v2 = cinematicKeyframe.Rotation.eulerAngles;
				if (ImGui.DragFloat3("Rotation (euler °)", ref v2, 0.5f))
				{
					cinematicKeyframe.Rotation = Quaternion.Euler(v2);
				}
				ImGui.SliderFloat("FOV", ref cinematicKeyframe.Fov, 5f, 130f);
				ImGui.BeginDisabled(currentPath.ConstantSpeed);
				ImGui.DragFloat("Speed (m/s)", ref cinematicKeyframe.Speed, 0.1f, 0.05f, 200f);
				ImGui.EndDisabled();
				ImGui.SliderFloat("Hold duration (s)", ref cinematicKeyframe.HoldDuration, 0f, 30f);
				int current_item = (int)cinematicKeyframe.Ease;
				if (ImGui.Combo("Ease", ref current_item, _easeLabels, _easeLabels.Length))
				{
					cinematicKeyframe.Ease = (CinematicEase)current_item;
				}
				ImGui.TreePop();
			}
			ImGui.PopID();
		}
		if (num2 >= 0)
		{
			cam.RemoveKeyframe(num2);
		}
		else if (num >= 0)
		{
			cam.MoveKeyframe(num, to);
		}
		cam.SelectedKeyframeIndex = selectedKeyframeIndex;
	}

	private static void DrawPathSaveLoad(CinematicCamera cam)
	{
		ImGui.Text("Save / Load");
		ImGui.InputText("Save as", ref _saveAsName, 64u);
		ImGui.SameLine();
		ImGui.BeginDisabled(string.IsNullOrWhiteSpace(_saveAsName) || cam.CurrentPath == null);
		if (ImGui.Button("Save"))
		{
			cam.CurrentPath.Name = _saveAsName.Trim();
			if (cam.CurrentPath.SaveToDisk())
			{
				_savedListDirty = true;
			}
		}
		ImGui.EndDisabled();
		if (_savedListDirty || _savedPathNames == null)
		{
			_savedPathNames = CinematicPath.ListSavedNames();
			_savedListDirty = false;
		}
		if (ImGui.SmallButton("Refresh"))
		{
			_savedListDirty = true;
		}
		ImGui.SameLine();
		ImGui.TextDisabled($"({_savedPathNames.Count} saved)");
		if (_savedPathNames.Count == 0)
		{
			ImGui.TextDisabled("(none — paths save under <save>/cinematic_paths/)");
			return;
		}
		for (int i = 0; i < _savedPathNames.Count; i++)
		{
			string text = _savedPathNames[i];
			ImGui.PushID(text);
			ImGui.AlignTextToFramePadding();
			ImGui.Text(text);
			ImGui.SameLine();
			if (ImGui.SmallButton("Load"))
			{
				CinematicPath cinematicPath = CinematicPath.LoadFromDisk(text);
				if (cinematicPath != null)
				{
					cam.StopPath();
					SetCurrentPath(cam, cinematicPath);
					_saveAsName = cinematicPath.Name ?? text;
				}
			}
			ImGui.SameLine();
			if (ImGui.SmallButton("Delete") && CinematicPath.DeleteFromDisk(text))
			{
				_savedListDirty = true;
			}
			ImGui.PopID();
		}
	}

	private static void SetCurrentPath(CinematicCamera cam, CinematicPath p)
	{
		cam.CurrentPath.Name = p.Name;
		cam.CurrentPath.Keyframes = p.Keyframes ?? new List<CinematicKeyframe>();
		cam.SelectedKeyframeIndex = -1;
	}

	private static void DrawTether(CinematicCamera cam)
	{
		if (ImGui.CollapsingHeader("Tether (player leash)"))
		{
			ImGui.TextWrapped("0 = unlimited. Soft-clamps the camera within this radius of the player. Terrain LODs already stream around the camera, so this is mostly a memory bound.");
			float v = cam.TetherRadius;
			if (ImGui.SliderFloat("Radius (m)", ref v, 0f, 1000f))
			{
				cam.TetherRadius = v;
			}
		}
	}

	private static void DrawStatus(CinematicCamera cam)
	{
		if (ImGui.CollapsingHeader("Status", ImGuiTreeNodeFlags.DefaultOpen))
		{
			Transform transform = ((CameraController.CurrentCamera != null) ? CameraController.CurrentCamera.transform : null);
			Vector3 vector = ((transform != null) ? transform.position : Vector3.zero);
			Vector3 vector2 = ((transform != null) ? transform.forward : Vector3.forward);
			float num = ((CameraController.CurrentCamera != null) ? CameraController.CurrentCamera.fieldOfView : 0f);
			ImGui.Text($"Position : {vector.x:0.00}, {vector.y:0.00}, {vector.z:0.00}");
			ImGui.Text($"Forward  : {vector2.x:0.00}, {vector2.y:0.00}, {vector2.z:0.00}");
			ImGui.Text($"FOV      : {num:0.0}°");
			ImGui.Text($"Speed    : {cam.CurrentSpeed:0.00} m/s  (target {cam.BaseSpeed:0.00})");
		}
	}

	private static void DrawActions()
	{
		if (ImGui.Button("Close"))
		{
			Close();
		}
		ImGui.SameLine();
		if (ImGui.Button("Exit Cinematic"))
		{
			CinematicCamera.SetActive(on: false);
		}
	}

	private static int IndexOf<T>(T[] values, T target)
	{
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		for (int i = 0; i < values.Length; i++)
		{
			if (equalityComparer.Equals(values[i], target))
			{
				return i;
			}
		}
		return -1;
	}
}
