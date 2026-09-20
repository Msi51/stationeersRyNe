using System;
using System.Text;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class BatteryCell : ItemRenamable, IAtmospherical, ISpatial, IPhysical, IProfile, IDensePoolable, ILogicable, IReferencable, IEvaluable, IChargable, IPowered
{
	[Header("Battery")]
	[SerializeField]
	private Material[] batteryStateMaterials;

	private static readonly int ChargeMaterialIndex = 1;

	[SerializeField]
	private MeshRenderer batteryRenderer;

	private bool _destroyed;

	private bool _powerUpdate;

	private string _modeString;

	public Event OnBatteryChangeChargeState;

	private BatteryCellState _batteryState;

	private UniTask _batteryCriticalTask;

	public static string[] BatteryStateStrings = Enum.GetNames(typeof(BatteryCellState));

	private float _powerStored;

	public float PowerMaximum = 36000f;

	private int _previousBatteryPercentage;

	private byte _currentPowerPercentage;

	public static float _lossInCold = 50f;

	public BatteryCellState BatteryState
	{
		get
		{
			return _batteryState;
		}
		private set
		{
			_batteryState = value;
			if (OnBatteryChangeChargeState != null)
			{
				OnBatteryChangeChargeState();
			}
		}
	}

	public override string[] ModeStrings => BatteryStateStrings;

	public float PowerStored
	{
		get
		{
			return _powerStored;
		}
		set
		{
			if (float.IsNaN(value))
			{
				return;
			}
			if (value >= PowerMaximum && _powerStored < PowerMaximum && this.OnFullyCharged != null)
			{
				if (GameManager.IsThread)
				{
					UnityMainThreadDispatcher.Instance().Enqueue(delegate
					{
						this.OnFullyCharged();
					});
					return;
				}
				this.OnFullyCharged();
			}
			_powerStored = Mathf.Clamp(value, 0f, PowerMaximum);
		}
	}

	public override float GetQuantity => _powerStored / PowerMaximum;

	public float PowerRatio => PowerStored / PowerMaximum;

	public float PowerDelta => PowerMaximum - PowerStored;

	public bool IsEmpty => Mode == 0;

	public bool IsCritical => Mode <= 1;

	public bool IsLow => Mode <= 3;

	public bool IsCharged => Mode == 6;

	[ByteArraySync]
	public byte CurrentPowerPercentage
	{
		get
		{
			return _currentPowerPercentage;
		}
		set
		{
			if (value != _currentPowerPercentage)
			{
				_currentPowerPercentage = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
				else
				{
					base.ParentSlot?.RefreshQuantity();
				}
			}
		}
	}

	public event Event OnFullyCharged;

	public float GetPowerMaximum()
	{
		return PowerMaximum;
	}

	private async UniTask CriticalBatteryAnim()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		while (!cancelToken.IsCancellationRequested && BatteryState == BatteryCellState.Critical)
		{
			await UniTask.Delay(250, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				break;
			}
			SetBatteryStateMaterial(BatteryCellState.Empty);
			await UniTask.Delay(250, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				break;
			}
			SetBatteryStateMaterial(BatteryCellState.Critical);
		}
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.AppendLine(GameStrings.ItemInSlotValue.AsString(ToTooltip(), GetQuantityText()));
		return extendedText;
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.BatteryCategory);
	}

	public override object GetModXmlType()
	{
		return new BatteryCellData();
	}

	public override void DeserializModData(ThingModData modData)
	{
		base.DeserializModData(modData);
		if (modData is BatteryCellData batteryCellData && !float.IsNaN(batteryCellData.PowerMaximum))
		{
			PowerMaximum = batteryCellData.PowerMaximum;
		}
	}

	public static float GetLogicValue(DynamicThing dynamicThing, LogicSlotType logicSlotType)
	{
		BatteryCell batteryCell = dynamicThing as BatteryCell;
		if (!batteryCell)
		{
			return 0f;
		}
		return logicSlotType switch
		{
			LogicSlotType.Charge => batteryCell.PowerStored, 
			LogicSlotType.ChargeRatio => batteryCell.PowerRatio, 
			_ => 0f, 
		};
	}

	public override void Awake()
	{
		base.Awake();
		ElectricityManager.Register(this);
		SetBatteryStateMaterial(BatteryState);
	}

	private void SetBatteryStateMaterial(BatteryCellState state)
	{
		if (!(batteryRenderer == null))
		{
			Material[] sharedMaterials = batteryRenderer.sharedMaterials;
			if (sharedMaterials != null && sharedMaterials.Length >= 1)
			{
				sharedMaterials[1] = batteryStateMaterials[(int)state];
				batteryRenderer.materials = sharedMaterials;
			}
		}
	}

	public override string GetQuantityText()
	{
		return StringManager.Get(CurrentPowerPercentage) + "<size=75%>%</size>";
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		_modeString = BatteryStateStrings[Mode];
		BatteryState = (BatteryCellState)Mode;
		SetBatteryStateMaterial(BatteryState);
		if (BatteryState == BatteryCellState.Critical && _batteryCriticalTask.Status != UniTaskStatus.Pending)
		{
			_batteryCriticalTask = CriticalBatteryAnim();
		}
		if (base.ParentSlot != null)
		{
			base.ParentSlot.Parent.OnChildBatteryCellChange(this);
			if (base.ParentSlot != null && base.ParentSlot.Display != null)
			{
				base.ParentSlot.RefreshQuantity();
			}
		}
	}

	public override void OnFinishedInteractionSync(Interactable interactable)
	{
		base.OnFinishedInteractionSync(interactable);
		_modeString = BatteryStateStrings[Mode];
		BatteryState = (BatteryCellState)Mode;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		result.Action = _modeString;
		return result;
	}

	public float AddPowerSafe(float availablePower)
	{
		float num = Mathf.Min(PowerDelta, availablePower);
		PowerStored += num;
		return availablePower - num;
	}

	public BatteryCellState GetPowerState()
	{
		return BatteryState;
	}

	public override void PhysicsUpdate()
	{
		base.PhysicsUpdate();
		if (!GameManager.RunSimulation && CurrentPowerPercentage != _previousBatteryPercentage)
		{
			_previousBatteryPercentage = CurrentPowerPercentage * 100;
			base.ParentSlot?.RefreshQuantity();
		}
	}

	public override float CalculateUniqueRatioIdentifier()
	{
		return _powerStored / PowerMaximum;
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if ((bool)InventoryManager.ParentHuman && InventoryManager.ParentHuman.UnlimitedPower)
		{
			_powerStored = PowerMaximum;
		}
		if (_destroyed)
		{
			return;
		}
		CurrentPowerPercentage = (byte)(Math.Round(_powerStored / PowerMaximum, 2) * 100.0);
		if (CurrentPowerPercentage != _previousBatteryPercentage)
		{
			_previousBatteryPercentage = CurrentPowerPercentage;
			if (base.ParentSlot != null)
			{
				base.ParentSlot.RefreshQuantity();
			}
		}
		UpdateBatteryState();
		if (!_powerUpdate && GameManager.RunSimulation && BatteryState != (BatteryCellState)Mode)
		{
			_powerUpdate = true;
			WaitThenUpdateChargeDisplay().Forget();
		}
		if (base.ParentSlot == null)
		{
			HandleColdLoss(base.WorldAtmosphere, 1f);
		}
		else if ((bool)base.ParentSlot.Occupant)
		{
			if (base.ParentSlot.Occupant.InternalAtmosphere != null)
			{
				HandleColdLoss(base.ParentSlot.Occupant.InternalAtmosphere, 1f);
			}
			else
			{
				HandleColdLoss(base.ParentSlot.Occupant.WorldAtmosphere, base.ParentSlot.Occupant.ThermodynamicsScale);
			}
		}
	}

	public void HandleColdLoss(Atmosphere atmosphere, float scale)
	{
		if (atmosphere != null && atmosphere.Temperature < Chemistry.Temperature.ZeroDegrees)
		{
			float num = (TemperatureKelvin.One - atmosphere.Temperature / Chemistry.Temperature.ZeroDegrees).ToFloat();
			PowerStored -= _lossInCold * num * scale * atmosphere.HeatExchangeRatio();
		}
	}

	private async UniTaskVoid WaitThenUpdateChargeDisplay()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		await UniTask.Delay(500);
		Mode = (int)BatteryState;
		OnServer.Interact(base.InteractMode, Mode);
		_powerUpdate = false;
	}

	public void UpdateBatteryState()
	{
		float num = _powerStored / PowerMaximum;
		BatteryCellState batteryState = ((num >= 0.5f) ? ((num >= 0.999f) ? BatteryCellState.Full : ((!(num >= 0.75f)) ? BatteryCellState.Medium : BatteryCellState.High)) : ((num >= 0.1f) ? ((!(num >= 0.25f)) ? BatteryCellState.VeryLow : BatteryCellState.Low) : ((num > 0f) ? BatteryCellState.Critical : BatteryCellState.Empty)));
		BatteryState = batteryState;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new BatteryCellSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is BatteryCellSaveData batteryCellSaveData)
		{
			PowerStored = batteryCellSaveData.PowerStored;
			_modeString = BatteryStateStrings[Mode];
			BatteryState = (BatteryCellState)Mode;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is BatteryCellSaveData batteryCellSaveData)
		{
			batteryCellSaveData.PowerStored = PowerStored;
		}
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
			_destroyed = true;
			if (GameManager.GameState != GameState.None)
			{
				ElectricityManager.Deregister(this);
			}
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteByte(CurrentPowerPercentage);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			CurrentPowerPercentage = reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte(CurrentPowerPercentage);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		CurrentPowerPercentage = reader.ReadByte();
	}
}
