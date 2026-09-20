using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Trading;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Items;

public class Motherboard : CharacterItem, IPowered, IDensePoolable, IReferencable, IEvaluable
{
	[Header("Motherboard")]
	public List<GameObject> Screens = new List<GameObject>();

	public Scrollbar ScrollBar;

	[SerializeField]
	private Button _btnUp;

	[SerializeField]
	private Button _btnDown;

	[SerializeField]
	private Button _btnSearch;

	[ReadOnly]
	public IComputer ParentComputer;

	[ReadOnly]
	private List<Device> _linkedDevices = new List<Device>();

	private int _flag;

	private Motherboard _masterMotherboard;

	[ReadOnly]
	public List<Motherboard> Slaves = new List<Motherboard>();

	private readonly List<long> _linkedDeviceIds = new List<long>();

	private long _masterMotherboardId = -1L;

	private byte _scrollBarValueByte;

	public static readonly List<MotherboardCommand> NewCommands = new List<MotherboardCommand>();

	public virtual Vector3 UiAudioOffSet => new Vector3(0.12f, 0.583f, -0.232f);

	protected byte ScrollBarValueByte
	{
		get
		{
			return _scrollBarValueByte;
		}
		private set
		{
			_scrollBarValueByte = value;
			if ((bool)ScrollBar)
			{
				ScrollBar.value = (float)(int)value / 100f;
			}
		}
	}

	public virtual bool IsOperable => LinkedDevices.Count > 0;

	public int Flag
	{
		get
		{
			if (!(_masterMotherboard != null) || !(_masterMotherboard != this))
			{
				return _flag;
			}
			return _masterMotherboard.Flag;
		}
		set
		{
			if (_masterMotherboard != null)
			{
				_masterMotherboard.Flag = value;
				return;
			}
			_flag = value;
			OnFlagChanged();
		}
	}

	public virtual bool IsError => !IsOperable;

	public List<Device> LinkedDevices
	{
		get
		{
			if (!_masterMotherboard || !(_masterMotherboard != this))
			{
				return _linkedDevices;
			}
			return _masterMotherboard.LinkedDevices;
		}
	}

	public Motherboard MasterMotherboard
	{
		get
		{
			return _masterMotherboard;
		}
		set
		{
			_masterMotherboard = value;
			if (_masterMotherboard == null)
			{
				_linkedDevices = new List<Device>();
				SetFlag(0);
			}
			else
			{
				SetFlag(Flag);
			}
			OnSlaveStateChange();
		}
	}

	public override void Awake()
	{
		base.Awake();
		if ((bool)_btnUp)
		{
			_btnUp.onClick.AddListener(ButtonUp);
		}
		if ((bool)_btnDown)
		{
			_btnDown.onClick.AddListener(ButtonDown);
		}
		SetScrollBarSyncValue();
		ElectricityManager.Register(this);
	}

	private void SetScrollBarSyncValue()
	{
		if ((bool)ScrollBar)
		{
			ScrollBarValueByte = (byte)Mathf.RoundToInt(ScrollBar.value * 100f);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SetScrollBarSyncValue();
	}

	public static void UseComputer(int commandId, long motherboardId, long referenceId, int referenceInt, bool sendToAll, string text = "")
	{
		if (GameManager.RunSimulation)
		{
			OnServer.UseComputer(commandId, motherboardId, referenceId, referenceInt, sendToAll, text);
		}
		else if (NetworkManager.IsClient)
		{
			MotherboardCommand command = new MotherboardCommand(commandId, motherboardId, referenceId, referenceInt, text);
			command.Execute();
			NetworkClient.SendToServer(new MotherBoardCommandMessage
			{
				Command = command,
				BroadcastToAll = sendToAll
			});
		}
	}

	public override string GetQuantityText()
	{
		return DisplayName;
	}

	public void FlagAdd(int setting)
	{
		Flag |= setting;
	}

	public void FlagClear(int setting)
	{
		Flag &= ~setting;
	}

	public bool FlagGet(int setting)
	{
		return (Flag & setting) != 0;
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.MotherboardCategory);
	}

	public virtual void OnFlagChanged()
	{
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if ((object)sourceItem == null)
		{
			return null;
		}
		if (sourceItem is Screwdriver screwdriver)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 2f,
				ActionMessage = ActionStrings.Clear
			};
			if (screwdriver.IsBroken)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceBroken);
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			UseComputer(20, base.ReferenceId, 0L, 0, sendToAll: true);
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}

	public virtual void FlashCircuit()
	{
		MasterMotherboard = null;
		for (int i = 0; i < LinkedDevices.Count; i++)
		{
			RemoveLinkedDevice(LinkedDevices[i]);
		}
	}

	public void AddLinkedDevice(Device device)
	{
		_linkedDevices.Add(device);
		device.OnLinkWithBoard(this);
	}

	public void RemoveLinkedDevice(Device device)
	{
		_linkedDevices.Remove(device);
		device.OnUnlinkWithBoard(this);
	}

	public virtual bool IsDeviceConnected(Device device)
	{
		if (ParentComputer != null && (bool)device && ParentComputer.DataCableNetwork != null)
		{
			return ParentComputer.DataCableNetwork.IsNetworkDevice(device);
		}
		return false;
	}

	public virtual bool CanDeviceLink(Device device)
	{
		if (!(typeof(Device) == device.GetType()))
		{
			return device.GetType().IsSubclassOf(typeof(Device));
		}
		return true;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new MotherboardSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public virtual void RefreshScreen()
	{
		ParentComputer?.CheckStatus();
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		MotherboardSaveData motherboardSaveData = savedData as MotherboardSaveData;
		Flag = motherboardSaveData.Flag;
		DeserializeWhenLoaded(motherboardSaveData).Forget();
	}

	public virtual void OnSlaveStateChange()
	{
		OnDeviceListChanged();
	}

	public virtual void SetFlag(int page)
	{
	}

	public virtual void SetMainScreenVisibility()
	{
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte(ScrollBarValueByte);
		writer.WriteInt32(LinkedDevices.Count);
		foreach (Device linkedDevice in LinkedDevices)
		{
			writer.WriteInt64(linkedDevice.ReferenceId);
		}
		bool flag = MasterMotherboard != null;
		writer.WriteBoolean(flag);
		if (flag)
		{
			writer.WriteInt64(MasterMotherboard.ReferenceId);
		}
		writer.WriteInt32(_flag);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		ScrollBarValueByte = reader.ReadByte();
		int num = reader.ReadInt32();
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				_linkedDeviceIds.Add(reader.ReadInt64());
			}
		}
		if (reader.ReadBoolean())
		{
			_masterMotherboardId = reader.ReadInt64();
		}
		_flag = reader.ReadInt32();
		NetworkClient.ClientFinishedJoining += LinkDevicesOnJoinComplete;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteByte(ScrollBarValueByte);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			ScrollBarValueByte = reader.ReadByte();
		}
	}

	public virtual void LinkDevicesOnJoinComplete()
	{
		foreach (long linkedDeviceId in _linkedDeviceIds)
		{
			Device device = Thing.Find<Device>(linkedDeviceId);
			if ((object)device != null && !LinkedDevices.Contains(device))
			{
				AddLinkedDevice(device);
				OnDeviceListChanged(device);
			}
		}
		if (_masterMotherboardId != -1)
		{
			Motherboard masterMotherboard = Thing.Find<Motherboard>(_masterMotherboardId);
			MasterMotherboard = masterMotherboard;
			if (!MasterMotherboard.Slaves.Contains(this))
			{
				MasterMotherboard.Slaves.Add(this);
			}
		}
		SetFlag(Flag);
		NetworkClient.ClientFinishedJoining -= LinkDevicesOnJoinComplete;
	}

	public async UniTask DeserializeWhenLoaded(ThingSaveData saveData)
	{
		if (!(saveData is MotherboardSaveData motherboardSaveData))
		{
			return;
		}
		await UniTask.WaitUntil(() => GameManager.GameState != GameState.Loading);
		List<Device> list = new List<Device>();
		if (motherboardSaveData.LinkedDeviceReferences != null)
		{
			long[] linkedDeviceReferences = motherboardSaveData.LinkedDeviceReferences;
			for (int num = 0; num < linkedDeviceReferences.Length; num++)
			{
				Device device = Thing.Find<Device>(linkedDeviceReferences[num]);
				if ((bool)device && (bool)device && !list.Contains(device))
				{
					list.Add(device);
					UseComputer(1, base.netId, device.netId, -1, sendToAll: true);
				}
			}
		}
		Motherboard motherboard = Thing.Find<Motherboard>(motherboardSaveData.MasterMotherboard);
		if (motherboard != null)
		{
			MasterMotherboard = motherboard;
			UseComputer(9, motherboard.netId, base.netId, -1, sendToAll: true);
		}
		SetFlag(Flag);
		OnMotherboardDeserialized(saveData);
	}

	public virtual void OnMotherboardDeserialized(ThingSaveData saveData)
	{
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		MotherboardSaveData motherboardSaveData = savedData as MotherboardSaveData;
		List<long> list = MakeDeviceSaveRefs();
		motherboardSaveData.LinkedDeviceReferences = list.ToArray();
		motherboardSaveData.Flag = Flag;
		if ((bool)MasterMotherboard)
		{
			motherboardSaveData.MasterMotherboard = MasterMotherboard.ReferenceId;
		}
	}

	protected virtual List<long> MakeDeviceSaveRefs()
	{
		List<long> list = new List<long>();
		foreach (Device linkedDevice in LinkedDevices)
		{
			list.Add(linkedDevice.ReferenceId);
		}
		return list;
	}

	public virtual void MotherboardCommand(int command, Thing reference, int referenceInt, string text)
	{
		switch ((ButtonCommands)command)
		{
		case ButtonCommands.IndexUp:
		case ButtonCommands.IndexDown:
			ScrollBar.value = (float)referenceInt / 100f;
			break;
		case ButtonCommands.FlashCircuit:
			FlashCircuit();
			break;
		}
	}

	public virtual void ButtonUp()
	{
		ScrollBar.value = Mathf.Clamp01(ScrollBar.value + 0.1f);
		SetScrollBarSyncValue();
		SendButtonMessage(ButtonCommands.IndexUp);
	}

	public virtual void ButtonDown()
	{
		ScrollBar.value = Mathf.Clamp01(ScrollBar.value - 0.1f);
		SetScrollBarSyncValue();
		SendButtonMessage(ButtonCommands.IndexDown);
	}

	private void SendButtonMessage(ButtonCommands cmd)
	{
		UseComputer((int)cmd, base.ReferenceId, base.ReferenceId, ScrollBarValueByte, sendToAll: false);
	}

	public virtual void SetMode(bool isNormal)
	{
		if (Screens.Count > 0)
		{
			Screens[0].SetActive(value: true);
		}
	}

	public virtual void OnInsertedToComputer(IComputer computer)
	{
	}

	public virtual void OnRemovedFromComputer(IComputer computer)
	{
	}

	public virtual void OnDeviceListChanged()
	{
		ParentComputer?.CheckStatus();
	}

	public virtual void OnDeviceListChanged(Device device)
	{
		OnDeviceListChanged();
	}

	public override void OnDestroy()
	{
		if (ParentComputer != null)
		{
			ParentComputer.CurrentMotherboard = null;
			foreach (GameObject screen in Screens)
			{
				Object.Destroy(screen);
			}
			if (ParentComputer is Computer computer)
			{
				computer.EnableAppropriateScreen();
				computer.CheckMotherboardMissingError();
			}
			ParentComputer = null;
		}
		base.OnDestroy();
		if (GameManager.GameState != GameState.None)
		{
			ElectricityManager.Deregister(this);
		}
	}

	public virtual void RefreshDevice(Device device)
	{
	}

	public static void SerializeNewMotherboards(RocketBinaryWriter writer)
	{
		writer.WriteUInt16((ushort)NewCommands.Count);
		foreach (MotherboardCommand newCommand in NewCommands)
		{
			newCommand.Serialize(writer);
		}
		NewCommands.Clear();
	}

	public static void DeserializeNewMotherboards(RocketBinaryReader reader)
	{
		ushort num = reader.ReadUInt16();
		for (int i = 0; i < num; i++)
		{
			MotherboardCommand motherboardCommand = default(MotherboardCommand);
			motherboardCommand.Deserialize(reader);
			motherboardCommand.Execute();
		}
	}
}
