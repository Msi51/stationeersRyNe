using Assets.Scripts.Util;

namespace Objects.Items;

public class StationDrillHeadProperties
{
	public readonly string SpeedMultiplier;

	public readonly string ReagentYieldMultiplier;

	public readonly string IceYieldMultiplier;

	public readonly string HealthMultiplier;

	public readonly string PowerConsumptionMultiplier;

	public StationDrillHeadProperties(RocketMiningDrillHead drillHead)
	{
		SpeedMultiplier = (drillHead.SpeedMultiplier * 100f).ToStringPercent("yellow");
		ReagentYieldMultiplier = (drillHead.ReagentYieldMultiplier * 100f).ToStringPercent("yellow");
		IceYieldMultiplier = (drillHead.IceYieldMultiplier * 100f).ToStringPercent("yellow");
		HealthMultiplier = (drillHead.HealthMultiplier * 100f).ToStringPercent("yellow");
		PowerConsumptionMultiplier = (drillHead.PowerConsumptionMultiplier * 100f).ToStringPercent("yellow");
	}

	public StationDrillHeadProperties()
	{
	}
}
