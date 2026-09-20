using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Networks;
using Trading;

namespace Objects.RoboticArm;

public class RoboticArmRailBase : SmallGrid, INetworkedRoboticArm, INetworkedStructure, INetworkMember, IReferencable, IEvaluable, ISmartRotatable
{
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

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

	public RoboticArmNetwork RoboticArmNetwork => StructureNetwork as RoboticArmNetwork;

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
				new RoboticArmNetwork(0L).Add(this);
			}
		}
		base.OnRegistered(cell);
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting || IsCursor || GameManager.GameState == GameState.None)
		{
			return;
		}
		List<INetworkedStructure> list = new List<INetworkedStructure>(ConnectedStructures());
		RoboticArmNetwork?.Remove(this);
		if (GameManager.RunSimulation)
		{
			foreach (INetworkedStructure item in list)
			{
				if (item is INetworkedRoboticArm networkedRoboticArm)
				{
					networkedRoboticArm.RoboticArmNetwork.RebuildNetworkServer(networkedRoboticArm);
				}
			}
		}
		base.OnDestroy();
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		if (thingSaveData is RoboticArmRailBaseSaveData roboticArmRailBaseSaveData)
		{
			roboticArmRailBaseSaveData.RoboticArmNetworkId = RoboticArmNetwork?.ReferenceId ?? 0;
		}
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		if (thingSaveData is RoboticArmRailBaseSaveData { RoboticArmNetworkId: var roboticArmNetworkId })
		{
			(Referencable.Find<RoboticArmNetwork>(roboticArmNetworkId) ?? new RoboticArmNetwork(roboticArmNetworkId)).Add(this);
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(RoboticArmNetwork?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		RoboticArmNetwork roboticArmNetwork = Referencable.Find<RoboticArmNetwork>(reader.ReadInt64());
		if (GameManager.GameState != GameState.Joining && StructureNetwork.Merge(StructureNetwork.ConnectedNetworks(this), out var mergedNetwork))
		{
			roboticArmNetwork = mergedNetwork as RoboticArmNetwork;
		}
		roboticArmNetwork?.Add(this);
	}

	public override bool IsConnected(Connection otherEnd)
	{
		Grid3 facingGrid = otherEnd.GetFacingGrid();
		foreach (Connection openEnd in OpenEnds)
		{
			if ((openEnd.ConnectionType & otherEnd.ConnectionType) != NetworkType.None && !(facingGrid != openEnd.GetLocalGrid()))
			{
				if (!(otherEnd.Parent is IRoboticArmRail))
				{
					return true;
				}
				return ThreadedManager.IsThread ? RocketMath.CompareVectors(openEnd.TransformUp, otherEnd.TransformUp) : RocketMath.CompareVectors(openEnd.Transform.up, otherEnd.Transform.up);
			}
		}
		return false;
	}

	public List<INetworkedStructure> ConnectedStructures()
	{
		return ConnectedRoboticArms();
	}

	public void OnStructureNetworkUpdated()
	{
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
}
