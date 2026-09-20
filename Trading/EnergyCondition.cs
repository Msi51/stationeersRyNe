using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using UnityEngine;

namespace Trading;

[XmlType("Energy")]
public class EnergyCondition : ConditionComparable
{
	[XmlAttribute("Ratio")]
	public float Ratio = float.NaN;

	[XmlAttribute("Percent")]
	public float Percent = float.NaN;

	[XmlAttribute("State")]
	public BatteryCellState State = BatteryCellState.Full;

	public override string DebugName
	{
		get
		{
			if (!float.IsNaN(Ratio))
			{
				return $"Energy Ratio {Ratio}";
			}
			if (!float.IsNaN(Percent))
			{
				return $"Energy Percent {Percent}";
			}
			return $"Energy State {State}";
		}
	}

	public override int GetChecksum()
	{
		return (int)(((uint)((((base.GetChecksum() ^ (int)(Ratio * 1000f)) * 41) ^ (int)(Percent * 1000f)) * 41) ^ (uint)State) * 41);
	}

	public override bool Evaluate<T>(T t)
	{
		BatteryCell batteryCell = null;
		if (t is BatteryCell batteryCell2)
		{
			batteryCell = batteryCell2;
		}
		else if (t is Thing thing && thing is IBatteryPowered batteryPowered)
		{
			batteryCell = batteryPowered.Battery;
		}
		bool flag = false;
		if (batteryCell != null)
		{
			flag = ((!float.IsNaN(Ratio)) ? Compare((float)(int)batteryCell.CurrentPowerPercentage / 100f, Ratio) : (float.IsNaN(Percent) ? Compare((int)batteryCell.GetPowerState(), (int)State) : Compare(Mathf.RoundToInt((int)batteryCell.CurrentPowerPercentage), Mathf.RoundToInt(Percent))));
		}
		if (flag)
		{
			return base.Evaluate(t);
		}
		return false;
	}

	private bool Coalesce(StringBuilder stringBuilder, string compare)
	{
		if (float.IsNaN(Ratio) && float.IsNaN(Percent))
		{
			return false;
		}
		int num = Mathf.RoundToInt(Ratio * 100f);
		int num2 = Mathf.RoundToInt(Percent);
		BatteryCellState value;
		if (num == 100 || num2 == 100)
		{
			value = BatteryCellState.Full;
		}
		else
		{
			if (num != 0 && num2 != 0)
			{
				return false;
			}
			value = BatteryCellState.Empty;
		}
		stringBuilder.AppendLine(GameStrings.TradeEnergyState.AsString(compare.AsColor("white"), value.GetName().AsColor("yellow")));
		return true;
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		string text = CompareOperator.DisplayString();
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		if (!Coalesce(stringBuilder, text))
		{
			if (!float.IsNaN(Ratio))
			{
				stringBuilder.AppendLine(GameStrings.TradeEnergyRatio.AsString(text.AsColor("white"), (Ratio * 100f).ToStringPercent("yellow")));
			}
			if (!float.IsNaN(Percent))
			{
				stringBuilder.AppendLine(GameStrings.TradeEnergyRatio.AsString(text.AsColor("white"), Mathf.RoundToInt(Percent).ToStringPercent("yellow")));
			}
			if (State != BatteryCellState.Full || (float.IsNaN(Ratio) && float.IsNaN(Percent)))
			{
				stringBuilder.AppendLine(GameStrings.TradeEnergyState.AsString(text.AsColor("white"), State.GetName().AsColor("yellow")));
			}
		}
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
