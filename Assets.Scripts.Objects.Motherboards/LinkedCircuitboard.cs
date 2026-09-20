using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class LinkedCircuitboard : Circuitboard
{
	public Text MainButtonText;

	public Text ButtonLockText;

	public Text ButtonModeText;

	private int _toggles;

	public LinkedControlMode ControlMode => (LinkedControlMode)base.Flag;

	public virtual int CommandToggle => 0;

	public virtual int CommandLinkedActive => 0;

	public virtual int CommandLinkedInactive => 0;

	public bool IsLockedMode => FlagGet(1);

	public bool IsLinkedMode => FlagGet(2);

	public bool IsLinkedActive => FlagGet(4);

	public override void Awake()
	{
		base.Awake();
		SetFlag(base.Flag);
	}

	public virtual void SetButtonText()
	{
		MainButtonText.text = (IsLinkedMode ? string.Format("{0}\nDEVICES", IsLinkedActive ? "INACTIVE" : "ACTIVE") : "TOGGLE\nDEVICES");
	}

	public override void RemoteToggle(bool open)
	{
		if (IsLinkedMode)
		{
			int num = 0;
			if (IsLockedMode)
			{
				num++;
			}
			if (IsLinkedMode)
			{
				num += 2;
			}
			if (open)
			{
				_toggles++;
			}
			else
			{
				_toggles--;
			}
			if (IsLinkedActive && _toggles <= 0)
			{
				num = num;
				Motherboard.UseComputer(CommandLinkedInactive, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, -1, sendToAll: false);
				Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, num, sendToAll: true);
				Motherboard.UseComputer(6, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, -1, sendToAll: true);
			}
			else if (!IsLinkedActive && _toggles > 0)
			{
				num += 4;
				Motherboard.UseComputer(CommandLinkedActive, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, -1, sendToAll: false);
				Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, num, sendToAll: true);
				Motherboard.UseComputer(6, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, -1, sendToAll: true);
			}
		}
		else
		{
			Motherboard.UseComputer(0, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, -1, sendToAll: false);
		}
	}

	public override void RefreshScreen()
	{
		base.RefreshScreen();
		SetButtonText();
	}

	public override void OnDeviceListChanged(Device device)
	{
		base.OnDeviceListChanged(device);
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (base.LinkedDevices.Contains(device))
		{
			if (IsDeviceConnected(device))
			{
				return;
			}
			OnServer.Interact(device, InteractableType.Lock, IsLockedMode ? 1 : 0);
			if (IsLinkedMode)
			{
				OnServer.Interact(device, ButtonMode, IsLinkedActive ? 1 : 0);
			}
		}
		else
		{
			OnServer.Interact(device, InteractableType.Lock, 0);
			OnServer.Interact(device.InteractOnOff, 0);
		}
		SetButtonText();
	}

	public void ButtonToggleLock()
	{
		int num = 0;
		if (IsLinkedMode)
		{
			num += 2;
		}
		if (IsLinkedActive)
		{
			num += 4;
		}
		num += ((!IsLockedMode) ? 1 : 0);
		Motherboard.UseComputer(3, base.netId, base.netId, num, sendToAll: true);
	}

	public override void ButtonToggleMode()
	{
		int num = 0;
		if (IsLockedMode)
		{
			num++;
		}
		if (IsLinkedActive)
		{
			num += 4;
		}
		num += ((!IsLinkedMode) ? 2 : 0);
		Motherboard.UseComputer(3, base.netId, base.netId, num, sendToAll: true);
	}

	public override void SetFlag(int page)
	{
		base.SetFlag(page);
		base.Flag = page;
		ButtonLockText.text = (IsLockedMode ? "Manual Operation\n<b>Disabled</b>" : "Manual Operation\n<b>Enabled</b>");
		ButtonModeText.text = (IsLinkedMode ? "Mode\n<b>Linked</b>" : "Mode\n<b>Toggle</b>");
		SetButtonText();
		if (!GameManager.RunSimulation)
		{
			return;
		}
		foreach (Device linkedDevice in base.LinkedDevices)
		{
			if (IsDeviceConnected(linkedDevice))
			{
				OnServer.Interact(linkedDevice, InteractableType.Lock, IsLockedMode ? 1 : 0);
				if (IsLinkedMode)
				{
					OnServer.Interact(linkedDevice, ButtonMode, IsLinkedActive ? 1 : 0);
				}
			}
		}
	}

	public override void ButtonToggle()
	{
		foreach (Device linkedDevice in base.LinkedDevices)
		{
			if (!linkedDevice.AllowInteraction)
			{
				return;
			}
		}
		if (IsLinkedMode)
		{
			int num = 0;
			if (IsLockedMode)
			{
				num++;
			}
			if (IsLinkedMode)
			{
				num += 2;
			}
			num += ((!IsLinkedActive) ? 4 : 0);
			Motherboard.UseComputer(IsLinkedActive ? CommandLinkedInactive : CommandLinkedActive, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, -1, sendToAll: false);
			Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, num, sendToAll: true);
			Motherboard.UseComputer(6, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, -1, sendToAll: true);
		}
		else
		{
			Motherboard.UseComputer(0, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, -1, sendToAll: false);
		}
	}
}
