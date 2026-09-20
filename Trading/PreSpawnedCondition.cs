using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

public class PreSpawnedCondition : ConditionData
{
	[XmlAttribute("Value")]
	public bool WasPreSpawned;

	public override string DebugName => "Was pre-spawned: " + StringManager.Get(WasPreSpawned);

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ (WasPreSpawned ? 1 : 0)) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is Thing thing)
		{
			flag = ThingSpawnData.PreSpawnedThingIds.Contains(thing.ReferenceId) == WasPreSpawned;
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
		stringBuilder.AppendLine(GameStrings.PreSpawnedCondition.AsString(StringManager.Get(WasPreSpawned)));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
