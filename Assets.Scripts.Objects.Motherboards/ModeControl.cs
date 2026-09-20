using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Motherboards;

public class ModeControl : LinkedCircuitboard
{
	public override int CommandLinkedActive => 13;

	public override int CommandLinkedInactive => 12;

	public override bool CanDeviceLink(Device device)
	{
		return device;
	}

	public override void SetButtonText()
	{
		MainButtonText.text = (base.IsLinkedMode ? $"{(base.IsLinkedActive ? GameStrings.ModeControlActive : GameStrings.ModeControlInactive)}\n{GameStrings.ModeControlMode}" : $"{GameStrings.ModeControlToggle}\n{GameStrings.ModeControlMode}");
	}
}
