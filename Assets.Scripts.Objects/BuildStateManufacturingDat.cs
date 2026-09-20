using System;
using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects;

[Serializable]
public class BuildStateManufacturingDat
{
	private static EnumCollection<MachineTier, int> MachineTiers = new EnumCollection<MachineTier, int>();

	public MachineTier MachinesTier;

	public float BuildTimeMultiplier = 1f;

	public int ItemSpawnMultiplier = 1;

	public float EnergyCostMultiplier = 1f;

	public float MaterialCostMultiplier = 1f;

	public void AddString(StringBuilder requirementsString)
	{
		if (MachinesTier != MachineTier.Undefined)
		{
			requirementsString.AppendLine(GameStrings.ManufacturesAtTier.AsString(MachineTiers.GetName(MachinesTier)));
		}
		if (!RocketMath.Approximately(BuildTimeMultiplier, 1f, 0.01f))
		{
			requirementsString.AppendLine(GameStrings.ManufacturesAppliesMultiplier.AsString("Build Time", StringGenerator.GetString((int)(BuildTimeMultiplier * 100f), Unit.Percent)));
		}
		if (ItemSpawnMultiplier != 1)
		{
			requirementsString.AppendLine(GameStrings.ManufacturesAppliesMultiplier.AsString("Output", StringGenerator.GetString(ItemSpawnMultiplier * 100, Unit.Percent)));
		}
		if (!RocketMath.Approximately(EnergyCostMultiplier, 1f, 0.01f))
		{
			requirementsString.AppendLine(GameStrings.ManufacturesAppliesMultiplier.AsString("Energy Cost", StringGenerator.GetString((int)(EnergyCostMultiplier * 100f), Unit.Percent)));
		}
		if (!RocketMath.Approximately(MaterialCostMultiplier, 1f, 0.01f))
		{
			requirementsString.AppendLine(GameStrings.ManufacturesAppliesMultiplier.AsString("Material Cost", StringGenerator.GetString((int)(MaterialCostMultiplier * 100f), Unit.Percent)));
		}
	}
}
