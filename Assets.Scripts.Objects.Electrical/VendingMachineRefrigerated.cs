using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class VendingMachineRefrigerated : VendingMachine, ISetable, ILogicable, IReferencable, IEvaluable, IThermal
{
	private static readonly MoleEnergy HeatTransferJoulesPerTick = new MoleEnergy(1000.0);

	private static readonly TemperatureKelvin GoalTemperature = new TemperatureKelvin(142.0);

	private float _powerUsedDuringTick;

	public static float DoorOpenThermodynamicsChange = 0.01f;

	private Atmosphere _worldAtmosphere;

	private double _setting = 50.0;

	public Transform Wheel;

	public float WheelSpinSpeed = 10f;

	private float MaxWheelSetting = 500f;

	private Quaternion _wheelBaseRotation;

	private float _lastAngle;

	private UniTask _rotatingWheel;

	[ByteArraySync]
	public double Setting
	{
		get
		{
			return _setting;
		}
		set
		{
			if (value < 1.0)
			{
				return;
			}
			_setting = value;
			if (GameManager.GameState == GameState.Running)
			{
				CheckWheel();
			}
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
			if (!GameManager.RunSimulation || this.OnStackerSettingChanged == null)
			{
				return;
			}
			if (GameManager.IsThread)
			{
				UnityMainThreadDispatcher.Instance().Enqueue(delegate
				{
					this.OnStackerSettingChanged();
				});
			}
			this.OnStackerSettingChanged();
		}
	}

	public int SettingInt => (int)(Setting + 1E-06);

	public override float ConvectionFactor => ThermodynamicsScale + (IsOpen ? DoorOpenThermodynamicsChange : 0f);

	protected override bool IsOperable => true;

	public override bool HasReadableAtmosphere => true;

	public event Event OnStackerSettingChanged;

	public override void Awake()
	{
		base.Awake();
		_wheelBaseRotation = Wheel.localRotation;
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(10.0), 0L);
			base.InternalAtmosphere.GasMixture.Add(new Mole(Chemistry.GasType.Nitrogen, new MoleQuantity(0.8579999804496765), MoleEnergy.Zero));
			Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
			if (atmosphere != null && atmosphere.IsAboveArmstrong())
			{
				base.InternalAtmosphere.GasMixture.TotalEnergy = IdealGas.Energy(base.InternalAtmosphere.GasMixture.HeatCapacity, atmosphere.Temperature);
				return;
			}
			TemperatureKelvin twentyDegrees = Chemistry.Temperature.TwentyDegrees;
			base.InternalAtmosphere.GasMixture.TotalEnergy = IdealGas.Energy(base.InternalAtmosphere.GasMixture.HeatCapacity, twentyDegrees);
		}
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

	protected override void TryProcessImport()
	{
		if ((object)ImportingThing != null)
		{
			FindFreeSlot();
		}
		OnServer.Interact(base.InteractImport, 0);
	}

	private void FindFreeSlot()
	{
		List<Slot> list = new List<Slot>();
		List<Slot> list2 = new List<Slot>();
		for (int i = 2; i < Slots.Count; i++)
		{
			if (!Slots[i].IsInteractable)
			{
				if ((object)Slots[i].Occupant != null)
				{
					list.Add(Slots[i]);
				}
				else
				{
					list2.Add(Slots[i]);
				}
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			if ((object)ImportingThing == null)
			{
				return;
			}
			Slot slot = list[j];
			if (slot.Occupant.PrefabHash == ImportingThing.PrefabHash)
			{
				if (ImportingThing is Stackable mergeable && slot.Occupant is Stackable stackable && stackable.Quantity < SettingInt)
				{
					stackable.Merge(mergeable);
				}
			}
			else if (j == Slots.Count - 1)
			{
				PlayNetworkSound(FabricatorBase.ImportErrorHash);
			}
		}
		foreach (Slot item in list2)
		{
			DynamicThing importingThing = ImportingThing;
			if ((object)importingThing == null)
			{
				break;
			}
			if (importingThing is Stackable stackable2)
			{
				if (stackable2.Quantity <= 0)
				{
					break;
				}
				Stackable stackable3 = stackable2;
				if (stackable3.Quantity > SettingInt || stackable3.Quantity > stackable3.MaxQuantity)
				{
					stackable3.SplitStack(Mathf.Min(stackable3.MaxQuantity, SettingInt), item);
					continue;
				}
			}
			OnServer.MoveToSlot(ImportingThing, item);
			break;
		}
	}

	public override void OnAtmosphericTick()
	{
		_worldAtmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		if (OnOff && Powered && base.InternalAtmosphere != null)
		{
			MoleEnergy moleEnergy = IdealGas.Energy(base.InternalAtmosphere.GasMixture.HeatCapacity, GoalTemperature) - base.InternalAtmosphere.GasMixture.TotalEnergy;
			MoleEnergy energy2;
			if (moleEnergy < MoleEnergy.Zero)
			{
				MoleEnergy energy = RocketMath.Min(HeatTransferJoulesPerTick, -moleEnergy);
				energy2 = base.InternalAtmosphere.GasMixture.RemoveEnergy(energy);
				_worldAtmosphere.GasMixture.AddEnergy(energy2);
			}
			else
			{
				energy2 = RocketMath.Min(HeatTransferJoulesPerTick, moleEnergy);
				base.InternalAtmosphere.GasMixture.AddEnergy(energy2);
			}
			_powerUsedDuringTick = energy2.ToFloat();
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		Labeller labeller = interaction.SourceSlot.Occupant as Labeller;
		if ((bool)labeller)
		{
			delayedActionInstance.ActionMessage = ActionStrings.Set;
			delayedActionInstance.AppendStateMessage(GameStrings.DeviceManualInputWindow);
			if (!labeller.OnOff)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
			}
			if (!labeller.IsOperable)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			labeller.Set(this);
			return delayedActionInstance.Succeed();
		}
		switch (interactable.Action)
		{
		case InteractableType.Button3:
			if (IsLocked)
			{
				return GetLockedText(interactable);
			}
			if (!doAction)
			{
				delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalStackSize, StringManager.Get(Setting));
				delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
				return delayedActionInstance.Succeed();
			}
			SettingWheel.PlayWheelSound(this, Wheel, increaseSetting: true, interaction.AltKey, Convert.ToSingle(Setting), 1f, MaxWheelSetting, 10f, 1f);
			if (GameManager.RunSimulation)
			{
				Setting = (int)Math.Min(Setting + (double)(interaction.AltKey ? 1f : 10f), MaxWheelSetting);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button4:
			if (IsLocked)
			{
				return GetLockedText(interactable);
			}
			if (!doAction)
			{
				delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalStackSize, StringManager.Get(Setting));
				delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
				return delayedActionInstance.Succeed();
			}
			SettingWheel.PlayWheelSound(this, Wheel, increaseSetting: false, interaction.AltKey, Convert.ToSingle(Setting), 1f, MaxWheelSetting, 10f, 1f);
			if (GameManager.RunSimulation)
			{
				Setting = (int)Math.Max(Setting - (double)(interaction.AltKey ? 1f : 10f), 1.0);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public DelayedActionInstance GetLockedText(Interactable interactable)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance();
		delayedActionInstance.Duration = 0f;
		delayedActionInstance.ActionMessage = interactable.ContextualName;
		delayedActionInstance.AppendStateMessage(GameStrings.DeviceLocked);
		return delayedActionInstance.Fail();
	}

	public override void InitializeDevice()
	{
		base.InitializeDevice();
		_lastAngle = (float)Setting * WheelSpinSpeed;
		Wheel.Rotate(_lastAngle, 0f, 0f, Space.Self);
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		_lastAngle = (float)Setting * WheelSpinSpeed;
		Wheel.localRotation = _wheelBaseRotation;
		Wheel.Rotate(_lastAngle, 0f, 0f, Space.Self);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new VendingMachineRefrigeratedSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is VendingMachineRefrigeratedSaveData vendingMachineRefrigeratedSaveData)
		{
			Setting = vendingMachineRefrigeratedSaveData.Setting;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is VendingMachineRefrigeratedSaveData vendingMachineRefrigeratedSaveData)
		{
			vendingMachineRefrigeratedSaveData.Setting = Setting;
		}
	}

	public void CheckWheel()
	{
		if ((bool)Wheel && _rotatingWheel.Status != UniTaskStatus.Pending)
		{
			_rotatingWheel = RotateWheel();
		}
	}

	private async UniTask RotateWheel()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		while (!base.BeingDestroyed && Math.Abs((double)_lastAngle - Setting) > 0.10000000149011612)
		{
			_lastAngle = ((Setting < (double)MaxWheelSetting) ? Mathf.Lerp(_lastAngle, (float)Setting * WheelSpinSpeed, 2f * Time.deltaTime) : Mathf.Lerp(_lastAngle, MaxWheelSetting * WheelSpinSpeed, 2f * Time.deltaTime));
			Wheel.localRotation = _wheelBaseRotation;
			Wheel.Rotate(_lastAngle, 0f, 0f, Space.Self);
			await UniTask.NextFrame(cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				break;
			}
		}
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (!OnOff || cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			return 0f;
		}
		if (!IsOperable)
		{
			return UsedPower;
		}
		return UsedPower + _powerUsedDuringTick;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Temperature || logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Temperature || logicType == LogicType.Setting)
		{
			return base.InternalAtmosphere.Temperature.ToDouble();
		}
		return base.GetLogicValue(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Setting)
		{
			Setting = (float)Math.Max(value, 1.0);
		}
		base.SetLogicValue(logicType, value);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Deregister(this);
		}
	}
}
