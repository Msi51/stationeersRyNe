using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Networks;
using UnityEngine;

namespace Objects.Rockets;

public static class RocketRelinker
{
	private readonly struct OwnedCell(Vector3 position, RocketInternalCellType cellType)
	{
		public readonly Vector3 Position = position;

		public readonly RocketInternalCellType CellType = cellType;
	}

	private readonly struct AnchorPart(IRocketInternals internals, Vector3 position)
	{
		public readonly IRocketInternals Internals = internals;

		public readonly Vector3 Position = position;
	}

	private const float CELL_MATCH_TOLERANCE = 0.25f;

	public static RocketRelinkPlan Compute(RocketNetwork network)
	{
		List<OwnedCell> list = new List<OwnedCell>(256);
		foreach (INetworkedStructure structure in network.StructureList)
		{
			if (!(structure is INetworkedRocketPart networkedRocketPart))
			{
				continue;
			}
			Vector3 position = networkedRocketPart.GetAsThing.Transform.position;
			foreach (RocketInternalCellOffset internalCellOffset in networkedRocketPart.InternalCellOffsets)
			{
				list.Add(new OwnedCell(position + internalCellOffset.Offset * 0.5f, internalCellOffset.CellType));
			}
		}
		if (list.Count == 0)
		{
			return RocketRelinkPlan.Failed(RocketRelinkStatus.NoHull);
		}
		Dictionary<long, RocketInternalCellType> dictionary = new Dictionary<long, RocketInternalCellType>(list.Count);
		foreach (OwnedCell item in list)
		{
			long key = CellKey(item.Position);
			dictionary[key] = (dictionary.TryGetValue(key, out var value) ? (value | item.CellType) : item.CellType);
		}
		List<long> list2 = new List<long>(dictionary.Keys);
		List<AnchorPart> list3 = new List<AnchorPart>(32);
		DensePool<Thing>.ActiveEnumerable.Enumerator enumerator4 = OcclusionManager.AllThings.Active().GetEnumerator();
		while (enumerator4.MoveNext())
		{
			Thing current3 = enumerator4.Current;
			if (current3 is IRocketInternals { RocketNetwork: null, StrictlyInternal: not false } rocketInternals)
			{
				list3.Add(new AnchorPart(rocketInternals, current3.Transform.position));
			}
		}
		if (list3.Count == 0)
		{
			return RocketRelinkPlan.Failed(RocketRelinkStatus.NoOrphans);
		}
		Dictionary<long, int> dictionary2 = new Dictionary<long, int>();
		Dictionary<long, Vector3> dictionary3 = new Dictionary<long, Vector3>();
		int num = 0;
		foreach (AnchorPart item2 in list3)
		{
			foreach (long item3 in list2)
			{
				Vector3 vector = KeyToPosition(item3) - item2.Position;
				long key2 = CellKey(vector);
				if (!dictionary2.TryGetValue(key2, out var value2))
				{
					value2 = (dictionary2[key2] = CountAnchorMatches(list3, dictionary, vector));
					dictionary3[key2] = vector;
				}
				if (value2 > num)
				{
					num = value2;
				}
			}
		}
		if (num == 0)
		{
			return RocketRelinkPlan.Failed(RocketRelinkStatus.NoAlignment, list3.Count);
		}
		Vector3 vector2 = Vector3.zero;
		int num3 = -1;
		foreach (KeyValuePair<long, int> item4 in dictionary2)
		{
			if (item4.Value == num)
			{
				Vector3 vector3 = dictionary3[item4.Key];
				int num4 = ClusterCoverage(list, vector3);
				if (num4 > num3)
				{
					num3 = num4;
					vector2 = vector3;
				}
			}
		}
		if (vector2.sqrMagnitude < 0.0625f)
		{
			return RocketRelinkPlan.Failed(RocketRelinkStatus.AlreadyInPlace, list3.Count);
		}
		List<Structure> list4 = new List<Structure>(256);
		HashSet<Thing> seen = new HashSet<Thing>();
		int num5 = 0;
		foreach (OwnedCell item5 in list)
		{
			SmallCell smallCell = GridController.World.GetSmallCell(item5.Position - vector2);
			if (smallCell != null && ((bool)smallCell.Device || (bool)smallCell.Cable || (bool)smallCell.Chute || (bool)smallCell.Pipe))
			{
				SmallCell smallCell2 = GridController.World.GetSmallCell(item5.Position);
				if (smallCell2 != null && (IsForeignOccupant(smallCell2.Device, network) || IsForeignOccupant(smallCell2.Cable, network) || IsForeignOccupant(smallCell2.Chute, network) || IsForeignOccupant(smallCell2.Pipe, network)))
				{
					num5++;
				}
				AddOccupant(smallCell.Device, network, list4, seen);
				AddOccupant(smallCell.Cable, network, list4, seen);
				AddOccupant(smallCell.Chute, network, list4, seen);
				AddOccupant(smallCell.Pipe, network, list4, seen);
			}
		}
		return new RocketRelinkPlan((list4.Count == 0) ? RocketRelinkStatus.NothingToMove : ((num5 > 0) ? RocketRelinkStatus.TargetsOccupied : RocketRelinkStatus.Ready), vector2, num, list3.Count, num3, num5, list4);
	}

	public static void Apply(RocketNetwork network, RocketRelinkPlan plan)
	{
		if (!plan.IsReady)
		{
			return;
		}
		foreach (Structure item in plan.MoveSet)
		{
			item.DetatchFromGrid();
		}
		foreach (Structure item2 in plan.MoveSet)
		{
			item2.ThingTransform.position += plan.Translation;
			item2.RebuildGridState();
		}
		Transform transform = network.Rocket?.RocketParentTransform;
		foreach (Structure item3 in plan.MoveSet)
		{
			item3.AttachToGrid();
			if (transform != null)
			{
				item3.ThingTransform.SetParent(transform);
			}
			if (!(item3 is IRocketInternals rocketInternals))
			{
				continue;
			}
			foreach (Connection accessOpenEnd in rocketInternals.AccessOpenEnds)
			{
				accessOpenEnd?.Initialize();
			}
		}
		network.RefreshRocket();
		network.RebuildAllGridState();
	}

	private static int CountAnchorMatches(List<AnchorPart> anchors, Dictionary<long, RocketInternalCellType> ownedByKey, Vector3 translation)
	{
		int num = 0;
		foreach (AnchorPart anchor in anchors)
		{
			long key = CellKey(anchor.Position + translation);
			if (ownedByKey.TryGetValue(key, out var value) && (value & anchor.Internals.InternalCellType) == anchor.Internals.InternalCellType)
			{
				num++;
			}
		}
		return num;
	}

	private static int ClusterCoverage(List<OwnedCell> ownedCells, Vector3 translation)
	{
		int num = 0;
		foreach (OwnedCell ownedCell in ownedCells)
		{
			SmallCell smallCell = GridController.World.GetSmallCell(ownedCell.Position - translation);
			if (smallCell != null && (CellPermits(ownedCell.CellType, smallCell.Device) || CellPermits(ownedCell.CellType, smallCell.Cable) || CellPermits(ownedCell.CellType, smallCell.Chute) || CellPermits(ownedCell.CellType, smallCell.Pipe)))
			{
				num++;
			}
		}
		return num;
	}

	private static bool CellPermits(RocketInternalCellType cellType, Thing occupant)
	{
		if (occupant is IRocketInternals rocketInternals)
		{
			return (cellType & rocketInternals.InternalCellType) == rocketInternals.InternalCellType;
		}
		return false;
	}

	private static void AddOccupant(Thing occupant, RocketNetwork network, List<Structure> moveSet, HashSet<Thing> seen)
	{
		if ((object)occupant != null && seen.Add(occupant) && (!(occupant is IRocketInternals { RocketNetwork: { } rocketNetwork }) || rocketNetwork == network) && occupant is Structure item)
		{
			moveSet.Add(item);
		}
	}

	private static bool IsForeignOccupant(Thing occupant, RocketNetwork network)
	{
		if ((object)occupant == null)
		{
			return false;
		}
		if (occupant is IRocketInternals { RocketNetwork: { } rocketNetwork } && rocketNetwork == network)
		{
			return false;
		}
		return true;
	}

	private static long CellKey(Vector3 position)
	{
		long num = ((long)Mathf.RoundToInt(position.x * 2f) + 1048576L) & 0x1FFFFF;
		long num2 = ((long)Mathf.RoundToInt(position.y * 2f) + 1048576L) & 0x1FFFFF;
		long num3 = ((long)Mathf.RoundToInt(position.z * 2f) + 1048576L) & 0x1FFFFF;
		return (num << 42) | (num2 << 21) | num3;
	}

	private static Vector3 KeyToPosition(long key)
	{
		long num = (key & 0x1FFFFF) - 1048576;
		long num2 = ((key >> 21) & 0x1FFFFF) - 1048576;
		return new Vector3((float)(((key >> 42) & 0x1FFFFF) - 1048576) * 0.5f, (float)num2 * 0.5f, (float)num * 0.5f);
	}
}
