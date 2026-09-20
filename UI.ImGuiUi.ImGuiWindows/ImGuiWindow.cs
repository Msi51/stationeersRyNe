using ImGuiNET;
using UnityEngine;

namespace UI.ImGuiUi.ImGuiWindows;

public abstract class ImGuiWindow
{
	public bool IsShowing;

	private readonly string _windowTitle;

	private readonly Vector2 _initialSize;

	private readonly Vector2 _initialPosition;

	public virtual ImGuiWindowFlags Flags => (ImGuiWindowFlags)18688;

	public virtual bool MouseControlMode => true;

	public virtual bool CaptureKeyInput => true;

	public abstract void OnOpen();

	public abstract void OnClose();

	public abstract void DrawContent();

	public ImGuiWindow(string title, Vector2 initialSize)
	{
		_windowTitle = title;
		_initialSize = initialSize;
		_initialPosition = new Vector2((float)Screen.width - initialSize.x, 0f);
	}

	public void Draw()
	{
		ImGuiWindowFlags flags = Flags;
		bool isShowing = IsShowing;
		ImGui.Begin(_windowTitle, ref IsShowing, flags);
		if (isShowing && !IsShowing)
		{
			CloseWindow();
			ImGui.End();
			return;
		}
		if (ImGui.IsWindowFocused() && KeyManager.GetButtonDown(KeyCode.Escape))
		{
			CloseWindow();
			ImGui.End();
			return;
		}
		ImGui.SetWindowSize(_initialSize, ImGuiCond.Once);
		ImGui.SetWindowPos(_initialPosition, ImGuiCond.Once);
		DrawContent();
		ImGui.End();
	}

	public void CloseWindow()
	{
		ImGuiWindowManager.Close(this);
	}
}
