using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using Networks;
using UnityEngine;

namespace Trading;

public class SizeCondition : ConditionComparable
{
	[XmlAttribute("X")]
	public int ValueX;

	[XmlAttribute("Y")]
	public int ValueY;

	public override string DebugName => $"PadSize {ValueX} x {ValueY}";

	public override int GetChecksum()
	{
		return (((base.GetChecksum() ^ ValueX) * 41) ^ ValueY) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is LandingPadNetwork landingPadNetwork)
		{
			flag = landingPadNetwork.LandingPadCenter != null && landingPadNetwork.LandingPadCenter.CheckPadSize(new Vector2(ValueX, ValueY));
		}
		if (flag)
		{
			return base.Evaluate(t);
		}
		return false;
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		GameStrings.SizeTwoDCondition.AppendFormat(stringBuilder, CompareOperator.DisplayString(), StringManager.Get(ValueX), StringManager.Get(ValueY));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
