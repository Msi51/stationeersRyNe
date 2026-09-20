using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;

namespace Trading;

public class BuildStateCondition : ConditionData
{
	[XmlAttribute("IsCompleted")]
	public bool IsCompleted;

	[XmlAttribute("CanManufacture")]
	public bool CanManufacture;

	[XmlAttribute("MachineTier")]
	public MachineTier MachineTier;

	[XmlAttribute("BlockGravity")]
	public bool BlockGravity;

	public override string DebugName
	{
		get
		{
			string text = (IsCompleted ? "IsCompleted" : string.Empty);
			string text2 = (CanManufacture ? "CanManufacture" : string.Empty);
			string text3 = ((MachineTier != MachineTier.Undefined) ? $"MachineTier: {MachineTier}" : string.Empty);
			return "Structure " + text + " " + text2 + " " + text3;
		}
	}

	public override int GetChecksum()
	{
		return (int)(((uint)((((base.GetChecksum() ^ (IsCompleted ? 1 : 0)) * 41) ^ (CanManufacture ? 1 : 0)) * 41) ^ (uint)MachineTier) * 41);
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is Structure structure)
		{
			if (CanManufacture && structure.CurrentBuildState.CanManufacture)
			{
				flag = true;
			}
			if (IsCompleted && structure.IsStructureCompleted)
			{
				flag = true;
			}
			if (MachineTier != MachineTier.Undefined && structure.CurrentBuildState.ManufactureDat.MachinesTier == MachineTier)
			{
				flag = true;
			}
			if (BlockGravity && structure.CurrentBuildState.BlockGravity)
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
		if (IsCompleted)
		{
			stringBuilder.AppendLine(GameStrings.StructureIsCompleted);
		}
		if (CanManufacture)
		{
			stringBuilder.AppendLine(GameStrings.StructureCanManufacture);
		}
		if (MachineTier != MachineTier.Undefined)
		{
			stringBuilder.AppendLine(EnumCollections.MachineTier.GetName(MachineTier));
		}
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
