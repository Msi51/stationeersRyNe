using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Networking;

namespace Objects.Rockets.Mining;

public class DepositComposition
{
	public bool IsContentsHidden;

	public List<DepositMaterialOre> Ores = new List<DepositMaterialOre>();

	public List<DepositMaterialReagentMix> ReagentMixes = new List<DepositMaterialReagentMix>();

	public List<DepositMaterialGas> FrozenGasses = new List<DepositMaterialGas>();

	public List<DepositMaterialGas> Gasses = new List<DepositMaterialGas>();

	public DepositComposition()
	{
	}

	public DepositComposition(DepositCompositionData data)
	{
		IsContentsHidden = data.IsContentsHidden;
		foreach (DepositMaterialOreData ore in data.Ores)
		{
			Ores.Add(new DepositMaterialOre(ore));
		}
		foreach (DepositMaterialReagentMixData reagentMix in data.ReagentMixes)
		{
			ReagentMixes.Add(new DepositMaterialReagentMix(reagentMix));
		}
		foreach (DepositMaterialGasData frozenGasMix in data.FrozenGasMixes)
		{
			FrozenGasses.Add(new DepositMaterialGas(frozenGasMix));
		}
		foreach (DepositMaterialGasData gasMix in data.GasMixes)
		{
			Gasses.Add(new DepositMaterialGas(gasMix));
		}
	}

	public DepositComposition(RocketBinaryReader reader)
	{
		IsContentsHidden = reader.ReadBoolean();
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			Ores.Add(new DepositMaterialOre(reader));
		}
		Network.ReadIndex<byte>(reader, out var value2);
		for (int j = 0; j < value2; j++)
		{
			ReagentMixes.Add(new DepositMaterialReagentMix(reader));
		}
		Network.ReadIndex<byte>(reader, out var value3);
		for (int k = 0; k < value3; k++)
		{
			FrozenGasses.Add(new DepositMaterialGas(reader));
		}
		Network.ReadIndex<byte>(reader, out value3);
		for (int l = 0; l < value3; l++)
		{
			Gasses.Add(new DepositMaterialGas(reader));
		}
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteBoolean(IsContentsHidden);
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		foreach (DepositMaterialOre ore in Ores)
		{
			ore.Write(writer);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
		Network.WriteIndex<byte>(writer, out count, out bufferIndex);
		foreach (DepositMaterialReagentMix reagentMix in ReagentMixes)
		{
			reagentMix.Write(writer);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
		Network.WriteIndex<byte>(writer, out count, out bufferIndex);
		foreach (DepositMaterialGas frozenGass in FrozenGasses)
		{
			frozenGass.Write(writer);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
		Network.WriteIndex<byte>(writer, out count, out bufferIndex);
		foreach (DepositMaterialGas gass in Gasses)
		{
			gass.Write(writer);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public void AddToTree(TreeString parentString)
	{
		TreeString myParent = TreeString.Node("Composition", parentString);
		if (IsContentsHidden)
		{
			TreeString.Variable("Hidden", myParent);
		}
		if (Ores.Count > 0)
		{
			TreeString myParent2 = TreeString.Node("Ores", myParent);
			foreach (DepositMaterialOre ore in Ores)
			{
				TreeString.Node(ore.OrePrefab.DisplayName, myParent2);
			}
		}
		if (ReagentMixes.Count > 0)
		{
			TreeString parentString2 = TreeString.Node("ReagentMixes", myParent);
			foreach (DepositMaterialReagentMix reagentMix in ReagentMixes)
			{
				reagentMix?.AddToTree(parentString2);
			}
		}
		if (FrozenGasses.Count > 0)
		{
			TreeString parentString3 = TreeString.Node("FrozenGases", myParent);
			foreach (DepositMaterialGas frozenGass in FrozenGasses)
			{
				frozenGass?.AddToTree(parentString3);
			}
		}
		if (Gasses.Count <= 0)
		{
			return;
		}
		TreeString parentString4 = TreeString.Node("Gases", myParent);
		foreach (DepositMaterialGas gass in Gasses)
		{
			gass?.AddToTree(parentString4);
		}
	}
}
