namespace Assets.Scripts.UI;

public class InputWindowBase : UserInterfaceBase
{
	public static bool IsInputWindow
	{
		get
		{
			if (InputSourceCode.InputState != InputPanelState.Waiting && InputWindow.InputState != InputPanelState.Waiting)
			{
				return InputPrefabs.InputState == InputPanelState.Waiting;
			}
			return true;
		}
	}

	public static void Cancel()
	{
		if (KeyManager.InputState != KeyInputState.Paused || InputSourceCode.InputState != InputPanelState.Waiting)
		{
			InputWindow.CancelInput();
			InputSourceCode.CancelInput();
			InputPrefabs.CancelInput();
		}
	}

	protected virtual void CloseOnClientConnected(bool isPaused, string message)
	{
		if (IsVisible && isPaused)
		{
			Cancel();
		}
	}

	public virtual void Initialize()
	{
		NetworkBase.PausedForClientConnectEvent += CloseOnClientConnected;
	}

	private void OnDestroy()
	{
		NetworkBase.PausedForClientConnectEvent -= CloseOnClientConnected;
	}

	public override void SetVisible(bool isVisble)
	{
		base.SetVisible(isVisble);
		SetInputKeyState(isVisble);
	}

	protected void SetInputKeyState(bool isTyping)
	{
		string key = "InputWindow_" + base.name;
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
