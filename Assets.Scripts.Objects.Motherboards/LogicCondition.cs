using System;
using System.Collections.Generic;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI.Motherboard;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

[Serializable]
public class LogicCondition : LogicBase
{
	[NonSerialized]
	public LogicState ParentState;

	[Tooltip("The class of variable or attribute that is part of this logic")]
	public LogicType Type;

	[Tooltip("The operator applied to this logic condition")]
	public ConditionOperation Operation;

	[Tooltip("Script Component that contains the visual elements displaying the logic")]
	public ScreenCondition ScreenCondition;

	public double Value;

	public bool IsTrue;

	public bool IsDisconnected;

	private AreaPowerControl _apc;

	private Battery _largeBattery;

	private GasSensor _gasSensor;

	private PipeAnalysizer _pipeAnalysizer;

	private IPoweredVent _poweredVent;

	private DeviceAtmospherics _deviceAtmospherics;

	private Furnace _furnace;

	private ElevatorShaft _elevatorShaft;

	private SolarPanel _solarPanel;

	private DaylightSensor _daylightSensor;

	private LogicUnitBase _logicUnit;

	public int Index => ParentState.Conditions.FindIndex((LogicCondition condition) => condition == this);

	public bool RelativeTruth(bool baseAnswer)
	{
		if (Operation != ConditionOperation.Equals)
		{
			return !baseAnswer;
		}
		return baseAnswer;
	}

	public bool CompareWithOperator(float value)
	{
		return Operation switch
		{
			ConditionOperation.Equals => RocketMath.Approximately(Value, value), 
			ConditionOperation.Greater => (double)value > Value, 
			ConditionOperation.Less => (double)value < Value, 
			_ => false, 
		};
	}

	public bool CompareWithOperator(double value)
	{
		return Operation switch
		{
			ConditionOperation.Equals => RocketMath.Approximately(value, Value), 
			ConditionOperation.Greater => value > Value, 
			ConditionOperation.Less => value < Value, 
			_ => false, 
		};
	}

	private bool AssessCharge()
	{
		_largeBattery = Device as Battery;
		if (_largeBattery != null)
		{
			return CompareWithOperator(_largeBattery.PowerStored);
		}
		_apc = Device as AreaPowerControl;
		if (_apc != null)
		{
			return CompareWithOperator(_apc.Battery ? _apc.Battery.PowerStored : 0f);
		}
		_solarPanel = Device as SolarPanel;
		if (_solarPanel != null)
		{
			return CompareWithOperator(_solarPanel.GenerationRate);
		}
		return false;
	}

	private bool AssessFire()
	{
		if (Device.InternalAtmosphere != null)
		{
			return Device.InternalAtmosphere.Sparked;
		}
		_gasSensor = Device as GasSensor;
		if (_gasSensor != null)
		{
			return _gasSensor.AirIgnited;
		}
		_pipeAnalysizer = Device as PipeAnalysizer;
		if (_pipeAnalysizer != null)
		{
			return _pipeAnalysizer.PipeIgnited;
		}
		return false;
	}

	private bool AssessTemperature()
	{
		if (Device.InternalAtmosphere != null)
		{
			return CompareWithOperator(Device.InternalAtmosphere.Temperature.ToFloat());
		}
		_gasSensor = Device as GasSensor;
		if (_gasSensor != null)
		{
			return CompareWithOperator(_gasSensor.AirTemperature.ToFloat());
		}
		_pipeAnalysizer = Device as PipeAnalysizer;
		if (_pipeAnalysizer != null)
		{
			return CompareWithOperator(_pipeAnalysizer.PipeTemperature.ToFloat());
		}
		return false;
	}

	private bool AssessReagents()
	{
		_furnace = Device as Furnace;
		if (_furnace != null)
		{
			return CompareWithOperator(_furnace.ReadableReagentMixture.TotalReagents);
		}
		return false;
	}

	private bool AssessElevatorSpeed()
	{
		_elevatorShaft = Device as ElevatorShaft;
		if (_elevatorShaft != null)
		{
			return CompareWithOperator(_elevatorShaft.ElevatorSpeed);
		}
		return false;
	}

	private bool AssessGasRatio(LogicType logicType)
	{
		_gasSensor = Device as GasSensor;
		if (_gasSensor != null)
		{
			return CompareWithOperator(_gasSensor.GasRatio(logicType));
		}
		_pipeAnalysizer = Device as PipeAnalysizer;
		if (_pipeAnalysizer != null)
		{
			return CompareWithOperator(_pipeAnalysizer.GasRatio(logicType));
		}
		return false;
	}

	private bool AssessSolarAngle()
	{
		_daylightSensor = Device as DaylightSensor;
		if (_daylightSensor != null && _daylightSensor.HasLight)
		{
			return CompareWithOperator(_daylightSensor.SolarAngle);
		}
		return false;
	}

	private bool AssessSetting()
	{
		_deviceAtmospherics = Device as DeviceAtmospherics;
		if (_deviceAtmospherics != null)
		{
			return CompareWithOperator(_deviceAtmospherics.OutputSetting);
		}
		_logicUnit = Device as LogicUnitBase;
		if (_logicUnit != null)
		{
			return CompareWithOperator(_logicUnit.Setting);
		}
		Stacker stacker = Device as Stacker;
		if (stacker != null)
		{
			return CompareWithOperator(stacker.Setting);
		}
		return false;
	}

	private bool AssessPressure()
	{
		if (Type == LogicType.Pressure)
		{
			if (Device.InternalAtmosphere != null)
			{
				return CompareWithOperator(Device.InternalAtmosphere.PressureGassesAndLiquids.ToFloat());
			}
			_gasSensor = Device as GasSensor;
			if (_gasSensor != null)
			{
				return CompareWithOperator(_gasSensor.AirPressure.ToFloat());
			}
			_pipeAnalysizer = Device as PipeAnalysizer;
			if (_pipeAnalysizer != null)
			{
				return CompareWithOperator(_pipeAnalysizer.PipePressure.ToFloat());
			}
		}
		else
		{
			_poweredVent = Device as IPoweredVent;
			if (_poweredVent != null)
			{
				return CompareWithOperator((Type == LogicType.PressureExternal) ? _poweredVent.ExternalPressure.ToFloat() : _poweredVent.InternalPressure.ToFloat());
			}
		}
		return false;
	}

	public void Assess()
	{
		if (!(Device == null))
		{
			switch (Type)
			{
			case LogicType.Activate:
				IsTrue = RelativeTruth(Device.Activate == (int)Value);
				break;
			case LogicType.Power:
				IsTrue = RelativeTruth(Device.OnOff == ((int)Value == 1));
				break;
			case LogicType.Mode:
				IsTrue = CompareWithOperator(Device.Mode);
				break;
			case LogicType.Color:
				IsTrue = CompareWithOperator(Device.ColorState);
				break;
			case LogicType.Open:
				IsTrue = RelativeTruth(Device.IsOpen == ((int)Value == 1));
				break;
			case LogicType.Lock:
				IsTrue = RelativeTruth(Device.IsLocked == ((int)Value == 1));
				break;
			case LogicType.Error:
				IsTrue = RelativeTruth(Device.Error == (int)Value);
				break;
			case LogicType.Charge:
				IsTrue = AssessCharge();
				break;
			case LogicType.Temperature:
				IsTrue = AssessTemperature();
				break;
			case LogicType.Combustion:
				IsTrue = AssessFire();
				break;
			case LogicType.Pressure:
			case LogicType.PressureExternal:
			case LogicType.PressureInternal:
				IsTrue = AssessPressure();
				break;
			case LogicType.Setting:
				IsTrue = AssessSetting();
				break;
			case LogicType.ElevatorSpeed:
				IsTrue = AssessElevatorSpeed();
				break;
			case LogicType.Reagents:
				IsTrue = AssessReagents();
				break;
			case LogicType.RatioOxygen:
			case LogicType.RatioCarbonDioxide:
			case LogicType.RatioNitrogen:
			case LogicType.RatioPollutant:
			case LogicType.RatioMethane:
			case LogicType.RatioWater:
			case LogicType.RatioNitrousOxide:
			case LogicType.RatioLiquidNitrogen:
			case LogicType.RatioLiquidOxygen:
			case LogicType.RatioLiquidMethane:
			case LogicType.RatioSteam:
			case LogicType.RatioLiquidCarbonDioxide:
			case LogicType.RatioLiquidPollutant:
			case LogicType.RatioLiquidNitrousOxide:
			case LogicType.RatioHydrogen:
			case LogicType.RatioLiquidHydrogen:
			case LogicType.RatioPollutedWater:
			case LogicType.RatioHydrazine:
			case LogicType.RatioLiquidHydrazine:
			case LogicType.RatioLiquidAlcohol:
			case LogicType.RatioHelium:
			case LogicType.RatioLiquidSodiumChloride:
			case LogicType.RatioSilanol:
			case LogicType.RatioLiquidSilanol:
			case LogicType.RatioHydrochloricAcid:
			case LogicType.RatioLiquidHydrochloricAcid:
			case LogicType.RatioOzone:
			case LogicType.RatioLiquidOzone:
			case LogicType.RatioHydrogenInput:
			case LogicType.RatioHydrogenInput2:
			case LogicType.RatioHydrogenOutput:
			case LogicType.RatioHydrogenOutput2:
			case LogicType.RatioLiquidHydrogenInput:
			case LogicType.RatioLiquidHydrogenInput2:
			case LogicType.RatioLiquidHydrogenOutput:
			case LogicType.RatioLiquidHydrogenOutput2:
			case LogicType.RatioPollutedWaterInput:
			case LogicType.RatioPollutedWaterInput2:
			case LogicType.RatioPollutedWaterOutput:
			case LogicType.RatioPollutedWaterOutput2:
			case LogicType.RatioHydrazineInput:
			case LogicType.RatioHydrazineInput2:
			case LogicType.RatioHydrazineOutput:
			case LogicType.RatioHydrazineOutput2:
			case LogicType.RatioLiquidHydrazineInput:
			case LogicType.RatioLiquidHydrazineInput2:
			case LogicType.RatioLiquidHydrazineOutput:
			case LogicType.RatioLiquidHydrazineOutput2:
			case LogicType.RatioLiquidAlcoholInput:
			case LogicType.RatioLiquidAlcoholInput2:
			case LogicType.RatioLiquidAlcoholOutput:
			case LogicType.RatioLiquidAlcoholOutput2:
			case LogicType.RatioHeliumInput:
			case LogicType.RatioHeliumInput2:
			case LogicType.RatioHeliumOutput:
			case LogicType.RatioHeliumOutput2:
			case LogicType.RatioLiquidSodiumChlorideInput:
			case LogicType.RatioLiquidSodiumChlorideInput2:
			case LogicType.RatioLiquidSodiumChlorideOutput:
			case LogicType.RatioLiquidSodiumChlorideOutput2:
			case LogicType.RatioSilanolInput:
			case LogicType.RatioSilanolInput2:
			case LogicType.RatioSilanolOutput:
			case LogicType.RatioSilanolOutput2:
			case LogicType.RatioLiquidSilanolInput:
			case LogicType.RatioLiquidSilanolInput2:
			case LogicType.RatioLiquidSilanolOutput:
			case LogicType.RatioLiquidSilanolOutput2:
			case LogicType.RatioHydrochloricAcidInput:
			case LogicType.RatioHydrochloricAcidInput2:
			case LogicType.RatioHydrochloricAcidOutput:
			case LogicType.RatioHydrochloricAcidOutput2:
			case LogicType.RatioLiquidHydrochloricAcidInput:
			case LogicType.RatioLiquidHydrochloricAcidInput2:
			case LogicType.RatioLiquidHydrochloricAcidOutput:
			case LogicType.RatioLiquidHydrochloricAcidOutput2:
			case LogicType.RatioOzoneInput:
			case LogicType.RatioOzoneInput2:
			case LogicType.RatioOzoneOutput:
			case LogicType.RatioOzoneOutput2:
			case LogicType.RatioLiquidOzoneInput:
			case LogicType.RatioLiquidOzoneInput2:
			case LogicType.RatioLiquidOzoneOutput:
			case LogicType.RatioLiquidOzoneOutput2:
				IsTrue = AssessGasRatio(Type);
				break;
			case LogicType.SolarAngle:
				IsTrue = AssessSolarAngle();
				break;
			case LogicType.Horizontal:
			case LogicType.Vertical:
			case LogicType.Maximum:
			case LogicType.Ratio:
			case LogicType.PowerPotential:
			case LogicType.PowerActual:
			case LogicType.Quantity:
			case LogicType.On:
			case LogicType.RequiredPower:
			case LogicType.HorizontalRatio:
			case LogicType.VerticalRatio:
			case LogicType.PowerRequired:
			case LogicType.Idle:
			case LogicType.RecipeHash:
			case LogicType.ExportSlotHash:
			case LogicType.ImportSlotHash:
				IsTrue = CompareWithOperator(Device.GetLogicValue(Type));
				break;
			}
		}
	}

	public void PopulateDevices(ref List<Dropdown.OptionData> optionData)
	{
		if (Device == null)
		{
			IsDisconnected = false;
			ScreenCondition.Device.options = optionData;
			ScreenCondition.Device.SetValueWithoutNotify(0);
			Device = ParentState.ParentMotherboard.GetListDevice(0);
			ScreenCondition.Device.interactable = true;
			return;
		}
		int num = ParentState.ParentMotherboard.DisplayedDevices.FindIndex((Device d) => d == Device);
		if (num < 0)
		{
			IsDisconnected = true;
			ScreenCondition.Device.options = new List<Dropdown.OptionData>
			{
				new Dropdown.OptionData("?" + Device.DisplayName + "?")
			};
			ScreenCondition.Device.interactable = false;
		}
		else
		{
			IsDisconnected = false;
			ScreenCondition.Device.options = optionData;
			ScreenCondition.Device.SetValueWithoutNotify((num >= 0) ? num : 0);
			ScreenCondition.Device.interactable = true;
		}
	}

	public override void Read(RocketBinaryReader reader)
	{
		base.Read(reader);
		Operation = (ConditionOperation)reader.ReadByte();
		Network.ReadLogicValue(reader, out Type, out Value);
	}

	public override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		writer.WriteByte((byte)Operation);
		Network.WriteLogicValue(writer, Type, Value);
	}
}
