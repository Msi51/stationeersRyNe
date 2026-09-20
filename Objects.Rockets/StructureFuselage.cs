using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Networks;
using Trading;
using UI.ImGuiUi;
using Unity.Mathematics;
using UnityEngine;

namespace Objects.Rockets;

public class StructureFuselage : LargeStructure, INetworkedRocketPart, INetworkedStructure, INetworkMember, IReferencable, IEvaluable, IRocketComponent, IRocketMassContributor
{
	[Header("Grid offsets")]
	[SerializeField]
	private List<RocketInternalCellTypeRange> _cellTypeRanges;

	[SerializeField]
	private List<RocketInternalCellOffset> _gridOffsets;

	private const int OCCUPIED_GRID_MAX_COUNT = 125;

	[SerializeField]
	private int2 offSetX = new int2(2, -2);

	[SerializeField]
	private int2 offSetY = new int2(2, -2);

	[SerializeField]
	private int2 offSetZ = new int2(2, -2);

	public virtual float MassContribution => 250f;

	public override string CustomName
	{
		get
		{
			return RocketNetwork?.Rocket?.CustomName ?? string.Empty;
		}
		set
		{
			if (RocketNetwork?.Rocket != null)
			{
				RocketNetwork.Rocket.CustomName = value;
			}
		}
	}

	public List<RocketInternalCellOffset> InternalCellOffsets => _gridOffsets;

	public StructureNetwork StructureNetwork { get; set; }

	ReferencableNetwork INetworkMember.Network
	{
		get
		{
			return StructureNetwork;
		}
		set
		{
			StructureNetwork = (StructureNetwork)value;
		}
	}

	public RocketNetwork RocketNetwork => StructureNetwork as RocketNetwork;

	public override void OnRegistered(Cell cell)
	{
		if (GameManager.GameState != GameState.Loading && GameManager.RunSimulation)
		{
			if (StructureNetwork.Merge(StructureNetwork.ConnectedNetworks(this), out var mergedNetwork))
			{
				mergedNetwork.Add(this);
			}
			else
			{
				new RocketNetwork(0L).Add(this);
			}
		}
		base.OnRegistered(cell);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return null;
		}
		Labeller labeller = sourceItem as Labeller;
		if ((bool)labeller)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = ActionStrings.Rename
			};
			if (!labeller.OnOff)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
			}
			if (!labeller.IsOperable)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			labeller.Rename(this);
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting || IsCursor || GameManager.GameState == GameState.None)
		{
			return;
		}
		List<INetworkedStructure> list = new List<INetworkedStructure>(ConnectedStructures());
		RocketNetwork?.Remove(this);
		if (GameManager.RunSimulation)
		{
			foreach (INetworkedStructure item in list)
			{
				if (item is INetworkedRocketPart networkedRocketPart)
				{
					networkedRocketPart.RocketNetwork.RebuildNetworkServer(networkedRocketPart);
				}
			}
		}
		base.OnDestroy();
	}

	private List<INetworkedStructure> ConnectedRocketParts()
	{
		List<INetworkedStructure> list = new List<INetworkedStructure>(2);
		WorldGrid worldGrid = new WorldGrid(RegisteredLocalGrid + Grid3.Up);
		WorldGrid worldGrid2 = new WorldGrid(RegisteredLocalGrid - Grid3.Up);
		Structure structure = base.GridController.Get<Structure>(worldGrid);
		Structure structure2 = base.GridController.Get<Structure>(worldGrid2);
		if ((bool)structure2 && structure2 != this && structure2 is INetworkedRocketPart item)
		{
			list.Add(item);
		}
		if ((bool)structure && structure != this && structure is INetworkedRocketPart item2)
		{
			list.Add(item2);
		}
		return list;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new StructureFuselageSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is StructureFuselageSaveData structureFuselageSaveData)
		{
			structureFuselageSaveData.RocketNetworkId = RocketNetwork?.ReferenceId ?? 0;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is StructureFuselageSaveData { RocketNetworkId: var rocketNetworkId })
		{
			(Referencable.Find<RocketNetwork>(rocketNetworkId) ?? new RocketNetwork(rocketNetworkId)).Add(this);
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(RocketNetwork?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		RocketNetwork rocketNetwork = Referencable.Find<RocketNetwork>(reader.ReadInt64());
		if (GameManager.GameState != GameState.Joining && StructureNetwork.Merge(StructureNetwork.ConnectedNetworks(this), out var mergedNetwork))
		{
			rocketNetwork = mergedNetwork as RocketNetwork;
		}
		rocketNetwork?.Add(this);
	}

	public virtual bool CanMerge()
	{
		return false;
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		RocketNetwork?.Rocket?.MarkIconDirty();
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		RocketNetwork?.Rocket?.MarkIconDirty();
	}

	public override CanConstructInfo CanConstruct()
	{
		CanConstructInfo result = base.CanConstruct();
		if (!result.CanConstruct)
		{
			return result;
		}
		foreach (RocketInternalCellOffset internalCellOffset in InternalCellOffsets)
		{
			Vector3 worldPosition = Transform.position + internalCellOffset.Offset * 0.5f;
			Grid3 localGrid = new Grid3(worldPosition);
			SmallCell smallCell = GridController.World.GetSmallCell(localGrid);
			if (smallCell == null || !smallCell.IsValid())
			{
				continue;
			}
			if ((bool)smallCell.Device)
			{
				_ = smallCell.Device;
				if (!(smallCell.Device is IRocketInternals rocketInternals))
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.GridBlockedByStructure.AsString(smallCell.Device.DisplayName));
				}
				if ((rocketInternals.InternalCellType & internalCellOffset.CellType) == 0)
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.GridBlockedByStructure.AsString(smallCell.Device.DisplayName));
				}
			}
			if ((bool)smallCell.Chute)
			{
				_ = smallCell.Chute;
				if ((smallCell.Chute.InternalCellType & internalCellOffset.CellType) == 0)
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.GridBlockedByStructure.AsString(smallCell.Chute.DisplayName));
				}
			}
			if ((bool)smallCell.Pipe)
			{
				_ = smallCell.Pipe;
				if ((smallCell.Pipe.InternalCellType & internalCellOffset.CellType) == 0)
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.GridBlockedByStructure.AsString(smallCell.Pipe.DisplayName));
				}
			}
			if ((bool)smallCell.Cable)
			{
				_ = smallCell.Cable;
				if ((smallCell.Cable.InternalCellType & internalCellOffset.CellType) == 0)
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.GridBlockedByStructure.AsString(smallCell.Cable.DisplayName));
				}
			}
			if (smallCell.Rail != null)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.GridBlockedByStructure.AsString(smallCell.Rail.DisplayName));
			}
			if ((bool)smallCell.Other)
			{
				_ = smallCell.Other;
				if (!(smallCell.Other is IRocketInternals rocketInternals2))
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.GridBlockedByStructure.AsString(smallCell.Other.DisplayName));
				}
				if ((rocketInternals2.InternalCellType & internalCellOffset.CellType) == 0)
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.GridBlockedByStructure.AsString(smallCell.Device.DisplayName));
				}
			}
		}
		return result;
	}

	protected override CanConstructInfo CanDeconstruct()
	{
		if (base.CurrentBuildStateIndex <= 0)
		{
			Grid3 grid = new Grid3(base.ThingTransformPosition);
			WorldGrid worldGrid = new WorldGrid(grid + Grid3.Up);
			Structure structure = base.GridController.Get<Structure>(worldGrid);
			if (structure is StructureFuselage && structure != this)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.InvalidFuselageDeconstruct.AsString(structure.DisplayName));
			}
			foreach (RocketInternalCellOffset internalCellOffset in InternalCellOffsets)
			{
				Vector3 worldPosition = Transform.position + internalCellOffset.Offset * 0.5f;
				Grid3 localGrid = new Grid3(worldPosition);
				SmallCell smallCell = GridController.World.GetSmallCell(localGrid);
				if (smallCell != null && smallCell.IsValid() && ((bool)smallCell.Device || (bool)smallCell.Chute || (bool)smallCell.Pipe || (bool)smallCell.Cable || (bool)smallCell.Other))
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.RocketFuselageHasInternals.DisplayString);
				}
			}
		}
		return base.CanDeconstruct();
	}

	public List<INetworkedStructure> ConnectedStructures()
	{
		return ConnectedRocketParts();
	}

	public bool IsConnected(Connection otherEnd)
	{
		throw new NotImplementedException();
	}

	public void OnStructureNetworkUpdated()
	{
		throw new NotImplementedException();
	}

	public void OnImGuiDraw()
	{
		ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.Orange;
		ImGuiExtensions.Rendering.DrawCube(ThingTransform.position, RocketGrid.GridSquare);
		if (Vector3.Distance(InventoryManager.ParentHuman.Position, base.Position) < 2f)
		{
			ImGuiExtensions.Rendering.DrawTextInWorld(delegate
			{
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.White;
				ImguiHelper.Text("RocketNetwork: " + StringManager.Get(RocketNetwork.ReferenceId));
				ImguiHelper.Text("RocketID: " + StringManager.Get(RocketNetwork.Rocket?.ReferenceId ?? 0));
				ImguiHelper.Text("PartId: " + StringManager.Get(base.ReferenceId));
			}, ThingTransform.position, (int)base.ReferenceId);
		}
	}
}
