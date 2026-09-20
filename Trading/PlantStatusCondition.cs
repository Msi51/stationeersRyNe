using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

public class PlantStatusCondition : ConditionData
{
	[XmlAttribute("Status")]
	public PlantStatusType StatusType;

	[XmlAttribute("Value")]
	public bool Value;

	public override string DebugName => $"PlantStatus {StatusType} {Value}";

	public override int GetChecksum()
	{
		return (int)(((((uint)base.GetChecksum() ^ (uint)StatusType) * 41) ^ (uint)(Value ? 1 : 0)) * 41);
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is Plant plant)
		{
			bool currentState = plant.PlantStatus.GetCurrentState(StatusType);
			if (Value == currentState)
			{
				flag = true;
			}
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
		stringBuilder.AppendLine(GameStrings.PlantStatusCondition.AsString(StatusType.ToString().AsColor("white"), StringManager.Get(Value)));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
