using System;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Electrical;

public class AirConditioner : DeviceInputOutputCircuit, IThermal
{
	[Header("Air Conditioner")]
	public static readonly float MaxEnergy = 6000f;

	[SerializeField]
	private GameObject computerScreen;

	[SerializeField]
	private GameObject tempWarning;

	[SerializeField]
	private GameObject pressureWarning;

	[SerializeField]
	private Button openCloseButton;

	[SerializeField]
	private Text goalTemperatureText;

	[SerializeField]
	private Color stopColor = Color.red;

	[SerializeField]
	private Text openCloseButtonText;

	private ColorBlock _openColors;

	private ColorBlock _closeColors;

	private float _goalPressure = 101.325f;

	private bool _isTempWarning;

	private bool _isPressureWarning;

	private float _powerUsedDuringTick;

	private float _energyUsedDuringTick;

	private float HeatPumpIdlePower = 345f;

	private float _energyAddedDuringTick;

	private float _temperatureDifferentialEfficiencyCache;

	private float _operationalTemperatureLimitorCache;

	private float _optimalPressureScalarCache;

	private MoleEnergy _energyMoved;

	[Tooltip("How efficient the cooler will be depending on the temperature of input and waste")]
	public AnimationCurve TemperatureDeltaEfficiency;

	[Tooltip("How efficient the cooler will be depending on the temperature difference between input and waste")]
	public AnimationCurve InputAndWasteEfficiency;

	public TemperatureKelvin GoalTemperature
	{
		get
		{
			return new TemperatureKelvin(base.OutputSetting);
		}
		set
		{
			if (!RocketMath.Approximately(value, GoalTemperature))
			{
				base.OutputSetting = value.ToFloat();
				RefreshButtonOpenClose();
			}
		}
	}

	protected override bool ShouldFansRotate
	{
		get
		{
			if (OnOff && Powered)
			{
				return Mode == 1;
			}
			return false;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted)
			{
				return false;
			}
			bool flag = base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
			bool flag2 = base.IsInputValid && base.IsOutputValid && !flag;
			if (Error == 1)
			{
				if (!flag2)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag2)
			{
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

	public bool IsFullyConnected
	{
		get
		{
			if (InputNetwork != null && OutputNetwork != null)
			{
				return OutputNetwork2 != null;
			}
			return false;
		}
	}

	public float TemperatureDifferentialEfficiency
	{
		get
		{
			return _temperatureDifferentialEfficiencyCache;
		}
		set
		{
			_temperatureDifferentialEfficiencyCache = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
			}
		}
	}

	public float OperationalTemperatureLimitor
	{
		get
		{
			return _operationalTemperatureLimitorCache;
		}
		set
		{
			_operationalTemperatureLimitorCache = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 4096;
			}
		}
	}

	public float OptimalPressureScalar
	{
		get
		{
			return _optimalPressureScalarCache;
		}
		set
		{
			_optimalPressureScalarCache = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
		}
	}

	public MoleEnergy EnergyMoved
	{
		get
		{
			return _energyMoved;
		}
		set
		{
			if (NetworkManager.IsServer && !RocketMath.Approximately(value, EnergyMoved, 1.0))
			{
				base.NetworkUpdateFlags |= 32768;
			}
			_energyMoved = value;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		if (hitCollider == OutputConnection2.Collider)
		{
			result.Title = InterfaceStrings.ConnectionWaste;
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	protected override StringBuilder GetInfoPanelOperationText()
	{
		StringBuilder infoPanelOperationText = base.GetInfoPanelOperationText();
		infoPanelOperationText.AppendLine(GameStrings.OperationalTemperatureEfficiency.AsString((OperationalTemperatureLimitor * 100f).ToStringPercent("yellow")));
		infoPanelOperationText.AppendLine(GameStrings.TemperatureDifferentialEfficiency.AsString((TemperatureDifferentialEfficiency * 100f).ToStringPercent("yellow")));
		infoPanelOperationText.AppendLine(GameStrings.PressureEfficiency.AsString((OptimalPressureScalar * 100f).ToStringPercent("yellow")));
		if (EnergyMoved < MoleEnergy.Zero)
		{
			infoPanelOperationText.AppendLine(GameStrings.HeatingAmount.AsString(Mathf.Abs(EnergyMoved.ToFloat()).ToStringPrefix("J", "yellow")));
		}
		else if (EnergyMoved > MoleEnergy.Zero)
		{
			infoPanelOperationText.AppendLine(GameStrings.CoolingAmount.AsString(Mathf.Abs(EnergyMoved.ToFloat()).ToStringPrefix("J", "yellow")));
		}
		return infoPanelOperationText;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new AirConditionerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void Awake()
	{
		base.Awake();
		tempWarning.SetActive(value: false);
		pressureWarning.SetActive(value: false);
		_openColors = openCloseButton.colors;
		_closeColors = _openColors;
		_closeColors.normalColor = stopColor;
		Color color = stopColor;
		color.r *= 1.5f;
		color.g *= 1.5f;
		color.b *= 1.5f;
		_closeColors.highlightedColor = color;
		color.r *= 1.5f;
		color.g *= 1.5f;
		color.b *= 1.5f;
		_closeColors.pressedColor = color;
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			return 0f;
		}
		float num = 0f;
		if (!OnOff)
		{
			return num;
		}
		num += UsedPower;
		return num + _powerUsedDuringTick;
	}

	public override void OnAnimationStart()
	{
		base.OnAnimationStart();
		RefreshButtonOpenClose();
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		RefreshButtonOpenClose();
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		base.OutputSetting = 293.15f;
	}

	public void RefreshButtonOpenClose()
	{
		if (GameManager.RunSimulation && !IsFullyConnected && Mode == 1 && GameManager.GameState == GameState.Running)
		{
			OnServer.Interact(base.InteractMode, 0);
		}
		if (ThreadedManager.IsThread)
		{
			RefreshFromThread().Forget();
		}
		else
		{
			RefreshInworldUi();
		}
	}

	private async UniTaskVoid RefreshFromThread()
	{
		await UniTask.SwitchToMainThread();
		await UniTask.NextFrame();
		RefreshInworldUi();
	}

	private void RefreshInworldUi()
	{
		if (!base.IsBeingDestroyed && !GameManager.IsBatchMode)
		{
			if ((bool)openCloseButtonText)
			{
				openCloseButtonText.text = ((Mode == 1) ? GameStrings.GlobalStop.AsString().ToUpper() : GameStrings.GlobalStart.AsString().ToUpper());
			}
			if ((bool)openCloseButton)
			{
				openCloseButton.colors = ((Mode == 1) ? _closeColors : _openColors);
				openCloseButton.interactable = IsFullyConnected;
			}
			if ((bool)goalTemperatureText)
			{
				goalTemperatureText.text = (GoalTemperature - Chemistry.Temperature.ZeroDegrees).ToFloat().ToString("F0");
			}
			if (tempWarning != null)
			{
				tempWarning.SetActive(_isTempWarning);
			}
			if (pressureWarning != null)
			{
				pressureWarning.SetActive(_isPressureWarning);
			}
		}
	}

	public void OnButtonMode()
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(this, InteractableType.Mode, (Mode != 1) ? 1 : 0);
		}
		else
		{
			NetworkClient.Interact(base.InteractMode, (Mode != 1) ? 1 : 0);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = base.InteractWith(interactable, interaction, doAction);
		switch (interactable.Action)
		{
		case InteractableType.Button3:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				base.OutputSetting += (interaction.AltKey ? 1 : 10);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button4:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				base.OutputSetting -= (interaction.AltKey ? 1 : 10);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return delayedActionInstance;
		}
	}

	public void OnButtonIncrease()
	{
		Interaction interaction = new Interaction(InventoryManager.Parent, InventoryManager.ActiveHandSlot, this, KeyManager.GetButton(KeyMap.QuantityModifier));
		if (GameManager.RunSimulation)
		{
			OnServer.InteractWith(base.InteractButton3, interaction);
		}
		else
		{
			NetworkClient.InteractWith(base.InteractButton3, interaction);
		}
	}

	public void OnButtonDecrease()
	{
		Interaction interaction = new Interaction(InventoryManager.Parent, InventoryManager.ActiveHandSlot, this, KeyManager.GetButton(KeyMap.QuantityModifier));
		if (GameManager.RunSimulation)
		{
			OnServer.InteractWith(base.InteractButton4, interaction);
		}
		else
		{
			NetworkClient.InteractWith(base.InteractButton4, interaction);
		}
	}

	public override void OnOutputSettingChanged()
	{
		base.OnOutputSettingChanged();
		RefreshButtonOpenClose();
	}

	public override void InitializeDevice()
	{
		base.InitializeDevice();
		RefreshButtonOpenClose();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		computerScreen.SetActive(Powered && OnOff);
	}

	public override void OnAddPipeNetwork(PipeNetwork newNetwork)
	{
		base.OnAddPipeNetwork(newNetwork);
		RefreshButtonOpenClose();
	}

	public override void OnRemovePipeNetwork(PipeNetwork oldNetwork)
	{
		base.OnRemovePipeNetwork(oldNetwork);
		RefreshButtonOpenClose();
	}

	public override void OnFinishedInteractionSync(Interactable interactable)
	{
		base.OnFinishedInteractionSync(interactable);
		RefreshButtonOpenClose();
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		computerScreen.SetActive(Powered && OnOff);
		RefreshButtonOpenClose();
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(100.0), 0L);
		}
	}

	public override void OnAtmosphericTick()
	{
		_powerUsedDuringTick = 0f;
		if (OnOff && Powered && Mode == 1 && IsFullyConnected && IsOperable)
		{
			if (RocketMath.Abs(GoalTemperature - InputNetwork.Atmosphere.Temperature) >= TemperatureKelvin.One)
			{
				PressurekPa pressurekPa = new PressurekPa(0.1);
				float num = Mathf.Clamp01(RocketMath.Min(InputNetwork.Atmosphere.PressureGasses / Chemistry.OneAtmosphere - pressurekPa, OutputNetwork2.Atmosphere.PressureGasses / Chemistry.OneAtmosphere - pressurekPa).ToFloat());
				MoleQuantity moleQuantity = (base.ProcessedMoles = IdealGas.Quantity(base.PressurePerTick, new VolumeLitres(100.0), InputNetwork.Atmosphere.Temperature));
				if (moleQuantity > MoleQuantity.Zero)
				{
					base.InternalAtmosphere.Add(InputNetwork.Atmosphere.Remove(moleQuantity, AtmosphereHelper.MatterState.All));
					float num2 = 14000f;
					TemperatureKelvin temperatureKelvin = ((GoalTemperature > base.InternalAtmosphere.GasMixture.Temperature) ? (OutputNetwork2.Atmosphere.Temperature - base.InternalAtmosphere.GasMixture.Temperature) : (base.InternalAtmosphere.GasMixture.Temperature - OutputNetwork2.Atmosphere.Temperature));
					float num3 = TemperatureDeltaEfficiency.Evaluate(temperatureKelvin.ToFloat());
					float num4 = Math.Min(InputAndWasteEfficiency.Evaluate(base.InternalAtmosphere.GasMixture.Temperature.ToFloat()), InputAndWasteEfficiency.Evaluate(OutputNetwork2.Atmosphere.GasMixture.Temperature.ToFloat()));
					double num5 = 1.0;
					MoleEnergy moleEnergy = new MoleEnergy((double)(num2 * num3 * num4 * num) * num5);
					if (GoalTemperature > base.InternalAtmosphere.GasMixture.Temperature)
					{
						base.InternalAtmosphere.GasMixture.AddEnergy(OutputNetwork2.Atmosphere.GasMixture.RemoveEnergy(moleEnergy));
					}
					else
					{
						OutputNetwork2.Atmosphere.GasMixture.AddEnergy(base.InternalAtmosphere.GasMixture.RemoveEnergy(moleEnergy));
					}
					EnergyMoved = moleEnergy;
					_powerUsedDuringTick = HeatPumpIdlePower;
					OutputNetwork.Atmosphere.Add(base.InternalAtmosphere.GasMixture);
					base.InternalAtmosphere.GasMixture.Reset();
					TemperatureDifferentialEfficiency = num3;
					OperationalTemperatureLimitor = num4;
					OptimalPressureScalar = num;
				}
			}
			else
			{
				base.ProcessedMoles = MoleQuantity.Zero;
			}
		}
		else
		{
			base.ProcessedMoles = MoleQuantity.Zero;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteSingle(OperationalTemperatureLimitor);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteSingle(OptimalPressureScalar);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteSingle(TemperatureDifferentialEfficiency);
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			writer.WriteSingle(EnergyMoved.ToFloat());
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			OperationalTemperatureLimitor = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			OptimalPressureScalar = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			TemperatureDifferentialEfficiency = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			EnergyMoved = new MoleEnergy(reader.ReadSingle());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(OperationalTemperatureLimitor);
		writer.WriteSingle(OptimalPressureScalar);
		writer.WriteSingle(TemperatureDifferentialEfficiency);
		writer.WriteSingle(EnergyMoved.ToFloat());
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		OperationalTemperatureLimitor = reader.ReadSingle();
		OptimalPressureScalar = reader.ReadSingle();
		TemperatureDifferentialEfficiency = reader.ReadSingle();
		EnergyMoved = new MoleEnergy(reader.ReadSingle());
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType - 150 <= LogicType.Open)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.OperationalTemperatureEfficiency => OperationalTemperatureLimitor, 
			LogicType.TemperatureDifferentialEfficiency => TemperatureDifferentialEfficiency, 
			LogicType.PressureEfficiency => OptimalPressureScalar, 
			_ => base.GetLogicValue(logicType), 
		};
	}
}
