using System;
using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Items;

public class RocketMiningDrillHead : Consumable
{
	private static readonly System.Random UseRandom = new System.Random();

	private const float CHANCE_TO_DAMAGE = 0.1f;

	[Range(0f, 10f)]
	public float SpeedMultiplier = 1f;

	[Range(0f, 10f)]
	public float ReagentYieldMultiplier = 1f;

	[Range(0f, 10f)]
	public float IceYieldMultiplier = 1f;

	[Range(0f, 10f)]
	public float HealthMultiplier = 1f;

	[Range(0f, 10f)]
	public float PowerConsumptionMultiplier = 1f;

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.AppendLine(GameStrings.MiningHeadSpeedMultiplierTooltip.AsString((SpeedMultiplier * 100f).ToStringPercent("yellow")));
		extendedText.AppendLine(GameStrings.MiningHeadReagentYieldMultiplierTooltip.AsString((ReagentYieldMultiplier * 100f).ToStringPercent("yellow")));
		extendedText.AppendLine(GameStrings.MiningHeadIceYieldMultiplierTooltip.AsString((IceYieldMultiplier * 100f).ToStringPercent("yellow")));
		extendedText.AppendLine(GameStrings.MiningHeadHealthMultiplierTooltip.AsString((HealthMultiplier * 100f).ToStringPercent("yellow")));
		extendedText.AppendLine(GameStrings.MiningHeadPowerConsuptionSpeedTooltip.AsString((PowerConsumptionMultiplier * 100f).ToStringPercent("yellow")));
		return extendedText;
	}

	public void OnResourceCollected()
	{
		if (UseRandom.NextDouble() < (double)(0.1f / HealthMultiplier))
		{
			base.Quantity -= 1f;
		}
	}

	public override void Recycle()
	{
		base.Recycle();
		DestroyItem();
	}
}
