using System.Collections.Generic;
using Assets.Scripts.UI;

namespace UI.ImGuiUi.ImGuiWindows;

public static class ImGuiWindowManager
{
	private static List<ImGuiWindow> _openWindows = new List<ImGuiWindow>();

	public static void Draw()
	{
		for (int num = _openWindows.Count - 1; num >= 0; num--)
		{
			_openWindows[num].Draw();
		}
	}

	public static void Open(ImGuiWindow window)
	{
		_openWindows.Add(window);
		window.IsShowing = true;
		window.OnOpen();
		CheckInputState();
	}

	public static void Close(ImGuiWindow window)
	{
		_openWindows.Remove(window);
		window.IsShowing = false;
		window.OnClose();
		CheckInputState();
	}

	private static void CheckInputState()
	{
		bool mouseControl = false;
		bool inputKeyState = false;
		foreach (ImGuiWindow openWindow in _openWindows)
		{
			if (openWindow.MouseControlMode)
			{
				mouseControl = true;
			}
			if (openWindow.CaptureKeyInput)
			{
				inputKeyState = true;
			}
		}
		InputMouse.SetMouseControl(mouseControl);
		SetInputKeyState(inputKeyState);
	}

	private static void SetInputKeyState(bool isTyping)
	{
		string key = "Input_WindowManager";
		if (isTyping)
		{
			KeyManager.SetInputState(key, KeyInputState.Typing);
		}
		else
		{
			KeyManager.RemoveInputState(key);
		}
	}
}
