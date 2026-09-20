using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Objects.Rockets.Log;
using Objects.Rockets.Log.RocketEvents;
using Objects.Rockets.Mining;
using Objects.Rockets.Scanning;
using Objects.Rockets.UI;
using Trading;
using UnityEngine;

namespace Objects.Rockets;

public class SpaceMapNode : IReferencable, IEvaluable, ISyncListable
{
	public SpaceMapNodeData Data;

	public readonly SpaceMapCode Code;

	public static readonly List<SpaceMapNode> AllSpaceMapNodes = new List<SpaceMapNode>();

	public static SyncList<SpaceMapNode> NewToSend = new SyncList<SpaceMapNode>(DeserializeNew);

	public static SyncList<DestroyEvent> DestroyToSend = new SyncList<DestroyEvent>(DeserializeDestroy);

	public readonly List<NodeConnection> ChildConnections = new List<NodeConnection>();

	public NodeConnection ParentConnection;

	private int _surveyPoints;

	private int _chartPoints;

	private int _discoverPoints;

	private bool _isCharted;

	public MineableDeposit Deposit;

	public const float MAX_SURVEY_PERCENT = 1000f;

	public SurveyData SurveyData;

	public ChartData ChartData;

	public DiscoverSiteData DiscoverData;

	public SpaceMapNodeMineData MineData;

	public DeployData DeployData;

	public SurfaceScanData SurfaceScanData;

	private const float MINE_ACTIONS_TO_ONE_SCAN_POINT = 4f;

	private int _mineCount;

	public string Id => Data?.Id;

	public string DebugName
	{
		get
		{
			if (Data != null && !string.IsNullOrEmpty(Data.Name))
			{
				return Data.Name;
			}
			if (Owner != null)
			{
				return Owner.DisplayName;
			}
			return Id;
		}
	}

	public string DisplayName
	{
		get
		{
			if (!_isCharted)
			{
				return GameStrings.UnchartedLocation.DisplayString;
			}
			if (Data != null)
			{
				LocalizedStringReference name = Data.Name;
				if (name == null)
				{
					return string.Empty;
				}
				return name;
			}
			if (Owner != null)
			{
				return Owner.DisplayName;
			}
			return Id;
		}
	}

	public ushort NetworkUpdateFlags { get; set; }

	public long ReferenceId { get; set; }

	public bool BeingDestroyed { get; set; }

	public SpaceMap SpaceMap => SpaceMap.Current;

	public MapDisplayData MapDisplayData { get; }

	public bool IsAccessible => IsCharted;

	public bool IsDestroyedLaunchPadNode
	{
		get
		{
			if (NodeType == NodeType.LaunchPad)
			{
				if (!BeingDestroyed && Owner != null && !Owner.BeingDestroyed)
				{
					if (Owner is Thing thing)
					{
						return thing.IsBroken;
					}
					return false;
				}
				return true;
			}
			return false;
		}
	}

	public int SurveyPoints
	{
		get
		{
			return _surveyPoints;
		}
		set
		{
			if (SurveyData == null)
			{
				_surveyPoints = 0;
				return;
			}
			_surveyPoints = value;
			FlagToSendNetworkUpdate(1);
			OnSurveyPointsUpdated();
		}
	}

	public int ChartPoints
	{
		get
		{
			return _chartPoints;
		}
		set
		{
			if (ChartData == null)
			{
				_chartPoints = 0;
				return;
			}
			_chartPoints = value;
			FlagToSendNetworkUpdate(16);
			OnChartPointsUpdated();
		}
	}

	public int DiscoverPoints
	{
		get
		{
			return _discoverPoints;
		}
		set
		{
			if (DiscoverData == null)
			{
				_discoverPoints = 0;
				return;
			}
			_discoverPoints = value;
			FlagToSendNetworkUpdate(32);
			OnDiscoverPointsUpdated();
		}
	}

	public bool IsCharted
	{
		get
		{
			return _isCharted;
		}
		set
		{
			_isCharted = value;
			FlagToSendNetworkUpdate(4);
			RocketCanvas.Instance.RefreshMap();
		}
	}

	public float SurveyPercent
	{
		get
		{
			if (SurveyData == null)
			{
				return 0f;
			}
			return (float)SurveyPoints / (float)SurveyData.Difficulty * 100f;
		}
	}

	public ISpaceMapNodeOwner Owner { get; set; }

	public NodeType NodeType { get; }

	public int DynamicNodeCapacity => 9;

	public MapLocation MapLocation { get; set; }

	public List<Rocket> RocketsHere { get; } = new List<Rocket>();

	public int IdHash => Data?.IdHash ?? 0;

	public void PrintDebugInfo(bool verbose = false)
	{
	}

	public bool IsValid()
	{
		switch (NodeType)
		{
		case NodeType.None:
			return false;
		case NodeType.Entry:
			return this == SpaceMap.Current.EntryNode;
		case NodeType.Static:
		{
			SpaceMapNode spaceMapNode = Get(Id);
			if (spaceMapNode != null)
			{
				return spaceMapNode == this;
			}
			return false;
		}
		case NodeType.Generated:
			if (DataCollection.Get<SpaceMapNodeData>(Id) != null)
			{
				return ParentConnection?.Parent != null;
			}
			return false;
		case NodeType.LaunchPad:
		case NodeType.LowOrbitLaunchPad:
			if (Owner != null)
			{
				return ParentConnection?.Parent != null;
			}
			return false;
		case NodeType.LowOrbitHub:
			return this == SpaceMap.Current.LowOrbitHubNode;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public void FlagToSendNetworkUpdate(ushort flag)
	{
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			NetworkUpdateFlags |= flag;
		}
	}

	private void OnSurveyPointsUpdated()
	{
	}

	private void OnDiscoverPointsUpdated()
	{
		if (GameManager.RunSimulation && DiscoverData != null && DiscoverPoints >= DiscoverData.Difficulty)
		{
			DiscoverPoints = 0;
			CreateSite(DiscoverData.SiteGenerations.Pick(), this, 0L);
		}
	}

	private void OnChartPointsUpdated()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		foreach (NodeConnection childConnection in ChildConnections)
		{
			if (childConnection?.Child != null && !childConnection.Child.IsCharted && childConnection.Difficulty <= ChartPoints)
			{
				childConnection.Child.IsCharted = true;
				RocketLog.Append(new NavPointChartEvent(null, childConnection.Child)).Forget();
			}
		}
	}

	private bool HasUnchartedNodes()
	{
		foreach (NodeConnection childConnection in ChildConnections)
		{
			if (!childConnection.Child.IsCharted)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsActionAvailable(RocketMode actionMode)
	{
		switch (actionMode)
		{
		case RocketMode.Invalid:
			return false;
		case RocketMode.None:
			return true;
		case RocketMode.Mine:
		{
			int result;
			if (HasAction(actionMode))
			{
				MineableDeposit deposit = Deposit;
				result = ((deposit != null && !deposit.IsDepleted) ? 1 : 0);
			}
			else
			{
				result = 0;
			}
			return (byte)result != 0;
		}
		case RocketMode.Survey:
			return HasAction(actionMode) && SurveyPercent < 1000f;
		case RocketMode.Discover:
			return HasAction(actionMode) && DynamicNodeCount() < DynamicNodeCapacity;
		case RocketMode.Chart:
			return HasAction(actionMode) && HasUnchartedNodes();
		case RocketMode.Deploy:
			return HasAction(actionMode);
		case RocketMode.SurfaceScan:
			return HasAction(actionMode) && SpaceMap.EntryNode == this;
		case RocketMode.Transfer:
			return RocketsHere.Count > 1;
		default:
			throw new ArgumentOutOfRangeException("actionMode", actionMode, null);
		}
	}

	public int DynamicNodeCount()
	{
		int num = 0;
		for (int num2 = ChildConnections.Count - 1; num2 >= 0; num2--)
		{
			NodeType nodeType = ChildConnections[num2].Child.NodeType;
			if (nodeType == NodeType.Generated || nodeType == NodeType.LaunchPad || nodeType == NodeType.LowOrbitLaunchPad)
			{
				num++;
			}
		}
		return num;
	}

	public (int charted, int total) GetChartedNavPointCount()
	{
		int num = 0;
		int num2 = 0;
		foreach (NodeConnection childConnection in ChildConnections)
		{
			NodeType nodeType = childConnection.Child.NodeType;
			if (nodeType != NodeType.Generated && nodeType != NodeType.LaunchPad && nodeType != NodeType.LowOrbitLaunchPad)
			{
				num2++;
				if (childConnection.Child.IsCharted)
				{
					num++;
				}
			}
		}
		return (charted: num, total: num2);
	}

	public void PrintNodeDebug()
	{
		TreeString treeString = new TreeString(string.Format("Node: {0} (#{1})", DebugName ?? "Unnamed", ReferenceId));
		TreeString.Variable("NodeType: " + EnumCollections.NodeTypes.GetName(NodeType), treeString);
		if (!string.IsNullOrEmpty(Id))
		{
			TreeString.Variable("TemplateId: " + Id, treeString);
		}
		TreeString.Variable($"Code: {Code}", treeString);
		TreeString.Variable($"Charted: {IsCharted}", treeString);
		if (ParentConnection != null)
		{
			TreeString myParent = TreeString.Node("ParentConnection", treeString);
			TreeString.Variable($"Parent: {ParentConnection.Parent.DebugName} (#{ParentConnection.Parent.ReferenceId})", myParent);
			TreeString.Variable($"Distance: {ParentConnection.Distance():F3}", myParent);
		}
		if (SurveyPoints > 0)
		{
			TreeString.Variable($"SurveyPoints: {SurveyPoints}", treeString);
		}
		if (ChartPoints > 0)
		{
			TreeString.Variable($"ChartPoints: {ChartPoints}", treeString);
		}
		if (DiscoverPoints > 0)
		{
			TreeString.Variable($"DiscoverPoints: {DiscoverPoints}", treeString);
		}
		if (Deposit != null)
		{
			Deposit.AddToTree(treeString);
		}
		treeString.ToConsole();
	}

	private int GetChildIndex(SpaceMapNode spaceMapNode)
	{
		for (int i = 0; i < ChildConnections.Count; i++)
		{
			if (ChildConnections[i].Child.ReferenceId == spaceMapNode.ReferenceId)
			{
				return i;
			}
		}
		return -1;
	}

	public void OnAssignedReference()
	{
		SpaceMapCode.Register(this, Code);
		SpaceMap.AddNode(this);
		AllSpaceMapNodes.Add(this);
		if (NodeType == NodeType.Entry)
		{
			if (SpaceMap.EntryNode != null)
			{
				ConsoleWindow.PrintError("ERROR: SpaceMap " + SpaceMap.DisplayName + " has multiple entry nodes!");
			}
			SpaceMap.EntryNode = this;
		}
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			NewToSend.Add(this);
		}
		RocketCanvas.Instance.RefreshMap();
	}

	public static void DeserializeDestroy(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		SpaceMapNode spaceMapNode = Get(referenceId);
		if (spaceMapNode != null)
		{
			spaceMapNode.BeingDestroyed = true;
			spaceMapNode.DeRegister();
		}
	}

	public static void DeserializeNew(RocketBinaryReader reader)
	{
		SpaceMapNode spaceMapNode = Create(reader);
		RocketCanvas.Instance.AddDynamicNodeToSpaceMap(spaceMapNode.ParentConnection.Parent, spaceMapNode);
	}

	public void DeRegister()
	{
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			DestroyToSend.Add(DestroyEvent.Create(this));
		}
		RemoveAllConnections();
		SpaceMap.RemoveNode(this);
		AllSpaceMapNodes.Remove(this);
		if (Owner != null && Owner.SpaceMapNode == this)
		{
			Owner.SpaceMapNode = null;
		}
		Referencable.Deregister(this);
		SpaceMapCode.Deregister(this);
		RocketCanvas.Instance.RefreshMap();
	}

	private void InstanceMineableDeposit(SpaceMapNodeData data)
	{
		if (data.DepositData != null)
		{
			Deposit = new MineableDeposit(data.DepositData);
			Deposit.SetParent(this);
		}
	}

	private void PopulateDataReferences(SpaceMapNodeData data)
	{
		foreach (SpaceMapNodeActionData availableAction in data.AvailableActions)
		{
			if (!(availableAction is ChartData chartData))
			{
				if (!(availableAction is SurfaceScanData surfaceScanData))
				{
					if (!(availableAction is DiscoverSiteData discoverData))
					{
						if (!(availableAction is SpaceMapNodeMineData mineData))
						{
							if (!(availableAction is SurveyData surveyData))
							{
								if (!(availableAction is DeployData deployData))
								{
									throw new ArgumentOutOfRangeException("actionData");
								}
								DeployData = deployData;
							}
							else
							{
								SurveyData = surveyData;
							}
						}
						else
						{
							MineData = mineData;
						}
					}
					else
					{
						DiscoverData = discoverData;
					}
				}
				else
				{
					SurfaceScanData = surfaceScanData;
				}
			}
			else
			{
				ChartData = chartData;
			}
		}
	}

	public static void ClearAll()
	{
		AllSpaceMapNodes.Clear();
		SpaceMapCode.Clear();
	}

	public bool HasAction(RocketMode mode)
	{
		return mode switch
		{
			RocketMode.None => true, 
			RocketMode.Invalid => true, 
			RocketMode.Mine => MineData != null, 
			RocketMode.Survey => SurveyData != null, 
			RocketMode.Discover => DiscoverData != null, 
			RocketMode.Chart => ChartData != null, 
			RocketMode.Deploy => DeployData != null, 
			RocketMode.SurfaceScan => SurfaceScanData != null, 
			RocketMode.Transfer => RocketsHere.Count > 1, 
			_ => false, 
		};
	}

	public RocketAction GetAction(RocketMode mode)
	{
		if (!HasAction(mode))
		{
			return null;
		}
		return mode switch
		{
			RocketMode.None => null, 
			RocketMode.Invalid => null, 
			RocketMode.Mine => MineData.ToInstance(this), 
			RocketMode.Survey => SurveyData.ToInstance(this), 
			RocketMode.Discover => DiscoverData.ToInstance(this), 
			RocketMode.Chart => ChartData.ToInstance(this), 
			RocketMode.Deploy => DeployData.ToInstance(this), 
			RocketMode.SurfaceScan => SurfaceScanData.ToInstance(this), 
			RocketMode.Transfer => new RocketTransfer(null, this), 
			_ => null, 
		};
	}

	public static bool IsNetworkUpdateRequired(uint toCheck, uint flags)
	{
		return (flags & toCheck) != 0;
	}

	public bool SerializeDeltaState(RocketBinaryWriter writer)
	{
		if (NetworkUpdateFlags == 0)
		{
			return false;
		}
		Network.WritePackedId(writer, this);
		ushort networkUpdateFlags = NetworkUpdateFlags;
		NetworkUpdateFlags = 0;
		writer.WriteUInt16(networkUpdateFlags);
		if (IsNetworkUpdateRequired(1u, networkUpdateFlags))
		{
			writer.WriteUInt16((ushort)SurveyPoints);
		}
		if (IsNetworkUpdateRequired(16u, networkUpdateFlags))
		{
			writer.WriteUInt16((ushort)ChartPoints);
		}
		if (IsNetworkUpdateRequired(32u, networkUpdateFlags))
		{
			writer.WriteUInt16((ushort)DiscoverPoints);
		}
		if (IsNetworkUpdateRequired(4u, networkUpdateFlags))
		{
			writer.WriteBoolean(IsCharted);
		}
		if (IsNetworkUpdateRequired(8u, networkUpdateFlags))
		{
			MineableDeposit.SerializeDeltaState(writer, Deposit);
		}
		return true;
	}

	public static void DeSerializeDeltaState(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		ushort flags = reader.ReadUInt16();
		SpaceMapNode spaceMapNode = Referencable.Find<SpaceMapNode>(referenceId);
		if (IsNetworkUpdateRequired(1u, flags))
		{
			ushort surveyPoints = reader.ReadUInt16();
			if (spaceMapNode != null)
			{
				spaceMapNode.SurveyPoints = surveyPoints;
			}
		}
		if (IsNetworkUpdateRequired(16u, flags))
		{
			ushort chartPoints = reader.ReadUInt16();
			if (spaceMapNode != null)
			{
				spaceMapNode.ChartPoints = chartPoints;
			}
		}
		if (IsNetworkUpdateRequired(32u, flags))
		{
			ushort discoverPoints = reader.ReadUInt16();
			if (spaceMapNode != null)
			{
				spaceMapNode.DiscoverPoints = discoverPoints;
			}
		}
		if (IsNetworkUpdateRequired(4u, flags))
		{
			bool isCharted = reader.ReadBoolean();
			if (spaceMapNode != null)
			{
				spaceMapNode.IsCharted = isCharted;
			}
		}
		if (IsNetworkUpdateRequired(8u, flags))
		{
			MineableDeposit.DeSerializeDeltaState(reader, spaceMapNode?.Deposit);
		}
	}

	private void RemoveAllConnections()
	{
		(ParentConnection?.Parent)?.ChildConnections.Remove(ParentConnection);
		ParentConnection = null;
		foreach (NodeConnection childConnection in ChildConnections)
		{
			SpaceMapNode child = childConnection.Child;
			if (child.ParentConnection?.Parent == this)
			{
				child.ParentConnection = null;
			}
		}
		ChildConnections.Clear();
	}

	public void Write(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, this);
		writer.WriteInt32(IdHash);
		writer.WriteUInt64(Code);
		Network.WriteNullable(writer, Deposit);
		writer.WriteByte((byte)NodeType);
		writer.WriteUInt16((ushort)SurveyPoints);
		writer.WriteUInt16((ushort)ChartPoints);
		writer.WriteUInt16((ushort)DiscoverPoints);
		writer.WriteBoolean(IsCharted);
		NodeType nodeType = NodeType;
		if (nodeType == NodeType.LaunchPad || nodeType == NodeType.LowOrbitLaunchPad)
		{
			MapDisplayData.Write(writer);
		}
		Network.WriteNullable(writer, ParentConnection);
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		foreach (NodeConnection childConnection in ChildConnections)
		{
			if (childConnection.Child?.SpaceMap != null)
			{
				childConnection.Child.Write(writer);
				count++;
			}
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public SpaceMapNode(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		int num = reader.ReadInt32();
		if (num != 0)
		{
			Data = DataCollection.Get<SpaceMapNodeData>(num);
		}
		Code = SpaceMapCode.Create(reader.ReadUInt64());
		MapDisplayData = Data?.MapDisplay;
		Network.ReadNullable(reader, ref Deposit);
		if (Data != null)
		{
			PopulateDataReferences(Data);
		}
		NodeType = (NodeType)reader.ReadByte();
		SurveyPoints = reader.ReadUInt16();
		ChartPoints = reader.ReadUInt16();
		DiscoverPoints = reader.ReadUInt16();
		IsCharted = reader.ReadBoolean();
		NodeType nodeType = NodeType;
		if (nodeType == NodeType.LaunchPad || nodeType == NodeType.LowOrbitLaunchPad)
		{
			MapDisplayData = MapDisplayData.Create(reader);
		}
		Deposit?.SetParent(this);
		Referencable.RegisterAs(this, referenceId, force: true);
		Network.ReadNullable(reader, ref ParentConnection);
		Network.ReadIndex<byte>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			new SpaceMapNode(reader);
		}
	}

	public static SpaceMapNode Create(LaunchMount owner)
	{
		return new SpaceMapNode(owner, owner.IsOrbital, 0L);
	}

	public static SpaceMapNode Create(LaunchPadNodeReference saveData)
	{
		return new SpaceMapNode(null, saveData.IsOrbital, saveData.Id);
	}

	public static SpaceMapNode Create(SiteNodeReference saveData)
	{
		SpaceMapNodeData spaceMapNodeData = DataCollection.Get<SpaceMapNodeData>(saveData.TemplateId);
		SpaceMapNode spaceMapNode = Get(saveData.ParentId);
		if (spaceMapNodeData != null && spaceMapNode != null && saveData.Id != 0L)
		{
			return CreateSite(spaceMapNodeData, spaceMapNode, saveData.Id);
		}
		return null;
	}

	public static SpaceMapNode Create(RocketBinaryReader reader)
	{
		return new SpaceMapNode(reader);
	}

	public static SpaceMapNode CreateSite(SpaceMapNodeData template, SpaceMapNode parent, long referenceId = 0L)
	{
		SpaceMapNode spaceMapNode = new SpaceMapNode(template, SpaceMapCode.Create(parent), NodeType.Generated, referenceId)
		{
			IsCharted = true
		};
		NodeConnection item = (spaceMapNode.ParentConnection = new NodeConnection(spaceMapNode, parent, 100f, ConnectionType.Standard));
		parent.ChildConnections.Add(item);
		RocketCanvas.Instance.AddDynamicNodeToSpaceMap(parent, spaceMapNode);
		return spaceMapNode;
	}

	public SpaceMapNode(SpaceMapNodeData data, SpaceMapCode code, NodeType type, long referenceId = 0L)
	{
		Data = data;
		MapDisplayData = data.MapDisplay;
		NodeType = type;
		Code = code;
		IsCharted = data.IsCharted;
		InstanceMineableDeposit(data);
		PopulateDataReferences(data);
		if (referenceId == 0L)
		{
			Referencable.RegisterNew(this);
		}
		else
		{
			Referencable.RegisterAs(this, referenceId);
		}
	}

	private SpaceMapNode(ISpaceMapNodeOwner owner, bool isOrbital, long referenceId = 0L)
	{
		NodeType nodeType = (isOrbital ? NodeType.LowOrbitLaunchPad : NodeType.LaunchPad);
		SpaceMapNode spaceMapNode = (isOrbital ? SpaceMap.LowOrbitHubNode : SpaceMap.EntryNode);
		Data = null;
		Owner = owner;
		MapDisplayData = new MapDisplayData(NodeIcon.Find(LaunchMount.IconIdHash));
		Code = SpaceMapCode.Create(spaceMapNode, nodeType);
		NodeType = nodeType;
		IsCharted = true;
		NodeConnection item = (ParentConnection = new NodeConnection(this, spaceMapNode, isOrbital ? 200f : SpaceMap.DistanceToOrbit, ConnectionType.Orbit));
		spaceMapNode.ChildConnections.Add(item);
		if (referenceId == 0L)
		{
			Referencable.RegisterNew(this);
		}
		else
		{
			Referencable.RegisterAs(this, referenceId);
		}
	}

	public int GetHighestChartDifficulty()
	{
		int num = 0;
		foreach (NodeConnection childConnection in ChildConnections)
		{
			num = Mathf.Max(num, childConnection.Difficulty);
		}
		return num;
	}

	public static SpaceMapNode Get(string templateId)
	{
		if (string.IsNullOrEmpty(templateId))
		{
			return null;
		}
		foreach (SpaceMapNode node in SpaceMap.Current.Nodes)
		{
			if (node.NodeType == NodeType.Static && string.Equals(node.Data.Id, templateId, StringComparison.InvariantCulture))
			{
				return node;
			}
		}
		return null;
	}

	public static SpaceMapNode Get(long referenceId)
	{
		return Referencable.Find<SpaceMapNode>(referenceId);
	}

	public virtual MapNodeReference SerializeSave()
	{
		switch (NodeType)
		{
		case NodeType.None:
			throw new NotImplementedException("Cannot save a node with no type");
		case NodeType.Entry:
		case NodeType.Static:
		case NodeType.LowOrbitHub:
			return new StaticNodeReference(this);
		case NodeType.Generated:
			return new SiteNodeReference(this);
		case NodeType.LaunchPad:
		case NodeType.LowOrbitLaunchPad:
			return new LaunchPadNodeReference(this);
		default:
			throw new NotImplementedException("Unknown node type for serialization");
		}
	}

	public void Serialize(RocketBinaryWriter writer)
	{
		Write(writer);
	}

	public TreeString ToTreeWithChildren(TreeString parent = null)
	{
		TreeString treeString = new TreeString(string.Format("{0} #{1}", DebugName ?? "Unnamed", ReferenceId), parent);
		TreeString.Node(EnumCollections.NodeTypes.GetName(NodeType), treeString);
		if (!IsCharted)
		{
			TreeString.Variable("Uncharted", treeString);
		}
		if (DiscoverPoints > 0)
		{
			TreeString.Variable($"Discover: {DiscoverPoints}", treeString);
		}
		if (ChartPoints > 0)
		{
			TreeString.Variable($"Chart: {ChartPoints}", treeString);
		}
		if (SurveyPoints > 0)
		{
			TreeString.Variable($"Survey: {SurveyPoints}", treeString);
		}
		foreach (NodeConnection childConnection in ChildConnections)
		{
			childConnection.Child.ToTreeWithChildren(treeString);
		}
		return treeString;
	}

	public void OnMined()
	{
		_mineCount++;
		if ((float)_mineCount >= 4f)
		{
			SurveyPoints++;
			_mineCount = 0;
		}
	}
}
