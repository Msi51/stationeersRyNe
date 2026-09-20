using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

public class EntityStateCondition : ConditionData
{
	[XmlElement("IsOnline")]
	public BoolReference IsOnline;

	[XmlElement("State")]
	public EnumReference<EntityState> EntityState;

	public override string DebugName
	{
		get
		{
			string text = ((IsOnline != null) ? $"IsOnline {IsOnline.Value}" : "");
			string text2 = ((EntityState != null) ? ("State " + EnumCollections.EntityStates.GetName(EntityState)) : "");
			return "Player is " + text + " " + text2;
		}
	}

	public override int GetChecksum()
	{
		return (int)(((uint)((base.GetChecksum() ^ (IsOnline ? 1 : 0)) * 41) ^ (uint)EntityState.Value) * 41);
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is Human human)
		{
			if (IsOnline == null && EntityState == null)
			{
				flag = true;
			}
			else
			{
				if (IsOnline != null)
				{
					flag = human.OrganBrain != null && human.OrganBrain.IsOnline == IsOnline.Value;
				}
				if (EntityState != null)
				{
					flag = human.State == (EntityState)EntityState;
				}
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
		stringBuilder.AppendLine(GameStrings.HumanCondition.AsString((IsOnline != null) ? StringManager.Get(IsOnline).AsColor("white") : "", (EntityState != null) ? EnumCollections.EntityStates.GetName(EntityState).AsColor("white") : ""));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
