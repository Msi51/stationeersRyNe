using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI.HelperHints;
using Networks;
using UnityEngine;

namespace Trading;

public class ThingPrefabCondition : ConditionData
{
	[XmlAttribute("Id")]
	public string PrefabName;

	[XmlIgnore]
	public int PrefabNameHash;

	public override string DebugName => "Item " + PrefabName;

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ ((!string.IsNullOrEmpty(PrefabName)) ? Animator.StringToHash(PrefabName) : 0)) * 41;
	}

	public override void Initialise()
	{
		base.Initialise();
		if (!string.IsNullOrEmpty(PrefabName))
		{
			PrefabNameHash = Animator.StringToHash(PrefabName);
		}
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is AtmosAnalyser atmosAnalyser)
		{
			if (atmosAnalyser.ScannedAtmosphere?.Thing != null && atmosAnalyser.ScannedAtmosphere.Thing.PrefabHash == PrefabNameHash)
			{
				return base.Evaluate(t);
			}
		}
		else if (t is PlantAnalyserCartridge plantAnalyserCartridge)
		{
			if ((bool)plantAnalyserCartridge.ScannedPlant && plantAnalyserCartridge.ScannedPlant.PrefabHash == PrefabNameHash)
			{
				return base.Evaluate(t);
			}
		}
		else
		{
			if (t is Thing thing)
			{
				if (thing.PrefabHash == PrefabNameHash)
				{
					flag = true;
				}
				if (flag)
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (t is CableNetwork cableNetwork)
			{
				Device t2 = null;
				for (int num = cableNetwork.DeviceList.Count - 1; num >= 0; num--)
				{
					Device device = cableNetwork.DeviceList[num];
					if (device.GetAsThing.PrefabHash == PrefabNameHash)
					{
						t2 = device;
						flag = true;
						break;
					}
				}
				flag = flag && base.Evaluate(t2);
			}
			else if (t is PipeNetwork pipeNetwork)
			{
				IReferencable referencable = null;
				for (int num2 = pipeNetwork.StructureList.Count - 1; num2 >= 0; num2--)
				{
					INetworkedStructure networkedStructure = pipeNetwork.StructureList[num2];
					if (networkedStructure.GetAsThing.PrefabHash == PrefabNameHash)
					{
						referencable = networkedStructure;
						flag = true;
						break;
					}
				}
				if (referencable == null)
				{
					for (int num3 = pipeNetwork.DeviceList.Count - 1; num3 >= 0; num3--)
					{
						Device device2 = pipeNetwork.DeviceList[num3];
						if (device2.GetAsThing.PrefabHash == PrefabNameHash)
						{
							referencable = device2;
							flag = true;
							break;
						}
					}
				}
				flag = flag && base.Evaluate(referencable as Structure);
			}
			else if (t is StructureNetwork structureNetwork)
			{
				INetworkedStructure networkedStructure2 = null;
				for (int num4 = structureNetwork.StructureList.Count - 1; num4 >= 0; num4--)
				{
					INetworkedStructure networkedStructure3 = structureNetwork.StructureList[num4];
					if (networkedStructure3.GetAsThing.PrefabHash == PrefabNameHash)
					{
						networkedStructure2 = networkedStructure3;
						flag = true;
						break;
					}
				}
				flag = flag && base.Evaluate(networkedStructure2 as Structure);
			}
		}
		return flag;
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		if (!Prefab.TryFind(PrefabNameHash, out var thing))
		{
			ConsoleWindow.PrintError("Cannot find prefab " + PrefabName + " for child condition.");
			return;
		}
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		stringBuilder.AppendLine(GameStrings.CreateThingCondition.AsString(thing.ToTooltip()));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
