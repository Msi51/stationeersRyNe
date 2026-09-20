using System;
using System.Diagnostics;
using Assets.Scripts.UI.ImGuiUi;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using UI.ImGuiUi;
using UnityEngine;

namespace Assets.Scripts.UI;

public class ImGuiLoadingScreen
{
	private const string NO_STATE = "No State";

	private const float TimeBetweenProgressUpdates = 100f;

	private static readonly Stopwatch ProgressStopwatch = new Stopwatch();

	private static Texture backgroundTexture;

	public static string _title = "LoadingScreen";

	private const ImGuiWindowFlags windowFlags = (ImGuiWindowFlags)787375;

	public static string State { get; private set; }

	public static float Progress { get; set; }

	public static string WorldName { get; set; }

	public static bool IsShowing { get; private set; }

	public static bool HideBackground { get; set; }

	public static event Action OnStateChanged;

	public static void SetRandomBackgroundTexture()
	{
		backgroundTexture = ImGuiManager.RandomLoadingTexture();
	}

	public static async UniTask SetState(string state, bool resetProgress = true)
	{
		if (!(state == State))
		{
			State = state;
			ImGuiLoadingScreen.OnStateChanged?.Invoke();
			if (resetProgress)
			{
				await SetProgress(0f);
			}
		}
	}

	public static async UniTask SetProgress(float progress, bool reset = false)
	{
		if (!reset && !IsShowing)
		{
			ProgressStopwatch.Reset();
			return;
		}
		if (!reset && !ProgressStopwatch.IsRunning)
		{
			ProgressStopwatch.Start();
		}
		Progress = progress;
		if (!reset && (float)ProgressStopwatch.ElapsedMilliseconds > 100f)
		{
			await UniTask.Yield();
			ProgressStopwatch.Restart();
			ImGuiLoadingScreen.OnStateChanged?.Invoke();
		}
	}

	public static async UniTaskVoid FakeProgress()
	{
		float progress = 0f;
		string curState = State;
		while (progress < 1f && curState == State)
		{
			progress += 0.1f;
			await SetProgress(progress);
		}
	}

	public static void SetActive(bool active)
	{
		if (active != IsShowing)
		{
			IsShowing = active;
			State = "No State";
			SetRandomBackgroundTexture();
			ProgressStopwatch.Reset();
			if (!active)
			{
				HideBackground = false;
			}
		}
	}

	public static void DrawSplashLoading(float progress)
	{
		Progress = progress;
		DrawProgressBar(centered: false);
	}

	public static void DrawStandardLoading()
	{
		if ((bool)backgroundTexture && !HideBackground)
		{
			DrawBackground(stretched: false);
		}
		DrawProgressBar(centered: true);
	}

	private static void DrawBackground(bool stretched)
	{
		if (stretched)
		{
			ImGuiUn.DrawImageBack(backgroundTexture, Vector2.zero, ImguiHelper.ScreenSize, ImguiHelper.ColorConvertFloat4ToU32(Color.white));
			return;
		}
		float num = (float)backgroundTexture.width / (float)backgroundTexture.height;
		int width = Screen.width;
		float num2 = (float)width / num;
		int num3 = Mathf.RoundToInt(((float)Screen.height - num2) * 0.5f);
		Vector2 p_min = new Vector2(0f, num3);
		Vector2 p_max = new Vector2(width, Screen.height - num3);
		ImDrawListPtr backgroundDrawList = ImGui.GetBackgroundDrawList();
		backgroundDrawList.AddRectFilled(Vector2.zero, ImguiHelper.ScreenSize, ImGuiColor.Integer.Black);
		ImGuiUn.DrawImageBack(backgroundTexture, p_min, p_max, ImguiHelper.ColorConvertFloat4ToU32(Color.white));
		IntPtr user_texture_id = ImGuiManager.ImGuiPointerFor(backgroundTexture);
		backgroundDrawList.AddImage(user_texture_id, p_min, p_max, default(Vector2), new Vector2(1f, 1f), ImguiHelper.ColorConvertFloat4ToU32(Color.white));
	}

	private static void DrawProgressBar(bool centered)
	{
		ImGui.Begin(_title, (ImGuiWindowFlags)787375);
		if (!centered)
		{
			ImGui.SetWindowSize(new Vector2(Screen.width, 120f * ImguiHelper.UIScale));
			float x = ImguiHelper.ScreenCenter.x - ImGui.GetWindowWidth() / 2f;
			float y = (float)Screen.height - (ImGui.GetWindowHeight() + ImguiHelper.PaddingScaled);
			ImGui.SetWindowPos(new Vector2(x, y), ImGuiCond.Always);
			if (!string.IsNullOrEmpty(State))
			{
				ImGuiUn.Text(State, TextAlignment.Center);
			}
			ImguiHelper.SetCursorForCenter(420f * ImguiHelper.UIScale);
		}
		else
		{
			ImguiHelper.SetCurrentWindowToCenter();
			if (!string.IsNullOrEmpty(State))
			{
				ImguiHelper.TextShadow(State, ImGuiColor.Integer.White, TextAlignment.Center);
			}
		}
		ImGui.ProgressBar(Progress, new Vector2(420f, 0f) * ImguiHelper.UIScale);
		ImGui.End();
	}
}
