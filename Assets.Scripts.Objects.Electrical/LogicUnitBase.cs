using System.Collections.Generic;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Objects.Electrical;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicUnitBase : SmallDevice, IPrefabHash, ISetable, ILogicable, IReferencable, IEvaluable, ISmartRotatable
{
	public static List<Device> AllLogicPrefabs = new List<Device>();

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	[Header("LogicUnit Base")]
	public LogicOnOffButton OnOffButton;

	protected static readonly Vector3 SoundOffset = new Vector3(0f, 0f, 0.1f);

	private CableNetwork _inputNetwork1;

	private List<ILogicable> _inputNetwork1DevicesSorted;

	private CableNetwork _outputNetwork1;

	private List<ILogicable> _outputNetwork1DevicesSorted;

	private double _setting;

	public int Input1Index;

	public int OutputIndex = 1;

	private static readonly Vector3 AudioOffset = new Vector3(0f, 0f, 0.15f);

	public virtual bool ShowStateTooltip => true;

	public bool IsSet => Setting > 0.001;

	public CableNetwork InputNetwork1
	{
		get
		{
			return _inputNetwork1;
		}
		set
		{
			if (_inputNetwork1 != value)
			{
				_inputNetwork1 = value;
				LogicNetworkChange();
			}
		}
	}

	public List<ILogicable> InputNetwork1DevicesSorted
	{
		get
		{
			if (_inputNetwork1DevicesSorted == null)
			{
				_inputNetwork1DevicesSorted = Logicable.RecalculateSortedDevicesList(InputNetwork1);
			}
			return _inputNetwork1DevicesSorted;
		}
	}

	public CableNetwork OutputNetwork1
	{
		get
		{
			return _outputNetwork1;
		}
		set
		{
			if (_outputNetwork1 != value)
			{
				_outputNetwork1 = value;
				LogicNetworkChange();
			}
		}
	}

	public List<ILogicable> OutputNetwork1DevicesSorted
	{
		get
		{
			if (_outputNetwork1DevicesSorted == null)
			{
				_outputNetwork1DevicesSorted = Logicable.RecalculateSortedDevicesList(OutputNetwork1);
			}
			return _outputNetwork1DevicesSorted;
		}
	}

	[ByteArraySync]
	public virtual double Setting
	{
		get
		{
			return _setting;
		}
		set
		{
			double setting = _setting;
			_setting = value;
			if (!RocketMath.Approximately(setting, _setting))
			{
				OnSettingChanged();
			}
			LogicChanged();
			this.OnLogicNetworkChanged?.Invoke();
		}
	}

	public int CurrentHash
	{
		get
		{
			return (int)Setting;
		}
		set
		{
			Setting = value;
		}
	}

	public bool ShouldPlayLogicSound
	{
		get
		{
			if (GameManager.GameState == GameState.Running && InventoryManager.Parent != null)
			{
				return Vector3.SqrMagnitude(base.Position - InventoryManager.ParentPosition) < 200f;
			}
			return false;
		}
	}

	public double LabelSetting => Setting;

	public virtual int LogicOnHash => Animator.StringToHash("LogicOn");

	public virtual int LogicOffHash => Animator.StringToHash("LogicOff");

	public event Event OnLogicNetworkChanged;

	public event Event OnLogicChanged;

	public void LogicNetworkChange()
	{
		this.OnLogicNetworkChanged?.Invoke();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteDouble(Setting);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			Setting = reader.ReadDouble();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteDouble(Setting);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Setting = reader.ReadDouble();
	}

	public virtual void OnSettingChanged()
	{
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 256;
		}
	}

	public void LogicChanged()
	{
		this.OnLogicChanged?.Invoke();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicBaseSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LogicBaseSaveData logicBaseSaveData)
		{
			Setting = logicBaseSaveData.Setting;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicBaseSaveData logicBaseSaveData)
		{
			logicBaseSaveData.Setting = Setting;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (!base.IsStructureCompleted)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		foreach (Connection openEnd in OpenEnds)
		{
			if (hitCollider == openEnd.Collider && hitCollider != null)
			{
				return result.Populate(openEnd);
			}
		}
		if (ShowStateTooltip)
		{
			Localization.Variable1 = GetStateText();
			result.Extended = InterfaceStrings.LogicState;
		}
		return result;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (ShowStateTooltip)
		{
			extendedText.Append("State ");
			extendedText.AppendLine(GetStateText().AsColor("yellow"));
		}
		return extendedText;
	}

	public virtual string GetStateText()
	{
		return Setting.ToStringExact();
	}

	public void SetFromLabeller(double value)
	{
		Setting = value;
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

	public override void OnAddCableNetwork(CableNetwork newNetwork)
	{
		base.OnAddCableNetwork(newNetwork);
		CheckConnections();
		OnNetworkChange();
	}

	public override void OnRemoveCableNetwork(CableNetwork oldNetwork)
	{
		base.OnRemoveCableNetwork(oldNetwork);
		CheckConnections();
		OnNetworkChange();
	}

	public override void OnDeviceConnectToNetwork(Device device)
	{
		base.OnDeviceConnectToNetwork(device);
		OnNetworkChange();
	}

	public override void OnNetworkedRefresh(Device device)
	{
		base.OnNetworkedRefresh(device);
		OnNetworkChange();
	}

	public override void OnDeviceDisconnectFromNetwork(Device device)
	{
		base.OnDeviceDisconnectFromNetwork(device);
		OnNetworkChange();
	}

	public virtual void OnNetworkChange()
	{
		_inputNetwork1DevicesSorted = null;
		_outputNetwork1DevicesSorted = null;
	}

	protected override void CheckConnections()
	{
		base.CheckConnections();
		if (Input1Index >= 0 && Input1Index < OpenEnds.Count)
		{
			Cable cable = OpenEnds[Input1Index].GetCable();
			InputNetwork1 = (cable ? cable.CableNetwork : null);
		}
		if (OutputIndex >= 0 && OutputIndex < OpenEnds.Count)
		{
			Cable cable2 = OpenEnds[OutputIndex].GetCable();
			OutputNetwork1 = (cable2 ? cable2.CableNetwork : null);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (!GameManager.IsBatchMode && doAction && interactable.Action == InteractableType.OnOff && OnOffButton != null)
		{
			OnOffButton.PlayButtonInteractSound(OnOff);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (OnOffButton != null && (interactable.Action == InteractableType.Powered || interactable.Action == InteractableType.Error))
		{
			OnOffButton.RefreshState();
		}
	}

	public void ScrewSound()
	{
		PlayPooledAudioSound(Defines.Sounds.ScrewdriverSound, Vector3.zero);
	}

	public override void OnFinishedLoad()
	{
		CheckConnections();
		OnNetworkChange();
	}
}
