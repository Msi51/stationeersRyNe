using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Networks;
using UnityEngine;

namespace Objects.Rockets;

public static class RocketCloner
{
	public static RocketCloneResult Clone(Rocket source, LaunchMount target)
	{
		RocketNetwork rocketNetwork = source?.RocketNetwork;
		if (rocketNetwork == null || target == null)
		{
			return RocketCloneResult.Failed(RocketCloneStatus.NoSource);
		}
		EngineFuselage engineFuselage = null;
		foreach (INetworkedStructure structure2 in rocketNetwork.StructureList)
		{
			if (structure2 is EngineFuselage engineFuselage2)
			{
				engineFuselage = engineFuselage2;
				break;
			}
		}
		if (engineFuselage == null)
		{
			return RocketCloneResult.Failed(RocketCloneStatus.NoEngineFuselage);
		}
		Vector3 translation = target.RocketTransformPosition - engineFuselage.ThingTransform.position;
		List<Structure> list = new List<Structure>(64);
		List<Structure> list2 = new List<Structure>(256);
		HashSet<Thing> hashSet = new HashSet<Thing>();
		foreach (INetworkedStructure structure3 in rocketNetwork.StructureList)
		{
			if (structure3 is Structure item && hashSet.Add(item))
			{
				list.Add(item);
			}
		}
		foreach (IRocketInternals @internal in rocketNetwork.Internals)
		{
			if (@internal is Structure item2 && hashSet.Add(item2))
			{
				list2.Add(item2);
			}
		}
		if (list.Count == 0 && list2.Count == 0)
		{
			return RocketCloneResult.Failed(RocketCloneStatus.NothingToClone);
		}
		list.Sort(CompareByHeight);
		list2.Sort(CompareByHeight);
		int num = CountOccupiedTargets(list, list2, translation);
		if (num > 0)
		{
			return RocketCloneResult.Failed(RocketCloneStatus.TargetOccupied, num);
		}
		int num2 = 0;
		EngineFuselage engineFuselage3 = null;
		foreach (Structure item3 in list)
		{
			Structure structure = Recreate(item3, translation, copyCustomName: false);
			if (!(structure == null))
			{
				num2++;
				if (engineFuselage3 == null && structure is EngineFuselage engineFuselage4)
				{
					engineFuselage3 = engineFuselage4;
				}
			}
		}
		foreach (Structure item4 in list2)
		{
			if (Recreate(item4, translation, copyCustomName: true) != null)
			{
				num2++;
			}
		}
		RocketNetwork rocketNetwork2 = engineFuselage3?.RocketNetwork;
		if (rocketNetwork2 != null)
		{
			rocketNetwork2.RefreshRocket();
			rocketNetwork2.RebuildAllGridState();
		}
		return new RocketCloneResult(RocketCloneStatus.Success, rocketNetwork2?.Rocket, num2, 0);
	}

	private static Structure Recreate(Structure source, Vector3 translation, bool copyCustomName)
	{
		Thing thing = Prefab.Find(source.PrefabHash);
		if (thing == null)
		{
			return null;
		}
		Vector3 position = source.ThingTransform.position + translation;
		Quaternion rotation = source.ThingTransform.rotation;
		Structure structure = OnServer.Create<Structure>(thing, position, rotation);
		if (structure.BuildStates != null && structure.BuildStates.Count > 0)
		{
			structure.CurrentBuildStateIndex = structure.BuildStates.Count - 1;
		}
		if (structure.PaintableMaterial != null && source.CustomColor != null && source.CustomColor.Index >= 0)
		{
			structure.SetCustomColor(source.CustomColor.Index);
		}
		if (copyCustomName && !string.IsNullOrEmpty(source.CustomName))
		{
			structure.CustomName = source.CustomName;
		}
		return structure;
	}

	private static int CountOccupiedTargets(List<Structure> exterior, List<Structure> interior, Vector3 translation)
	{
		int num = 0;
		foreach (Structure item in exterior)
		{
			WorldGrid worldGrid = new WorldGrid(item.ThingTransform.position + translation);
			if (GridController.World.Get<Structure>(worldGrid) != null)
			{
				num++;
			}
		}
		foreach (Structure item2 in interior)
		{
			SmallCell smallCell = GridController.World.GetSmallCell(item2.ThingTransform.position + translation);
			if (smallCell != null && ((bool)smallCell.Device || (bool)smallCell.Cable || (bool)smallCell.Chute || (bool)smallCell.Pipe || (bool)smallCell.Other || smallCell.Rail != null))
			{
				num++;
			}
		}
		return num;
	}

	private static int CompareByHeight(Structure a, Structure b)
	{
		return a.ThingTransform.position.y.CompareTo(b.ThingTransform.position.y);
	}
}
