using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

[XmlRoot]
public class PlantRecord
{
	[XmlIgnore]
	public Plant Plant;

	public long PlantReference;

	public float Age;

	public float LightStress;

	public float TimeLitRatio = 0.5f;

	public float TimeDarknessRatio = 0.5f;

	public float TimeDehydrated;

	public float TimeFrozen;

	public float TimeOverHeated;

	public float TimeSuffocated;

	public float TimeLowPressure;

	public float TimeHighPressure;

	public float TimePolluted;

	[XmlIgnore]
	private AnimationCurve _darknessLightUsageMultiplier = new AnimationCurve(new Keyframe(-5f, 0f), new Keyframe(0f, 1f), new Keyframe(1f, 1f), new Keyframe(10f, 5f));

	[XmlIgnore]
	private AnimationCurve _darknessLightGainMultiplier = new AnimationCurve(new Keyframe(-5f, 5f), new Keyframe(-0.01f, 2f), new Keyframe(0f, 1f), new Keyframe(1f, 1f), new Keyframe(5f, 0f));

	public PlantRecord()
	{
	}

	public PlantRecord(Plant plant)
	{
		Plant = plant;
		PlantReference = plant.ReferenceId;
	}

	public PlantRecord(PlantRecord recordToCopy)
	{
		PlantReference = recordToCopy.PlantReference;
		Plant = Thing.Find<Plant>(recordToCopy.PlantReference);
		Age = recordToCopy.Age;
		LightStress = recordToCopy.LightStress;
		TimeDehydrated = recordToCopy.TimeDehydrated;
		TimeLitRatio = recordToCopy.TimeLitRatio;
		TimeDarknessRatio = recordToCopy.TimeDarknessRatio;
		TimeFrozen = recordToCopy.TimeFrozen;
		TimeOverHeated = recordToCopy.TimeOverHeated;
		TimeSuffocated = recordToCopy.TimeSuffocated;
		TimeLowPressure = recordToCopy.TimeLowPressure;
		TimeHighPressure = recordToCopy.TimeHighPressure;
		TimePolluted = recordToCopy.TimePolluted;
	}

	public float GetRecord(PlantStatusType statusType)
	{
		return statusType switch
		{
			PlantStatusType.Dehydrated => TimeDehydrated, 
			PlantStatusType.Lit => TimeLitRatio, 
			PlantStatusType.Darkness => TimeDarknessRatio, 
			PlantStatusType.LowTemperature => TimeFrozen, 
			PlantStatusType.HighTemperature => TimeOverHeated, 
			PlantStatusType.Suffocated => TimeSuffocated, 
			PlantStatusType.LowPressure => TimeLowPressure, 
			PlantStatusType.HighPressure => TimeHighPressure, 
			PlantStatusType.UnDesiredGas => TimePolluted, 
			PlantStatusType.LowWaterTemperature => TimeFrozen, 
			PlantStatusType.HighWaterTemperature => TimeOverHeated, 
			_ => 0f, 
		};
	}

	public void UpdateRecord(Plant plant, float tickDeltaTime)
	{
		if ((object)plant == null || PlantReference == 0L || PlantReference != plant.ReferenceId)
		{
			return;
		}
		Age += tickDeltaTime;
		if (plant.PlantStatus.GetCurrentState(PlantStatusType.Dehydrated))
		{
			TimeDehydrated += tickDeltaTime;
		}
		if (plant.PlantStatus.GetCurrentState(PlantStatusType.LowTemperature) || plant.PlantStatus.GetCurrentState(PlantStatusType.LowWaterTemperature))
		{
			TimeFrozen += tickDeltaTime;
		}
		if (plant.PlantStatus.GetCurrentState(PlantStatusType.HighTemperature) || plant.PlantStatus.GetCurrentState(PlantStatusType.HighWaterTemperature))
		{
			TimeOverHeated += tickDeltaTime;
		}
		if (plant.PlantStatus.GetCurrentState(PlantStatusType.Suffocated))
		{
			TimeSuffocated += tickDeltaTime;
		}
		if (plant.PlantStatus.GetCurrentState(PlantStatusType.LowPressure))
		{
			TimeLowPressure += tickDeltaTime;
		}
		if (plant.PlantStatus.GetCurrentState(PlantStatusType.HighPressure))
		{
			TimeHighPressure += tickDeltaTime;
		}
		if (plant.PlantStatus.GetCurrentState(PlantStatusType.UnDesiredGas))
		{
			TimePolluted += tickDeltaTime;
		}
		if ((float)plant.lifeRequirements.LightPerDay > 0f)
		{
			if (plant.PlantStatus.GetCurrentState(PlantStatusType.Lit))
			{
				TimeLitRatio += tickDeltaTime / (float)plant.lifeRequirements.LightPerDay * plant.CurrentLightExposure * _darknessLightGainMultiplier.Evaluate(TimeLitRatio);
			}
			TimeLitRatio -= tickDeltaTime / (float)OrbitalSimulation.GetDayLengthSeconds() * _darknessLightUsageMultiplier.Evaluate(TimeLitRatio);
		}
		else
		{
			TimeLitRatio = 1f;
		}
		if ((float)plant.lifeRequirements.DarknessPerDay > 0f)
		{
			if (plant.PlantStatus.GetCurrentState(PlantStatusType.Darkness))
			{
				TimeDarknessRatio += tickDeltaTime / (float)plant.lifeRequirements.DarknessPerDay * _darknessLightGainMultiplier.Evaluate(TimeDarknessRatio);
			}
			TimeDarknessRatio -= tickDeltaTime / (float)OrbitalSimulation.GetDayLengthSeconds() * _darknessLightUsageMultiplier.Evaluate(TimeDarknessRatio);
		}
		else
		{
			TimeDarknessRatio = 1f;
		}
		float num = 2f;
		if (TimeLitRatio < 0f || TimeDarknessRatio < 0f)
		{
			LightStress += tickDeltaTime / (float)OrbitalSimulation.GetDayLengthSeconds() * num;
		}
		else
		{
			LightStress = Mathf.Lerp(LightStress, 0f, tickDeltaTime / (float)OrbitalSimulation.GetDayLengthSeconds() * Mathf.Clamp(LightStress, 0.5f, 5f));
		}
		plant.PlantStatus.LightEfficiency = 1f / (1f + LightStress);
		float num2 = 3f;
		if (TimeLitRatio > 1f && TimeDarknessRatio > 0f)
		{
			plant.PlantStatus.LightEfficiency *= Mathf.Lerp(1f, 2f, (TimeLitRatio - 1f) / num2);
		}
	}

	public void PrintDebugInfo()
	{
		ConsoleWindow.Print("");
		ConsoleWindow.Print("Plant Record:");
		ConsoleWindow.Print($"Age: {Age}");
		ConsoleWindow.Print($"Light Stress: {LightStress}");
		ConsoleWindow.Print($"Time Lit Ratio: {TimeLitRatio}");
		ConsoleWindow.Print($"Time DarknessRatio: {TimeDarknessRatio}");
		ConsoleWindow.Print($"Time Dehydrated: {TimeDehydrated}");
		ConsoleWindow.Print($"Time Frozen: {TimeFrozen}");
		ConsoleWindow.Print($"Time OverHeated: {TimeOverHeated}");
		ConsoleWindow.Print($"Time Suffocated: {TimeSuffocated}");
		ConsoleWindow.Print($"Time LowPressure: {TimeLowPressure}");
		ConsoleWindow.Print($"Time HighPressure: {TimeHighPressure}");
		ConsoleWindow.Print($"Time Polluted: {TimePolluted}");
		ConsoleWindow.Print("");
	}
}
