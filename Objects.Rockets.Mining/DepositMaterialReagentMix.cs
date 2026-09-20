using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Reagents;

namespace Objects.Rockets.Mining;

public class DepositMaterialReagentMix
{
	public float Weight;

	public ReagentMixture Mixture;

	public Thing MixPrefab;

	public const string MIX_PREFAB_NAME = "ItemSpaceOre";

	public DepositMaterialReagentMix(RocketBinaryReader reader)
	{
		float weight = reader.ReadSingle();
		int prefabHash = reader.ReadInt32();
		Mixture = new ReagentMixture(reader);
		Weight = weight;
		MixPrefab = Prefab.Find(prefabHash);
	}

	public DepositMaterialReagentMix(DepositMaterialReagentMixData data)
	{
		Weight = data.Weight;
		MixPrefab = Prefab.Find("ItemSpaceOre");
		Mixture = new ReagentMixture(data.CreateRecipe());
	}

	public void Write(RocketBinaryWriter writer)
	{
		if (Mixture == null || (object)MixPrefab == null)
		{
			throw new NullReferenceException("Mixture or MixPrefab is null");
		}
		writer.WriteSingle(Weight);
		writer.WriteInt32(MixPrefab.PrefabHash);
		Mixture.Write(writer);
	}

	public void AddToTree(TreeString parentString)
	{
		TreeString myParent = TreeString.Node(MixPrefab.DisplayName, parentString);
		List<ReagentMixIngredientSaveData> list = Mixture.ToIngredientList();
		for (int i = 0; i < list.Count; i++)
		{
			ReagentMixIngredientSaveData reagentMixIngredientSaveData = list[i];
			TreeString.Node($"{reagentMixIngredientSaveData.ReagentName} x {reagentMixIngredientSaveData.Quantity}", myParent);
		}
	}
}
