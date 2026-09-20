using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class StateChangeDevice : SettableAtmosDevice, IThermal
{
	public Collider infoScreenCollider;

	public Collider temperatureGaugeCollider;

	public Transform temperatureGaugeTransform;

	public Vector3 temperatureGaugeRange = new Vector3(0f, 0.125f, 0f);

	public Vector3 pressureGaugeRange = new Vector3(0f, 0f, 270f);

	public AnimationCurve temperatureCurve;

	public const float HEAT_EXCHANGER_AREA = 45f;

	private MoleEnergy _energyTransfer;

	public override float ConvectionFactor => ThermodynamicsScale * 0.01f;

	public override float RadiationFactor => ThermodynamicsScale * 0.0005f;

	public VolumeLitres Volume => new VolumeLitres(400.0);

	public PressurekPa MaxPressure => Chemistry.Limits.MAXPressureGasPipe;

	public override float WheelSettingIncrement => 100f;

	public override bool HasReadableAtmosphere => true;

	protected MoleEnergy EnergyTransfer
	{
		get
		{
			return _energyTransfer;
		}
		set
		{
			if (!RocketMath.Approximately(value, _energyTransfer))
			{
				_energyTransfer = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	public override void Awake()
	{
		base.Awake();
		MaxSetting = 6000f;
		if (SettingWheel != null)
		{
			SettingWheel.WheelSpinSpeed = 1.5f;
		}
	}

	public override void InitInternalAtmosphere()
	{
		base.InitInternalAtmosphere();
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
	}

	protected float HeatExchangeRatio()
	{
		float num = InputNetwork2.Atmosphere.HeatExchangeRatio();
		float num2 = base.InternalAtmosphere.HeatExchangeRatio();
		return num * num2;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = HandleButtonSetting(interactable, interaction, doAction);
		if (delayedActionInstance != null)
		{
			return delayedActionInstance;
		}
		DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		delayedActionInstance2.AppendStateMessage(GameStrings.TargetPressureKPA, StringManager.Get((int)base.OutputSetting));
		delayedActionInstance2.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
		delayedActionInstance2.AppendStateMessage(GameStrings.UseLabelerToSet);
		switch (interactable.Action)
		{
		case InteractableType.Button1:
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel.Wheel, increaseSetting: true, interaction.AltKey, base.OutputSetting, MinSetting, MaxSetting, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				base.OutputSetting += (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button2:
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel.Wheel, increaseSetting: false, interaction.AltKey, base.OutputSetting, MinSetting, MaxSetting, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				base.OutputSetting -= (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return base.GetLogicValue(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting && !IsCursor && GameManager.GameState != GameState.None)
		{
			if (GameManager.GameState != GameState.None && !IsCursor)
			{
				AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, base.InternalAtmosphere.GasMixture, base.InternalAtmosphere.Inflamed);
				AtmosphericEventInstance.Reset(base.InternalAtmosphere);
			}
			base.OnDestroy();
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (base.IsStructureCompleted)
		{
			if (InputNetwork2?.Atmosphere != null)
			{
				MoleEnergy convectionHeat = AtmosphereHelper.GetConvectionHeat(InputNetwork2.Atmosphere, base.InternalAtmosphere, 45f * HeatExchangeRatio());
				InputNetwork2.Atmosphere.GasMixture.TransferEnergyTo(ref base.InternalAtmosphere.GasMixture, convectionHeat * AtmosphericsManager.Instance.TickSpeedSeconds);
				EnergyTransfer = convectionHeat;
			}
			if (IsOperable)
			{
				AtmosphericsProcessing();
			}
		}
	}

	protected virtual void AtmosphericsProcessing()
	{
	}

	public override void UpdateEachFrame()
	{
		if (base.IsStructureCompleted && base.InternalAtmosphere != null)
		{
			float t = temperatureCurve.Evaluate(base.InternalAtmosphere.Temperature.ToFloat());
			Vector3 b = Vector3.Lerp(-temperatureGaugeRange, temperatureGaugeRange, t);
			temperatureGaugeTransform.localPosition = Vector3.Lerp(temperatureGaugeTransform.localPosition, b, Time.deltaTime);
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteFloatHalf(EnergyTransfer.ToFloat());
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		EnergyTransfer = new MoleEnergy(reader.ReadFloatHalf());
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteFloatHalf(EnergyTransfer.ToFloat());
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			EnergyTransfer = new MoleEnergy(reader.ReadFloatHalf());
		}
	}
}
