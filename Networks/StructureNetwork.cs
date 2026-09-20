using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using Objects.RoboticArm;
using Trading.Waypoints;
using UnityEngine;

namespace Networks;

public abstract class StructureNetwork : ReferencableNetwork<INetworkedStructure>, ISyncListable
{
	public static readonly List<StructureNetwork> AllStructureNetworks = new List<StructureNetwork>();

	public static SyncList<StructureNetwork> NewToSend = new SyncList<StructureNetwork>(DeserializeNew);

	public static bool ForceNextDeltaFull = true;

	[ReadOnly]
	public Bounds Bounds;

	public virtual StructureNetworkType NetworkType => StructureNetworkType.None;

	public List<INetworkedStructure> StructureList => Members;

	public StructureNetwork(long referenceId = 0L)
		: base(referenceId)
	{
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		AllStructureNetworks.Add(this);
		HelperHintsManager.Register(this);
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			NewToSend.Add(this);
		}
	}

	protected override void OnDeregister()
	{
		base.OnDeregister();
		AllStructureNetworks.Remove(this);
		HelperHintsManager.Deregister(this);
	}

	protected override void OnMemberAdded(INetworkedStructure member)
	{
		CalculateBoundsForStructure(member as Structure);
	}

	protected override void OnMemberRemoved(INetworkedStructure member)
	{
		RecalculateBounds();
	}

	protected override IEnumerable<INetworkedStructure> GetNeighbours(INetworkedStructure segment)
	{
		return segment.ConnectedStructures();
	}

	public static List<StructureNetwork> ConnectedNetworks(INetworkedStructure iNetworkedStructure)
	{
		List<StructureNetwork> list = new List<StructureNetwork>();
		foreach (INetworkedStructure item in iNetworkedStructure.ConnectedStructures())
		{
			if (!list.Contains(item.StructureNetwork) && item.StructureNetwork != null)
			{
				list.Add(item.StructureNetwork);
			}
		}
		return list;
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		Network.WriteIndex<ushort>(writer, out var count, out var bufferIndex);
		foreach (StructureNetwork allStructureNetwork in AllStructureNetworks)
		{
			Network.WritePackedId(writer, allStructureNetwork);
			writer.WriteByte((byte)allStructureNetwork.NetworkType);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static async UniTask DeserializeOnJoin(RocketBinaryReader reader)
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenDeserializingNetworks.DisplayString);
		ushort num = reader.ReadUInt16();
		for (int i = 0; i < num; i++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			if (Referencable.Find(referenceId) != null)
			{
				throw new Exception("Error! Tried to create a network using a ReferenceId that is already in use!");
			}
			CreateFromType((StructureNetworkType)reader.ReadByte(), referenceId);
		}
	}

	private static void CreateFromType(StructureNetworkType networkType, long referenceId)
	{
		switch (networkType)
		{
		case StructureNetworkType.LandingPad:
			new LandingPadNetwork(referenceId);
			break;
		case StructureNetworkType.Pipe:
			new PipeNetwork(referenceId);
			break;
		case StructureNetworkType.Chute:
			new ChuteNetwork(referenceId);
			break;
		case StructureNetworkType.Rocket:
			new RocketNetwork(referenceId);
			break;
		case StructureNetworkType.RoboticArm:
			new RoboticArmNetwork(referenceId);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, this);
		writer.WriteByte((byte)NetworkType);
	}

	private static void DeserializeNew(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		CreateFromType((StructureNetworkType)reader.ReadByte(), referenceId);
	}

	protected virtual bool IsUpdateDirty()
	{
		return false;
	}

	public static void SerializeDeltaState(RocketBinaryWriter writer)
	{
		bool forceNextDeltaFull = ForceNextDeltaFull;
		ForceNextDeltaFull = false;
		Network.WriteIndex<ushort>(writer, out var count, out var bufferIndex);
		for (int i = 0; i < AllStructureNetworks.Count; i++)
		{
			StructureNetwork structureNetwork = AllStructureNetworks[i];
			if (structureNetwork != null && structureNetwork.IsNetworkValid() && (forceNextDeltaFull || structureNetwork.IsUpdateDirty()))
			{
				Network.WritePackedId(writer, structureNetwork);
				structureNetwork.BuildUpdate(writer);
				count++;
			}
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static void DeserializeDeltaState(RocketBinaryReader reader)
	{
		ushort num = reader.ReadUInt16();
		for (int i = 0; i < num; i++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			Referencable.Find<StructureNetwork>(referenceId).ProcessUpdate(reader);
		}
	}

	protected virtual void BuildUpdate(RocketBinaryWriter writer)
	{
	}

	protected virtual void ProcessUpdate(RocketBinaryReader reader)
	{
	}

	public static void ClearAll()
	{
		foreach (StructureNetwork allStructureNetwork in AllStructureNetworks)
		{
			allStructureNetwork.OnClear();
		}
		AllStructureNetworks.Clear();
		LandingPadNetwork.AllLandingPadNetworks.Clear();
		PipeNetwork.AllPipeNetworks.Clear();
		ChuteNetwork.AllChuteNetworks.Clear();
		RocketNetwork.AllRocketNetworks.Clear();
		RoboticArmNetwork.AllRoboticArmNetworks.Clear();
	}

	protected virtual void OnClear()
	{
	}

	public void CalculateBoundsForStructure(Structure structure)
	{
		if (!(structure == null))
		{
			Bounds bounds = new Bounds(structure.ThingTransformLocalPosition + structure.Bounds.center, structure.Bounds.size);
			Bounds.Encapsulate(bounds);
		}
	}

	public void RecalculateBounds()
	{
		Bounds = default(Bounds);
		for (int num = StructureList.Count - 1; num >= 0; num--)
		{
			INetworkedStructure networkedStructure = StructureList[num];
			CalculateBoundsForStructure(networkedStructure as Structure);
		}
	}

	public virtual void ValidateOnLoad(int saveVersion)
	{
		RefreshNetwork();
		ValidateSaveVersion(saveVersion);
	}

	protected virtual void ValidateSaveVersion(int saveVersion)
	{
	}

	public virtual bool Merge(StructureNetwork oldNetwork)
	{
		return MergeMembersFrom(oldNetwork);
	}

	public static bool Merge(List<StructureNetwork> structureNetworks, out StructureNetwork mergedNetwork)
	{
		if (structureNetworks == null || structureNetworks.Count == 0)
		{
			mergedNetwork = null;
			return false;
		}
		if (structureNetworks.Count == 1)
		{
			mergedNetwork = structureNetworks[0];
			return true;
		}
		mergedNetwork = structureNetworks[0];
		for (int i = 1; i < structureNetworks.Count; i++)
		{
			mergedNetwork.Merge(structureNetworks[i]);
		}
		return true;
	}

	public virtual void OnImguiDraw()
	{
	}

	public void ClearWaypointLinksOnNetwork()
	{
		foreach (INetworkedStructure structure in StructureList)
		{
			if (structure is IWaypoint waypoint)
			{
				if (waypoint.NextWaypoint != null)
				{
					waypoint.NextWaypoint.PreviousWaypoint = null;
				}
				waypoint.NextWaypoint = null;
				if (waypoint.PreviousWaypoint != null)
				{
					waypoint.PreviousWaypoint.NextWaypoint = null;
				}
				waypoint.PreviousWaypoint = null;
			}
		}
	}

	protected virtual void OnFinishedLoad()
	{
	}

	public static void StructureNetworksOnFinishedLoad()
	{
		for (int num = AllStructureNetworks.Count - 1; num >= 0; num--)
		{
			AllStructureNetworks[num]?.OnFinishedLoad();
		}
	}
}
