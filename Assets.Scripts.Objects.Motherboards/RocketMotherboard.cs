using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Objects.Rockets;
using Objects.Rockets.UI;
using UnityEngine;

namespace Assets.Scripts.Objects.Motherboards;

public class RocketMotherboard : Motherboard, IRocketPanelHolder
{
	[NonSerialized]
	public readonly Dictionary<int, double> SelectedDeviceLogicValues = new Dictionary<int, double>();

	[SerializeField]
	private RocketMotherboardPanel _rocketMotherboardPanel;

	private List<RocketDataUpLink> _upLinks = new List<RocketDataUpLink>();

	private ConnectedRocketReference[] _rocketReferences;

	private ConnectedRocketInfo[] _connectedRockets = Array.Empty<ConnectedRocketInfo>();

	private int _selectedIndex;

	private readonly List<SpaceMapNode> _nodesToRemove = new List<SpaceMapNode>(16);

	public static SyncList<PinDeviceEvent> PinDeviceEvents = new SyncList<PinDeviceEvent>(PinDeviceEvent.Deserialize);

	public static SyncList<PinLogicValueEvent> PinLogicValueEvents = new SyncList<PinLogicValueEvent>(PinLogicValueEvent.Deserialize);

	private List<long> _pinnedDevices = new List<long>();

	private List<(long, LogicType)> _pinnedLogicValues = new List<(long, LogicType)>();

	public ConnectedRocketInfo[] ConnectedRockets
	{
		get
		{
			return _connectedRockets;
		}
		set
		{
			_connectedRockets = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 4096;
			}
		}
	}

	public int SelectedIndex
	{
		get
		{
			return _selectedIndex;
		}
		set
		{
			_selectedIndex = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 32768;
			}
		}
	}

	public override bool IsOperable => true;

	public override void OnAssignedReference()
	{
		InitLogicValuesLookup();
		base.OnAssignedReference();
	}

	public ILogicable SelectedLogicable()
	{
		return SelectedRocket()?.SelectedLogicable;
	}

	public void SetSelectedLogicable(ILogicable logicable)
	{
		SelectedRocket().SelectedLogicable = logicable;
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			base.NetworkUpdateFlags |= 4096;
		}
		FlushLogicValuesLookup();
	}

	private void InitLogicValuesLookup()
	{
		SelectedDeviceLogicValues.Clear();
		ushort[] valuesAsInts = EnumCollections.LogicTypes.ValuesAsInts;
		foreach (ushort key in valuesAsInts)
		{
			SelectedDeviceLogicValues.TryAdd(key, 0.0);
		}
	}

	private void FlushLogicValuesLookup()
	{
		for (int i = 0; i < EnumCollections.LogicTypes.Length; i++)
		{
			ushort key = EnumCollections.LogicTypes.ValuesAsInts[i];
			SelectedDeviceLogicValues[key] = 0.0;
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		RefreshSelectedDeviceValues();
	}

	private void RefreshSelectedDeviceValues()
	{
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			base.NetworkUpdateFlags |= 16384;
		}
		ILogicable logicable = SelectedLogicable();
		if (logicable != null)
		{
			for (int num = EnumCollections.LogicTypes.Length - 1; num >= 0; num--)
			{
				ushort num2 = EnumCollections.LogicTypes.ValuesAsInts[num];
				SelectedDeviceLogicValues[num2] = (logicable.CanLogicRead((LogicType)num2) ? logicable.GetLogicValue((LogicType)num2) : 0.0);
			}
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
			ConnectedRocketInfo[] connectedRockets = ConnectedRockets;
			for (int i = 0; i < connectedRockets.Length; i++)
			{
				connectedRockets[i].Write(writer);
				count++;
			}
			Network.WriteIndex(writer, count, bufferIndex);
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			writer.WriteByte((byte)_selectedIndex);
		}
		if (!Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			return;
		}
		ILogicable logicable = SelectedLogicable();
		Network.WriteIndex<byte>(writer, out var count2, out var bufferIndex2);
		if (logicable != null)
		{
			for (int num = EnumCollections.LogicTypes.Values.Length - 1; num >= 0; num--)
			{
				LogicType logicType = EnumCollections.LogicTypes.Values[num];
				if (logicable.CanLogicRead(logicType))
				{
					Network.WriteLogicValue(writer, logicType, logicable.GetLogicValue(logicType));
					count2++;
				}
			}
		}
		Network.WriteIndex(writer, count2, bufferIndex2);
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			Network.ReadIndex<byte>(reader, out var value);
			ConnectedRockets = new ConnectedRocketInfo[value];
			for (int i = 0; i < value; i++)
			{
				ConnectedRockets[i] = ConnectedRocketInfo.Create(reader);
			}
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			SelectedIndex = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			Network.ReadIndex<byte>(reader, out var value2);
			for (int j = 0; j < value2; j++)
			{
				Network.ReadLogicValue(reader, out var logicType, out var value3);
				SelectedDeviceLogicValues[(int)logicType] = value3;
			}
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)SelectedIndex);
		Network.WriteIndex<byte>(writer, out var count, out var bufferIndex);
		for (int i = 0; i < ConnectedRockets.Length; i++)
		{
			ConnectedRocketReference.Create(ConnectedRockets[i]).Write(writer);
			count++;
		}
		Network.WriteIndex(writer, count, bufferIndex);
		ILogicable logicable = SelectedLogicable();
		Network.WriteIndex<byte>(writer, out var count2, out bufferIndex);
		if (logicable != null)
		{
			for (int num = EnumCollections.LogicTypes.Length - 1; num >= 0; num--)
			{
				LogicType logicType = EnumCollections.LogicTypes.Values[num];
				if (logicable.CanLogicRead(logicType))
				{
					Network.WriteLogicValue(writer, logicType, logicable.GetLogicValue((LogicType)num));
					count2++;
				}
			}
		}
		Network.WriteIndex(writer, count2, bufferIndex);
		Network.WriteIndex<ushort>(writer, out var count3, out var bufferIndex2);
		foreach (long pinnedDevice in _pinnedDevices)
		{
			Network.WritePackedId(writer, pinnedDevice);
			count3++;
		}
		Network.WriteIndex(writer, count3, bufferIndex2);
		Network.WriteIndex<ushort>(writer, out var count4, out var bufferIndex3);
		foreach (var pinnedLogicValue in _pinnedLogicValues)
		{
			Network.WritePackedId(writer, pinnedLogicValue.Item1);
			writer.WriteUInt16((ushort)pinnedLogicValue.Item2);
			count4++;
		}
		Network.WriteIndex(writer, count4, bufferIndex3);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		SelectedIndex = reader.ReadByte();
		Network.ReadIndex<byte>(reader, out var value);
		_rocketReferences = new ConnectedRocketReference[value];
		for (int i = 0; i < value; i++)
		{
			_rocketReferences[i] = ConnectedRocketReference.Create(reader);
		}
		Network.ReadIndex<byte>(reader, out var value2);
		for (int j = 0; j < value2; j++)
		{
			Network.ReadLogicValue(reader, out var logicType, out var value3);
			SelectedDeviceLogicValues[(int)logicType] = value3;
		}
		Network.ReadIndex<ushort>(reader, out var value4);
		for (int k = 0; k < value4; k++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			_pinnedDevices.Add(referenceId);
		}
		Network.ReadIndex<ushort>(reader, out var value5);
		for (int l = 0; l < value5; l++)
		{
			Network.ReadPackedId(reader, out var referenceId2);
			ushort item = reader.ReadUInt16();
			_pinnedLogicValues.Add((referenceId2, (LogicType)item));
		}
	}

	public override void OnFinishJoin()
	{
		base.OnFinishJoin();
		ConnectedRockets = new ConnectedRocketInfo[_rocketReferences.Length];
		for (int i = 0; i < _rocketReferences.Length; i++)
		{
			ConnectedRockets[i] = new ConnectedRocketInfo(_rocketReferences[i]);
		}
	}

	public ConnectedRocketInfo SelectedRocket()
	{
		if (SelectedIndex < 0 || ConnectedRockets == null || ConnectedRockets.Length == 0)
		{
			return null;
		}
		if (SelectedIndex >= ConnectedRockets.Length)
		{
			return null;
		}
		return ConnectedRockets[SelectedIndex];
	}

	private bool GetConnectedDeviceFromCurrentRocket(long referenceId, out Device device)
	{
		foreach (Device device2 in SelectedRocket().DownLink.DataCableNetwork.DeviceList)
		{
			if (device2.ReferenceId == referenceId)
			{
				device = device2;
				return true;
			}
		}
		device = null;
		return false;
	}

	public void ToggleDevicePinState(long referenceId)
	{
		bool flag = _pinnedDevices.Contains(referenceId);
		PinDevice(!flag, referenceId);
	}

	public void ToggleLogicValuePinState(long referenceId, LogicType logicType)
	{
		bool flag = _pinnedLogicValues.Contains((referenceId, logicType));
		PinLogicValue(!flag, referenceId, logicType);
	}

	public void DeviceSelected(long referenceId)
	{
		Device device;
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new SelectDeviceMessage
			{
				DeviceRefId = referenceId,
				MotherboardRefId = base.ReferenceId
			});
		}
		else if (GetConnectedDeviceFromCurrentRocket(referenceId, out device))
		{
			SetSelectedLogicable(device);
		}
		else
		{
			SetSelectedLogicable(SelectedRocket().DownLink.DataCableNetwork.DeviceList[0]);
		}
	}

	public void LogicValueChanged(float newValue, LogicType logicType, long deviceReferenceId)
	{
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new SetLogicValueMessage
			{
				DeviceReferenceId = deviceReferenceId,
				LogicType = logicType,
				LogicValue = newValue
			});
		}
		else
		{
			Thing.Find<Device>(deviceReferenceId).SetLogicValue(logicType, newValue);
		}
	}

	public void RemoveSpaceMapNode(SpaceMapNode node)
	{
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new RemoveSpaceMapNodeMessage
			{
				NodeRefId = node.ReferenceId,
				MotherboardRefId = base.ReferenceId
			});
		}
		else
		{
			node.DeRegister();
		}
	}

	public void RemoveSpaceMapNodesOfType(SpaceMapNode node)
	{
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new RemoveSpaceMapNodesOfTypeMessage
			{
				NodeRefId = node.ReferenceId,
				MotherboardRefId = base.ReferenceId
			});
			return;
		}
		foreach (NodeConnection childConnection in node.ParentConnection.Parent.ChildConnections)
		{
			SpaceMapNode child = childConnection.Child;
			if (child.Data.Id == node.Data.Id)
			{
				_nodesToRemove.Add(child);
			}
		}
		foreach (SpaceMapNode item in _nodesToRemove)
		{
			item.DeRegister();
		}
		_nodesToRemove.Clear();
	}

	public void RocketSelected(int index)
	{
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new SelectRocketMessage
			{
				MotherboardRefId = base.ReferenceId,
				Index = (byte)index
			});
		}
		else
		{
			SelectedIndex = index;
		}
	}

	public void RocketSelected(ConnectedRocketInfo rocketInfo)
	{
		for (int i = 0; i < ConnectedRockets.Length; i++)
		{
			if (ConnectedRockets[i].Equals(rocketInfo))
			{
				RocketSelected(i);
				break;
			}
		}
	}

	public void OnAutoShutOffToggled(bool newState)
	{
		if ((bool)SelectedRocket()?.Avionics && SelectedRocket().Avionics.Rocket != null)
		{
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(new AutoShutOffRocketMessage
				{
					AvionicsId = SelectedRocket().Avionics.ReferenceId,
					NewState = newState
				});
			}
			else
			{
				SelectedRocket().Avionics.SetAutoShutOff(newState);
			}
		}
	}

	public void OnAutoLandToggled(bool newState)
	{
		if ((bool)SelectedRocket()?.Avionics && SelectedRocket().Avionics.Rocket != null)
		{
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(new AutoLandRocketMessage
				{
					AvionicsId = SelectedRocket().Avionics.ReferenceId,
					NewState = newState
				});
			}
			else
			{
				SelectedRocket().Avionics.SetAutoLand(newState);
			}
		}
	}

	public void AbandonRocket()
	{
		if ((bool)SelectedRocket().Avionics && SelectedRocket().Avionics.Rocket != null)
		{
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(new AbandonRocketMessage
				{
					RocketId = SelectedRocket().Avionics.Rocket.ReferenceId
				});
			}
			else
			{
				SelectedRocket().Avionics.Rocket.AbandonRocket();
			}
		}
	}

	public void OnLandingProfileSelected(int index)
	{
		if ((bool)SelectedRocket()?.Avionics && SelectedRocket().Avionics.Rocket != null)
		{
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(new SetReEntryProfileMessage
				{
					AvionicsId = SelectedRocket().Avionics.ReferenceId,
					ReEntryProfile = (ReEntryProfile)(index + 1)
				});
			}
			else
			{
				SelectedRocket().Avionics.SetReEntryProfile((ReEntryProfile)(index + 1));
			}
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new RocketMotherboardSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (!(savedData is RocketMotherboardSaveData rocketMotherboardSaveData))
		{
			return;
		}
		foreach (long pinnedDevice in rocketMotherboardSaveData.PinnedDevices)
		{
			_pinnedDevices.Add(pinnedDevice);
		}
		for (int i = 0; i < rocketMotherboardSaveData.PinnedLogicValueDevice.Count; i++)
		{
			long item = rocketMotherboardSaveData.PinnedLogicValueDevice[i];
			byte item2 = rocketMotherboardSaveData.PinnedLogicValueType[i];
			_pinnedLogicValues.Add((item, (LogicType)item2));
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (!(savedData is RocketMotherboardSaveData rocketMotherboardSaveData))
		{
			return;
		}
		rocketMotherboardSaveData.PinnedDevices = new List<long>();
		rocketMotherboardSaveData.PinnedLogicValueDevice = new List<long>();
		rocketMotherboardSaveData.PinnedLogicValueType = new List<byte>();
		foreach (long pinnedDevice in _pinnedDevices)
		{
			rocketMotherboardSaveData.PinnedDevices.Add(pinnedDevice);
		}
		foreach (var pinnedLogicValue in _pinnedLogicValues)
		{
			rocketMotherboardSaveData.PinnedLogicValueDevice.Add(pinnedLogicValue.Item1);
			rocketMotherboardSaveData.PinnedLogicValueType.Add((byte)pinnedLogicValue.Item2);
		}
	}

	private bool TryGetAvionics(RocketDataDownLink downLink, out RocketAvionicsDevice avionics)
	{
		avionics = null;
		if (!downLink)
		{
			return false;
		}
		if (downLink.DataCableNetwork == null)
		{
			downLink.FindDataCable();
			if (downLink.DataCableNetwork == null)
			{
				return false;
			}
		}
		return TryGetDevice<RocketAvionicsDevice>(downLink.DataCableNetwork.DeviceList, out avionics);
	}

	private bool TryGetDownLink(RocketDataUpLink upLink, out RocketDataDownLink dataDownLink)
	{
		if (upLink.ConnectedDataNetTransmitter is RocketDataDownLink rocketDataDownLink)
		{
			dataDownLink = rocketDataDownLink;
			return true;
		}
		dataDownLink = null;
		return false;
	}

	private bool TryGetDevice<T>(List<Device> devices, out T device) where T : Device
	{
		foreach (Device device2 in devices)
		{
			if (device2 is T val)
			{
				device = val;
				return true;
			}
		}
		device = null;
		return false;
	}

	public void RefreshConnectedRockets()
	{
		if (!GameManager.IsRunning || !GameManager.RunSimulation)
		{
			return;
		}
		List<RocketDataDownLink> list = new List<RocketDataDownLink>();
		for (int num = _upLinks.Count - 1; num >= 0; num--)
		{
			RocketDataUpLink rocketDataUpLink = _upLinks[num];
			if ((bool)rocketDataUpLink && !rocketDataUpLink.IsBeingDestroyed && rocketDataUpLink.GetIsOperable() && TryGetDownLink(rocketDataUpLink, out var dataDownLink) && TryGetAvionics(dataDownLink, out var avionics) && avionics.Rocket != null && !avionics.Rocket.BeingDestroyed)
			{
				list.Add(dataDownLink);
			}
		}
		List<ConnectedRocketInfo> list2 = new List<ConnectedRocketInfo>();
		foreach (RocketDataDownLink item in list)
		{
			TryGetAvionics(item, out var avionics2);
			list2.Add(new ConnectedRocketInfo(avionics2, item, item.DataCableNetwork.DeviceList[0]));
		}
		list2.Sort((ConnectedRocketInfo a, ConnectedRocketInfo b) => a.Avionics.Rocket.ReferenceId.CompareTo(b.Avionics.Rocket.ReferenceId));
		ConnectedRockets = list2.ToArray();
	}

	public override void Update1000MS(float deltaTime)
	{
		base.Update1000MS(deltaTime);
		RefreshScreen();
	}

	public override void RefreshScreen()
	{
		base.RefreshScreen();
		if (ConnectedRockets != null && ConnectedRockets.Length > SelectedIndex)
		{
			_rocketMotherboardPanel.SetConnectedRocketInfo(ConnectedRockets[SelectedIndex].Avionics);
		}
		else
		{
			_rocketMotherboardPanel.SetConnectedRocketInfo(null);
		}
	}

	public override void Awake()
	{
		base.Awake();
		_rocketMotherboardPanel.Initialize(this);
	}

	public void ToggleUI()
	{
		RocketCanvas.Instance.Show(this);
	}

	public override void RefreshDevice(Device device)
	{
		base.RefreshDevice(device);
		OnDeviceListChanged();
	}

	public override void OnInsertedToComputer(IComputer computer)
	{
		base.OnInsertedToComputer(computer);
		OnDeviceListChanged();
	}

	public override void OnDeviceListChanged()
	{
		if (GameManager.GameState == GameState.Running)
		{
			RebuildDeviceList();
			RefreshConnectedRockets();
		}
		base.OnDeviceListChanged();
	}

	private void RebuildDeviceList()
	{
		if (ParentComputer == null || !ParentComputer.AsThing().isActiveAndEnabled)
		{
			return;
		}
		List<ILogicable> list = ParentComputer.DeviceList();
		_upLinks.Clear();
		foreach (ILogicable item2 in list)
		{
			if (item2 is RocketDataUpLink item && !_upLinks.Contains(item))
			{
				_upLinks.Add(item);
			}
		}
	}

	public new static void ClearAll()
	{
		PinDeviceEvents.Clear();
		PinLogicValueEvents.Clear();
	}

	public bool IsDevicePinned(Device device)
	{
		return _pinnedDevices.Contains(device.ReferenceId);
	}

	public bool IsLogicValuePinned(ILogicable device, LogicType logicType)
	{
		return _pinnedLogicValues.Contains((device.ReferenceId, logicType));
	}

	public void PinDeviceClient(bool pinned, long deviceReferenceId)
	{
		if (pinned)
		{
			if (!_pinnedDevices.Contains(deviceReferenceId))
			{
				_pinnedDevices.Add(deviceReferenceId);
			}
		}
		else
		{
			_pinnedDevices.Remove(deviceReferenceId);
		}
	}

	public void PinLogicValueClient(bool pinned, long deviceReferenceId, LogicType logicType)
	{
		if (pinned)
		{
			if (!_pinnedLogicValues.Contains((deviceReferenceId, logicType)))
			{
				_pinnedLogicValues.Add((deviceReferenceId, logicType));
			}
		}
		else
		{
			_pinnedLogicValues.Remove((deviceReferenceId, logicType));
		}
	}

	public void PinDevice(bool pinned, long deviceReferenceId)
	{
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new PinDeviceMessage
			{
				Pinned = pinned,
				DeviceRefId = deviceReferenceId,
				MotherboardRefId = base.ReferenceId
			});
		}
		else
		{
			PinDeviceClient(pinned, deviceReferenceId);
			PinDeviceEvents.Add(new PinDeviceEvent(pinned, this, deviceReferenceId));
		}
	}

	public void PinLogicValue(bool pinned, long deviceReferenceId, LogicType logicType)
	{
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new PinLogicValueMessage
			{
				Pinned = pinned,
				DeviceRefId = deviceReferenceId,
				MotherboardRefId = base.ReferenceId,
				LogicType = logicType
			});
		}
		else
		{
			PinLogicValueClient(pinned, deviceReferenceId, logicType);
			PinLogicValueEvents.Add(new PinLogicValueEvent(pinned, this, deviceReferenceId, logicType));
		}
	}
}
