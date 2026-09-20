using System;
using System.Collections.Generic;
using Assets.Scripts.Genetics;
using Assets.Scripts.Networking;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class PlantStatus
{
	public static readonly int StateCount = Enum.GetValues(typeof(PlantStatusType)).Length;

	private bool[] _currentStates = new bool[StateCount];

	private float[] _aggregateStates = new float[StateCount];

	public float BreathingEfficiency = 1f;

	public float TemperatureEfficiency = 1f;

	public float HydrationEfficiency = 1f;

	public float PressureEfficiency = 1f;

	public float LightEfficiency = 1f;

	private readonly Plant _plant;

	private byte _breathingEfficiencyPercent;

	private byte _temperatureEfficiencyPercent;

	private byte _lightEfficiencyPercent;

	private byte _pressureEfficiencyPercent;

	private byte _hydrationEfficiencyPercent;

	private byte _currentLightExposurePercent;

	private ushort _lightStressPercent;

	private byte _lightPercent;

	private byte _darknessPercent;

	private static float HealThreshold = 0.2f;

	public ushort PackedStates { get; private set; }

	public byte BreathingEfficiencyPercent
	{
		get
		{
			return _breathingEfficiencyPercent;
		}
		set
		{
			if (value != _breathingEfficiencyPercent)
			{
				_breathingEfficiencyPercent = value;
				if (NetworkManager.IsServer)
				{
					_plant.GrowStatusFlags |= 2;
				}
			}
		}
	}

	public byte TemperatureEfficiencyPercent
	{
		get
		{
			return _temperatureEfficiencyPercent;
		}
		set
		{
			if (value != _temperatureEfficiencyPercent)
			{
				_temperatureEfficiencyPercent = value;
				if (NetworkManager.IsServer)
				{
					_plant.GrowStatusFlags |= 4;
				}
			}
		}
	}

	public byte LightEfficiencyPercent
	{
		get
		{
			return _lightEfficiencyPercent;
		}
		set
		{
			if (value != _lightEfficiencyPercent)
			{
				_lightEfficiencyPercent = value;
				if (NetworkManager.IsServer)
				{
					_plant.GrowStatusFlags |= 8;
				}
			}
		}
	}

	public byte PressureEfficiencyPercent
	{
		get
		{
			return _pressureEfficiencyPercent;
		}
		set
		{
			if (value != _pressureEfficiencyPercent)
			{
				_pressureEfficiencyPercent = value;
				if (NetworkManager.IsServer)
				{
					_plant.GrowStatusFlags |= 16;
				}
			}
		}
	}

	public byte HydrationEfficiencyPercent
	{
		get
		{
			return _hydrationEfficiencyPercent;
		}
		set
		{
			if (value != _hydrationEfficiencyPercent)
			{
				_hydrationEfficiencyPercent = value;
				if (NetworkManager.IsServer)
				{
					_plant.GrowStatusFlags |= 32;
				}
			}
		}
	}

	public byte CurrentLightExposurePercent
	{
		get
		{
			return _currentLightExposurePercent;
		}
		set
		{
			if (value != _currentLightExposurePercent)
			{
				_currentLightExposurePercent = value;
				if (NetworkManager.IsServer)
				{
					_plant.GrowStatusFlags |= 64;
				}
			}
		}
	}

	public ushort LightStressPercent
	{
		get
		{
			return _lightStressPercent;
		}
		set
		{
			if (value != _lightStressPercent)
			{
				_lightStressPercent = value;
				if (NetworkManager.IsServer)
				{
					_plant.GrowStatusFlags |= 128;
				}
			}
		}
	}

	public byte LightPercent
	{
		get
		{
			return _lightPercent;
		}
		set
		{
			if (value != _lightPercent)
			{
				_lightPercent = value;
				if (NetworkManager.IsServer)
				{
					_plant.GrowStatusFlags |= 256;
				}
			}
		}
	}

	public byte DarknessPercent
	{
		get
		{
			return _darknessPercent;
		}
		set
		{
			if (value != _darknessPercent)
			{
				_darknessPercent = value;
				if (NetworkManager.IsServer)
				{
					_plant.GrowStatusFlags |= 512;
				}
			}
		}
	}

	public PlantStatus()
	{
	}

	public PlantStatus(Plant plant)
	{
		_plant = plant;
	}

	public bool GetCurrentState(PlantStatusType plantStatusType)
	{
		return _currentStates[(int)plantStatusType];
	}

	public void SetCurrentState(PlantStatusType plantStatusType, bool value)
	{
		_currentStates[(int)plantStatusType] = value;
		if (value)
		{
			_aggregateStates[(int)plantStatusType] += GameManager.LastTickTimeSeconds;
		}
		else
		{
			_aggregateStates[(int)plantStatusType] = Mathf.Max(_aggregateStates[(int)plantStatusType] - GameManager.LastTickTimeSeconds * 5f, 0f);
		}
	}

	public void PrepareStateNetworkMessage()
	{
		BreathingEfficiencyPercent = RatioToPercentageByte(BreathingEfficiency);
		TemperatureEfficiencyPercent = RatioToPercentageByte(TemperatureEfficiency);
		LightEfficiencyPercent = RatioToPercentageByte(LightEfficiency);
		PressureEfficiencyPercent = RatioToPercentageByte(PressureEfficiency);
		HydrationEfficiencyPercent = RatioToPercentageByte(HydrationEfficiency);
		CurrentLightExposurePercent = RatioToPercentageByte(_plant.CurrentLightExposure);
		LightStressPercent = RatioToPercentageUShort(_plant.PlantRecord.LightStress);
		LightPercent = RatioToPercentageByte(_plant.PlantRecord.TimeLitRatio);
		DarknessPercent = RatioToPercentageByte(_plant.PlantRecord.TimeDarknessRatio);
		ushort num = PackCurrentStates();
		if (PackedStates != num)
		{
			PackedStates = num;
			if (NetworkManager.IsServer && (bool)_plant)
			{
				_plant.GrowStatusFlags |= 1024;
			}
		}
	}

	private byte RatioToPercentageByte(float value)
	{
		return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 100f), 0, 255);
	}

	private ushort RatioToPercentageUShort(float value)
	{
		return (ushort)Mathf.Clamp(Mathf.RoundToInt(value * 100f), 0, 65535);
	}

	public void SerializeAggregateStates(List<StateWrapper> listToPopulate)
	{
		listToPopulate.Clear();
		for (int i = 0; i < _aggregateStates.Length; i++)
		{
			listToPopulate.Add(new StateWrapper((PlantStatusType)i, _aggregateStates[i]));
		}
	}

	public void SerializeCurrentStates(List<BooleanStateWrapper> listToPopulate)
	{
		listToPopulate.Clear();
		for (int i = 0; i < _currentStates.Length; i++)
		{
			listToPopulate.Add(new BooleanStateWrapper((PlantStatusType)i, _currentStates[i]));
		}
	}

	public void ApplySerializedAggregateStates(List<StateWrapper> listToApply)
	{
		foreach (StateWrapper item in listToApply)
		{
			_aggregateStates[(int)item.State] = item.Value;
		}
	}

	public void ApplySerializedCurrentStates(List<BooleanStateWrapper> listToApply)
	{
		foreach (BooleanStateWrapper item in listToApply)
		{
			_currentStates[(int)item.State] = item.Value;
		}
	}

	public bool CanHeal(Plant plant)
	{
		PlantLifeRequirements lifeRequirements = plant.lifeRequirements;
		for (int i = 0; i < _aggregateStates.Length; i++)
		{
			float num = _aggregateStates[i];
			switch ((PlantStatusType)i)
			{
			case PlantStatusType.Dehydrated:
				if ((float)lifeRequirements.TimeUntilDehydrationDamage > 0f && num > (float)lifeRequirements.TimeUntilDehydrationDamage * HealThreshold)
				{
					return false;
				}
				break;
			case PlantStatusType.LowTemperature:
				if ((float)lifeRequirements.TimeUntilFrozenDamage > 0f && num > (float)lifeRequirements.TimeUntilFrozenDamage * HealThreshold)
				{
					return false;
				}
				break;
			case PlantStatusType.HighTemperature:
				if ((float)lifeRequirements.TimeUntilOverHeatedDamage > 0f && num > (float)lifeRequirements.TimeUntilOverHeatedDamage * HealThreshold)
				{
					return false;
				}
				break;
			case PlantStatusType.Suffocated:
				if ((float)lifeRequirements.TimeUntilSuffocatedDamage > 0f && num > (float)lifeRequirements.TimeUntilSuffocatedDamage * HealThreshold)
				{
					return false;
				}
				break;
			case PlantStatusType.LowPressure:
				if ((float)lifeRequirements.TimeUntilLowPressureDamage > 0f && num > (float)lifeRequirements.TimeUntilLowPressureDamage * HealThreshold)
				{
					return false;
				}
				break;
			case PlantStatusType.HighPressure:
				if ((float)lifeRequirements.TimeUntilHighPressureDamage > 0f && num > (float)lifeRequirements.TimeUntilHighPressureDamage * HealThreshold)
				{
					return false;
				}
				break;
			case PlantStatusType.UnDesiredGas:
				if ((float)lifeRequirements.TimeUntilUndesiredGasDamage > 0f && num > (float)lifeRequirements.TimeUntilUndesiredGasDamage * HealThreshold)
				{
					return false;
				}
				break;
			case PlantStatusType.LowWaterTemperature:
				if ((float)lifeRequirements.TimeUntilFrozenDamage > 0f && num > (float)lifeRequirements.TimeUntilFrozenDamage * HealThreshold)
				{
					return false;
				}
				break;
			case PlantStatusType.HighWaterTemperature:
				if ((float)lifeRequirements.TimeUntilOverHeatedDamage > 0f && num > (float)lifeRequirements.TimeUntilOverHeatedDamage * HealThreshold)
				{
					return false;
				}
				break;
			case PlantStatusType.Lit:
				if ((float)lifeRequirements.TimeUntilLightDamage > 0f && num > (float)lifeRequirements.TimeUntilLightDamage * HealThreshold)
				{
					return false;
				}
				break;
			case PlantStatusType.Darkness:
				if ((float)lifeRequirements.TimeUntilDarknessDamage > 0f && num > (float)lifeRequirements.TimeUntilDarknessDamage * HealThreshold)
				{
					return false;
				}
				break;
			}
		}
		return true;
	}

	public bool WillDamage(PlantStatusType statusToDamage, Plant plant)
	{
		float num = _aggregateStates[(int)statusToDamage];
		switch (statusToDamage)
		{
		case PlantStatusType.Dehydrated:
			if ((float)plant.lifeRequirements.TimeUntilDehydrationDamage > 0f && num > (float)plant.lifeRequirements.TimeUntilDehydrationDamage)
			{
				return true;
			}
			break;
		case PlantStatusType.LowTemperature:
			if ((float)plant.lifeRequirements.TimeUntilFrozenDamage > 0f && num > (float)plant.lifeRequirements.TimeUntilFrozenDamage)
			{
				return true;
			}
			break;
		case PlantStatusType.HighTemperature:
			if ((float)plant.lifeRequirements.TimeUntilOverHeatedDamage > 0f && num > (float)plant.lifeRequirements.TimeUntilOverHeatedDamage)
			{
				return true;
			}
			break;
		case PlantStatusType.Suffocated:
			if ((float)plant.lifeRequirements.TimeUntilSuffocatedDamage > 0f && num > (float)plant.lifeRequirements.TimeUntilSuffocatedDamage)
			{
				return true;
			}
			break;
		case PlantStatusType.LowPressure:
			if ((float)plant.lifeRequirements.TimeUntilLowPressureDamage > 0f && num > (float)plant.lifeRequirements.TimeUntilLowPressureDamage)
			{
				return true;
			}
			break;
		case PlantStatusType.HighPressure:
			if ((float)plant.lifeRequirements.TimeUntilHighPressureDamage > 0f && num > (float)plant.lifeRequirements.TimeUntilHighPressureDamage)
			{
				return true;
			}
			break;
		case PlantStatusType.UnDesiredGas:
			if ((float)plant.lifeRequirements.TimeUntilUndesiredGasDamage > 0f && num > (float)plant.lifeRequirements.TimeUntilUndesiredGasDamage)
			{
				return true;
			}
			break;
		case PlantStatusType.LowWaterTemperature:
			if ((float)plant.lifeRequirements.TimeUntilFrozenDamage > 0f && num > (float)plant.lifeRequirements.TimeUntilFrozenDamage)
			{
				return true;
			}
			break;
		case PlantStatusType.HighWaterTemperature:
			if ((float)plant.lifeRequirements.TimeUntilOverHeatedDamage > 0f && num > (float)plant.lifeRequirements.TimeUntilOverHeatedDamage)
			{
				return true;
			}
			break;
		case PlantStatusType.Lit:
			if ((float)plant.lifeRequirements.TimeUntilLightDamage > 0f && num > (float)plant.lifeRequirements.TimeUntilLightDamage)
			{
				return true;
			}
			break;
		case PlantStatusType.Darkness:
			if ((float)plant.lifeRequirements.TimeUntilDarknessDamage > 0f && num > (float)plant.lifeRequirements.TimeUntilDarknessDamage)
			{
				return true;
			}
			break;
		default:
			return false;
		}
		return false;
	}

	public ushort PackCurrentStates()
	{
		ushort num = 0;
		if (StateCount > 16)
		{
			throw new ArgumentOutOfRangeException();
		}
		for (int i = 0; i < _currentStates.Length; i++)
		{
			if (_currentStates[i])
			{
				num |= (ushort)(1 << i);
			}
		}
		return num;
	}

	public void UnpackCurrentStates(ushort value)
	{
		if (StateCount > 16)
		{
			throw new ArgumentOutOfRangeException();
		}
		for (int i = 0; i < _currentStates.Length; i++)
		{
			_currentStates[i] = (value & (1 << i)) != 0;
		}
	}

	public void PrintDebugInfo()
	{
		ConsoleWindow.Print("");
		ConsoleWindow.Print("Plant Efficiencies:");
		ConsoleWindow.Print($"Breathing Efficiency: {BreathingEfficiency}");
		ConsoleWindow.Print($"Temperature Efficiency: {TemperatureEfficiency}");
		ConsoleWindow.Print($"Hydration Efficiency: {HydrationEfficiency}");
		ConsoleWindow.Print($"Pressure Efficiency: {PressureEfficiency}");
		ConsoleWindow.Print($"Light Efficiency: {LightEfficiency}");
		ConsoleWindow.Print("");
	}
}
