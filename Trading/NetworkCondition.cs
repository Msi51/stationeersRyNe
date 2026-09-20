using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using Networks;

namespace Trading;

public class NetworkCondition : ConditionData
{
	[XmlAttribute("Type")]
	public StructureNetworkType NetworkType;

	public override string DebugName => $"Network {NetworkType}";

	public override int GetChecksum()
	{
		return (int)(((uint)base.GetChecksum() ^ (uint)NetworkType) * 41);
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is AtmosAnalyser { ScannedAtmosphere: var scannedAtmosphere } atmosAnalyser)
		{
			flag = scannedAtmosphere != null && scannedAtmosphere.Mode == AtmosphereHelper.AtmosphereMode.Network;
			switch (NetworkType)
			{
			case StructureNetworkType.LandingPad:
				if (flag && atmosAnalyser.ScannedAtmosphere.AtmosphericsNetwork is LandingPadNetwork t3)
				{
					return base.Evaluate(t3);
				}
				break;
			case StructureNetworkType.Pipe:
				if (flag && atmosAnalyser.ScannedAtmosphere.AtmosphericsNetwork is PipeNetwork t2)
				{
					return base.Evaluate(t2);
				}
				break;
			default:
				flag = false;
				break;
			}
		}
		if (t is PlantAnalyserCartridge { ScannedPlant: not null } plantAnalyserCartridge)
		{
			return base.Evaluate(plantAnalyserCartridge.ScannedPlant);
		}
		if (t is StructureNetwork structureNetwork)
		{
			flag = structureNetwork.NetworkType == NetworkType;
		}
		if (NetworkType == StructureNetworkType.Cable && t is CableNetwork)
		{
			flag = true;
		}
		if (NetworkType == StructureNetworkType.Pipe && t is PipeNetwork)
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
		GameStrings.BuildNetworkCondition.AppendFormat(stringBuilder, EnumCollections.StructureNetworkType.GetName(NetworkType).AsColor("white"));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
