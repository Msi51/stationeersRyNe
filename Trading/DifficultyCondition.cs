using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using UnityEngine;

namespace Trading;

[XmlType("Difficulty")]
public class DifficultyCondition : ConditionComparable
{
	[XmlAttribute("Id")]
	public string Id;

	public override string DebugName => "Difficulty " + Id;

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ ((!string.IsNullOrEmpty(Id)) ? Animator.StringToHash(Id) : 0)) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		int b = DifficultySetting.Find(Id)?.Index ?? int.MinValue;
		int a = DifficultySetting.Current?.Index ?? int.MinValue;
		if (Compare(a, b))
		{
			return base.Evaluate(t);
		}
		return false;
	}

	private bool Coalesce(StringBuilder stringBuilder, string compare)
	{
		stringBuilder.AppendLine(GameStrings.DifficultyState.AsString(compare.AsColor("white"), Id.AsColor("yellow")));
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
			stringBuilder.AppendLine(GameStrings.DifficultyState.AsString(text.AsColor("white"), Id.AsColor("yellow")));
		}
	}
}
