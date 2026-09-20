using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI.HelperHints;
using UnityEngine;

namespace Trading;

public class ObjectiveCompleteCondition : ConditionData
{
	[XmlAttribute("Id")]
	public string ObjectiveId;

	[XmlIgnore]
	public int ObjectiveIdHash;

	public override string DebugName => "Complete Objective " + ObjectiveId;

	public override void Initialise()
	{
		base.Initialise();
		if (!string.IsNullOrEmpty(ObjectiveId))
		{
			ObjectiveIdHash = Animator.StringToHash(ObjectiveId);
		}
	}

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ ObjectiveId.GetHashCode()) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is WorldObjectiveState worldObjectiveState && worldObjectiveState.WorldObjective.IdHash == ObjectiveIdHash && worldObjectiveState.Completed)
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
		if (ObjectiveId != null)
		{
			stringBuilder.AppendLine(GameStrings.ObjectiveIsCompletedCondition.AsString(ObjectiveId));
		}
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
