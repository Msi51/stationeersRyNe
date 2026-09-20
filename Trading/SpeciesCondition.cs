using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using CharacterCustomisation;

namespace Trading;

[XmlType("Species")]
public class SpeciesCondition : ConditionData
{
	[XmlAttribute("Id")]
	public SpeciesClass Id;

	public override string DebugName => $"Species {Id}";

	public override int GetChecksum()
	{
		return (int)(((uint)base.GetChecksum() ^ (uint)Id) * 41);
	}

	public override bool Evaluate<T>(T t)
	{
		if (!(t is Human human))
		{
			return false;
		}
		if (human.SpeciesClass == Id)
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
		stringBuilder.AppendLine(GameStrings.SpeciesState.AsString(EnumCollections.Species.GetName(Id).AsColor("yellow")));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
