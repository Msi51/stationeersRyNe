using System.Collections.Generic;
using System.Text;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Electrical.Helper;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class DeviceInputOutputCircuit : DeviceInputOutput, ICircuitHolder, IDensePoolable
{
	[SerializeField]
	protected Collider infoPanel;

	private static string[] _modeStrings;

	private MoleQuantity _processedMoles;

	[SerializeField]
	protected SlidingPanelAnimationComponent _slidingPanel;

	[SerializeField]
	private List<Rotator> _fanRotators;

	protected int CodeErrorState;

	public ILogicable[] Devices = new ILogicable[2];

	private List<ILogicable> _dataNetworkDevicesSorted;

	private long[] _DeviceIDs = new long[2];

	private string[] _DeviceLabels = new string[2] { "", "" };

	private byte _labelUpdateFlags;

	private float ExplosionForce = 200f;

	private float ExplosionRadius = 2.3f;

	public ulong LastEditedBy { get; set; }

	public override string[] ModeStrings => _modeStrings;

	public MoleQuantity ProcessedMoles
	{
		get
		{
			return _processedMoles;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(value, ProcessedMoles))
			{
				base.NetworkUpdateFlags |= 512;
			}
			_processedMoles = value;
		}
	}

	protected virtual bool ShouldFansRotate
	{
		get
		{
			if (OnOff)
			{
				return Powered;
			}
			return false;
		}
	}

	protected virtual Slot ProgrammableChipSlot
	{
		get
		{
			List<Slot> slots = Slots;
			if (slots == null || slots.Count <= 0)
			{
				return null;
			}
			return Slots[0];
		}
	}

	public ProgrammableChip ProgrammableChip => ProgrammableChipSlot?.Get<ProgrammableChip>();

	public List<ILogicable> DataNetworkDevicesSorted
	{
		get
		{
			if (_dataNetworkDevicesSorted == null)
			{
				_dataNetworkDevicesSorted = Logicable.RecalculateSortedDevicesList(base.DataCableNetwork);
			}
			return _dataNetworkDevicesSorted;
		}
	}

	public void HasPut()
	{
	}

	public List<LogicBinding> GetLogicBindings()
	{
		return new List<LogicBinding>
		{
			new LogicBinding("DEVICE"),
			new LogicBinding(0, "SCREW_0"),
			new LogicBinding(1, "SCREW_1")
		};
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (GameManager.IsBatchMode || !base.IsStructureCompleted || IsOccluded)
		{
			return;
		}
		foreach (Rotator fanRotator in _fanRotators)
		{
			fanRotator.DoUpdate(ShouldFansRotate);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if ((bool)_slidingPanel)
		{
			_slidingPanel.RefreshState(skipAnimation);
		}
	}

	public override void OnPrefabLoad()
	{
		base.OnPrefabLoad();
		_modeStrings = new string[2]
		{
			ActionStrings.Idle,
			ActionStrings.Active
		};
	}

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = base.InteractWith(interactable, interaction, doAction);
		ProgrammableChip?.AppendErrorsToActionInstance(delayedActionInstance);
		return interactable.Action switch
		{
			InteractableType.Button1 => Logicable._TryGetNextLogicDevice(interactable, interaction, ref Devices[0], DataNetworkDevicesSorted, doAction, 0), 
			InteractableType.Button2 => Logicable._TryGetNextLogicDevice(interactable, interaction, ref Devices[1], DataNetworkDevicesSorted, doAction, 1), 
			_ => delayedActionInstance, 
		};
	}

	protected virtual StringBuilder GetInfoPanelOperationText()
	{
		StringBuilder stringBuilder = new StringBuilder(Localization.GetName(base.InteractMode));
		stringBuilder.Append(" ");
		stringBuilder.AppendLine(ModeStrings[Mode].AsColor("green"));
		stringBuilder.AppendLine(GameStrings.ProcessedMoles.AsString(ProcessedMoles.ToFloat().ToStringPrefix("mol", "yellow")));
		return stringBuilder;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (passiveTooltip.Title.Equals(string.Empty))
		{
			passiveTooltip.Title = DisplayName;
		}
		if (infoPanel != null && hitCollider == infoPanel && Powered)
		{
			if (Error == 0)
			{
				passiveTooltip.Extended = GetInfoPanelOperationText().ToString();
			}
			else if ((bool)ProgrammableChip)
			{
				passiveTooltip.Extended = ProgrammableChip.GetErrorCode();
			}
			else
			{
				passiveTooltip.Extended = ActionStrings.Error;
			}
		}
		return passiveTooltip;
	}

	public override void OnAnimationStart()
	{
		base.OnAnimationStart();
		if (IsOpen)
		{
			SetContentsVisibility(isVisible: true);
		}
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		if (!IsOpen)
		{
			SetContentsVisibility(isVisible: false);
		}
	}

	public void SetContentsVisibility(bool isVisible)
	{
		WaitSetVisibility(isVisible).Forget();
	}

	private async UniTaskVoid WaitSetVisibility(bool isVisible = true)
	{
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		while (GameManager.GameState != GameState.Running)
		{
			await UniTask.NextFrame(cancelToken);
		}
		await UniTask.NextFrame(cancelToken);
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		InteractableType action = ProgrammableChipSlot.Action;
		foreach (Interactable interactable in Interactables)
		{
			if (interactable.Action != action)
			{
				InteractableType action2 = interactable.Action;
				if (action2 != InteractableType.Button1 && action2 != InteractableType.Button2)
				{
					continue;
				}
			}
			if ((bool)interactable.Collider)
			{
				interactable.Collider.enabled = isVisible;
			}
		}
		if (ProgrammableChip != null)
		{
			ProgrammableChip.SetVisibility(isVisible);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		RefreshError();
		if (GameManager.GameState == GameState.Running && newChild is ProgrammableChip)
		{
			ProgrammableChip.Reset();
			if (NetworkManager.IsServer)
			{
				_labelUpdateFlags |= 63;
				base.NetworkUpdateFlags |= 512;
			}
			ClearError();
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (GameManager.GameState == GameState.Running && previousChild is ProgrammableChip programmableChip)
		{
			programmableChip.Reset();
		}
		RefreshError();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Open)
		{
			ToggleInteractableColliders();
		}
		if (interactable.Action != InteractableType.Error)
		{
			RefreshError();
		}
	}

	protected virtual void ToggleInteractableColliders()
	{
		foreach (Interactable interactable in Interactables)
		{
			InteractableType action = interactable.Action;
			if ((action == InteractableType.Slot1 || (uint)(action - 31) <= 1u) && interactable.Collider != null)
			{
				interactable.Collider.enabled = IsOpen;
			}
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		RefreshError();
	}

	public void RefreshError()
	{
		if (GameManager.RunSimulation)
		{
			if (!IsOperable && Error == 0)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (IsOperable && Error == 1)
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		return interactable.Action switch
		{
			InteractableType.Button1 => GetDeviceNameWithLabel(0), 
			InteractableType.Button2 => GetDeviceNameWithLabel(1), 
			_ => base.GetContextualName(interactable), 
		};
	}

	private string GetDeviceNameWithLabel(int deviceIndex)
	{
		string text = ((Devices[deviceIndex] != null) ? Devices[deviceIndex].DisplayName : ("<color=red>" + InterfaceStrings.LogicNoDevice + "</color>"));
		if ((bool)ProgrammableChip)
		{
			string text2 = _DeviceLabels[deviceIndex];
			if (!string.IsNullOrEmpty(text2))
			{
				text = "<color=yellow>" + text2 + "</color> " + text;
			}
		}
		return text;
	}

	public void ClearError()
	{
		RaiseError(0);
	}

	public void RaiseError(int state)
	{
		CodeErrorState = state;
		RefreshError();
	}

	public ILogicable GetLogicableFromIndex(int deviceIndex, int networkIndex = int.MinValue)
	{
		if (deviceIndex == int.MaxValue)
		{
			return this;
		}
		if (base.DataCableNetwork != null && !base.DataCableNetwork.DataDeviceList.Contains(Devices[deviceIndex] as Device))
		{
			return null;
		}
		return Devices[deviceIndex];
	}

	public ILogicable GetLogicableFromId(int deviceId, int networkIndex = int.MinValue)
	{
		if (deviceId == 0L)
		{
			return null;
		}
		Device device = Referencable.Find<Device>(deviceId);
		if (base.DataCableNetwork != null && !base.DataCableNetwork.DataDeviceList.Contains(device))
		{
			return null;
		}
		if (networkIndex != int.MinValue)
		{
			return ((IConnected)device)?.GetNetwork(networkIndex);
		}
		return device;
	}

	public override void OnAddCableNetwork(CableNetwork newNetwork)
	{
		base.OnAddCableNetwork(newNetwork);
		OnNetworkChange();
	}

	public override void OnRemoveCableNetwork(CableNetwork oldNetwork)
	{
		base.OnRemoveCableNetwork(oldNetwork);
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

	private void OnNetworkChange()
	{
		_dataNetworkDevicesSorted = null;
	}

	public List<ILogicable> GetBatchOutput()
	{
		if (InputNetwork == null)
		{
			return null;
		}
		return DataNetworkDevicesSorted;
	}

	public bool IsValidIndex(int index)
	{
		if (index != int.MaxValue && index != 0)
		{
			return index == 1;
		}
		return true;
	}

	public void SetDeviceLabel(int index, string label)
	{
		if (index < 0 || index >= 6)
		{
			return;
		}
		string text = ((label.Length > 10) ? label.Substring(0, 10) : label);
		_DeviceLabels[index] = text;
		if (NetworkManager.IsServer)
		{
			switch (index)
			{
			case 0:
				_labelUpdateFlags |= 1;
				break;
			case 1:
				_labelUpdateFlags |= 2;
				break;
			}
			base.NetworkUpdateFlags |= 512;
		}
	}

	public async UniTask HaltAndCatchFire()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
			if (ThingTransform.GetCancellationTokenOnDestroy().IsCancellationRequested)
			{
				return;
			}
		}
		base.InternalAtmosphere.Sparked = true;
		global::Explosion.Explode(ExplosionForce, base.Position, ExplosionRadius);
		OnServer.Destroy(this);
	}

	public string GetSourceCode()
	{
		if (!ProgrammableChip)
		{
			return "";
		}
		return ProgrammableChip.GetSourceCode();
	}

	public void SetSourceCode(string sourceCode)
	{
		if ((bool)ProgrammableChip)
		{
			ProgrammableChip.SetSourceCode(sourceCode, this);
			ProgrammableChip.SendUpdate();
		}
	}

	public void Execute()
	{
		if (GameManager.RunSimulation && !IsCursor && OnOff && Powered && GameManager.GameState == GameState.Running && !WorldManager.IsGamePaused && GameManager.GameState != GameState.None && !(ProgrammableChip == null) && !ProgrammableChip.CompilationError)
		{
			ProgrammableChip.Execute(128);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteInt64(Devices[0]?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			writer.WriteInt64(Devices[1]?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			WriteLabels(_labelUpdateFlags, writer);
			_labelUpdateFlags = 0;
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(ProcessedMoles.ToFloat());
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Devices[0] = Referencable.Find<ILogicable>(reader.ReadInt64());
		}
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			Devices[1] = Referencable.Find<ILogicable>(reader.ReadInt64());
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			ReadLabels(reader.ReadByte(), reader);
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			ProcessedMoles = new MoleQuantity(reader.ReadSingle());
		}
	}

	private void WriteLabels(byte updateFlags, RocketBinaryWriter writer)
	{
		writer.WriteByte(updateFlags);
		if ((updateFlags & 1) != 0)
		{
			writer.WriteString(_DeviceLabels[0]);
		}
		if ((updateFlags & 2) != 0)
		{
			writer.WriteString(_DeviceLabels[1]);
		}
	}

	private void ReadLabels(byte updateFlags, RocketBinaryReader reader)
	{
		if ((updateFlags & 1) != 0)
		{
			_DeviceLabels[0] = reader.ReadString();
		}
		if ((updateFlags & 2) != 0)
		{
			_DeviceLabels[1] = reader.ReadString();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		ILogicable[] devices = Devices;
		for (int i = 0; i < devices.Length; i++)
		{
			writer.WriteInt64(devices[i]?.ReferenceId ?? 0);
		}
		string[] deviceLabels = _DeviceLabels;
		foreach (string value in deviceLabels)
		{
			writer.WriteString(value);
		}
		writer.WriteSingle(ProcessedMoles.ToFloat());
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		for (int i = 0; i < Devices.Length; i++)
		{
			_DeviceIDs[i] = reader.ReadInt64();
		}
		for (int j = 0; j < _DeviceLabels.Length; j++)
		{
			_DeviceLabels[j] = reader.ReadString();
		}
		ProcessedMoles = new MoleQuantity(reader.ReadSingle());
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if ((bool)_slidingPanel)
		{
			_slidingPanel.RefreshState(skipAnimation: true);
		}
		for (int i = 0; i < _DeviceIDs.Length; i++)
		{
			Devices[i] = Referencable.Find<Device>(_DeviceIDs[i]);
		}
		if ((bool)ProgrammableChip)
		{
			ClearError();
		}
		RefreshError();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new DeviceInputOutputCircuitSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is DeviceInputOutputCircuitSaveData { DeviceIDs: not null } deviceInputOutputCircuitSaveData)
		{
			for (int i = 0; i < _DeviceIDs.Length; i++)
			{
				_DeviceIDs[i] = deviceInputOutputCircuitSaveData.DeviceIDs[i];
			}
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is DeviceInputOutputCircuitSaveData deviceInputOutputCircuitSaveData)
		{
			deviceInputOutputCircuitSaveData.DeviceIDs = new long[Devices.Length];
			for (int i = 0; i < Devices.Length; i++)
			{
				deviceInputOutputCircuitSaveData.DeviceIDs[i] = ((Devices[i] == null) ? 0 : Devices[i].ReferenceId);
			}
		}
	}
}
