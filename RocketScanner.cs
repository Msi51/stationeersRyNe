using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Networks;
using Objects.Items;
using Objects.Rockets;
using Objects.Rockets.Scanning;
using Trading;
using UnityEngine;

public class RocketScanner : Device, IRocketInternals, IRocketComponent, ISmartRotatable, IRocketActionProgressable, IReferencable, IEvaluable, IRocketMassContributor
{
	public float CycleTime = 30f;

	public int Points = 100;

	public float DiscoveryMultiplier = 1f;

	public float SurveyMultiplier = 1f;

	private float _currentCycleTime;

	public float RocketMass = 50f;

	private RocketScanningHead _scanningHead;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public float MassContribution => RocketMass;

	public float CurrentCycleTime
	{
		get
		{
			return _currentCycleTime;
		}
		set
		{
			_currentCycleTime = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public float GetActionProgress => CurrentCycleTime / CycleTime;

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

	public bool HasHead<T>(out T head)
	{
		if (_scanningHead is T val)
		{
			head = val;
			return true;
		}
		head = default(T);
		return false;
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.RocketPayloadCategory);
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild is RocketScanningHead scanningHead)
		{
			_scanningHead = scanningHead;
		}
		Error = (((object)_scanningHead == null) ? 1 : 0);
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (previousChild is RocketScanningHead rocketScanningHead && _scanningHead == rocketScanningHead)
		{
			_scanningHead = null;
		}
		Error = (((object)_scanningHead == null) ? 1 : 0);
	}

	public void ResetCycleTime()
	{
		CurrentCycleTime = 0f;
	}

	public void Progress(ScanAction action, float amount)
	{
		CurrentCycleTime += amount;
		if (CurrentCycleTime > CycleTime)
		{
			ResetCycleTime();
			action.DoScanCycle(this);
			action.OnCycleComplete();
		}
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketScannerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RocketScannerSaveData rocketScannerSaveData)
		{
			CurrentCycleTime = rocketScannerSaveData.CurrentCycleTime;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RocketScannerSaveData rocketScannerSaveData)
		{
			rocketScannerSaveData.CurrentCycleTime = CurrentCycleTime;
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(CurrentCycleTime);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		CurrentCycleTime = reader.ReadSingle();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(CurrentCycleTime);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			CurrentCycleTime = reader.ReadSingle();
		}
	}

	public RocketActionResult CanProgressAction(ScanAction action)
	{
		if ((object)_scanningHead == null)
		{
			return RocketActionResult.Failure(GameStrings.RocketScannerHasNoScanningHead, this);
		}
		if (action is SurfaceScan)
		{
			if (!(_scanningHead is RocketDeepScanningHead))
			{
				return RocketActionResult.Failure(GameStrings.RocketScannerWrongHeadType, this);
			}
		}
		else if (_scanningHead is RocketDeepScanningHead)
		{
			return RocketActionResult.Failure(GameStrings.RocketScannerWrongHeadType, this);
		}
		if (_scanningHead.Quantity <= 0f)
		{
			return RocketActionResult.Failure(GameStrings.RocketScannerHeadWornOut, this);
		}
		if (!OnOff || !Powered)
		{
			return RocketActionResult.Failure(GameStrings.DeviceOffOrUnpowered, this);
		}
		return RocketActionResult.Success;
	}

	public void DoScanOnLinkedMotherboards()
	{
		foreach (Device device in base.DataCableNetwork.DeviceList)
		{
			if (!(device is RocketDataDownLink rocketDataDownLink) || !GetConnectedDevice<RocketAvionicsDevice>(rocketDataDownLink.DataCableNetwork, out var deviceAsT) || !deviceAsT.GetIsOperable() || !deviceAsT.GetOrbitalPosition(out var orbitalPosition))
			{
				continue;
			}
			foreach (IReceiveDataNetworkDevices connectedDataNetReceiver in rocketDataDownLink.ConnectedDataNetReceivers)
			{
				if (!(connectedDataNetReceiver is RocketDataUpLink rocketDataUpLink) || !rocketDataUpLink.GetIsOperable() || !rocketDataUpLink.OnOff || !rocketDataUpLink.Powered)
				{
					continue;
				}
				foreach (Device device2 in rocketDataUpLink.DataCableNetwork.DeviceList)
				{
					if (device2 is IComputer { CurrentMotherboard: MapMotherboard currentMotherboard })
					{
						currentMotherboard.MapUpdate(orbitalPosition);
					}
				}
			}
		}
	}

	private bool GetConnectedDevice<T>(CableNetwork network, out T deviceAsT)
	{
		if (network == null)
		{
			deviceAsT = default(T);
			return false;
		}
		foreach (Device device in network.DeviceList)
		{
			if (device is T val)
			{
				deviceAsT = val;
				return true;
			}
		}
		deviceAsT = default(T);
		return false;
	}

	public string GetActionInfoText()
	{
		if ((object)_scanningHead == null)
		{
			return GameStrings.RocketScanNoHeadInfo.AsString();
		}
		if (_scanningHead is RocketDeepScanningHead)
		{
			return GameStrings.RocketSurfaceScanActionInfo.AsString(OnOff ? "On" : "Off");
		}
		return GameStrings.RocketScanActionInfo.AsString(OnOff ? "On" : "Off", StringManager.Get(Points), StringManager.Get(CycleTime));
	}

	public bool CanProgressAction(out RocketActionResult result)
	{
		return result = RocketActionResult.Success;
	}

	public void ClearAction()
	{
	}
}
