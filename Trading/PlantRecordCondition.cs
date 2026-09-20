using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

public class PlantRecordCondition : ConditionComparable
{
	[XmlAttribute("Status")]
	public PlantStatusType StatusType;

	[XmlAttribute("Value")]
	public float Value;

	public override string DebugName => $"PlantRecord {StatusType} {CompareOperator} {Value}";

	public override int GetChecksum()
	{
		return (int)(((((uint)base.GetChecksum() ^ (uint)StatusType) * 41) ^ (uint)(int)(Value * 1000f)) * 41);
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is Plant plant)
		{
			float record = plant.PlantRecord.GetRecord(StatusType);
			flag = Compare(record, Value);
		}
		if (flag)
		{
			return base.Evaluate(t);
		}
		return false;
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		string value = CompareOperator.DisplayString();
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		stringBuilder.AppendLine(GameStrings.PlantRecordCondition.AsString(StatusType.ToString().AsColor("white"), value.AsColor("white"), StringManager.Get(Value)));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
