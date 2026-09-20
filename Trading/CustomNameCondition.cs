using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using UnityEngine;

namespace Trading;

public class CustomNameCondition : ConditionData
{
	[XmlAttribute("Value")]
	public string Value;

	[XmlIgnore]
	public int ValueHash;

	public override string DebugName => "CustomName " + Value;

	public override void Initialise()
	{
		base.Initialise();
		if (Value != null)
		{
			ValueHash = Animator.StringToHash(Value);
		}
	}

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ Value.GetHashCode()) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is Thing thing && !string.IsNullOrEmpty(thing.CustomName) && Animator.StringToHash(thing.CustomName) == ValueHash)
		{
			flag = true;
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
		GameStrings.CustomNameCondition.AppendFormat(stringBuilder, Value);
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
