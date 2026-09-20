using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Trading;
using UI.ImGuiUi;
using UnityEngine;

namespace Assets.Scripts.Networks;

public class CableNetwork : IReferencable, IEvaluable, ILogicable, ISyncListable, IDensePoolable, IMemoryReadable, IMemory
{
	private class CableNetworkStrings
	{
		public const string CableNetwork = "CableNetwork ->";

		public const string NetworkId = "ReferenceId: ";

		public const string Potential = "Potential: ";

		public const string Required = "Required: ";

		public const string PowerProviders = "PowerProviders: ";
	}

	private const int MAX_CABLE_NETWORKS = 4096;

	public static readonly ConcurrentDensePool<CableNetwork> AllCableNetworks = new ConcurrentDensePool<CableNetwork>("AllCableNetworks", 4096);

	public static bool InUseWithEvent = false;

	public static Transform NetworkParent;

	public readonly List<Cable> CableList = new List<Cable>();

	public readonly List<CableFuse> FuseList = new List<CableFuse>();

	public readonly List<Battery> BatteryList = new List<Battery>();

	[Obsolete("DeviceRegister dictionary should be replaced with a more efficient data structure")]
	public Dictionary<Device, HashSet<Cable>> DeviceRegister = new Dictionary<Device, HashSet<Cable>>();

	public float ShortfallLoad;

	public float RequiredLoad;

	public float CurrentLoad;

	public float PotentialLoad;

	public float DuringTickLoad;

	private float _lastSentRequiredLoad = float.NaN;

	private float _lastSentCurrentLoad = float.NaN;

	private float _lastSentPotentialLoad = float.NaN;

	public CableNetworkType CableNetworkType;

	private readonly DensePoolReference<CableNetwork> _densePoolReference = new DensePoolReference<CableNetwork>(AllCableNetworks);

	public readonly PowerTick PowerTick = new PowerTick();

	public readonly List<Device> DeviceList = new List<Device>();

	protected readonly List<Device> _dataDeviceList = new List<Device>();

	protected readonly List<Device> _powerDeviceList = new List<Device>();

	protected bool PowerDeviceListDirty;

	protected bool DataDeviceListDirty;

	private static List<long> _allNetworkIds;

	private static readonly Action<CableNetwork> AllNetworkIdAction = delegate(CableNetwork network)
	{
		if (network.IsNetworkValid())
		{
			_allNetworkIds.Add(network.ReferenceId);
		}
	};

	private static readonly Func<RocketBinaryWriter, CableNetwork, bool> WriteNetworkAction = delegate(RocketBinaryWriter writer, CableNetwork network)
	{
		if (network == null)
		{
			return false;
		}
		Network.WritePackedId(writer, network);
		writer.WriteByte((byte)network.CableNetworkType);
		return true;
	};

	public static SyncList<CableNetwork> NewToSend = new SyncList<CableNetwork>(DeserializeNew);

	private double[] _channels = new double[8]
	{
		double.NaN,
		double.NaN,
		double.NaN,
		double.NaN,
		double.NaN,
		double.NaN,
		double.NaN,
		double.NaN
	};

	private static int _baseIndex = 165;

	public string DisplayName => "CableNetwork: " + StringManager.Get(ReferenceId);

	public ushort NetworkUpdateFlags { get; set; }

	public long ReferenceId { get; set; }

	public bool BeingDestroyed { get; set; }

	public bool HasAnySlots => false;

	public float EstimatedRemainingLoad => PotentialLoad - CurrentLoad - DuringTickLoad;

	public List<Device> DataDeviceList
	{
		get
		{
			if (PowerDeviceListDirty)
			{
				RefreshPowerAndDataDeviceLists();
			}
			return _dataDeviceList;
		}
	}

	public List<Device> PowerDeviceList
	{
		get
		{
			if (PowerDeviceListDirty || DataDeviceListDirty)
			{
				RefreshPowerAndDataDeviceLists();
			}
			return _powerDeviceList;
		}
	}

	public int TotalSlots => 0;

	public Thing GetAsThing
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public bool IsLoadDirty()
	{
		if (CurrentLoad == _lastSentCurrentLoad && PotentialLoad == _lastSentPotentialLoad)
		{
			return RequiredLoad != _lastSentRequiredLoad;
		}
		return true;
	}

	public void MarkLoadSent()
	{
		_lastSentCurrentLoad = CurrentLoad;
		_lastSentPotentialLoad = PotentialLoad;
		_lastSentRequiredLoad = RequiredLoad;
	}

	public bool OnAddToPool(object densePool, int slot)
	{
		if (_densePoolReference.CanAddToPool(densePool))
		{
			return _densePoolReference.AddToPool(densePool, slot);
		}
		return false;
	}

	public void OnRemoveFromPool(object densePool)
	{
		_densePoolReference.OnRemovedFrom(densePool);
	}

	public bool IsNetworkUpdateRequired(uint toCheck)
	{
		return true;
	}

	public void OnAssignedReference()
	{
		HelperHintsManager.Register(this);
	}

	public virtual void PrintDebugInfo(bool verbose = false)
	{
		throw new NotImplementedException();
	}

	public int GetNameHash()
	{
		return 0;
	}

	protected virtual void RefreshPowerAndDataDeviceLists()
	{
		if (DataDeviceListDirty)
		{
			_dataDeviceList.Clear();
		}
		if (PowerDeviceListDirty)
		{
			_powerDeviceList.Clear();
		}
		for (int num = DeviceList.Count - 1; num >= 0; num--)
		{
			Device device = DeviceList[num];
			if (DataDeviceListDirty)
			{
				HandleDataNetTransmissionDevice(device);
				Cable[] dataCables = device.DataCables;
				for (int i = 0; i < dataCables.Length; i++)
				{
					if (dataCables[i].CableNetwork == this)
					{
						_dataDeviceList.Add(device);
						break;
					}
				}
			}
			if (PowerDeviceListDirty)
			{
				Cable[] dataCables = device.PowerCables;
				for (int i = 0; i < dataCables.Length; i++)
				{
					if (dataCables[i].CableNetwork == this)
					{
						_powerDeviceList.Add(device);
						break;
					}
				}
			}
		}
		PowerDeviceListDirty = false;
		DataDeviceListDirty = false;
	}

	private void HandleDataNetTransmissionDevice(Device device)
	{
		if (device is IReceiveDataNetworkDevices receiveDataNetworkDevices && receiveDataNetworkDevices.DataConnectionActive() && receiveDataNetworkDevices.ConnectedDataNetTransmitter?.DataCableNetwork != null && receiveDataNetworkDevices.ConnectedDataNetTransmitter.DataCableNetwork != this)
		{
			List<Device> deviceList = receiveDataNetworkDevices.ConnectedDataNetTransmitter.DataCableNetwork.DeviceList;
			for (int num = deviceList.Count - 1; num >= 0; num--)
			{
				if (!_dataDeviceList.Contains(deviceList[num]))
				{
					_dataDeviceList.Add(deviceList[num]);
				}
			}
		}
		if (!(device is ITransmitDataNetworkDevices transmitDataNetworkDevices) || !transmitDataNetworkDevices.DataConnectionActive())
		{
			return;
		}
		for (int num2 = transmitDataNetworkDevices.ConnectedDataNetReceivers.Count - 1; num2 >= 0; num2--)
		{
			IReceiveDataNetworkDevices receiveDataNetworkDevices2 = transmitDataNetworkDevices.ConnectedDataNetReceivers[num2];
			if (receiveDataNetworkDevices2?.DataCableNetwork != null && receiveDataNetworkDevices2.DataCableNetwork != this)
			{
				receiveDataNetworkDevices2.DataCableNetwork.DirtyDataDeviceList();
			}
		}
	}

	public void DirtyPowerAndDataDeviceLists()
	{
		PowerDeviceListDirty = true;
		DataDeviceListDirty = true;
	}

	public void DirtyDataDeviceList()
	{
		DataDeviceListDirty = true;
	}

	public virtual void OnPowerTick()
	{
		if (DeviceList.Count != 0)
		{
			PowerTick.Initialise(this);
			PowerTick.CalculateState();
			PowerTick.ApplyState();
			DuringTickLoad = 0f;
			RequiredLoad = PowerTick.Required;
			CurrentLoad = PowerTick.Consumed;
			PotentialLoad = PowerTick.Potential;
			ShortfallLoad = ((PowerTick.Required > PowerTick.Potential) ? (PowerTick.Required - PowerTick.Potential) : 0f);
		}
	}

	public static long[] GetAllNetworkIds()
	{
		_allNetworkIds = new List<long>(AllCableNetworks.ActiveCount);
		AllCableNetworks.ForEach(AllNetworkIdAction);
		return _allNetworkIds.ToArray();
	}

	public bool IsNetworkDevice(Device device)
	{
		return DeviceList.Contains(device);
	}

	public static void AssignReference(CableNetwork network, long referenceId = 0L)
	{
		if (referenceId == 0L)
		{
			Referencable.RegisterNew(network);
		}
		else
		{
			Referencable.RegisterAs(network, referenceId);
		}
		AllCableNetworks.Add(network);
		if (NetworkManager.IsServer && NetworkBase.Clients.Count > 0)
		{
			NewToSend.Add(network);
		}
	}

	public static void ClearAll()
	{
		AllCableNetworks.Clear();
		NewToSend.Clear();
	}

	public void OnDeviceNameChanged(Device device)
	{
		foreach (Device device2 in DeviceList)
		{
			device2.OnNetworkedDeviceNameChanged(device);
		}
	}

	public void RequestDeviceRefresh(Device device)
	{
		foreach (Device device2 in DeviceList)
		{
			device2.OnNetworkedRefresh(device);
		}
	}

	public CableNetwork()
	{
		AssignReference(this, 0L);
		CableNetworkType = CableNetworkType.CableNetwork;
	}

	public CableNetwork(long cableNetworkId)
	{
		AssignReference(this, cableNetworkId);
		CableNetworkType = CableNetworkType.CableNetwork;
	}

	public CableNetwork(Cable cable)
	{
		AssignReference(this, 0L);
		CableNetworkType = CableNetworkType.CableNetwork;
		Add(cable);
	}

	public void OnImGuiDraw()
	{
		if (CableList.Count == 0)
		{
			return;
		}
		Vector3 position = Vector3.zero;
		float num = float.PositiveInfinity;
		foreach (Cable cable in CableList)
		{
			float num2 = Vector3.Distance(InventoryManager.ParentHuman.ThingTransformPosition, cable.transform.position);
			if (num2 < num)
			{
				position = cable.transform.position;
				num = num2;
			}
			ImGuiExtensions.Rendering.RenderingColor = ImGuiExtensions.GetThingColour(cable);
			cable.OnImGuiDraw();
		}
		ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.White;
		ImGuiExtensions.Rendering.DrawTextInWorld(delegate
		{
			ImguiHelper.Text("CableNetwork ->");
			ImguiHelper.Text("ReferenceId: ");
			ImguiHelper.SameLine();
			ImguiHelper.Text(StringManager.Get(ReferenceId));
			ImguiHelper.Text("Potential: ");
			ImguiHelper.SameLine();
			ImguiHelper.Text(StringManager.Get(PotentialLoad));
			ImguiHelper.Text("Required: ");
			ImguiHelper.SameLine();
			ImguiHelper.Text(StringManager.Get(RequiredLoad));
			if (PowerTick != null && PowerTick.Providers != null && PowerTick.Providers.Length != 0)
			{
				ImguiHelper.Text("PowerProviders: ");
				PowerProvider[] providers = PowerTick.Providers;
				foreach (PowerProvider powerProvider in providers)
				{
					if (powerProvider != null && !(powerProvider.Device == null))
					{
						ImguiHelper.SameLine();
						ImguiHelper.Text(StringManager.GetString(powerProvider.Device.DisplayName));
					}
				}
			}
		}, position, (int)ReferenceId);
	}

	public HashSet<Cable> GetDeviceRegistration(Device device)
	{
		DeviceRegister.TryGetValue(device, out var value);
		return value;
	}

	public void AddDevice(Cable cable, Device device)
	{
		HashSet<Cable> deviceRegistration = GetDeviceRegistration(device);
		if (deviceRegistration == null)
		{
			deviceRegistration = new HashSet<Cable> { cable };
			lock (DeviceList)
			{
				DeviceList.Add(device);
			}
			DeviceRegister.Add(device, deviceRegistration);
			Battery battery = device as Battery;
			if (battery != null)
			{
				BatteryList.Add(battery);
			}
			device.ConnectedCableNetworks.Add(this);
			RefreshNetworkDevice(device);
		}
		else if (!deviceRegistration.Contains(cable))
		{
			deviceRegistration.Add(cable);
			DeviceRegister[device] = deviceRegistration;
			RefreshNetworkDevice(device);
		}
		DirtyPowerAndDataDeviceLists();
	}

	public virtual void AddDevice(Device device)
	{
		DirtyPowerAndDataDeviceLists();
	}

	public void RefreshNetworkDevice(Device device)
	{
		device.OnAddCableNetwork(this);
		foreach (Device device2 in DeviceList)
		{
			if (!(device2 == device))
			{
				device2.OnDeviceConnectToNetwork(device);
			}
		}
	}

	public virtual void RemoveDevice(Device device)
	{
		DeviceRegister.Remove(device);
		lock (DeviceList)
		{
			DeviceList.Remove(device);
		}
		Battery battery = device as Battery;
		if (battery != null)
		{
			BatteryList.Remove(battery);
		}
		device.ConnectedCableNetworks.Remove(this);
		device.OnRemoveCableNetwork(this);
		HandleDeviceDisconnection(device);
		DirtyPowerAndDataDeviceLists();
	}

	protected void HandleDeviceDisconnection(Device device)
	{
		foreach (Device device2 in DeviceList)
		{
			if (!(device2 == device))
			{
				device2.OnDeviceDisconnectFromNetwork(device);
			}
		}
	}

	public void RemoveDevice(Cable cable, Device device)
	{
		HashSet<Cable> deviceRegistration = GetDeviceRegistration(device);
		if (deviceRegistration != null)
		{
			deviceRegistration.Remove(cable);
			if (deviceRegistration.Count == 0)
			{
				RemoveDevice(device);
			}
		}
	}

	public void Add(Cable cable)
	{
		if (CableList.Contains(cable) || cable.CableNetwork == this)
		{
			return;
		}
		if (cable.CableNetwork != null)
		{
			cable.CableNetwork.Remove(cable);
		}
		cable.CableNetwork = this;
		lock (CableList)
		{
			CableList.Add(cable);
		}
		CableFuse cableFuse = cable.SmallCell?.Device as CableFuse;
		if ((bool)cableFuse)
		{
			cableFuse.CheckForCable();
		}
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		cable.FillConnected<Device>(span, ref count);
		Span<SmallCellRef> span2 = span;
		Span<SmallCellRef> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			SmallCellRef smallCellRef = span3[i];
			if (smallCellRef.TryGet<Device>(out var found))
			{
				AddDevice(cable, found);
				found.FindDataCable();
			}
		}
		cable.CableNetworkId = ReferenceId;
	}

	public void Remove(Cable cable)
	{
		if (cable.CableNetwork != null && cable.CableNetwork == this)
		{
			cable.CableNetworkId = 0L;
			cable.CableNetwork = null;
			lock (CableList)
			{
				CableList.Remove(cable);
			}
			RefreshNetwork();
			DirtyPowerAndDataDeviceLists();
			CableFuse cableFuse = cable.SmallCell.Device as CableFuse;
			if ((bool)cableFuse)
			{
				cableFuse.CheckForCable();
			}
		}
	}

	public void Merge(CableNetwork oldNetwork)
	{
		if (oldNetwork == null || oldNetwork == this)
		{
			return;
		}
		for (int num = oldNetwork.CableList.Count - 1; num >= 0; num--)
		{
			Cable cable = oldNetwork.CableList[num];
			if ((object)cable != null)
			{
				Add(cable);
			}
		}
		oldNetwork.CableList.Clear();
		oldNetwork.RefreshNetwork();
		oldNetwork.DirtyPowerAndDataDeviceLists();
	}

	public static CableNetwork Merge(List<CableNetwork> cableNetworks)
	{
		if (cableNetworks.Count <= 0)
		{
			return null;
		}
		if (cableNetworks.Count == 1)
		{
			return cableNetworks[0];
		}
		CableNetwork cableNetwork = cableNetworks[0];
		for (int i = 1; i < cableNetworks.Count; i++)
		{
			cableNetwork.Merge(cableNetworks[i]);
		}
		return cableNetwork;
	}

	private static void RebuildNetwork(Cable cable, CableNetwork newNetwork, CableNetwork oldNetwork)
	{
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		cable.FillConnected<Cable>(span, ref count);
		Queue<Cable> queue = new Queue<Cable>(count);
		HashSet<Cable> hashSet = new HashSet<Cable>(oldNetwork?.CableList.Count ?? 16) { cable };
		Span<SmallCellRef> span2 = span;
		Span<SmallCellRef> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			SmallCellRef smallCellRef = span3[i];
			Cable cable2 = smallCellRef.Get<Cable>();
			if ((object)cable2 != null)
			{
				queue.Enqueue(cable2);
			}
		}
		Span<SmallCellRef> span4 = stackalloc SmallCellRef[32];
		while (queue.Count > 0)
		{
			Cable cable3 = queue.Dequeue();
			if ((object)cable3 == null || hashSet.Contains(cable3) || cable3.IsBeingDestroyed)
			{
				continue;
			}
			hashSet.Add(cable3);
			count = 0;
			cable3.FillConnected<Cable>(span, ref count);
			span2 = span;
			span3 = span2.Slice(0, count);
			for (int i = 0; i < span3.Length; i++)
			{
				SmallCellRef smallCellRef2 = span3[i];
				Cable cable4 = smallCellRef2.Get<Cable>();
				if ((object)cable4 != null && !hashSet.Contains(cable4))
				{
					queue.Enqueue(cable4);
				}
			}
			if (oldNetwork != null)
			{
				int count2 = 0;
				cable3.FillConnected<Device>(span4, ref count2);
				span2 = span4;
				span3 = span2.Slice(0, count2);
				for (int i = 0; i < span3.Length; i++)
				{
					SmallCellRef smallCellRef3 = span3[i];
					oldNetwork.RemoveDevice(cable3, smallCellRef3.Get<Device>());
				}
			}
			newNetwork.Add(cable3);
		}
	}

	public static void RebuildCableNetworkServer(SmallCellRef cableRef)
	{
		RebuildCableNetworkServer(cableRef.Get<Cable>());
	}

	public static void RebuildCableNetworkServer(Cable cable)
	{
		if (GameManager.GameState != GameState.None)
		{
			CableNetwork cableNetwork = cable.CableNetwork;
			CableNetwork cableNetwork2 = new CableNetwork(cable);
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				RebuildCableNetworkEvent.NewEvents.Add(new RebuildCableNetworkEvent(cable.ReferenceId, cableNetwork2.ReferenceId, cableNetwork.ReferenceId));
			}
			RebuildNetwork(cable, cableNetwork2, cableNetwork);
		}
	}

	public static void RebuildCableNetworkClient(Cable cable, CableNetwork newNetwork, CableNetwork oldNetwork)
	{
		if (GameManager.GameState != GameState.None)
		{
			if ((object)cable == null || newNetwork == null)
			{
				ConsoleWindow.PrintError("Cable or Network null during rebuild");
				return;
			}
			newNetwork.Add(cable);
			RebuildNetwork(cable, newNetwork, oldNetwork);
		}
	}

	protected void RefreshNetwork()
	{
		if (!IsNetworkValid() && GameManager.GameState != GameState.None && !InUseWithEvent)
		{
			Referencable.Deregister(this);
			AllCableNetworks.Remove(this);
			HelperHintsManager.Deregister(this);
			{
				foreach (Device item in new List<Device>(DeviceList))
				{
					RemoveDevice(item);
				}
				return;
			}
		}
		for (int num = CableList.Count - 1; num > -1; num--)
		{
			if (CableList[num] == null)
			{
				lock (CableList)
				{
					CableList.RemoveAt(num);
				}
			}
		}
	}

	public virtual bool IsNetworkValid()
	{
		return CableList.Count > 0;
	}

	public static List<CableNetwork> ConnectedNetworks(Cable cable)
	{
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		cable.FillConnected<Cable>(span, ref count);
		List<CableNetwork> list = new List<CableNetwork>(count);
		Span<SmallCellRef> span2 = span;
		Span<SmallCellRef> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			SmallCellRef smallCellRef = span3[i];
			Cable cable2 = smallCellRef.Get<Cable>();
			if (!list.Contains(cable2.CableNetwork))
			{
				list.Add(cable2.CableNetwork);
			}
		}
		return list;
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		Network.WriteIndex<ushort>(writer, out var count, out var bufferIndex);
		AllCableNetworks.ForEach(writer, WriteNetworkAction, ref count);
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static async UniTask DeserializeOnJoin(RocketBinaryReader reader)
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenDeserializeCableNetworks.DisplayString);
		ushort num = reader.ReadUInt16();
		for (int i = 0; i < num; i++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			CableNetworkType cableNetworkType = (CableNetworkType)reader.ReadByte();
			IReferencable referencable = Referencable.Find(referenceId);
			if (referencable != null)
			{
				throw new Exception($"Error: Tried to create a new CableNetwork with ReferenceId: {referenceId}. This ID is already in use and is assigned to: {referencable.DisplayName}");
			}
			switch (cableNetworkType)
			{
			case CableNetworkType.CableNetwork:
				new CableNetwork(referenceId);
				break;
			case CableNetworkType.WirelessNetwork:
				new WirelessNetwork(referenceId);
				break;
			}
		}
	}

	public void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteByte((byte)CableNetworkType);
		Network.WritePackedId(writer, this);
	}

	private static void DeserializeNew(RocketBinaryReader reader)
	{
		CableNetworkType cableNetworkType = (CableNetworkType)reader.ReadByte();
		Network.ReadPackedId(reader, out var referenceId);
		switch (cableNetworkType)
		{
		case CableNetworkType.CableNetwork:
			new CableNetwork(referenceId);
			break;
		case CableNetworkType.WirelessNetwork:
			new WirelessNetwork(referenceId);
			break;
		case CableNetworkType.None:
			break;
		}
	}

	public Slot GetSlot(int slotIndex)
	{
		throw new NotImplementedException();
	}

	public int GetPrefabHash()
	{
		throw new NotImplementedException();
	}

	public int GetNextSlotId(int slotIndex, bool isForward)
	{
		throw new NotImplementedException();
	}

	public string ToTooltip()
	{
		throw new NotImplementedException();
	}

	public bool IsLogicSlotReadable()
	{
		return false;
	}

	public bool IsLogicReadable()
	{
		return true;
	}

	public bool IsLogicWritable()
	{
		return true;
	}

	public bool CanLogicRead(LogicType logicType)
	{
		if (logicType - 165 <= LogicType.PressureExternal || logicType == LogicType.StackSize)
		{
			return true;
		}
		return false;
	}

	public bool CanLogicWrite(LogicType logicType)
	{
		if (logicType - 165 <= LogicType.PressureExternal)
		{
			return true;
		}
		return false;
	}

	private int GetChannelIndex(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Channel0:
		case LogicType.Channel1:
		case LogicType.Channel2:
		case LogicType.Channel3:
		case LogicType.Channel4:
		case LogicType.Channel5:
		case LogicType.Channel6:
		case LogicType.Channel7:
			return (int)logicType - _baseIndex;
		case LogicType.StackSize:
			return _dataDeviceList.Count;
		default:
			return -1;
		}
	}

	public void SetLogicValue(LogicType logicType, double value)
	{
		double[] channels = _channels;
		int channelIndex = GetChannelIndex(logicType);
		double num = ((logicType - 165 > LogicType.PressureExternal) ? _channels[GetChannelIndex(logicType)] : value);
		channels[channelIndex] = num;
	}

	public double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Channel0:
		case LogicType.Channel1:
		case LogicType.Channel2:
		case LogicType.Channel3:
		case LogicType.Channel4:
		case LogicType.Channel5:
		case LogicType.Channel6:
		case LogicType.Channel7:
			return _channels[GetChannelIndex(logicType)];
		case LogicType.StackSize:
			return GetStackSize();
		default:
			return double.NaN;
		}
	}

	public bool CanLogicRead(LogicSlotType logicSlotType, int slotId)
	{
		return false;
	}

	public double GetLogicValue(LogicSlotType logicSlotType, int slotId)
	{
		return double.NaN;
	}

	public int GetStackSize()
	{
		return _dataDeviceList.Count;
	}

	public double ReadMemory(int address)
	{
		return _dataDeviceList[address]?.ReferenceId ?? (-1);
	}
}
