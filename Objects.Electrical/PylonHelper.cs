using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using SyncedReferencables;
using TerrainSystem;
using UnityEngine;
using Util.Splines;

namespace Objects.Electrical;

public static class PylonHelper
{
	public static List<IPylonNodeOwner> AllPylonNodeOwners = new List<IPylonNodeOwner>();

	public static bool DebugLogEnabled = false;

	public static PylonNode CurrentNode;

	private static PylonNode _currentNodeHovered;

	private static bool _hoveredNodeValid;

	private static Material _cableMaterial;

	public const float MAX_DIST = 40f;

	private const float NODE_SELECTION_SIZE = 0.15f;

	private static int CableHash = Animator.StringToHash("ItemCableCoilSuperHeavy");

	private const float PATH_STEP = 0.5f;

	private static Dictionary<long, CableRenderData> _cableRenderDataLookup = new Dictionary<long, CableRenderData>();

	public static void DebugLog(string message)
	{
		if (DebugLogEnabled)
		{
			Debug.Log("[PylonNet] " + message);
		}
	}

	public static void SetCableMaterial(Material material)
	{
		_cableMaterial = material;
	}

	private static SelectionInstance GetNodeSelection(PylonNode node)
	{
		return new SelectionInstance
		{
			IsWorldMode = true,
			Position = node.Transform.position,
			Rotation = Quaternion.identity,
			Bounds = new Bounds(node.Transform.position, Vector3.one * 0.15f),
			ParentThingRefernceId = node.Owner.GetRefId(),
			InteractableId = node.Index
		};
	}

	public static void ClearLinkPreview()
	{
		_currentNodeHovered = null;
		_hoveredNodeValid = false;
	}

	public static void ValidateLinkState()
	{
		ClearLinkPreview();
		if (CurrentNode != null)
		{
			DynamicThing dynamicThing = InventoryManager.ActiveHandSlot?.Occupant;
			if (!(dynamicThing != null) || dynamicThing.PrefabHash != CableHash || !CurrentNode.IsValid || KeyManager.GetMouseDown("Secondary"))
			{
				CurrentNode = null;
			}
		}
	}

	public static bool TryGetLinkPreview(out Vector3 start, out Vector3 end, out bool valid)
	{
		start = default(Vector3);
		end = default(Vector3);
		valid = false;
		if (CurrentNode == null || !CurrentNode.IsValid)
		{
			return false;
		}
		start = CurrentNode.Transform.position;
		if (_currentNodeHovered != null && _currentNodeHovered.IsValid)
		{
			end = _currentNodeHovered.Transform.position;
			valid = _hoveredNodeValid;
			return true;
		}
		DynamicThing dynamicThing = InventoryManager.ActiveHandSlot?.Occupant;
		if (dynamicThing == null || dynamicThing.PrefabHash != CableHash)
		{
			return false;
		}
		end = dynamicThing.ThingTransformPosition;
		valid = (start - end).sqrMagnitude < 1600f;
		return true;
	}

	public static void AttackWithCompleteLocal(IPylonNodeOwner owner, Attack attack)
	{
		if (!owner.AsStructure().IsStructureCompleted || !attack.SourceItem || attack.SourceItem.PrefabHash != CableHash)
		{
			return;
		}
		PylonNode closestNode = GetClosestNode(owner, attack.Position);
		Stackable stackable = attack.SourceItem as Stackable;
		if (CurrentNode == null || !CurrentNode.IsValid)
		{
			CurrentNode = closestNode;
			return;
		}
		if (GetLinkCableCost(CurrentNode, closestNode, out var cost) && HasClearPath(CurrentNode, closestNode) && TryConnect(CurrentNode, closestNode))
		{
			stackable?.OnUseItem(cost, owner as Thing);
		}
		CurrentNode = null;
	}

	private static bool GetLinkCableCost(PylonNode a, PylonNode b, out int cost)
	{
		float magnitude = (a.Transform.position - b.Transform.position).magnitude;
		cost = (int)RocketMath.MapToScaleClamp(0f, 40f, 5f, 30f, magnitude);
		return magnitude <= 40f;
	}

	public static Thing.DelayedActionInstance AttackWith(IPylonNodeOwner owner, Attack attack, bool doAction)
	{
		if ((bool)attack.SourceItem && attack.SourceItem.PrefabHash == CableHash)
		{
			Thing.DelayedActionInstance actionInstance = new Thing.DelayedActionInstance
			{
				Duration = 1f,
				ActionMessage = GameStrings.PylonLink
			};
			PylonNode closestNode = GetClosestNode(owner, attack.Position);
			actionInstance.Selection = GetNodeSelection(closestNode);
			_currentNodeHovered = closestNode;
			Stackable stackable = attack.SourceItem as Stackable;
			if (!closestNode.CanConnect)
			{
				return actionInstance.Fail(GameStrings.ConnectionLimitReached);
			}
			if (CurrentNode != null && !CurrentNode.IsValid)
			{
				CurrentNode = null;
			}
			if (CurrentNode == null)
			{
				actionInstance.ActionMessage = GameStrings.PylonStartLink;
			}
			else
			{
				if (!GetLinkCableCost(CurrentNode, closestNode, out var cost))
				{
					_hoveredNodeValid = false;
					return actionInstance.Fail(GameStrings.PylonStartNodeTooFar);
				}
				if (stackable != null && !stackable.CanUseStack(cost, ref actionInstance))
				{
					_hoveredNodeValid = false;
					return actionInstance.Fail(GameStrings.PylonNeedCablesToLink, StringManager.Get(cost));
				}
				if (!CanConnect(CurrentNode, closestNode))
				{
					_hoveredNodeValid = false;
					return actionInstance.Fail(IsTerminusToTerminus(CurrentNode, closestNode) ? GameStrings.PylonTerminusNeedsPylon : GameStrings.PylonCantConnect);
				}
				if (!HasClearPath(CurrentNode, closestNode))
				{
					_hoveredNodeValid = false;
					return actionInstance.Fail(GameStrings.PylonPathObstructed);
				}
				_hoveredNodeValid = true;
				actionInstance.ActionMessage = GameStrings.PylonCompleteLink.AsString(StringManager.Get(cost));
			}
			return actionInstance;
		}
		if (attack.SourceItem is WireCutter)
		{
			Thing.DelayedActionInstance delayedActionInstance = new Thing.DelayedActionInstance
			{
				Duration = 1f,
				ActionMessage = GameStrings.PylonUnlink
			};
			PylonNode closestNode2 = GetClosestNode(owner, attack.Position);
			delayedActionInstance.Selection = GetNodeSelection(closestNode2);
			if (closestNode2.Connections.Count == 0)
			{
				return delayedActionInstance.Fail(GameStrings.PylonNoConnectionsToUnlink);
			}
			if (doAction)
			{
				ServerDisconnectNode(closestNode2, GetPlayerPosition(attack) ?? closestNode2.Transform.position, attack.OtherHand);
			}
			return delayedActionInstance;
		}
		return null;
	}

	public static PylonNode GetClosestNode(IPylonNodeOwner owner, Vector3 position)
	{
		PylonNode result = null;
		float num = float.PositiveInfinity;
		foreach (PylonNode node in owner.Nodes)
		{
			float sqrMagnitude = (node.Transform.position - position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = node;
			}
		}
		return result;
	}

	public static void ServerDisconnectNode(PylonNode node, Vector3? refundPosition = null, Slot refundSlot = null)
	{
		for (int num = node.Connections.Count - 1; num >= 0; num--)
		{
			PylonConnection pylonConnection = node.Connections[num];
			if (refundPosition.HasValue)
			{
				ServerRefundCable(node, pylonConnection, refundPosition.Value, refundSlot);
			}
			SyncedReferencable.Destroy(pylonConnection);
		}
	}

	public static void ServerRefundCable(PylonNode node, PylonConnection connection, Vector3 position, Slot handSlot)
	{
		if (GameManager.RunSimulation)
		{
			GetLinkCableCost(node, connection.GetOther(node), out var cost);
			if (cost > 0)
			{
				OnServer.CreateOrStack(Prefab.Find<Item>(CableHash), cost, position, Quaternion.identity, handSlot);
			}
		}
	}

	public static Vector3? GetPlayerPosition(Attack attack)
	{
		if (attack.ActiveHand == null || !attack.ActiveHand.Parent)
		{
			return null;
		}
		Human rootParentHuman = attack.ActiveHand.Parent.RootParentHuman;
		if (!rootParentHuman)
		{
			return null;
		}
		return rootParentHuman.ThingTransformPosition;
	}

	public static void ServerConnectNodes(PylonNode from, PylonNode to)
	{
		if (CanConnect(from, to))
		{
			PylonConnection.CreateNew(from, to);
		}
	}

	public static bool CanConnect(PylonNode a, PylonNode b)
	{
		if (a.CanConnect && b.CanConnect && !a.IsConnectedTo(b) && a.Owner != b.Owner && a != b)
		{
			return !IsTerminusToTerminus(a, b);
		}
		return false;
	}

	public static bool IsTerminusToTerminus(PylonNode a, PylonNode b)
	{
		if (!(a.Owner is PowerPylon))
		{
			return !(b.Owner is PowerPylon);
		}
		return false;
	}

	public static bool HasClearPath(PylonNode a, PylonNode b)
	{
		GridController world = GridController.World;
		if (world == null)
		{
			return true;
		}
		Vector3 position = a.Transform.position;
		Vector3 position2 = b.Transform.position;
		Spline spline = CableRenderData.BuildSpline(position, position2);
		int num = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(position, position2) / 0.5f));
		Grid3 value = new WorldGrid(position).Value;
		Grid3 value2 = new WorldGrid(position2).Value;
		Grid3 grid = value;
		for (int i = 1; i <= num; i++)
		{
			Vector3 position3 = spline.GetPosition((float)i / (float)num);
			Grid3 value3 = new WorldGrid(position3).Value;
			if (IsTerrainSolid(position3) && !value3.Equals(value) && !value3.Equals(value2))
			{
				return false;
			}
			if (!value3.Equals(grid))
			{
				if (IsCellObstructed(world, value3, value, value2))
				{
					return false;
				}
				if (IsFaceObstructed(world, grid, value3, value, value2))
				{
					return false;
				}
				grid = value3;
			}
		}
		return true;
	}

	private static bool IsTerrainSolid(Vector3 position)
	{
		return VoxelTerrain.GetDensityAtSize(position, 1) >= 0.49803922f;
	}

	private static bool IsCellObstructed(GridController controller, Grid3 cellGrid, Grid3 startCell, Grid3 endCell)
	{
		if (cellGrid.Equals(startCell) || cellGrid.Equals(endCell))
		{
			return false;
		}
		Cell cell = controller.GetCell(cellGrid);
		if (cell != null)
		{
			return cell.Lookup[StructureElement.Center] != null;
		}
		return false;
	}

	private static bool IsFaceObstructed(GridController controller, Grid3 fromGrid, Grid3 toGrid, Grid3 startCell, Grid3 endCell)
	{
		Grid3 grid = toGrid - fromGrid;
		Grid3 grid2 = new Grid3(grid.x / 2, grid.y / 2, grid.z / 2);
		StructureElement structureElement = StructuralArray.Get(grid2);
		if (structureElement == StructureElement.Invalid || structureElement == StructureElement.Center)
		{
			return false;
		}
		if (!fromGrid.Equals(startCell) && !fromGrid.Equals(endCell))
		{
			Cell cell = controller.GetCell(fromGrid);
			if (cell != null && cell.Lookup[grid2] != null)
			{
				return true;
			}
		}
		if (toGrid.Equals(startCell) || toGrid.Equals(endCell))
		{
			return false;
		}
		Cell cell2 = controller.GetCell(toGrid);
		if (cell2 != null)
		{
			return cell2.Lookup[new Grid3(-grid2.x, -grid2.y, -grid2.z)] != null;
		}
		return false;
	}

	public static bool TryConnect(PylonNode a, PylonNode b)
	{
		if (CanConnect(a, b))
		{
			if (GameManager.RunSimulation)
			{
				ServerConnectNodes(a, b);
			}
			else
			{
				NetworkClient.SendToServer(new LinkPylonsMessage
				{
					FromIndex = a.Index,
					FromRefId = a.Owner.GetRefId(),
					ToIndex = b.Index,
					ToRefId = b.Owner.GetRefId()
				});
			}
			return true;
		}
		return false;
	}

	public static PylonNode FindNode(long ownerRefId, byte nodeIndex)
	{
		if (!(Thing.Find<Thing>(ownerRefId) is IPylonNodeOwner pylonNodeOwner))
		{
			return null;
		}
		if (nodeIndex >= pylonNodeOwner.Nodes.Count)
		{
			return null;
		}
		return pylonNodeOwner.Nodes[nodeIndex];
	}

	private static void MarkInputsAsDirty()
	{
		foreach (IPylonNodeOwner allPylonNodeOwner in AllPylonNodeOwners)
		{
			if (allPylonNodeOwner is PowerPylonInput powerPylonInput)
			{
				powerPylonInput.SetDirty();
			}
		}
	}

	public static void RenderCables()
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		foreach (KeyValuePair<long, CableRenderData> item in _cableRenderDataLookup)
		{
			CableRenderData value = item.Value;
			Graphics.DrawMesh(value.CableMesh, value.Matrix, _cableMaterial, 0);
		}
	}

	public static void DisconnectAll(PylonNode node, Vector3? refundPosition = null)
	{
		for (int num = node.Connections.Count - 1; num >= 0; num--)
		{
			PylonConnection pylonConnection = node.Connections[num];
			if (GameManager.RunSimulation)
			{
				if (refundPosition.HasValue)
				{
					ServerRefundCable(node, pylonConnection, refundPosition.Value, null);
				}
				SyncedReferencable.Destroy(pylonConnection);
			}
			else
			{
				pylonConnection.DisconnectLocal();
			}
		}
	}

	public static void DisconnectNodes(PylonConnection connection, PylonNode from, PylonNode to)
	{
		if (GameManager.RunSimulation)
		{
			MarkInputsAsDirty();
		}
		from?.Connections.Remove(connection);
		to?.Connections.Remove(connection);
		if (!GameManager.IsBatchMode && _cableRenderDataLookup.TryGetValue(connection.ReferenceId, out var value))
		{
			value.Clear();
			_cableRenderDataLookup.Remove(connection.ReferenceId);
		}
		if (GameManager.RunSimulation)
		{
			RebuildCableTerminusNetworks(from, to);
		}
	}

	public static void ConnectNodes(PylonConnection connection, PylonNode from, PylonNode to)
	{
		if (GameManager.RunSimulation)
		{
			MarkInputsAsDirty();
		}
		from.Connections.Add(connection);
		to.Connections.Add(connection);
		if (!GameManager.IsBatchMode)
		{
			_cableRenderDataLookup.TryAdd(connection.ReferenceId, new CableRenderData(from, to));
		}
		if (GameManager.RunSimulation)
		{
			MergeCableTerminusNetworks(from, to);
		}
	}

	public static IEnumerable<PowerPylonCableTerminus> ReachableCableTerminuses(IPylonNodeOwner source)
	{
		DebugLog($"ReachableCableTerminuses: source #{source.GetRefId()} ({source.GetType().Name}), {source.Nodes.Count} nodes");
		HashSet<PylonNode> seenNodes = new HashSet<PylonNode>();
		Queue<PylonNode> open = new Queue<PylonNode>();
		foreach (PylonNode node in source.Nodes)
		{
			open.Enqueue(node);
			seenNodes.Add(node);
		}
		while (open.Count > 0)
		{
			PylonNode current2 = open.Dequeue();
			DebugLog($"  visiting node on owner #{current2.Owner.GetRefId()} ({current2.Owner.GetType().Name}), {current2.Connections.Count} connections");
			foreach (PylonConnection connection in current2.Connections)
			{
				PylonNode other = connection.GetOther(current2);
				if (!seenNodes.Add(other))
				{
					DebugLog($"    -> node on #{other.Owner.GetRefId()} already seen");
				}
				else if (other.Owner is PowerPylonCableTerminus powerPylonCableTerminus)
				{
					DebugLog($"    -> yield terminus #{powerPylonCableTerminus.ReferenceId}");
					yield return powerPylonCableTerminus;
				}
				else
				{
					DebugLog($"    -> continue through {other.Owner.GetType().Name} #{other.Owner.GetRefId()} on same node");
					open.Enqueue(other);
				}
			}
		}
	}

	public static void AppendPylonCableNeighbors(IPylonNodeOwner source, Span<SmallCellRef> buf, ref int count)
	{
		DebugLog($"AppendPylonCableNeighbors: source #{source.GetRefId()} ({source.GetType().Name})");
		HashSet<PylonNode> hashSet = new HashSet<PylonNode>();
		Queue<PylonNode> queue = new Queue<PylonNode>();
		foreach (PylonNode node in source.Nodes)
		{
			queue.Enqueue(node);
			hashSet.Add(node);
		}
		while (queue.Count > 0)
		{
			if (count >= buf.Length)
			{
				DebugLog($"  buffer full at {count}, stopping");
				return;
			}
			PylonNode pylonNode = queue.Dequeue();
			foreach (PylonConnection connection in pylonNode.Connections)
			{
				PylonNode other = connection.GetOther(pylonNode);
				if (!hashSet.Add(other))
				{
					continue;
				}
				if (other.Owner is PowerPylonCableTerminus powerPylonCableTerminus)
				{
					if (powerPylonCableTerminus.SmallCell != null && count < buf.Length)
					{
						buf[count++] = new SmallCellRef(powerPylonCableTerminus.SmallCell, SmallCellType.Cable);
						DebugLog($"  yield new terminus #{powerPylonCableTerminus.ReferenceId}");
					}
				}
				else
				{
					queue.Enqueue(other);
				}
			}
		}
		DebugLog($"AppendPylonCableNeighbors done: {count} neighbors added");
	}

	private static void MergeCableTerminusNetworks(PylonNode a, PylonNode b)
	{
		CableNetwork cableNetwork = OwningCableNetwork(a);
		CableNetwork cableNetwork2 = OwningCableNetwork(b);
		DebugLog("MergeCableTerminusNetworks: netA=#" + (cableNetwork?.ReferenceId.ToString() ?? "null") + ", netB=#" + (cableNetwork2?.ReferenceId.ToString() ?? "null"));
		if (cableNetwork == null || cableNetwork2 == null || cableNetwork == cableNetwork2)
		{
			DebugLog("  no merge needed");
			return;
		}
		DebugLog($"  merging #{cableNetwork2.ReferenceId} into #{cableNetwork.ReferenceId}");
		cableNetwork.Merge(cableNetwork2);
	}

	private static void RebuildCableTerminusNetworks(PylonNode a, PylonNode b)
	{
		PowerPylonCableTerminus powerPylonCableTerminus = CableTerminusOn(a);
		PowerPylonCableTerminus powerPylonCableTerminus2 = CableTerminusOn(b);
		DebugLog("RebuildCableTerminusNetworks: terminusA=#" + (powerPylonCableTerminus?.ReferenceId.ToString() ?? "null") + " (net #" + (powerPylonCableTerminus?.CableNetwork?.ReferenceId.ToString() ?? "null") + "), terminusB=#" + (powerPylonCableTerminus2?.ReferenceId.ToString() ?? "null") + " (net #" + (powerPylonCableTerminus2?.CableNetwork?.ReferenceId.ToString() ?? "null") + ")");
		if ((object)powerPylonCableTerminus != null && powerPylonCableTerminus.CableNetwork != null)
		{
			DebugLog($"  rebuilding from terminusA #{powerPylonCableTerminus.ReferenceId}");
			CableNetwork.RebuildCableNetworkServer(powerPylonCableTerminus);
		}
		if ((object)powerPylonCableTerminus2 != null && powerPylonCableTerminus2.CableNetwork != null && powerPylonCableTerminus2.CableNetwork != powerPylonCableTerminus?.CableNetwork)
		{
			DebugLog($"  rebuilding from terminusB #{powerPylonCableTerminus2.ReferenceId}");
			CableNetwork.RebuildCableNetworkServer(powerPylonCableTerminus2);
		}
	}

	private static CableNetwork OwningCableNetwork(PylonNode node)
	{
		if (node?.Owner is PowerPylonCableTerminus powerPylonCableTerminus)
		{
			return powerPylonCableTerminus.CableNetwork;
		}
		if (node == null)
		{
			return null;
		}
		using (IEnumerator<PowerPylonCableTerminus> enumerator = ReachableCableTerminusesFromNode(node).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current.CableNetwork;
			}
		}
		return null;
	}

	private static PowerPylonCableTerminus CableTerminusOn(PylonNode node)
	{
		if (node?.Owner is PowerPylonCableTerminus result)
		{
			return result;
		}
		if (node == null)
		{
			return null;
		}
		using (IEnumerator<PowerPylonCableTerminus> enumerator = ReachableCableTerminusesFromNode(node).GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		return null;
	}

	private static IEnumerable<PowerPylonCableTerminus> ReachableCableTerminusesFromNode(PylonNode startNode)
	{
		DebugLog($"ReachableCableTerminusesFromNode: node on #{startNode.Owner.GetRefId()} ({startNode.Owner.GetType().Name})");
		HashSet<PylonNode> seenNodes = new HashSet<PylonNode> { startNode };
		Queue<PylonNode> open = new Queue<PylonNode>();
		open.Enqueue(startNode);
		while (open.Count > 0)
		{
			PylonNode current = open.Dequeue();
			foreach (PylonConnection connection in current.Connections)
			{
				PylonNode other = connection.GetOther(current);
				if (seenNodes.Add(other))
				{
					if (other.Owner is PowerPylonCableTerminus powerPylonCableTerminus)
					{
						yield return powerPylonCableTerminus;
					}
					else
					{
						open.Enqueue(other);
					}
				}
			}
		}
	}

	public static void Clear()
	{
		foreach (KeyValuePair<long, CableRenderData> item in _cableRenderDataLookup)
		{
			item.Value.Clear();
		}
		_cableRenderDataLookup.Clear();
		CurrentNode = null;
		AllPylonNodeOwners.Clear();
		ClearLinkPreview();
	}
}
