using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Structures;

namespace Assets.Scripts.Objects.Motherboards;

public class DoorControl : LinkedCircuitboard
{
	public override int CommandLinkedActive => 4;

	public override int CommandLinkedInactive => 5;

	public override bool CanDeviceLink(Device device)
	{
		if (!(device is IDoorControl))
		{
			return device is Door;
		}
		return true;
	}

	public override void SetButtonText()
	{
		MainButtonText.text = (base.IsLinkedMode ? string.Format("{0}\nDOORS", base.IsLinkedActive ? "CLOSE" : "OPEN") : "TOGGLE\nDOORS");
	}
}
