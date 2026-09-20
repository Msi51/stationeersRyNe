using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.UI;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Networks;
using Objects.Pipes;
using Objects.Rockets;
using Trading;
using UI.ImGuiUi;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class Chute : SmallSingleGrid, ISmartRotatable, IChute, INetworkedChute, INetworkedStructure, INetworkMember, IReferencable, IEvaluable, ISmallGrid, ITooltip, IRocketInternals, IRocketComponent
{
	[Header("Chute")]
	public SmallGrid NextNeighbor;

	public Vector3 DropPosition;

	public Vector3 DropVelocity;

	public int NextTickMove;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private long _nextNeighborId;

	public static float VelocityScale = 2f;

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

	public ChuteNetwork ChuteNetwork => StructureNetwork as ChuteNetwork;

	public Slot TransportSlot => Slots[0];

	public List<Connection> SmallGridOpenEnds => OpenEnds;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Chutes;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public List<INetworkedStructure> ConnectedStructures()
	{
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected(span, ref count);
		List<INetworkedStructure> list = new List<INetworkedStructure>(count);
		Span<SmallCellRef> span2 = span;
		Span<SmallCellRef> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			SmallCellRef smallCellRef = span3[i];
			if (smallCellRef.TryGet<INetworkedChute>(out var found) && found.ReferenceId != base.ReferenceId)
			{
				list.Add(found);
			}
		}
		return list;
	}

	public override CanConstructInfo CanConstruct()
	{
		if (IsConnectingToUmbilical(out var found))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementIsNotUmbilicalConnector.AsString(found.AsThing.ToTooltip()));
		}
		return base.CanConstruct();
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.ChuteCategory);
	}

	public void OnStructureNetworkUpdated()
	{
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new ChuteSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is ChuteSaveData chuteSaveData)
		{
			chuteSaveData.ChuteNetworkId = ChuteNetwork?.ReferenceId ?? 0;
			chuteSaveData.NextNeighborId = ((NextNeighbor != null) ? NextNeighbor.ReferenceId : (-1));
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is ChuteSaveData { ChuteNetworkId: var chuteNetworkId } chuteSaveData)
		{
			(Referencable.Find<ChuteNetwork>(chuteNetworkId) ?? new ChuteNetwork(chuteNetworkId)).Add(this);
			_nextNeighborId = chuteSaveData.NextNeighborId;
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(ChuteNetwork?.ReferenceId ?? 0);
		writer.WriteInt64(_nextNeighborId);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		long referenceId = reader.ReadInt64();
		_nextNeighborId = reader.ReadInt64();
		ChuteNetwork chuteNetwork = Referencable.Find<ChuteNetwork>(referenceId);
		if (GameManager.GameState != GameState.Joining && StructureNetwork.Merge(StructureNetwork.ConnectedNetworks(this), out var mergedNetwork))
		{
			chuteNetwork = mergedNetwork as ChuteNetwork;
		}
		chuteNetwork.Add(this);
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected<Device>(span, ref count);
		Span<SmallCellRef> span2 = span;
		Span<SmallCellRef> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			SmallCellRef smallCellRef = span3[i];
			if (!smallCellRef.TryGet<Device>(out var found))
			{
				continue;
			}
			if (!(found is ChuteBin chuteBin))
			{
				if (found is ChuteExportBin chuteExportBin)
				{
					chuteExportBin.InputChute = this;
				}
			}
			else
			{
				chuteBin.OutputChute = this;
			}
		}
		if (_nextNeighborId > 0)
		{
			NextNeighbor = Referencable.Find<SmallGrid>(_nextNeighborId);
		}
		SetDropPoint();
	}

	public override void OnNeighborPlaced(SmallGrid neighbor)
	{
		base.OnNeighborPlaced(neighbor);
		if (!(neighbor is Chute chute))
		{
			if (!(neighbor is ChuteBin chuteBin))
			{
				if (neighbor is ChuteExportBin chuteExportBin)
				{
					chuteExportBin.InputChute = this;
					SetDropPoint();
				}
			}
			else
			{
				chuteBin.OutputChute = this;
				SetDropPoint();
			}
		}
		else
		{
			SetDropPoint();
			chute.SetDropPoint();
		}
	}

	public override void OnNeighborRemoved(SmallGrid neighbor)
	{
		base.OnNeighborRemoved(neighbor);
		if (!(neighbor is Chute chute))
		{
			if (!(neighbor is ChuteBin chuteBin))
			{
				if (neighbor is ChuteExportBin chuteExportBin)
				{
					chuteExportBin.InputChute = null;
					SetDropPoint();
				}
			}
			else
			{
				chuteBin.OutputChute = null;
				SetDropPoint();
			}
		}
		else
		{
			SetDropPoint();
			chute.SetDropPoint();
		}
	}

	protected virtual void OnPostItemMoved()
	{
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (!GameManager.RunSimulation || !TransportSlot.Occupant || NextTickMove == OcclusionManager.LastOnServerTick)
		{
			return;
		}
		if ((bool)NextNeighbor)
		{
			if (NextNeighbor is IChute chute && !chute.TransportSlot.Occupant && IsValidInputConnection(chute.SmallGridOpenEnds, this))
			{
				chute.SetNeighbor(this);
				OnServer.MoveToSlot(TransportSlot.Occupant, chute.TransportSlot);
				OnPostItemMoved();
			}
		}
		else
		{
			OnServer.MoveToWorld(TransportSlot.Occupant, DropPosition, ThingTransform.rotation, DropVelocity, UnityEngine.Random.insideUnitSphere);
			OnPostItemMoved();
		}
	}

	public static bool IsValidInputConnection(List<Connection> openEnds, SmallGrid smallGrid)
	{
		Connection connection = null;
		foreach (Connection openEnd in openEnds)
		{
			if (openEnd.GetChuteOrDevice() == smallGrid)
			{
				connection = openEnd;
			}
		}
		bool flag;
		if (connection != null)
		{
			ConnectionRole connectionRole = connection.ConnectionRole;
			if ((uint)(connectionRole - 3) <= 1u)
			{
				flag = true;
				goto IL_0055;
			}
		}
		flag = false;
		goto IL_0055;
		IL_0055:
		return !flag;
	}

	public virtual void SetDropPoint()
	{
		DropPosition = base.ThingTransformPosition;
		foreach (Connection openEnd in OpenEnds)
		{
			if (!(openEnd.GetChuteOrDevice() != null))
			{
				DropPosition = openEnd.Transform.position;
			}
		}
		DropVelocity = (DropPosition - base.ThingTransformPosition) * VelocityScale;
	}

	public virtual SmallGrid GetOutputNeighbor(SmallGrid inputNeighbor)
	{
		int index = ((OpenEnds[0].GetChuteOrDevice() == inputNeighbor) ? 1 : 0);
		return OpenEnds[index].GetChuteOrDevice();
	}

	public void SetNeighbor(SmallGrid sendingNeighbor)
	{
		SmallGrid outputNeighbor = GetOutputNeighbor(sendingNeighbor);
		if (outputNeighbor == null)
		{
			NextNeighbor = null;
			return;
		}
		NextTickMove = OcclusionManager.LastOnServerTick;
		SetDropPoint();
		NextNeighbor = outputNeighbor;
	}

	public override void OnRegistered(Cell cell)
	{
		DropPosition = base.ThingTransformPosition;
		if (GameManager.GameState != GameState.Loading && GameManager.RunSimulation)
		{
			StructureNetwork.Merge(StructureNetwork.ConnectedNetworks(this), out var mergedNetwork);
			if (mergedNetwork != null)
			{
				mergedNetwork.Add(this);
			}
			else
			{
				new ChuteNetwork(0L).Add(this);
			}
		}
		base.OnRegistered(cell);
		SetDropPoint();
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting || IsCursor || GameManager.GameState == GameState.None)
		{
			return;
		}
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected<INetworkedChute>(span, ref count);
		ChuteNetwork?.Remove(this);
		if (GameManager.RunSimulation)
		{
			Span<SmallCellRef> span2 = span;
			Span<SmallCellRef> span3 = span2.Slice(0, count);
			for (int i = 0; i < span3.Length; i++)
			{
				SmallCellRef smallCellRef = span3[i];
				INetworkedChute networkedChute = smallCellRef.Get<INetworkedChute>();
				networkedChute?.ChuteNetwork.RebuildNetworkServer(networkedChute);
			}
		}
		base.OnDestroy();
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild.ParentSlot == TransportSlot && TransportSlot.Interactable != null && (bool)TransportSlot.Interactable.Collider)
		{
			Vector3 size = TransportSlot.Interactable.Collider.bounds.size;
			Vector3 size2 = newChild.Bounds.size;
			float num = Mathf.Min(size.x / size2.x, size.y / size2.y, size.z / size2.z, 1f);
			newChild.ThingTransform.localScale = Vector3.one * num;
		}
	}

	public void OnImGuiDraw()
	{
		foreach (Connection openEnd in OpenEnds)
		{
			Vector3 vector = openEnd.Transform.position.GridCenter(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
			Vector3 vector2 = base.transform.position;
			Vector3 normalized = (vector2 - vector).normalized;
			ImGuiExtensions.Rendering.DrawLine(screenSpace2: ImGuiExtensions.WorldToScreen(vector + normalized * (SmallGrid.SmallGridSize * 0.5f)), screenSpace1: ImGuiExtensions.WorldToScreen(vector2));
		}
		if (!(Vector3.Distance(InventoryManager.ParentHuman.Position, base.Position) < 2f))
		{
			return;
		}
		ImGuiExtensions.Rendering.DrawTextInWorld(delegate
		{
			ImGuiExtensions.Rendering.RenderingColor = ImGuiExtensions.ColorConvertFloat4ToU32(Color.Lerp(CustomColor.Color, Color.white, 0.2f));
			ImguiHelper.DrawText("Network Id:");
			ImguiHelper.SameLine();
			ImguiHelper.DrawText(StringManager.Get(ChuteNetwork.ReferenceId));
			ImguiHelper.DrawText("Chute Id:");
			ImguiHelper.SameLine();
			ImguiHelper.DrawText(StringManager.Get(base.ReferenceId));
			if ((bool)TransportSlot.Occupant)
			{
				ImguiHelper.Text("Occupant:");
				ImguiHelper.SameLine();
				ImguiHelper.Text(TransportSlot.Occupant.DisplayName);
			}
		}, ThingTransform.position, (int)base.ReferenceId);
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = permutation;
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}
}
