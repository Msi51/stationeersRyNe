using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using Objects.Items;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class FuelOre : GrindableOre, ISolidFuel, IQuantity, ITradable, IEvaluable, IReferencable
{
	[Header("Fuel Ore")]
	public float EnergyPerSecond;

	public float GetEnergyPerSecond()
	{
		return EnergyPerSecond;
	}

	public static StringBuilder MakeSlotTooltip(StringBuilder sb, ISolidFuel solidFuel)
	{
		if (solidFuel.ParentSlot?.Parent is PowerGeneratorSlot)
		{
			if (!(solidFuel is Stackable stackable))
			{
				if (solidFuel is Consumable)
				{
					sb.AppendLine(GameStrings.ItemInSlotValue.AsString(solidFuel.ToTooltip(), solidFuel.GetQuantityText()));
				}
			}
			else
			{
				sb.AppendLine(GameStrings.ItemInSlotStack.AsString(solidFuel.ToTooltip(), StringManager.Get(stackable.Quantity)));
			}
			int value = Mathf.RoundToInt(solidFuel.GetEnergyPerSecond() * solidFuel.GetQuantity);
			sb.AppendLine(GameStrings.SlotItemProvidesPower.AsString(solidFuel.ToTooltip(), value.ToStringPrefix("W").AsColor("yellow")));
		}
		return sb;
	}

	public override StringBuilder GetSlotTooltip()
	{
		return MakeSlotTooltip(base.GetSlotTooltip(), this);
	}
}
