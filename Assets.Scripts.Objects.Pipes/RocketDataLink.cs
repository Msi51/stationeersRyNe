using Assets.Scripts.Objects.Electrical;

namespace Assets.Scripts.Objects.Pipes;

public class RocketDataLink : LogicUnitBase
{
	public override bool ShowStateTooltip => false;

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		_ = IsOperable;
	}
}
