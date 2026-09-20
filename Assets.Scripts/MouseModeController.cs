using System.Collections.Generic;
using Assets.Scripts.UI;
using UnityEngine;

namespace Assets.Scripts;

public static class MouseModeController
{
	public static bool InGame;

	public static bool InCharacterCustomisation;

	private static List<IModal> _openModals = new List<IModal>();

	private static int _cursorLockPrevious = -1;

	private static int _mouseControlPrevious = -1;

	private static int _panelHandsPrevious = -1;

	public static bool AltKeyDown => KeyManager.GetButton(KeyMap.MouseControl);

	public static void Reset()
	{
		_cursorLockPrevious = -1;
		_mouseControlPrevious = -1;
		_panelHandsPrevious = -1;
	}

	public static void AddModal(IModal modal)
	{
		if (!_openModals.Contains(modal))
		{
			_openModals.Add(modal);
		}
	}

	public static void RemoveModal(IModal modal)
	{
		_openModals.Remove(modal);
	}

	public static void Check()
	{
		if (!InGame)
		{
			SetState(locked: false);
			return;
		}
		if (InCharacterCustomisation)
		{
			SetState(locked: false);
			return;
		}
		if (AltKeyDown)
		{
			SetState(locked: false);
			return;
		}
		foreach (IModal openModal in _openModals)
		{
			if (openModal.UnlockCursor)
			{
				SetState(locked: false);
				return;
			}
		}
		SetState(locked: true);
	}

	private static void SetState(bool locked)
	{
		int num = (locked ? 1 : 0);
		bool flag = ((!locked) ? (Cursor.lockState == CursorLockMode.None && Cursor.visible) : (Cursor.lockState == CursorLockMode.Locked && !Cursor.visible));
		if (_cursorLockPrevious != num || !flag)
		{
			CursorManager.SetCursor(locked);
			_cursorLockPrevious = num;
		}
		int num2 = ((!locked) ? 1 : 0);
		if (_mouseControlPrevious != num2)
		{
			InputMouse.SetMouseControl(!locked);
			_mouseControlPrevious = num2;
		}
		int num3 = (locked ? 1 : 0);
		if (_panelHandsPrevious != num3)
		{
			if (locked)
			{
				PanelHands.Instance.ShowSlotInfo();
			}
			else
			{
				PanelHands.Instance.HideSlotInfo();
			}
			_panelHandsPrevious = num3;
		}
	}
}
