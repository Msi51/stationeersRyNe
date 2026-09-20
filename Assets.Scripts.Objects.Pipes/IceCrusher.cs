using System;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using Objects;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class IceCrusher : DeviceInputOutputImport, IThermal, ILogicAtmospheric, IDensePoolable
{
	public static readonly VolumeLitres Volume = Chemistry.PipeVolume * 20.0;

	public static readonly PressurekPa MaxInternalPressure = Chemistry.OneAtmosphere * 2.0 * 600.0;

	public static readonly MoleEnergy EnergyPerSmelt = new MoleEnergy(20.0);

	public static readonly MoleEnergy EnergyForHeating = new MoleEnergy(1000.0);

	public const float DEFAULT_TEMPERATURE_SETTING = 288.15f;

	[Tooltip("The collider for display")]
	public Collider InfoPanel;

	private UniTask _smeltingTask;

	private float _powerUsedDuringTick;

	[SerializeField]
	private Transform blade0;

	[SerializeField]
	private Transform blade1;

	private const float DEGREES_PER_SEC = 450f;

	private static ReagentMixture _meltingReagentMix;

	private static int _indicatorOff = Animator.StringToHash("Off");

	private static int _indicatorIdle = Animator.StringToHash("Idle");

	private static int _indicatorHeating = Animator.StringToHash("Heating");

	private int _currentIndicator = -1;

	public SphereCollider IndicatorCollider;

	public static readonly int IceCrusherActivateHash = Animator.StringToHash("IceCrusherActivate");

	public static readonly int IceCrusherActivateStartHash = Animator.StringToHash("IceCrusherActivateStart");

	public MaterialChanger HeatingIndicator;

	public DialKnob Knob;

	private TemperatureKelvin _minTemperature = new TemperatureKelvin(120.0);

	private TemperatureKelvin _maxTemperature = new TemperatureKelvin(320.0);

	public override bool PreventStateChange => true;

	public override bool CanIceMelt => false;

	public override bool CanBeginImport
	{
		get
		{
			if (base.CanBeginImport && OnOff && Powered && !IsAtmosFull)
			{
				return ImportingThing is Ice;
			}
			return false;
		}
	}

	private bool IsOperating
	{
		get
		{
			if (OnOff && Powered)
			{
				return !IsBroken;
			}
			return false;
		}
	}

	protected override bool IsOperable => !IsAtmosFull;

	public bool IsAtmosFull => base.InternalAtmosphere?.PressureGassesAndLiquids >= MaxInternalPressure;

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Pressure => true, 
			LogicType.Temperature => true, 
			LogicType.VolumeOfLiquid => true, 
			LogicType.Volume => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Pressure => base.InternalAtmosphere.PressureGassesAndLiquids.ToDouble(), 
			LogicType.Temperature => base.InternalAtmosphere.Temperature.ToDouble(), 
			LogicType.VolumeOfLiquid => base.InternalAtmosphere.TotalVolumeLiquids.ToDouble(), 
			LogicType.Volume => base.InternalAtmosphere.Volume.ToDouble(), 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (OnOff && Activate == 1 && Error == 0)
		{
			blade0.Rotate(Vector3.right, 450f * GameManager.DeltaTime);
			blade1.Rotate(Vector3.right, 450f * GameManager.DeltaTime);
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == InfoPanel)
		{
			PassiveTooltip result = new PassiveTooltip(true);
			result.Title = DisplayName;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(GameStrings.IceCrusherState.AsColor("white"));
			AddStateStrings(stringBuilder);
			stringBuilder.AppendLine(GameStrings.InternalAtmosphere.AsColor("white"));
			AtmosphericsManager.DisplayBasicAtmosphere(base.InternalAtmosphere, stringBuilder, Pipe.ContentType.All, indent: true);
			result.Extended = stringBuilder.ToString();
			return result;
		}
		if (hitCollider == IndicatorCollider)
		{
			PassiveTooltip result2 = new PassiveTooltip(true);
			result2.Title = GameStrings.IceCrusherHeater.DisplayString;
			string extended = ((_currentIndicator == _indicatorOff) ? ((string)GameStrings.Off) : ((_currentIndicator != _indicatorHeating) ? GameStrings.IceCrusherIndicatorIdle.AsColor("green") : GameStrings.IceCrusherIndicatorHeat.AsColor("red")));
			result2.Extended = extended;
			return result2;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	private void AddStateStrings(StringBuilder sb)
	{
		if (IsOperating && BelowTargetTemperature() && base.InternalAtmosphere.TotalMoles > MoleQuantity.Zero)
		{
			sb.Append(StringManager.Indent);
			sb.AppendLine(GameStrings.IceCrusherHeating.AsString(base.OutputSetting.ToStringPrefix("K", "yellow")).AsColor("red"));
		}
		if (IsAtmosFull)
		{
			sb.Append(StringManager.Indent);
			sb.AppendLine(GameStrings.IceCrusherAtmosFull.AsColor("red"));
		}
		Item occupant2;
		if (ImportSlot.Contains<Stackable>(out var occupant))
		{
			sb.Append(StringManager.Indent);
			sb.AppendLine(GameStrings.StackerContainsQuantity.AsString(occupant.Quantity.ToString(), occupant.ToTooltip()));
		}
		else if (ImportSlot.Contains<Item>(out occupant2))
		{
			sb.Append(StringManager.Indent);
			sb.AppendLine(GameStrings.SlotContainsItem.AsString(ImportSlot.ToTooltip(), occupant2.ToTooltip()).AsColor("red"));
		}
		sb.Append(StringManager.Indent);
		sb.AppendLine(GameStrings.IceCrusherSetting.AsString(base.OutputSetting.ToStringPrefix("K", "yellow")));
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			Atmosphere atmosphere = (base.InternalAtmosphere = new Atmosphere(this, Volume, 0L));
		}
	}

	private int ClampSetting(float setting)
	{
		return (int)Math.Round(Mathf.Clamp(setting, _minTemperature.ToFloat(), _maxTemperature.ToFloat()));
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!IsOperating || !base.IsStructureCompleted || base.InternalAtmosphere.TotalMoles <= MoleQuantity.Zero)
		{
			return;
		}
		if (BelowTargetTemperature())
		{
			_powerUsedDuringTick += EnergyForHeating.ToFloat();
			base.InternalAtmosphere.GasMixture.AddEnergy(EnergyForHeating);
		}
		if (base.InternalAtmosphere.Temperature > new TemperatureKelvin(base.OutputSetting - 1f))
		{
			if (base.IsOutputValid)
			{
				AtmosphereHelper.MoveToEqualize(base.InternalAtmosphere, OutputNetwork.Atmosphere, base.PressurePerTick, AtmosphereHelper.MatterState.Gas);
			}
			if (base.IsOutput2Valid)
			{
				AtmosphereHelper.MoveToEqualize(base.InternalAtmosphere, OutputNetwork2.Atmosphere, base.PressurePerTick, AtmosphereHelper.MatterState.Liquid);
			}
		}
	}

	private bool BelowTargetTemperature()
	{
		return base.InternalAtmosphere.Temperature.ToFloat() < base.OutputSetting;
	}

	private void UnlockCheck()
	{
		if (Importing == 1)
		{
			OnServer.Interact(base.InteractImport, 0);
		}
		if (Activate == 1)
		{
			OnServer.Interact(base.InteractActivate, 0);
		}
	}

	public override void Update100MS(float deltaTime)
	{
		base.Update100MS(deltaTime);
		int num = ((!OnOff || !Powered) ? _indicatorOff : ((Activate != 1 || !IsOperating || !BelowTargetTemperature()) ? _indicatorIdle : _indicatorHeating));
		if (num != _currentIndicator)
		{
			_currentIndicator = num;
			HeatingIndicator?.ChangeState(_currentIndicator);
		}
	}

	protected override void OnServerImportTick()
	{
		base.OnServerImportTick();
		if (!base.IsStructureCompleted || IsBroken)
		{
			UnlockCheck();
			return;
		}
		if (OnOff && Powered)
		{
			if (ImportingThing != null && !IsAtmosFull && ImportingThing is Ice)
			{
				if (CanBeginImport)
				{
					OnServer.Interact(base.InteractImport, 1);
				}
				if (base.IsImportClosed && Activate == 0)
				{
					OnServer.Interact(base.InteractActivate, 1);
				}
			}
			if (Activate == 1 && !IsAtmosFull && (!BelowTargetTemperature() || base.InternalAtmosphere.TotalMoles <= MoleQuantity.Zero))
			{
				if ((bool)(ImportingThing as Ice))
				{
					if (Importing == 0)
					{
						OnServer.Interact(base.InteractImport, 1);
						return;
					}
					_powerUsedDuringTick += EnergyPerSmelt.ToFloat();
					ImportingThing.Smelt(base.InternalAtmosphere, _meltingReagentMix);
					AtmosphericEventInstance.CreateAddEnergy(base.InternalAtmosphere, EnergyPerSmelt, spark: false);
					return;
				}
				OnServer.Interact(base.InteractActivate, 0);
				OnServer.Interact(base.InteractImport, 0);
			}
		}
		else
		{
			UnlockCheck();
		}
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (base.PowerCable == null || base.PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		if (!OnOff)
		{
			return _powerUsedDuringTick;
		}
		return UsedPower + _powerUsedDuringTick;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	public override void AssessError()
	{
		bool isAtmosFull = IsAtmosFull;
		bool flag = ImportingThing == null || ImportingThing is Ice;
		if (Error == 0 && (isAtmosFull || !flag))
		{
			if (GameManager.RunSimulation && HasErrorState)
			{
				OnServer.Interact(base.InteractError, 1);
			}
		}
		else if (Error == 1 && !isAtmosFull && flag && GameManager.RunSimulation && HasErrorState)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		AssessError();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		AssessError();
	}

	public override CanConstructInfo CanConstruct()
	{
		Vector3 worldPosition = base.ThingTransformPosition - ThingTransform.up * GridSize;
		Structure structure = base.GridController.Get<Structure>(worldPosition, StructureElement.Center);
		if (!structure || ((bool)structure && !structure.AllowMounting))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
		}
		return base.CanConstruct();
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (base.IsStructureCompleted)
		{
			SetKnob();
		}
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		if (base.IsStructureCompleted)
		{
			SetKnob();
		}
	}

	private void SetKnob()
	{
		float state = (Mathf.Clamp(base.OutputSetting, _minTemperature.ToFloat(), _maxTemperature.ToFloat()) - _minTemperature.ToFloat()) / (_maxTemperature.ToFloat() - _minTemperature.ToFloat());
		Knob?.SetState(state);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		InteractableType action = interactable.Action;
		if (action == InteractableType.Button1 || action == InteractableType.Button2)
		{
			if (!doAction)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalValue, base.OutputSetting.ToStringPrefix("K"));
				delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
				return delayedActionInstance;
			}
			if (!GameManager.RunSimulation)
			{
				return delayedActionInstance.Succeed();
			}
			float num = base.OutputSetting;
			switch (interactable.Action)
			{
			case InteractableType.Button1:
				num -= (interaction.AltKey ? 1f : 10f);
				break;
			case InteractableType.Button2:
				num += (interaction.AltKey ? 1f : 10f);
				break;
			}
			if (RocketMath.Approximately(num, base.OutputSetting))
			{
				return delayedActionInstance.Succeed();
			}
			base.OutputSetting = ClampSetting(num);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnOutputSettingChanged()
	{
		base.OnOutputSettingChanged();
		SetKnob();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Activate && Activate == 1 && OnOff && Error == 0)
		{
			PlaySound(IceCrusherActivateHash);
			PlaySound(IceCrusherActivateStartHash);
		}
		else
		{
			StopSound(IceCrusherActivateHash);
		}
	}
}
