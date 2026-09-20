using Assets.Scripts.Inventory;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using TMPro;

namespace Assets.Scripts.Objects.Items;

public class NetworkAnalyser : Cartridge
{
	public TextMeshProUGUI OutputText;

	public TextMeshProUGUI PowerUsed;

	public TextMeshProUGUI PowerAvailable;

	public TextMeshProUGUI PowerRequired;

	public TextMeshProUGUI TotalDevices;

	private string _devicesValueText = string.Empty;

	private string _actualValueText = string.Empty;

	private string _potentialValueText = string.Empty;

	private string _requiredValueText = string.Empty;

	private string _selectedText = string.Empty;

	private string _outputText = string.Empty;

	private CableNetwork _scannedNetwork;

	private CableNetwork _lastScannedNetwork;

	private string _tempDeviceOutput;

	private bool _isActiveHand;

	private bool _needTopScroll;

	private CableNetwork GetScannedNetwork()
	{
		if (!RootParent || !RootParent.HasAuthority || !CursorManager.CursorThing)
		{
			return null;
		}
		if (!InPlayerHand())
		{
			return null;
		}
		Cable cable = CursorManager.CursorThing as Cable;
		if (!cable)
		{
			return null;
		}
		return cable.CableNetwork;
	}

	public override void Start()
	{
		base.Start();
		OutputText.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
		OnActiveHandChanged();
		OutputText.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
		PowerUsed.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
		PowerAvailable.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
		PowerRequired.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
		TotalDevices.font = (Localization.CurrentFont ? Localization.CurrentFont : InventoryManager.Instance.DefualtFont);
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (parent is Tablet)
		{
			InventoryManager.OnActiveHandChanged += OnActiveHandChanged;
		}
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		if (oldParent is Tablet)
		{
			InventoryManager.OnActiveHandChanged -= OnActiveHandChanged;
		}
	}

	private void OnActiveHandChanged()
	{
		if ((bool)InventoryManager.Instance && InventoryManager.ActiveHandSlot != null)
		{
			AdvancedTablet advancedTablet = InventoryManager.ActiveHandSlot.Occupant as AdvancedTablet;
			if (advancedTablet != null && advancedTablet.CartridgeSlots.Exists((Slot s) => s.Occupant == this))
			{
				_isActiveHand = true;
				return;
			}
			Tablet tablet = InventoryManager.ActiveHandSlot.Occupant as Tablet;
			if (tablet != null && tablet.Cartridge == this)
			{
				_isActiveHand = true;
				return;
			}
		}
		_isActiveHand = false;
	}

	public override void OnPreScreenUpdate()
	{
		base.OnPreScreenUpdate();
		if (!_isActiveHand)
		{
			return;
		}
		_scannedNetwork = GetScannedNetwork();
		if (_scannedNetwork != null)
		{
			_selectedText = Localization.GetInterface("NetworkAnalyserNetwork") + " " + StringManager.Get(_scannedNetwork.ReferenceId);
			_actualValueText = _scannedNetwork.CurrentLoad.ToStringPrefix(Localization.GetInterface("WattEnergyMeasurement"));
			_potentialValueText = _scannedNetwork.PotentialLoad.ToStringPrefix(Localization.GetInterface("WattEnergyMeasurement"));
			_requiredValueText = _scannedNetwork.RequiredLoad.ToStringPrefix(Localization.GetInterface("WattEnergyMeasurement"));
			_devicesValueText = _scannedNetwork.DeviceList.Count.ToString();
			_tempDeviceOutput = string.Empty;
			if (_lastScannedNetwork != _scannedNetwork)
			{
				_needTopScroll = true;
			}
			_lastScannedNetwork = _scannedNetwork;
			int num = 0;
			foreach (Device device in _scannedNetwork.DeviceList)
			{
				num++;
				_tempDeviceOutput += $"\n{StringManager.Get(num)}.{device.DisplayName}";
				_tempDeviceOutput += $"<#A9A9A9> ";
				if (device.HasOnOffState)
				{
					_tempDeviceOutput = _tempDeviceOutput + "..." + (device.OnOff ? ActionStrings.On : ActionStrings.Off);
				}
				if (device.HasPowerState)
				{
					_tempDeviceOutput = _tempDeviceOutput + "..." + (device.Powered ? device.GetUsedPower(device.PowerCableNetwork).ToStringPrefix(Localization.GetInterface("WattEnergyMeasurement")) : ActionStrings.Unpowered);
				}
				if (device.HasOpenState)
				{
					_tempDeviceOutput = _tempDeviceOutput + "..." + (device.IsOpen ? ActionStrings.Opened : ActionStrings.Closed);
				}
				if (device.HasErrorState && device.Error >= 1)
				{
					_tempDeviceOutput = _tempDeviceOutput + "..." + ActionStrings.Error;
				}
				_tempDeviceOutput += $" </color>";
			}
			_outputText = _tempDeviceOutput;
		}
		else
		{
			_selectedText = Localization.GetInterface("NotApplicableString");
			_actualValueText = Localization.GetInterface("NotApplicableString");
			_potentialValueText = Localization.GetInterface("NotApplicableString");
			_devicesValueText = Localization.GetInterface("NotApplicableString");
			_requiredValueText = Localization.GetInterface("NotApplicableString");
			_outputText = string.Empty;
		}
	}

	public override void OnScreenUpdate()
	{
		base.OnScreenUpdate();
		SelectedTitle.text = _selectedText;
		PowerUsed.text = _actualValueText;
		PowerAvailable.text = _potentialValueText;
		PowerRequired.text = _requiredValueText;
		TotalDevices.text = _devicesValueText;
		OutputText.text = _outputText;
		if (_needTopScroll)
		{
			_needTopScroll = false;
			_scrollPanel.SetScrollPosition(0f);
		}
		_scrollPanel.SetContentHeight(OutputText.preferredHeight);
	}
}
