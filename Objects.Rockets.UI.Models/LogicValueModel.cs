using Assets.Scripts.Objects.Motherboards;

namespace Objects.Rockets.UI.Models;

public struct LogicValueModel
{
	public long DeviceReferenceId;

	public string DisplayName;

	public LogicType LogicType;

	public double Value;

	public bool CanLogicWrite;

	public bool Pinned;
}
