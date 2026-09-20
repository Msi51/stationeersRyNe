using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards.Comms;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI.Motherboard;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using UnityEngine;

namespace Assets.Scripts.Objects.Motherboards;

public class CommsMotherboard : Motherboard, ITrading
{
	[SerializeField]
	private CommsUIPanel _uiPanel;

	[XmlElement]
	[HideInInspector]
	public int _filterByEnumToInt = 3;

	[Header("Comms Motherboard")]
	private bool _devicesChanged;

	public readonly List<SatelliteDish> SatelliteDishes = new List<SatelliteDish>();

	private SatelliteDish _selectedDish;

	private int _selectedDishIndex;

	public readonly List<ITraderDestination> LandingPads = new List<ITraderDestination>();

	private ITraderDestination _selectedLandingPad;

	private int _selectedPadIndex;

	public readonly List<VendingMachine> Vendors = new List<VendingMachine>();

	public static readonly List<CommsMotherboard> CommsMotherboards = new List<CommsMotherboard>();

	private long _savedDishId;

	private long _savedPadId;

	private CommsMotherboardUIData _displayedData;

	private CommsMotherboardUIData _newDisplayData;

	public int FilterByEnumToInt
	{
		get
		{
			return _filterByEnumToInt;
		}
		set
		{
			if (_filterByEnumToInt != value)
			{
				_filterByEnumToInt = value;
				HandleFilter();
			}
		}
	}

	public SatelliteDish SelectedDish
	{
		get
		{
			return _selectedDish;
		}
		set
		{
			if (SelectedDish != value)
			{
				if (SelectedDish != null)
				{
					SatelliteDish selectedDish = SelectedDish;
					selectedDish.OnSignalsUpdated = (Action<SatelliteDish>)Delegate.Remove(selectedDish.OnSignalsUpdated, new Action<SatelliteDish>(SignalsUpdated));
				}
				if (value != null)
				{
					value.OnSignalsUpdated = (Action<SatelliteDish>)Delegate.Combine(value.OnSignalsUpdated, new Action<SatelliteDish>(SignalsUpdated));
				}
			}
			_selectedDish = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	public int SelectedDishIndex
	{
		get
		{
			return _selectedDishIndex;
		}
		set
		{
			_selectedDishIndex = value;
			if (GameManager.GameState != GameState.Loading)
			{
				SelectDish();
			}
		}
	}

	public ITraderDestination SelectedLandingPad
	{
		get
		{
			return _selectedLandingPad;
		}
		set
		{
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
			_selectedLandingPad = value;
		}
	}

	public int SelectedPadIndex
	{
		get
		{
			return _selectedPadIndex;
		}
		set
		{
			_selectedPadIndex = value;
			if (GameManager.GameState != GameState.Loading)
			{
				SelectLandingPad();
			}
		}
	}

	public override bool IsError => SatelliteDishes.Count == 0;

	private void SelectDish()
	{
		if (GameManager.RunSimulation)
		{
			SelectedDish = ((SatelliteDishes.Count > 0) ? SatelliteDishes[Mathf.Min(SatelliteDishes.Count - 1, SelectedDishIndex)] : null);
		}
	}

	private void SelectLandingPad()
	{
		if (GameManager.RunSimulation)
		{
			SelectedLandingPad = ((LandingPads.Count > 0) ? LandingPads[Mathf.Min(LandingPads.Count - 1, SelectedPadIndex)] : null);
		}
	}

	public override void Awake()
	{
		base.Awake();
		if (!IsCursor && GameManager.GameState != GameState.None)
		{
			_uiPanel.Initialise(this);
			TraderContact.OnInitialized += RefreshScreen;
			CommsMotherboards.Add(this);
		}
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			TraderContact.OnInitialized -= RefreshScreen;
			CommsMotherboards.Remove(this);
			_uiPanel.MotherboardDestroyed();
			base.OnDestroy();
		}
	}

	public override void MotherboardCommand(int command, Thing reference, int referenceInt, string text)
	{
		base.MotherboardCommand(command, reference, referenceInt, text);
		switch ((ButtonCommands)command)
		{
		case ButtonCommands.Special1:
			SelectedPadIndex = referenceInt;
			_uiPanel.ContactsTab.SelectPad(SelectedPadIndex);
			UpdateDishToggleUi();
			RefreshScreen();
			break;
		case ButtonCommands.Special2:
			SelectedDishIndex = referenceInt;
			_uiPanel.ContactsTab.SelectSatellite(SelectedDishIndex);
			UpdateDishToggleUi();
			RefreshScreen();
			break;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new CommsMotherboardSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is CommsMotherboardSaveData commsMotherboardSaveData)
		{
			FilterByEnumToInt = commsMotherboardSaveData.EnumFilterInt;
			SelectedDishIndex = commsMotherboardSaveData.SelectedDishIndex;
			SelectedPadIndex = commsMotherboardSaveData.SelectedPadIndex;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is CommsMotherboardSaveData commsMotherboardSaveData)
		{
			commsMotherboardSaveData.EnumFilterInt = FilterByEnumToInt;
			commsMotherboardSaveData.SelectedDishIndex = SelectedDishIndex;
			commsMotherboardSaveData.SelectedPadIndex = SelectedPadIndex;
		}
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		SelectedDishIndex = reader.ReadInt32();
		SelectedPadIndex = reader.ReadInt32();
		_savedDishId = reader.ReadInt64();
		_savedPadId = reader.ReadInt64();
		UpdateDishToggleUi();
		RefreshScreen();
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(SelectedDishIndex);
		writer.WriteInt32(SelectedPadIndex);
		writer.WriteInt64(SelectedDish?.ReferenceId ?? 0);
		writer.WriteInt64(SelectedLandingPad?.ReferenceId ?? 0);
	}

	public override void LinkDevicesOnJoinComplete()
	{
		SelectedDish = Thing.Find<SatelliteDish>(_savedDishId);
		SelectedLandingPad = Thing.Find<ITraderDestination>(_savedPadId);
		RefreshScreen();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteInt64(SelectedDish?.ReferenceId ?? 0);
			writer.WriteInt64(SelectedLandingPad?.ReferenceId ?? 0);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			SelectedDish = Thing.Find<SatelliteDish>(reader.ReadInt64());
			SelectedLandingPad = Thing.Find<ITraderDestination>(reader.ReadInt64());
			RefreshScreen();
		}
	}

	public async UniTask HandleDeviceListChange()
	{
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		while (GameManager.GameState != GameState.Running && this != null && !base.IsBeingDestroyed)
		{
			await UniTask.NextFrame();
		}
		await UniTask.NextFrame();
		if (ParentComputer == null || this == null || base.IsBeingDestroyed || !ParentComputer.AsThing().isActiveAndEnabled)
		{
			return;
		}
		List<ILogicable> list = ParentComputer.DeviceList();
		SatelliteDishes.Clear();
		LandingPads.Clear();
		Vendors.Clear();
		foreach (ILogicable item4 in list)
		{
			if (!(item4 is SatelliteDish item))
			{
				if (!(item4 is ITraderDestination item2))
				{
					if (!(item4 is LandingPadModularDevice { LandingPadCenter: var landingPadCenter }))
					{
						if (item4 is VendingMachine item3)
						{
							Vendors.Add(item3);
						}
					}
					else if ((object)landingPadCenter != null && !LandingPads.Contains(landingPadCenter))
					{
						LandingPads.Add(landingPadCenter);
					}
				}
				else if (!LandingPads.Contains(item2))
				{
					LandingPads.Add(item2);
				}
			}
			else
			{
				SatelliteDishes.Add(item);
			}
		}
		if (LandingPads.Count > 0)
		{
			LandingPads.Sort((ITraderDestination a, ITraderDestination b) => (a.ReferenceId > b.ReferenceId) ? 1 : (-1));
		}
		UpdateDishToggleUi();
		SelectDish();
		SelectLandingPad();
		RefreshScreen();
		_devicesChanged = false;
	}

	private void UpdateDishToggleUi()
	{
		_uiPanel.ContactsTab.UpdateSatelliteToggleGroup(SatelliteDishes.Count, SelectedDishIndex);
		_uiPanel.ContactsTab.UpdatePadToggleGroup(LandingPads.Count, SelectedPadIndex);
	}

	public override void OnDeviceListChanged()
	{
		base.OnDeviceListChanged();
		if (!_devicesChanged)
		{
			_devicesChanged = true;
			HandleDeviceListChange().Forget();
		}
	}

	private void HandleFilter()
	{
		RefreshScreen();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SelectDish();
		SelectLandingPad();
		RefreshScreen();
	}

	private void SignalsUpdated(SatelliteDish dish)
	{
		if (dish == SelectedDish)
		{
			RefreshScreen();
		}
	}

	public override void RefreshScreen()
	{
		base.RefreshScreen();
		if (ParentComputer != null)
		{
			UpdateDisplayData();
			RefreshScreenAsync().Forget();
		}
	}

	private void UpdateDisplayData()
	{
		_newDisplayData = new CommsMotherboardUIData(SelectedDish, SelectedLandingPad);
	}

	private async UniTaskVoid RefreshScreenAsync()
	{
		await UniTask.SwitchToMainThread();
		if (ParentComputer == null || ParentComputer.IsBeingDestroyed || !ParentComputer.ShowComputerScreen || _displayedData.Equals(_newDisplayData))
		{
			return;
		}
		if (!_displayedData.IsValid || _displayedData.SelectedDish != _newDisplayData.SelectedDish)
		{
			_uiPanel.ContactsTab.UpdateSatelliteText((SelectedDish == null) ? GameStrings.OperatorNone.DisplayString : SelectedDish.DisplayName);
		}
		if (!_displayedData.IsValid || _displayedData.SelectedLandingPad != _newDisplayData.SelectedLandingPad)
		{
			_uiPanel.ContactsTab.UpdatePadText((SelectedLandingPad == null) ? GameStrings.OperatorNone.DisplayString : SelectedLandingPad.DisplayName);
		}
		if ((object)SelectedDish == null || !SelectedDish.Powered || !SelectedDish.OnOff)
		{
			_uiPanel.ContactsTab.ClearAllContactItems();
			_displayedData = CommsMotherboardUIData.Invalid;
			return;
		}
		if (_newDisplayData.ScannedContactsDatas != null)
		{
			while (_uiPanel.ContactsTab.ContactItems.Count < _newDisplayData.ScannedContactsDatas.Length)
			{
				_uiPanel.ContactsTab.AddContactItem();
			}
			for (int i = 0; i < _uiPanel.ContactsTab.ContactItems.Count; i++)
			{
				ScreenContact screenContact = _uiPanel.ContactsTab.ContactItems[i];
				if (i < _newDisplayData.ScannedContactsDatas.Length)
				{
					screenContact.Assign(_newDisplayData.ScannedContactsDatas[i]);
				}
				else
				{
					screenContact.Clear();
				}
			}
		}
		_uiPanel.ContactsTab.RefreshScrollPanel();
		_displayedData = _newDisplayData;
	}

	public override void OnInsertedToComputer(IComputer computer)
	{
		base.OnInsertedToComputer(computer);
		_uiPanel.MotherboardInserted();
		SelectedDishIndex = 0;
		SelectedPadIndex = 0;
		if (!_devicesChanged)
		{
			_devicesChanged = true;
			HandleDeviceListChange().Forget();
		}
	}
}
