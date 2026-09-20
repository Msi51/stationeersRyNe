using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Networks;

namespace Objects.RoboticArm;

public class RoboticArmNetwork : StructureNetwork
{
	public static readonly List<RoboticArmNetwork> AllRoboticArmNetworks = new List<RoboticArmNetwork>();

	public readonly List<IRoboticArmJunction> JunctionList = new List<IRoboticArmJunction>();

	public readonly List<RailNode> RailNodeList = new List<RailNode>();

	public readonly List<RoboticArmDock> DockList = new List<RoboticArmDock>();

	public readonly List<IRoboticArmRail> RailList = new List<IRoboticArmRail>();

	private static readonly Comparison<RoboticArmDock> CompareDocks = (RoboticArmDock a, RoboticArmDock b) => a.ReferenceId.CompareTo(b.ReferenceId);

	public override string DisplayName => "Robotic Arm Network: " + StringManager.Get(base.ReferenceId);

	public override StructureNetworkType NetworkType => StructureNetworkType.RoboticArm;

	private int StartingDockIndex { get; set; }

	public bool StartingDockFlipped { get; private set; }

	public bool Looping { get; set; }

	public int MinJunctionIndex
	{
		get
		{
			if (!StartingDockFlipped)
			{
				return -StartingDockIndex;
			}
			return StartingDockIndex - (JunctionList.Count - 1);
		}
	}

	public int MaxJunctionIndex
	{
		get
		{
			if (!StartingDockFlipped)
			{
				return -StartingDockIndex + JunctionList.Count - 1;
			}
			return StartingDockIndex;
		}
	}

	public static long[] AllNetworkIds => ReferencableNetworkHelper.GetValidNetworkIds(AllRoboticArmNetworks);

	public int GetOffsetJunctionIndex(int absoluteJunctionIndex)
	{
		if (!StartingDockFlipped)
		{
			return absoluteJunctionIndex - StartingDockIndex;
		}
		return StartingDockIndex - absoluteJunctionIndex;
	}

	public IRoboticArmJunction GetJunctionFromOffsetIndex(int offsetJunctionIndex)
	{
		int index = (StartingDockFlipped ? (-offsetJunctionIndex + StartingDockIndex) : (offsetJunctionIndex + StartingDockIndex));
		return JunctionList[index];
	}

	public RoboticArmNetwork(long referenceId = 0L)
		: base(referenceId)
	{
	}

	public void ChangeStartingDockIndex(RoboticArmDock startingDock)
	{
		bool startingDockFlipped = StartingDockFlipped;
		int startingDockIndex = StartingDockIndex;
		StartingDockIndex = startingDock.JunctionIndex;
		StartingDockFlipped = startingDock.Flipped;
		if (!GameManager.RunSimulation)
		{
			return;
		}
		int num = StartingDockIndex - startingDockIndex;
		if (startingDockFlipped && StartingDockFlipped)
		{
			foreach (RoboticArmDock dock in DockList)
			{
				dock.TargetJunctionIndex += num;
			}
			return;
		}
		if (!startingDockFlipped && !StartingDockFlipped)
		{
			foreach (RoboticArmDock dock2 in DockList)
			{
				dock2.TargetJunctionIndex -= num;
			}
			return;
		}
		if (!startingDockFlipped && StartingDockFlipped)
		{
			foreach (RoboticArmDock dock3 in DockList)
			{
				dock3.TargetJunctionIndex = num - dock3.TargetJunctionIndex;
			}
			return;
		}
		if (!startingDockFlipped || StartingDockFlipped)
		{
			return;
		}
		foreach (RoboticArmDock dock4 in DockList)
		{
			dock4.TargetJunctionIndex = -num - dock4.TargetJunctionIndex;
		}
	}

	protected override void OnNetworkChanged()
	{
		base.OnNetworkChanged();
		JunctionList.Clear();
		RailNodeList.Clear();
		DockList.Clear();
		RailList.Clear();
		Looping = false;
		if (!GetStartingDock(out var startingDock))
		{
			return;
		}
		GetLeftMostRail(startingDock, out var rail, out var rightHandEnd);
		HashSet<long> hashSet = new HashSet<long>();
		IRoboticArmRail roboticArmRail = rail;
		Connection connection = rightHandEnd;
		while (true)
		{
			if (hashSet.Contains(roboticArmRail.ReferenceId))
			{
				Looping = true;
				break;
			}
			hashSet.Add(roboticArmRail.ReferenceId);
			bool flag = connection != roboticArmRail.AsSmallGrid.OpenEnds[1];
			RailList.Add(roboticArmRail);
			if (roboticArmRail is RoboticArmDock roboticArmDock)
			{
				roboticArmDock.Flipped = flag;
				DockList.Add(roboticArmDock);
			}
			if (roboticArmRail is IRoboticArmJunction roboticArmJunction)
			{
				roboticArmJunction.JunctionIndex = JunctionList.Count;
				JunctionList.Add(roboticArmJunction);
			}
			if (flag)
			{
				for (int num = roboticArmRail.RailNodes.Count - 1; num >= 0; num--)
				{
					AddRailNode(roboticArmRail.RailNodes[num], roboticArmRail, flip: true);
				}
			}
			else
			{
				foreach (RailNode railNode in roboticArmRail.RailNodes)
				{
					AddRailNode(railNode, roboticArmRail, flip: false);
				}
			}
			if (!ConnectedRoboticArm(roboticArmRail, connection, out var connected, out var connectedEnd))
			{
				break;
			}
			roboticArmRail = connected;
			connection = connected.OtherEnd(connectedEnd);
		}
		foreach (RoboticArmDock dock in DockList)
		{
			if (dock.IsStartingDock)
			{
				StartingDockIndex = dock.JunctionIndex;
				StartingDockFlipped = dock.Flipped;
				break;
			}
		}
		HandingLooping();
		HandleInnerCorners();
		CacheRailNodeIndices();
		foreach (IRoboticArmRail rail2 in RailList)
		{
			rail2.RailNetworkUpdated();
		}
	}

	private void HandingLooping()
	{
		if (Looping)
		{
			RailNodeList.Add(RailNodeList[0]);
		}
	}

	private void AddRailNode(RailNode node, IRoboticArmRail rail, bool flip)
	{
		node.Rail = rail;
		RailNodeList.Add(node);
		node.Flip(flip);
	}

	private void CacheRailNodeIndices()
	{
		for (int i = 0; i < RailNodeList.Count; i++)
		{
			RailNodeList[i].Index = i;
		}
	}

	private void HandleInnerCorners()
	{
		if (RailNodeList.Count < 2)
		{
			return;
		}
		for (int i = 0; i < RailNodeList.Count - 1; i++)
		{
			RailNode railNode = RailNodeList[i];
			RailNode railNode2 = RailNodeList[i + 1];
			if (RocketMath.Approximately(railNode.ArmPosition, railNode2.ArmPosition))
			{
				if (railNode.Rail is RoboticArmRailInnerCorner)
				{
					railNode2.SetRotation(railNode.ArmRotation);
				}
				else if (railNode2.Rail is RoboticArmRailInnerCorner)
				{
					railNode.SetRotation(railNode2.ArmRotation);
				}
			}
		}
	}

	private bool GetStartingDock(out RoboticArmDock startingDock)
	{
		List<RoboticArmDock> list = new List<RoboticArmDock>();
		foreach (INetworkedStructure structure in base.StructureList)
		{
			if (structure is RoboticArmDock item)
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			startingDock = null;
			return false;
		}
		list.Sort(CompareDocks);
		startingDock = list[0];
		return true;
	}

	private void GetLeftMostRail(RoboticArmDock startingDock, out IRoboticArmRail rail, out Connection rightHandEnd)
	{
		HashSet<long> hashSet = new HashSet<long>();
		IRoboticArmRail roboticArmRail = startingDock;
		Connection end = startingDock.LeftEnd;
		while (!hashSet.Contains(roboticArmRail.ReferenceId))
		{
			hashSet.Add(roboticArmRail.ReferenceId);
			if (!ConnectedRoboticArm(roboticArmRail, end, out var connected, out var connectedEnd))
			{
				break;
			}
			roboticArmRail = connected;
			end = connected.OtherEnd(connectedEnd);
		}
		rail = roboticArmRail;
		rightHandEnd = roboticArmRail.OtherEnd(end);
	}

	private bool ConnectedRoboticArm(IRoboticArmRail current, Connection end, out IRoboticArmRail connected, out Connection connectedEnd)
	{
		Grid3 localGrid = GridController.World.WorldToLocalGrid(end.Transform.position, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
		SmallCell smallCell = GridController.World.GetSmallCell(localGrid);
		if (smallCell == null)
		{
			connected = null;
			connectedEnd = null;
			return false;
		}
		if (smallCell.Rail != null && !smallCell.Rail.BeingDestroyed && smallCell.Rail != current && ((SmallGrid)smallCell.Rail).IsConnected(end, out var connectedEnd2))
		{
			connected = smallCell.Rail;
			connectedEnd = connectedEnd2;
			return true;
		}
		if (smallCell.Device != null && !smallCell.Device.BeingDestroyed && smallCell.Device != current && smallCell.Device.IsConnected(end, out var connectedEnd3) && smallCell.Device is IRoboticArmRail roboticArmRail)
		{
			connected = roboticArmRail;
			connectedEnd = connectedEnd3;
			return true;
		}
		connected = null;
		connectedEnd = null;
		return false;
	}

	public bool ArmIsStationaryAtIndex(int railNodeIndex, out RoboticArmDock dock)
	{
		dock = null;
		try
		{
			for (int num = DockList.Count - 1; num >= 0; num--)
			{
				RoboticArmDock roboticArmDock = DockList[num];
				if (roboticArmDock.ArmIsStationary(out var railNodeIndex2) && roboticArmDock.IsOpen && railNodeIndex == railNodeIndex2)
				{
					dock = roboticArmDock;
					return true;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		AllRoboticArmNetworks.Add(this);
	}

	protected override void OnDeregister()
	{
		base.OnDeregister();
		AllRoboticArmNetworks.Remove(this);
	}

	protected override ReferencableNetwork<INetworkedStructure> CreateNewNetwork()
	{
		return new RoboticArmNetwork(0L);
	}
}
