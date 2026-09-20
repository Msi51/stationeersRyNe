using System;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.UI;
using UnityEngine;

namespace InputSystem;

public class KeyWrap
{
	public readonly KeyCode[] SecondaryKeys;

	public bool log;

	public KeyCode Key { get; private set; }

	public bool IsPressed { get; private set; }

	public bool IsPressedThisFrame { get; private set; }

	public event InputEvent Event;

	public event Action KeyUp;

	public event Action KeyDown;

	public event Action KeyHeld;

	public event Action<KeyCode> OnKeyAssigned;

	public KeyWrap()
	{
		KeyMap.AddToPolling(this);
	}

	public KeyWrap(KeyCode key, params KeyCode[] secondaries)
	{
		Key = key;
		SecondaryKeys = secondaries;
		KeyMap.AddToPolling(this);
	}

	public void PollForInput()
	{
		KeyCode[] secondaryKeys = SecondaryKeys;
		if (secondaryKeys == null || !secondaryKeys.Any((KeyCode secondaryKey) => !Input.GetKey(secondaryKey)))
		{
			IsPressedThisFrame = false;
			if (Input.GetKeyDown(Key))
			{
				IsPressedThisFrame = true;
				InvokeEvent(InputPhase.Down);
			}
			if (Input.GetKey(Key))
			{
				IsPressed = true;
				InvokeEvent(InputPhase.Held);
			}
			if (Input.GetKeyUp(Key))
			{
				IsPressed = false;
				IsPressedThisFrame = false;
				InvokeEvent(InputPhase.Up);
			}
		}
	}

	public void AssignKey(KeyCode newKey)
	{
		Key = newKey;
		this.OnKeyAssigned?.Invoke(newKey);
	}

	private void InvokeEvent(InputPhase phase)
	{
		if (log)
		{
			Debug.Log($"[InputSystem] <b>{Key}</b> {phase}");
		}
		if (ConsoleWindow.IsOpen)
		{
			return;
		}
		bool flag = Input.GetKey(KeyMap.Help) || Input.GetKeyUp(KeyMap.Help);
		bool flag2 = Input.GetKey(KeyMap.Cancel) || Input.GetKeyUp(KeyMap.Cancel);
		if ((!Stationpedia.IsOpenAndLocked || flag) && (!InputWindowBase.IsInputWindow || flag2 || flag))
		{
			this.Event?.Invoke(new InputContext(this, phase));
			switch (phase)
			{
			case InputPhase.Down:
				this.KeyDown?.Invoke();
				break;
			case InputPhase.Up:
				this.KeyUp?.Invoke();
				break;
			case InputPhase.Held:
				this.KeyHeld?.Invoke();
				break;
			default:
				throw new ArgumentOutOfRangeException("phase", phase, null);
			}
		}
	}
}
