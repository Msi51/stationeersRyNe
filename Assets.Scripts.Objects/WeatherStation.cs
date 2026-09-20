using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;
using Weather;

namespace Assets.Scripts.Objects;

public class WeatherStation : Device, ISmartRotatable
{
	public static List<WeatherStation> AllWeatherStations = new List<WeatherStation>();

	private bool _isWeatherScheduled;

	private bool _isWeatherActive;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	[Header("Weather Machine")]
	[Tooltip("The turbine transform")]
	public Transform Turbine;

	[Tooltip("The speed at which the turbine spins when the storm is scheduled")]
	public float StormScheduledTurbineSpeed = 400f;

	[Tooltip("The speed at which the turbine spins when the storm is active")]
	public float StormActiveTurbineSpeed = 800f;

	private const float TurbineSpeedLerpMaxDelta = 150f;

	private float _turbineSpeed;

	private static readonly string[] StormModeStrings = new string[3] { "NoStorm", "StormIncoming", "InStorm" };

	private PressurekPa _localPressure;

	[ByteArraySync]
	public bool IsWeatherScheduled
	{
		get
		{
			return _isWeatherScheduled;
		}
		set
		{
			_isWeatherScheduled = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
			OnWeatherScheduled(value);
		}
	}

	[ByteArraySync]
	public bool IsWeatherActive
	{
		get
		{
			return _isWeatherActive;
		}
		set
		{
			_isWeatherActive = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
			OnWeatherActive(value);
		}
	}

	public override string[] ModeStrings => StormModeStrings;

	private bool IsValid
	{
		get
		{
			if (Powered)
			{
				return GetRoom() == null;
			}
			return false;
		}
	}

	public override void Start()
	{
		base.Start();
		if (!IsCursor && !AllWeatherStations.Contains(this))
		{
			AllWeatherStations.Add(this);
		}
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (!IsCursor && !WorldManager.IsGamePaused && GetRoom() == null && (_turbineSpeed != 0f || Button1 != 0))
		{
			_turbineSpeed = Mathf.MoveTowards(_turbineSpeed, GetTurbineSpeed(), 150f * Time.deltaTime);
			Turbine.Rotate(0f, _turbineSpeed * Time.deltaTime, 0f);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		_localPressure = AtmosphericsController.World.SampleGlobalAtmosphere(base.WorldGrid).PressureGasses;
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		if (!GameManager.RunSimulation)
		{
			_localPressure = AtmosphericsController.World.SampleGlobalAtmosphere(base.WorldGrid).PressureGasses;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteBoolean(IsWeatherScheduled);
			writer.WriteBoolean(IsWeatherActive);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			IsWeatherScheduled = reader.ReadBoolean();
			IsWeatherActive = reader.ReadBoolean();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteBoolean(IsWeatherScheduled);
		writer.WriteBoolean(IsWeatherActive);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		IsWeatherScheduled = reader.ReadBoolean();
		IsWeatherActive = reader.ReadBoolean();
	}

	private float GetTurbineSpeed()
	{
		float num = Mathf.Clamp01(_localPressure.ToFloat() / 2f);
		if (WeatherManager.CurrentWeatherEvent?.StormEffect != null)
		{
			return Button1 switch
			{
				1 => StormScheduledTurbineSpeed * num, 
				2 => StormActiveTurbineSpeed * num, 
				_ => 0f, 
			};
		}
		return 0f;
	}

	private void OnWeatherScheduled(bool scheduled)
	{
		if (GetRoom() == null && scheduled)
		{
			OnServer.Interact(base.InteractButton1, 1);
			OnServer.Interact(base.InteractMode, 1);
		}
	}

	private void OnWeatherActive(bool active)
	{
		if (GetRoom() == null)
		{
			if (active)
			{
				OnServer.Interact(base.InteractButton1, 2);
				OnServer.Interact(base.InteractMode, 2);
			}
			else
			{
				OnServer.Interact(base.InteractButton1, 0);
				OnServer.Interact(base.InteractMode, 0);
			}
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.NextWeatherEventTime || logicType == LogicType.NextWeatherHash)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (GetRoom() != null)
		{
			extendedText.AppendLine(GameStrings.DeviceMustBeOutside.AsString(ToTooltip()));
		}
		if (IsWeatherScheduled)
		{
			extendedText.AppendLine(GameStrings.WeatherEventComing.AsString());
		}
		return extendedText;
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.NextWeatherEventTime:
			if (!IsValid)
			{
				return 0.0;
			}
			return WeatherManager.GetSecondsWhenNextWeatherEventIsActive();
		case LogicType.NextWeatherHash:
			if (!IsValid)
			{
				return 0.0;
			}
			return WeatherManager.CurrentWeatherEvent?.IdHash ?? 0;
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public static void SetWeatherScheduled(bool scheduled)
	{
		int count = AllWeatherStations.Count;
		while (count-- > 0)
		{
			AllWeatherStations[count].IsWeatherScheduled = scheduled;
		}
	}

	public static void SetWeatherActive(bool active)
	{
		int count = AllWeatherStations.Count;
		while (count-- > 0)
		{
			AllWeatherStations[count].IsWeatherActive = active;
		}
	}

	public override void OnDestroy()
	{
		AllWeatherStations.Remove(this);
		base.OnDestroy();
	}

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

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (!base.IsStructureCompleted && GameManager.GameState == GameState.Running)
		{
			return false;
		}
		if (logicType == LogicType.Mode)
		{
			return false;
		}
		return base.CanLogicWrite(logicType);
	}
}
