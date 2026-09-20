using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class MultiConstructor : Stackable, IConstructionKit, IConstructionStarter, IReferencable, IEvaluable
{
	[Header("Multi-Constructable")]
	public List<Structure> Constructables = new List<Structure>();

	[NonSerialized]
	public int LastSelectedIndex;

	public override void OnPrefabLoad()
	{
		base.OnPrefabLoad();
		for (int num = Constructables.Count - 1; num >= 0; num--)
		{
			if (Constructables[num] == null)
			{
				Constructables.RemoveAt(num);
			}
		}
		if (LastSelectedIndex >= Constructables.Count)
		{
			LastSelectedIndex = Constructables.Count - 1;
		}
	}

	public virtual void Construct(Grid3 localPosition, Quaternion targetRotation, int optionIndex, Item offhandItem, bool authoringMode, ulong steamId)
	{
		int entryQuantity = Constructables[optionIndex].BuildStates[0].Tool.EntryQuantity;
		Construct(localPosition, targetRotation, optionIndex, offhandItem, authoringMode, steamId, entryQuantity);
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.KitCategory);
	}

	public virtual void Construct(Grid3 localPosition, Quaternion targetRotation, int optionIndex, Item offhandItem, bool authoringMode, ulong steamId, int quantity)
	{
		if (authoringMode || OnUseItem(quantity, null))
		{
			CreateStructureInstance createStructureInstance = new CreateStructureInstance(Constructables[optionIndex], localPosition, targetRotation, steamId);
			if (PaintableMaterial != null && CustomColor.Normal != null)
			{
				createStructureInstance.CustomColor = CustomColor.Index;
			}
			if (GameManager.RunSimulation)
			{
				Constructor.SpawnConstruct(createStructureInstance);
			}
		}
	}

	public bool CanBuild(int index)
	{
		int entryQuantity = Constructables[index].BuildStates[0].Tool.EntryQuantity;
		return base.Quantity >= entryQuantity;
	}

	public List<Thing> GetConstructedPrefabs()
	{
		List<Thing> list = new List<Thing>(Constructables.Count);
		foreach (Structure constructable in Constructables)
		{
			list.Add(constructable);
		}
		return list;
	}
}
