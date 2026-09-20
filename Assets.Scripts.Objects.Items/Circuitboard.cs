using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Assets.Scripts.Objects.Items;

public class Circuitboard : Motherboard
{
	[Header("Config Menu")]
	public GameObject ButtonPrefab;

	public GridLayoutGroup DynamicGrid;

	public Button CreateFilterButton;

	public Button ClearFilterButton;

	[Header("Main Screen")]
	public Text TitleText;

	private List<Button> _mainScreenButtons = new List<Button>();

	private List<Button> _configScreenButtons = new List<Button>();

	[ReadOnly]
	protected readonly List<ComputerButton> Buttons = new List<ComputerButton>();

	private bool _devicesChanged;

	public InteractableType ButtonMode;

	private string _filterString;

	public override Vector3 UiAudioOffSet => new Vector3(0f, 0f, 0.2f);

	[ByteArraySync]
	public string FilterString
	{
		get
		{
			return _filterString;
		}
		set
		{
			_filterString = value;
			if (GameManager.GameState == GameState.Running)
			{
				RedrawList().Forget();
			}
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is CircuitboardSaveData circuitboardSaveData)
		{
			circuitboardSaveData.FilterString = FilterString;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new CircuitboardSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is CircuitboardSaveData circuitboardSaveData)
		{
			FilterString = circuitboardSaveData.FilterString ?? string.Empty;
		}
		SetDisplayName();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (NetworkManager.IsClient)
		{
			OnFinishedThingSync();
		}
	}

	public override void OnFinishedThingSync()
	{
		base.OnFinishedThingSync();
		SetMainScreenVisibility();
	}

	public override void Awake()
	{
		base.Awake();
		SetMode(isNormal: true);
		_mainScreenButtons = new List<Button>(Screens[0].GetComponentsInChildren<Button>(includeInactive: true));
		_configScreenButtons = new List<Button>(Screens[1].GetComponentsInChildren<Button>(includeInactive: true));
		SetDisplayName();
	}

	public virtual void ButtonToggle()
	{
		Motherboard.UseComputer(0, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, -1, sendToAll: false);
	}

	public virtual void ButtonToggleMode()
	{
	}

	public virtual void RemoteToggle(bool open)
	{
		Motherboard.UseComputer(0, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, -1, sendToAll: false);
	}

	public override void SetMode(bool isNormal)
	{
		Screens[0].SetActive(isNormal);
		Screens[1].SetActive(!isNormal);
	}

	private bool IsEnslaveable(Console console)
	{
		if ((bool)console && (bool)console.CurrentMotherboard)
		{
			return console.CurrentMotherboard.GetType() == GetType();
		}
		return false;
	}

	public ComputerButton FindDeviceButton(Device device)
	{
		int num = Buttons.FindIndex((ComputerButton record) => record.AssignedDevice == device);
		if (num < 0)
		{
			return null;
		}
		return Buttons[num];
	}

	public virtual async UniTask DeviceListChangeTask()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		await UniTask.NextFrame(cancelToken);
		if (cancelToken.IsCancellationRequested)
		{
			_devicesChanged = false;
			return;
		}
		if (ParentComputer == null)
		{
			_devicesChanged = false;
			return;
		}
		if (!ParentComputer.AsThing().isActiveAndEnabled)
		{
			_devicesChanged = false;
			return;
		}
		List<Device> devices;
		if ((bool)base.MasterMotherboard)
		{
			devices = new List<Device> { base.MasterMotherboard.ParentComputer.AsDevice() };
		}
		else
		{
			devices = (ParentComputer?.DataCable ? new List<Device>(ParentComputer.DataCable.CableNetwork.DataDeviceList) : new List<Device>());
			foreach (Device linkedDevice in base.LinkedDevices)
			{
				if (!devices.Contains(linkedDevice))
				{
					devices.Add(linkedDevice);
				}
			}
		}
		devices.Remove(ParentComputer.AsDevice());
		int num = Buttons.Count - devices.Count;
		if (num > 0)
		{
			for (int i = devices.Count; i < Buttons.Count; i++)
			{
				Buttons[i].Button.gameObject.DestroyGameObject(this);
			}
			Buttons.RemoveRange(devices.Count, num);
		}
		for (int j = 0; j < devices.Count; j++)
		{
			ComputerButton computerButton;
			if (j >= Buttons.Count)
			{
				computerButton = MakeButton(devices[j]);
				await UniTask.WaitForEndOfFrame(cancelToken);
			}
			else
			{
				computerButton = Buttons[j];
				computerButton.AssignedDevice = devices[j];
				computerButton.Label.text = devices[j].DisplayName;
				computerButton.SetColor();
			}
			if ((object)computerButton.AssignedDevice == null)
			{
				Buttons.Remove(computerButton);
				Object.Destroy(computerButton.Button.gameObject);
				continue;
			}
			Console console = computerButton.AssignedDevice as Console;
			if ((bool)console && IsEnslaveable(console))
			{
				if (console.CurrentMotherboard == base.MasterMotherboard)
				{
					computerButton.Label.text = computerButton.Label.text + $" <color=red><b>{GameStrings.CircuitboardMasterLabel}</b></color>";
					computerButton.Button.interactable = false;
				}
				else if (console.CurrentMotherboard.MasterMotherboard == this)
				{
					computerButton.Label.text = computerButton.Label.text + $" $<color=red><b>{GameStrings.CircuitboardSlaveLabel}</b></color>";
					computerButton.Button.interactable = true;
				}
				else if (console.CurrentMotherboard.Slaves.Count > 0)
				{
					computerButton.Label.text = computerButton.Label.text + $" <b>{GameStrings.CircuitboardMasterLabel}</b>";
					computerButton.Button.interactable = false;
				}
				else if (console.CurrentMotherboard.MasterMotherboard != null)
				{
					computerButton.Label.text = computerButton.Label.text + $" <b>{GameStrings.CircuitboardSlaveLabel}</b>";
					computerButton.Button.interactable = false;
				}
				else
				{
					computerButton.Label.text = computerButton.Label.text + $" <color=green><b>{GameStrings.CircuitboardMakeSlaveLabel}</b></color>";
					computerButton.Button.interactable = true;
				}
			}
		}
		if (base.gameObject.activeInHierarchy || GameManager.GameState != GameState.Running)
		{
			RedrawList().Forget();
		}
		_devicesChanged = false;
	}

	public override void SetMainScreenVisibility()
	{
		SetButtonInteractionNextFrame().Forget();
		foreach (Motherboard slafe in Slaves)
		{
			slafe.SetMainScreenVisibility();
		}
	}

	private async UniTaskVoid SetButtonInteractionNextFrame()
	{
		await UniTask.NextFrame();
		if (GameManager.GameState == GameState.None || base.BeingDestroyed)
		{
			return;
		}
		foreach (Button mainScreenButton in _mainScreenButtons)
		{
			mainScreenButton.interactable = IsOperable;
		}
	}

	public override void OnRenamed()
	{
		base.OnRenamed();
		SetDisplayName();
	}

	public virtual void SetDisplayName()
	{
		if ((bool)TitleText && ParentComputer != null)
		{
			TitleText.text = ParentComputer.AsThing().DisplayName.ToUpper();
		}
	}

	public override void OnDeviceListChanged()
	{
		base.OnDeviceListChanged();
		SetMainScreenVisibility();
		if (!_devicesChanged && GameManager.GameState != GameState.None)
		{
			_devicesChanged = true;
			DeviceListChangeTask().Forget();
		}
	}

	public override void OnInsertedToComputer(IComputer computer)
	{
		base.OnInsertedToComputer(computer);
		OnDeviceListChanged();
		SetDisplayName();
	}

	public override void OnSlaveStateChange()
	{
		base.OnSlaveStateChange();
		foreach (Button configScreenButton in _configScreenButtons)
		{
			configScreenButton.interactable = !base.MasterMotherboard;
		}
	}

	public void ButtonSearch()
	{
		if (InputWindow.ShowInputPanel(GameStrings.InputPanelFilterDevicesTitle, string.Empty, this, 32))
		{
			InputWindow.OnSubmit += InputFinished;
		}
	}

	private void InputFinished(string value, string value2)
	{
		Motherboard.UseComputer(16, base.netId, base.netId, -1, sendToAll: true, value.ToLowerInvariant());
		UpdateFilterButton();
	}

	public void ButtonClear()
	{
		Motherboard.UseComputer(17, base.netId, base.netId, -1, sendToAll: true);
	}

	public void UpdateFilterButton()
	{
		if (GameManager.GameState != GameState.Running)
		{
			return;
		}
		int num = 0;
		bool flag = !string.IsNullOrEmpty(FilterString);
		ClearFilterButton.gameObject.SetActive(flag);
		CreateFilterButton.gameObject.SetActive(!flag);
		foreach (ComputerButton button in Buttons)
		{
			if (flag && !button.AssignedDevice.DisplayName.ToLowerInvariant().Contains(FilterString))
			{
				button.Button.gameObject.SetActive(value: false);
				continue;
			}
			button.Button.gameObject.SetActive(value: true);
			num++;
		}
		ScrollBar.value = ((num == 0) ? 0f : ((float)(int)base.ScrollBarValueByte / (float)num));
	}

	public override string GetQuantityText()
	{
		return string.Empty;
	}

	public override string GetSecondaryNameText()
	{
		return Localization.GetThingName(PrefabName);
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteString(FilterString ?? string.Empty);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			FilterString = reader.ReadString();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteString(FilterString ?? string.Empty);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		FilterString = reader.ReadString();
	}

	public void OnButtonClick(ComputerButton computerButton)
	{
		Console console = computerButton.AssignedDevice as Console;
		if ((bool)console && IsEnslaveable(console))
		{
			if (this == console.CurrentMotherboard.MasterMotherboard)
			{
				Motherboard.UseComputer(8, base.netId, computerButton.AssignedDevice.netId, -1, sendToAll: true);
			}
			else
			{
				Motherboard.UseComputer(7, base.netId, computerButton.AssignedDevice.netId, -1, sendToAll: true);
			}
		}
		if (base.LinkedDevices.Contains(computerButton.AssignedDevice))
		{
			Motherboard.UseComputer(2, base.netId, computerButton.AssignedDevice.netId, -1, sendToAll: true);
		}
		else
		{
			Motherboard.UseComputer(1, base.netId, computerButton.AssignedDevice.netId, -1, sendToAll: true);
		}
	}

	private ComputerButton MakeButton(Device device)
	{
		GameObject gameObject = Object.Instantiate(ButtonPrefab);
		gameObject.transform.SetParent(DynamicGrid.transform, worldPositionStays: false);
		ComputerButton computerButton = new ComputerButton
		{
			AssignedDevice = device,
			Button = gameObject.GetComponent<Button>(),
			Parent = this,
			Label = gameObject.GetComponentInChildren<Text>()
		};
		computerButton.Label.text = device.DisplayName;
		computerButton.Button.onClick.AddListener(delegate
		{
			OnButtonClick(computerButton);
		});
		computerButton.SetColor();
		Buttons.Add(computerButton);
		return computerButton;
	}

	private async UniTaskVoid RedrawList()
	{
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		await UniTask.NextFrame(cancelToken);
		if (!cancelToken.IsCancellationRequested)
		{
			UpdateFilterButton();
		}
	}

	public override void MotherboardCommand(int command, Thing reference, int referenceInt, string text)
	{
		switch ((ButtonCommands)command)
		{
		case ButtonCommands.Toggle:
		{
			if (ParentComputer == null || !ParentComputer.DataCable)
			{
				break;
			}
			int count4 = base.LinkedDevices.Count;
			while (count4-- > 0)
			{
				Device device5 = base.LinkedDevices[count4];
				if (!IsDeviceConnected(device5) || !device5.AllowInteraction || (ButtonMode == InteractableType.Open && !device5.OnOff) || !GameManager.RunSimulation)
				{
					continue;
				}
				Interactable interactable = device5.GetInteractable(ButtonMode);
				if (interactable != null)
				{
					int state = ((interactable.State != 1) ? 1 : 0);
					OnServer.Interact(device5, ButtonMode, state);
					if (device5 is IPoweredVent poweredVent)
					{
						poweredVent.ResetVent();
					}
				}
			}
			break;
		}
		case ButtonCommands.AddDevice:
		{
			Device device6 = reference as Device;
			if ((bool)device6 && !base.LinkedDevices.Contains(device6))
			{
				AddLinkedDevice(device6);
				OnDeviceListChanged(device6);
			}
			break;
		}
		case ButtonCommands.RemoveDevice:
		{
			Device device3 = reference as Device;
			if ((bool)device3)
			{
				RemoveLinkedDevice(device3);
				OnDeviceListChanged(device3);
			}
			break;
		}
		case ButtonCommands.SetFlag:
			SetFlag(referenceInt);
			break;
		case ButtonCommands.OpenAll:
		case ButtonCommands.CloseAll:
		{
			if (ParentComputer == null || !ParentComputer.DataCable)
			{
				break;
			}
			int count2 = base.LinkedDevices.Count;
			while (count2-- > 0)
			{
				Device device2 = base.LinkedDevices[count2];
				if (IsDeviceConnected(device2) && device2.AllowInteraction && (ButtonMode != InteractableType.Open || device2.OnOff) && GameManager.RunSimulation)
				{
					OnServer.Interact(device2, ButtonMode, (command == 4) ? 1 : 0);
				}
			}
			break;
		}
		case ButtonCommands.Refresh:
			RefreshScreen();
			foreach (Motherboard slafe in Slaves)
			{
				slafe.RefreshScreen();
			}
			break;
		case ButtonCommands.AddSlave:
		{
			Computer computer = reference as Computer;
			if ((bool)computer && (bool)computer.CurrentMotherboard)
			{
				computer.CurrentMotherboard.MasterMotherboard = this;
				if (!Slaves.Contains(computer.CurrentMotherboard))
				{
					Slaves.Add(computer.CurrentMotherboard);
				}
			}
			break;
		}
		case ButtonCommands.AddSlaveDirect:
		{
			Motherboard motherboard = reference as Motherboard;
			if ((bool)motherboard)
			{
				motherboard.MasterMotherboard = this;
				if (!Slaves.Contains(motherboard))
				{
					Slaves.Add(motherboard);
				}
			}
			break;
		}
		case ButtonCommands.RemoveSlave:
		{
			Computer computer2 = reference as Computer;
			if ((bool)computer2 && (bool)computer2.CurrentMotherboard)
			{
				Slaves.Remove(computer2.CurrentMotherboard);
				computer2.CurrentMotherboard.MasterMotherboard = null;
			}
			break;
		}
		case ButtonCommands.PowerOn:
		case ButtonCommands.PowerOff:
		{
			if (ParentComputer == null || !ParentComputer.DataCable)
			{
				break;
			}
			int count3 = base.LinkedDevices.Count;
			while (count3-- > 0)
			{
				Device device4 = base.LinkedDevices[count3];
				if (IsDeviceConnected(device4) && device4.AllowInteraction && GameManager.RunSimulation)
				{
					OnServer.Interact(device4, ButtonMode, (command == 10) ? 1 : 0);
				}
			}
			break;
		}
		case ButtonCommands.Mode0:
		case ButtonCommands.Mode1:
		{
			if (ParentComputer == null || !ParentComputer.DataCable)
			{
				break;
			}
			int count = base.LinkedDevices.Count;
			while (count-- > 0)
			{
				Device device = base.LinkedDevices[count];
				if (IsDeviceConnected(device) && device.AllowInteraction && GameManager.RunSimulation)
				{
					OnServer.Interact(device, ButtonMode, (command == 12) ? 1 : 0);
				}
			}
			break;
		}
		case ButtonCommands.ClearFilter:
			FilterString = string.Empty;
			break;
		case ButtonCommands.SetFilter:
			FilterString = text;
			break;
		}
		base.MotherboardCommand(command, reference, referenceInt, text);
	}
}
