using Assets.Scripts.Objects.Pipes;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Items;

public class ComputerButton
{
	public Button Button;

	public Text Label;

	public Device AssignedDevice;

	public Motherboard Parent;

	public void SetVisible(bool isVisible)
	{
		Button.gameObject.SetActive(isVisible);
	}

	public void SetColor()
	{
		ColorBlock colors = Button.colors;
		if (!Parent || Parent.ParentComputer == null || Parent.ParentComputer.DataCableNetwork == null || !AssignedDevice)
		{
			colors.normalColor = Color.red;
		}
		else if (Parent.LinkedDevices.Contains(AssignedDevice))
		{
			colors.normalColor = (Parent.ParentComputer.DataCableNetwork.IsNetworkDevice(AssignedDevice) ? Color.green : Color.red);
		}
		else
		{
			colors.normalColor = Color.white;
			Button.interactable = Parent.CanDeviceLink(AssignedDevice);
		}
		Button.colors = colors;
	}
}
