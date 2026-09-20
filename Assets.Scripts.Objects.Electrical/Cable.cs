using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Cable : SmallSingleGrid, IGridMergeable, ISmartRotatable, IRocketInternals, IRocketComponent
{
	public enum Type
	{
		normal,
		heavy,
		superHeavy
	}

	public delegate void OnMergeEvent(Thing cable);

	[Header("Cables")]
	public float MaxVoltage = 5000f;

	[ReadOnly]
	public long CableNetworkId;

	public CableRuptured RupturedPrefab;

	public Type CableType;

	public bool IsStraight;

	public int StraightUnitLength;

	public bool BlockMergeWithOtherCables;

	[SerializeField]
	private RocketInternalCellType _rocketInternalCellType;

	private CableNetwork _cableNetwork;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public RocketInternalCellType InternalCellType => _rocketInternalCellType;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public CableNetwork CableNetwork
	{
		get
		{
			return _cableNetwork;
		}
		set
		{
			if (!object.Equals(_cableNetwork, value))
			{
				_cableNetwork = value;
				if (this.OnPowerNetworkChanged != null)
				{
					this.OnPowerNetworkChanged();
				}
			}
		}
	}

	public event Event OnPowerNetworkChanged;

	public static event OnMergeEvent OnMerge;

	protected override CanConstructInfo CanDeconstruct()
	{
		if (AttachedDevices.Count <= 0)
		{
			return base.CanDeconstruct();
		}
		SmallGrid smallGrid = AttachedDevices[0];
		if ((object)smallGrid != null && !smallGrid.IsBeingDestroyed)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.InvalidAttachmentsDeconstruct.AsString(smallGrid.ToTooltip()));
		}
		return base.CanDeconstruct();
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.CableCategory);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new CableSaveSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is CableSaveSaveData { CableNetworkId: var cableNetworkId })
		{
			(Referencable.Find<CableNetwork>(cableNetworkId) ?? new CableNetwork(cableNetworkId)).Add(this);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is CableSaveSaveData cableSaveSaveData)
		{
			cableSaveSaveData.CableNetworkId = CableNetworkId;
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(CableNetworkId);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		CableNetworkId = reader.ReadInt64();
		CableNetwork cableNetwork = Referencable.Find<CableNetwork>(CableNetworkId);
		CableNetwork cableNetwork2 = cableNetwork;
		if (GameManager.GameState != GameState.Joining)
		{
			cableNetwork2 = CableNetwork.Merge(CableNetwork.ConnectedNetworks(this)) ?? cableNetwork;
		}
		cableNetwork2.Add(this);
	}

	public IEnumerator WaitThenBreak()
	{
		Break();
		yield break;
	}

	public void Break()
	{
		if (ThreadedManager.IsThread)
		{
			UnityMainThreadDispatcher.Instance().Enqueue(WaitThenBreak());
			return;
		}
		if (CableNetwork.RequiredLoad > 0f)
		{
			WorldManager.Spark(base.ThingTransformPosition, 20, base.GridController.RoomController.GetRoom(base.WorldGrid) != null);
		}
		CreateStructureInstance instance = new CreateStructureInstance(RupturedPrefab, this);
		OnServer.Destroy(this);
		Constructor.SpawnConstruct(instance);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		WeldingTorch weldingTorch = attack.SourceItem as WeldingTorch;
		if ((bool)weldingTorch && !base.Indestructable)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = "Burn"
			};
			if (!weldingTorch.IsOperable)
			{
				delayedActionInstance.IsDisabled = true;
				if (!weldingTorch.OnOff)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceNotOn);
				}
				if (weldingTorch.IsEmpty)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceNoFuel);
				}
				if (!weldingTorch.Inflamed)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceNotHotEnough);
				}
				return delayedActionInstance;
			}
			if (doAction)
			{
				Achievements.AssessZzzt(weldingTorch.RootParentHuman);
				Break();
			}
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}

	public override void OnRegistered(Cell cell)
	{
		if (GameManager.GameState != GameState.Loading && GameManager.RunSimulation)
		{
			CableNetwork cableNetwork = CableNetwork.Merge(CableNetwork.ConnectedNetworks(this));
			if (cableNetwork != null)
			{
				cableNetwork.Add(this);
			}
			else
			{
				new CableNetwork(this);
			}
		}
		base.OnRegistered(cell);
	}

	public void OnImGuiDraw()
	{
		foreach (Connection openEnd in OpenEnds)
		{
			Vector3 vector = openEnd.Transform.position.GridCenter(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
			Vector3 vector2 = base.transform.position;
			Vector3 normalized = (vector2 - vector).normalized;
			Vector3 worldPos = vector + normalized * (SmallGrid.SmallGridSize * 0.5f);
			switch (CableType)
			{
			case Type.normal:
				ImGuiExtensions.Rendering.DrawClippedLine(ImGuiExtensions.WorldToScreen(vector2), ImGuiExtensions.WorldToScreen(worldPos));
				ImGuiExtensions.Rendering.DrawClippedDottedLine(vector2, worldPos, Transform.up, 0.1f, 1);
				break;
			case Type.heavy:
			case Type.superHeavy:
				ImGuiExtensions.Rendering.DrawClippedDottedLine(vector2, worldPos);
				ImGuiExtensions.Rendering.DrawClippedDottedLine(vector2, worldPos, Transform.up, 0.1f, 1);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting || IsCursor || GameManager.GameState == GameState.None)
		{
			return;
		}
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected<Cable>(span, ref count);
		CableNetwork cableNetwork = CableNetwork;
		CableNetwork?.Remove(this);
		base.OnDestroy();
		if (GameManager.RunSimulation && cableNetwork != null)
		{
			Span<SmallCellRef> span2 = span;
			Span<SmallCellRef> span3 = span2.Slice(0, count);
			for (int i = 0; i < span3.Length; i++)
			{
				CableNetwork.RebuildCableNetworkServer(span3[i]);
			}
		}
	}

	protected override bool _IsCollision(SmallGrid smallGrid)
	{
		if (!(smallGrid is Cable cable))
		{
			return base._IsCollision(smallGrid);
		}
		if (cable.CableType != CableType)
		{
			return true;
		}
		if (!cable.BlockMergeWithOtherCables)
		{
			return BlockMergeWithOtherCables;
		}
		return true;
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public override CanConstructInfo CanConstruct()
	{
		if (IsConnectingToUmbilical(out var found))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementIsNotUmbilicalConnector.AsString(found.AsThing.ToTooltip()));
		}
		return base.CanConstruct();
	}

	public CanConstructInfo CanReplace(MultiConstructor constructor, Item inactiveHandItem)
	{
		if (base.Indestructable)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.CannotMergeIMergeable.AsString(ToTooltip()));
		}
		MultiMergeConstructor multiMergeConstructor = constructor as MultiMergeConstructor;
		if (multiMergeConstructor == null)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.CannotMergeIMergeable.AsString(ToTooltip()));
		}
		Grid3[] array = (Grid3[])GridBounds.GetLocalSmallGrid(base.ThingTransformPosition, base.ThingTransformRotation);
		foreach (Grid3 localGrid in array)
		{
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell == null || !smallCell.Cable)
			{
				continue;
			}
			if (inactiveHandItem == null)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.MergeRequiresTool.AsString(multiMergeConstructor.ToolExit.DisplayName));
			}
			if (multiMergeConstructor.ToolExit.PrefabHash == inactiveHandItem.PrefabHash || (inactiveHandItem.ReplacementOf != null && multiMergeConstructor.ToolExit.PrefabHash == inactiveHandItem.ReplacementOf.PrefabHash))
			{
				if (WillMergeWhenPlaced())
				{
					Cable.OnMerge?.Invoke(this);
				}
				if (smallCell.Cable.CableType != CableType)
				{
					return CanConstructInfo.InvalidPlacement(GameStrings.CannotMergeIMergeableOfDifferentType.AsString(smallCell.Cable.DisplayName));
				}
				return CanConstructInfo.ValidPlacement;
			}
			return CanConstructInfo.InvalidPlacement(GameStrings.MergeRequiresTool.AsString(multiMergeConstructor.ToolExit.DisplayName));
		}
		return CanConstructInfo.ValidPlacement;
	}

	public bool WillMergeWhenPlaced()
	{
		if (BlockMergeWithOtherCables)
		{
			return false;
		}
		Grid3[] array = (Grid3[])GridBounds.GetLocalSmallGrid(base.ThingTransformPosition, base.ThingTransformRotation);
		foreach (Grid3 localGrid in array)
		{
			SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
			if (smallCell != null && smallCell.Cable != null)
			{
				if (smallCell.Cable.CableType == CableType)
				{
					return !BlockMergeWithOtherCables;
				}
				return false;
			}
		}
		return false;
	}

	public List<ThingRenderer> GetThingRenderers()
	{
		return Renderers;
	}
}
