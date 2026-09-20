using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using Objects.Rockets.UI;
using UnityEngine;

namespace Objects.Rockets;

public class SpaceMap
{
	public const string FALLBACK_SPACEMAP_ID = "FallBack";

	public readonly SpaceMapData Data;

	public SpaceMapNode EntryNode;

	public SpaceMapNode LowOrbitHubNode;

	public List<SpaceMapNode> Nodes = new List<SpaceMapNode>();

	public static SpaceMap Current;

	public const int OrbitHubDistance = 200;

	public int IdHash => Data.IdHash;

	public string Id => Data.Id;

	public string DisplayName => Id;

	public bool BeingDestroyed { get; set; }

	public float DistanceToOrbit { get; }

	public void PrintDebugInfo(bool verbose = false)
	{
	}

	public void AddNode(SpaceMapNode node)
	{
		if (!Nodes.Contains(node))
		{
			Nodes.Add(node);
		}
	}

	public void RemoveNode(SpaceMapNode node)
	{
		Nodes.Remove(node);
	}

	public static void BuildSpaceMap(string startingMapId)
	{
		if (GameManager.GameState != GameState.None)
		{
			BuildSpaceMap(DataCollection.Get<SpaceMapData>(startingMapId) ?? DataCollection.Get<SpaceMapData>("FallBack"));
		}
	}

	public static void BuildSpaceMap(SpaceMapData startingMapData)
	{
		if (startingMapData == null)
		{
			startingMapData = DataCollection.Get<SpaceMapData>("FallBack");
		}
		if (!startingMapData.IsValid())
		{
			startingMapData = DataCollection.Get<SpaceMapData>(startingMapData.Id);
		}
		if (GameManager.GameState != GameState.None)
		{
			Current = Create(startingMapData);
			Current.GenerateMap(startingMapData);
		}
	}

	public static void BuildSpaceMapWithSavedReferenceIds(SpaceMapData startingMapData, SpaceMapSaveData saveData)
	{
		if (startingMapData == null || saveData == null)
		{
			BuildSpaceMap(startingMapData);
			return;
		}
		if (!startingMapData.IsValid())
		{
			startingMapData = DataCollection.Get<SpaceMapData>(startingMapData.Id);
		}
		Current = Create(startingMapData);
		Current.GenerateMap(startingMapData, saveData);
	}

	public static void TestAllPaths()
	{
		ConsoleWindow.PrintAction("Testing All Paths");
		ConsoleWindow.Print("Testing map " + Current.DisplayName);
		foreach (SpaceMapNode node in Current.Nodes)
		{
			foreach (SpaceMapNode node2 in Current.Nodes)
			{
				if (node2 == node)
				{
					continue;
				}
				ConsoleWindow.Print("Path from " + node.DisplayName + " to " + node2.DisplayName);
				if (!SpaceMapPathFinder.GetPath(node, node2, out var path))
				{
					ConsoleWindow.PrintError("ERROR: No Path Found");
					continue;
				}
				StringBuilder stringBuilder = new StringBuilder(node.DisplayName);
				for (int num = path.Count - 1; num >= 0; num--)
				{
					NodeTransit nodeTransit = path[num];
					stringBuilder.Append($" -> {nodeTransit.Destination}");
				}
				ConsoleWindow.Print(stringBuilder.ToString());
			}
		}
	}

	public static SpaceMap Create(SpaceMapData data)
	{
		return new SpaceMap(data);
	}

	public static SpaceMap Create(RocketBinaryReader reader)
	{
		return new SpaceMap(reader);
	}

	private SpaceMap(SpaceMapData spaceMapData)
	{
		Data = spaceMapData;
		DistanceToOrbit = spaceMapData.DistanceToOrbit;
	}

	private SpaceMap(RocketBinaryReader reader)
	{
		string b = reader.ReadString();
		if (!string.Equals(WorldSetting.Current.SpaceMapData.Id, b, StringComparison.InvariantCulture))
		{
			throw new Exception("Error! SpaceMapData of worldSetting " + WorldSetting.Current.Id + " does not match host.");
		}
		Data = WorldSetting.Current.SpaceMapData;
		Current = this;
		DistanceToOrbit = reader.ReadSingle();
		EntryNode = SpaceMapNode.Create(reader);
	}

	public void GenerateMap(SpaceMapData data, SpaceMapSaveData saveData = null)
	{
		EntryNode = null;
		Nodes.Clear();
		if (data?.EntryNodeData == null)
		{
			if (data == null)
			{
				throw new Exception("SpaceMap Loading failed! Unable to find SpaceMapData");
			}
			throw new Exception("SpaceMap Loading failed! Space Map " + data.Id + " does not have an entry node");
		}
		long referenceId = 0L;
		saveData?.GetSavedStaticNodeReferenceId(data.EntryNodeData.Id, out referenceId);
		EntryNode = new SpaceMapNode(data.EntryNodeData, SpaceMapCode.Create(data.EntryNodeData.Code), NodeType.Entry, referenceId);
		CreateLowOrbitHub(data);
		BuildNodeBranch(data.EntryNodeData, EntryNode, saveData);
	}

	private void CreateLowOrbitHub(SpaceMapData data)
	{
		if (data?.LowOrbitHubNodeData != null)
		{
			LowOrbitHubNode = new SpaceMapNode(data.LowOrbitHubNodeData, SpaceMapCode.Create(123uL), NodeType.LowOrbitHub, 0L);
			NodeConnection nodeConnection = new NodeConnection(LowOrbitHubNode, EntryNode, 1f, ConnectionType.Standard);
			EntryNode.ChildConnections.Add(nodeConnection);
			LowOrbitHubNode.ParentConnection = nodeConnection;
		}
	}

	private void BuildNodeBranch(SpaceMapNodeData currentNodeData, SpaceMapNode parentNode, SpaceMapSaveData saveData)
	{
		foreach (NodeConnectionData child in currentNodeData.Children)
		{
			long referenceId = 0L;
			if (child?.ConnectedNodeData != null)
			{
				saveData?.GetSavedStaticNodeReferenceId(child.ConnectedNodeData.Id, out referenceId);
				SpaceMapNode spaceMapNode = new SpaceMapNode(code: new SpaceMapCode(parentNode, NodeType.Static), data: child.ConnectedNodeData, type: NodeType.Static, referenceId: referenceId);
				float num = Vector2.Distance(new Vector2(parentNode.MapDisplayData.X, parentNode.MapDisplayData.Y), new Vector2(spaceMapNode.MapDisplayData.X, spaceMapNode.MapDisplayData.Y));
				num *= child.DistanceMultiplier;
				NodeConnection nodeConnection = new NodeConnection(spaceMapNode, parentNode, num, ConnectionType.Standard, child.ChartDifficulty);
				parentNode.ChildConnections.Add(nodeConnection);
				spaceMapNode.ParentConnection = nodeConnection;
				BuildNodeBranch(child.ConnectedNodeData, spaceMapNode, saveData);
			}
		}
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		if (Current == null)
		{
			throw new NullReferenceException("space map cannot be serialized as current is null");
		}
		Current.Write(writer);
	}

	public static async UniTask DeserializeOnJoin(RocketBinaryReader reader)
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenDeserializingSpaceMap.DisplayString);
		Create(reader);
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteString(Current.Id);
		writer.WriteSingle(DistanceToOrbit);
		Current.EntryNode.Write(writer);
	}

	public static void SerializeDeltaState(RocketBinaryWriter writer)
	{
		if (Current == null)
		{
			throw new NullReferenceException("space map cannot be serialized as current is null");
		}
		Network.WriteIndex<ushort>(writer, out var count, out var bufferIndex);
		foreach (SpaceMapNode node in Current.Nodes)
		{
			if (node.SerializeDeltaState(writer))
			{
				count++;
			}
		}
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static void DeserializeDeltaState(RocketBinaryReader reader)
	{
		Network.ReadIndex<ushort>(reader, out var value);
		for (int i = 0; i < value; i++)
		{
			SpaceMapNode.DeSerializeDeltaState(reader);
		}
	}

	public static void ClearAll()
	{
		Current = null;
		SpaceMapNode.ClearAll();
	}

	public static void RefreshMap(string startingMapId)
	{
		ConsoleWindow.PrintAction("Refreshing '" + startingMapId + "' spaceMap");
		List<Tuple<LaunchMount, Rocket>> list = new List<Tuple<LaunchMount, Rocket>>();
		Dictionary<ulong, SpaceMapNode> dictionary = new Dictionary<ulong, SpaceMapNode>();
		foreach (SpaceMapNode allSpaceMapNode in SpaceMapNode.AllSpaceMapNodes)
		{
			dictionary.Add(allSpaceMapNode.Code.Value, allSpaceMapNode);
			if (allSpaceMapNode.Owner is LaunchMount item)
			{
				Rocket item2 = ((allSpaceMapNode.RocketsHere.Count > 0) ? allSpaceMapNode.RocketsHere[0] : null);
				list.Add(new Tuple<LaunchMount, Rocket>(item, item2));
			}
		}
		List<Tuple<Rocket, LaunchMount>> list2 = new List<Tuple<Rocket, LaunchMount>>();
		List<Tuple<Rocket, int, int>> list3 = new List<Tuple<Rocket, int, int>>();
		foreach (Rocket allRocket in Rocket.AllRockets)
		{
			NodeType? nodeType = allRocket?.TargetNode?.NodeType;
			if (nodeType.HasValue && nodeType == NodeType.LaunchPad)
			{
				list2.Add(new Tuple<Rocket, LaunchMount>(allRocket, allRocket.TargetNode.Owner as LaunchMount));
			}
			nodeType = allRocket?.CurrentNode?.NodeType;
			bool flag;
			if (nodeType.HasValue)
			{
				NodeType valueOrDefault = nodeType.GetValueOrDefault();
				if ((uint)(valueOrDefault - 1) <= 1u)
				{
					flag = true;
					goto IL_0186;
				}
			}
			flag = false;
			goto IL_0186;
			IL_01ef:
			int item3 = (flag ? allRocket.TargetNode.IdHash : 0);
			int item4;
			list3.Add(new Tuple<Rocket, int, int>(allRocket, item4, item3));
			if (allRocket != null)
			{
				allRocket.RocketMode = RocketMode.None;
			}
			continue;
			IL_0186:
			item4 = (flag ? allRocket.CurrentNode.IdHash : 0);
			nodeType = allRocket?.TargetNode?.NodeType;
			if (nodeType.HasValue)
			{
				NodeType valueOrDefault = nodeType.GetValueOrDefault();
				if ((uint)(valueOrDefault - 1) <= 1u)
				{
					flag = true;
					goto IL_01ef;
				}
			}
			flag = false;
			goto IL_01ef;
		}
		SpaceMap current3 = Current;
		for (int num = current3.Nodes.Count - 1; num >= 0; num--)
		{
			current3.Nodes[num].DeRegister();
		}
		ClearAll();
		BuildSpaceMap(startingMapId);
		foreach (KeyValuePair<ulong, SpaceMapNode> item9 in dictionary)
		{
			item9.Deconstruct(out var key, out var value);
			ulong code = key;
			SpaceMapNode spaceMapNode = value;
			NodeType valueOrDefault = spaceMapNode.NodeType;
			if ((valueOrDefault == NodeType.Entry || valueOrDefault == NodeType.Static) && spaceMapNode.IsCharted)
			{
				SpaceMapNode spaceMapNode2 = SpaceMapCode.Get(code);
				if (spaceMapNode2 != null)
				{
					spaceMapNode2.IsCharted = true;
				}
			}
			Referencable.Deregister(spaceMapNode);
		}
		Rocket item5;
		foreach (Tuple<Rocket, int, int> item10 in list3)
		{
			item10.Deconstruct(out item5, out var item6, out var item7);
			Rocket rocket = item5;
			int num2 = item6;
			int num3 = item7;
			foreach (SpaceMapNode allSpaceMapNode2 in SpaceMapNode.AllSpaceMapNodes)
			{
				if (num2 == allSpaceMapNode2.IdHash)
				{
					rocket.SetCurrentNode(allSpaceMapNode2);
				}
				if (num3 == allSpaceMapNode2.IdHash)
				{
					rocket.ChangeTarget(allSpaceMapNode2);
				}
			}
		}
		LaunchMount item8;
		foreach (Tuple<LaunchMount, Rocket> item11 in list)
		{
			item11.Deconstruct(out item8, out item5);
			LaunchMount launchMount = item8;
			Rocket rocket2 = item5;
			launchMount.SpaceMapNode = SpaceMapNode.Create(launchMount);
			rocket2?.SetCurrentNode(launchMount.SpaceMapNode);
		}
		foreach (Tuple<Rocket, LaunchMount> item12 in list2)
		{
			item12.Deconstruct(out item5, out item8);
			Rocket rocket3 = item5;
			LaunchMount launchMount2 = item8;
			rocket3.TargetNode = launchMount2.SpaceMapNode;
			if (SpaceMapPathFinder.GetNextConnection(rocket3.CurrentNode, rocket3.TargetNode, out var next, out var overallPath))
			{
				rocket3.CurrentTransit = next;
				rocket3.AllPaths = overallPath;
			}
		}
		foreach (Rocket allRocket2 in Rocket.AllRockets)
		{
			if (allRocket2.CurrentNode == null || SpaceMapCode.Get(allRocket2.CurrentNode.Code.Value) == null)
			{
				RocketState rocketState = allRocket2.RocketState;
				if ((uint)rocketState > 1u && (uint)(rocketState - 2) <= 2u)
				{
					allRocket2.SetCurrentNode(Current.EntryNode);
					allRocket2.Reset();
					allRocket2.RocketState = RocketState.InSpace;
				}
				else
				{
					allRocket2.Reset();
					allRocket2.RocketNetwork.RefreshRocket();
				}
			}
		}
		RocketCanvas.Instance.ClearAll();
		ConsoleWindow.Print("Success");
	}

	public static void LoadGameSave(WorldSetting worldSetting, SpaceMapSaveData spaceMapData)
	{
		spaceMapData?.OnDataLoad();
		BuildSpaceMapWithSavedReferenceIds(worldSetting.SpaceMapData, spaceMapData);
		ApplySpaceMap(spaceMapData);
	}

	private static void ApplySpaceMap(SpaceMapSaveData spaceMapData)
	{
		if (spaceMapData?.Nodes == null)
		{
			return;
		}
		foreach (MapNodeReference node in spaceMapData.Nodes)
		{
			node.Deserialize();
		}
	}

	public static void CleanUpOnFinishedLoad()
	{
		for (int num = Current.Nodes.Count - 1; num >= 0; num--)
		{
			SpaceMapNode spaceMapNode = Current.Nodes[num];
			if (spaceMapNode == null)
			{
				Current.Nodes.RemoveAt(num);
			}
			else if (!spaceMapNode.IsValid())
			{
				spaceMapNode.DeRegister();
			}
		}
	}

	public SpaceMapSaveData SerializeSave()
	{
		return new SpaceMapSaveData(this);
	}
}
