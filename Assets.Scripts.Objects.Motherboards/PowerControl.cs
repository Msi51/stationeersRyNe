using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Motherboards;

public class PowerControl : LinkedCircuitboard
{
	public override int CommandLinkedActive => 10;

	public override int CommandLinkedInactive => 11;

	public override bool CanDeviceLink(Device device)
	{
		if (!(typeof(Device) == device.GetType()))
		{
			return device.GetType().IsSubclassOf(typeof(Device));
		}
		return true;
	}

	public override void SetButtonText()
	{
		MainButtonText.text = (base.IsLinkedMode ? $"{GameStrings.PowerControlPower}\n{(base.IsLinkedActive ? GameStrings.PowerControlPowerOff : GameStrings.PowerControlPowerOn)}" : $"{GameStrings.ModeControlToggle}\n{GameStrings.PowerControlPower}");
	}
}
