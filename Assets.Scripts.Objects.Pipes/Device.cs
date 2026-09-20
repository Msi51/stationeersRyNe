using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Events;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class Device : SmallGrid, ILogicable, IReferencable, IEvaluable, IConnected, ISlotWriteable, IWreckage, IPowered, IDensePoolable
{
	private const int MAX_DEVICES = 8192;

	public static readonly DensePool<Device> AllDevices = new DensePool<Device>("AllDevices", 8192);

	public const float RENDER_DISTANCE = 30f;

	public const float SHADOW_DISTANCE = 10f;

	public static List<Device> AllDevicePrefabs = new List<Device>();

	private static List<IRobotInput> AllRobotInputs = new List<IRobotInput>();

	[Header("Device")]
	[Tooltip("How much power (in Watts) does the device used while turned on.")]
	public float UsedPower = 10f;

	[ReadOnly]
	public List<ChuteNetwork> ConnectedChuteNetworks = new List<ChuteNetwork>();

	[ReadOnly]
	public List<PipeNetwork> ConnectedPipeNetworks = new List<PipeNetwork>();

	[ReadOnly]
	public List<CableNetwork> ConnectedCableNetworks = new List<CableNetwork>();

	[ReadOnly]
	public List<Cable> AttachedCables = new List<Cable>();

	[SerializeField]
	protected InfoScreenComponent _infoScreen;

	private Vector3 _centerPosition;

	public static int MaxProviderRecursionIterations = 512;

	public Event OnDeviceConnectToNetworkEvent;

	private readonly DensePoolReference<Device> _deviceDensePool = new DensePoolReference<Device>(AllDevices);

	private readonly DensePoolReference<ISolarRadiator> _solarRadiatorPool = new DensePoolReference<ISolarRadiator>(SolarRadiators.AllSolarRadiators);

	private readonly DensePoolReference<ICircuitHolder> _circuitHolderPool = new DensePoolReference<ICircuitHolder>(CircuitHolders.AllCircuitHolders);

	[SerializeField]
	private Wreckage[] wreckagePrefabs;

	public static readonly Action<Device> InitializeDeviceAction = delegate(Device device)
	{
		if (!(device == null) && !device.IsBeingDestroyed)
		{
			device.InitializeDevice();
			device.InitializeDataConnection();
		}
	};

	public bool IsDeviceActive { get; private set; }

	public virtual bool IsPowerProvider => false;

	public Connection DataConnection => OpenEnds.Find(delegate(Connection c)
	{
		NetworkType connectionType = c.ConnectionType;
		return connectionType == NetworkType.Data || connectionType == NetworkType.PowerAndData;
	});

	public virtual DropType DropType => DropType.Ore;

	public Cable DataCable { get; set; }

	public Cable PowerCable { get; private set; }

	public override Vector3 CenterPosition
	{
		get
		{
			if (IsCursor)
			{
				_centerPosition = base.ThingTransformPosition + ThingTransform.rotation * Bounds.center;
			}
			return _centerPosition;
		}
	}

	public new virtual int TotalSlots => Slots.Count;

	protected virtual bool IsOperable
	{
		get
		{
			if (base.IsStructureCompleted)
			{
				return !IsBroken;
			}
			return false;
		}
	}

	public virtual bool IsPowerInputOutput => false;

	public CableNetwork DataCableNetwork
	{
		get
		{
			if (!(DataCable == null))
			{
				return DataCable.CableNetwork;
			}
			return null;
		}
	}

	public CableNetwork PowerCableNetwork
	{
		get
		{
			if (!(PowerCable == null))
			{
				return PowerCable.CableNetwork;
			}
			return null;
		}
	}

	public Cable[] DataCables { get; private set; } = Array.Empty<Cable>();

	public Cable[] PowerCables { get; private set; } = Array.Empty<Cable>();

	protected virtual float EnergyToHeatRatio => 0.2f;

	public virtual Wreckage[] WreckagePrefabs => wreckagePrefabs;

	public virtual WreckageSize WreckageSize => WreckageSize.Small;

	public virtual int WreckageQuantity => 1;

	public int CustomColorIndex => CustomColor?.Index ?? (-1);

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(30f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(10f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	protected void OnDeviceActive()
	{
		IsDeviceActive = true;
	}

	protected void OnDeviceIdle()
	{
		IsDeviceActive = false;
	}

	public override void PrintDebugInfo(bool verbose = false)
	{
		base.PrintDebugInfo(verbose);
		ConsoleWindow.Print($"IsOperable: {IsOperable}");
		for (int i = 0; i < OpenEnds.Count; i++)
		{
			Connection connection = OpenEnds[i];
			ConsoleWindow.Print($"End #{i}: {connection.ConnectionType} {connection.ConnectionRole}");
			switch (connection.ConnectionType)
			{
			case NetworkType.Power:
			case NetworkType.Data:
			case NetworkType.PowerAndData:
			{
				Cable cable = connection.GetCable();
				if ((object)cable != null)
				{
					ConsoleWindow.Print($"\tCable: {cable.DisplayName} #{cable.ReferenceId}");
					ConsoleWindow.Print($"\tNetwork: #{cable.CableNetwork?.ReferenceId}");
				}
				break;
			}
			case NetworkType.Pipe:
			case NetworkType.PipeLiquid:
			{
				Pipe pipe = connection.GetPipe();
				if ((object)pipe != null)
				{
					ConsoleWindow.Print($"\tPipe: {pipe.DisplayName} #{pipe.ReferenceId}");
					ConsoleWindow.Print($"\tNetwork: #{pipe.PipeNetwork?.ReferenceId}");
				}
				break;
			}
			case NetworkType.Chute:
			{
				Chute chute = connection.GetChute();
				if ((object)chute != null)
				{
					ConsoleWindow.Print($"\tChute: {chute.DisplayName} #{chute.ReferenceId}");
					ConsoleWindow.Print($"\tNetwork: #{chute.ChuteNetwork?.ReferenceId}");
				}
				break;
			}
			}
		}
	}

	public CableNetwork GetNetwork(int networkIndex)
	{
		switch (networkIndex)
		{
		case int.MinValue:
			return null;
		case int.MaxValue:
		{
			for (int i = 0; i < OpenEnds.Count; i++)
			{
				CableNetwork cableNetwork = OpenEnds[i].GetCable()?.CableNetwork;
				if (cableNetwork != null)
				{
					return cableNetwork;
				}
			}
			break;
		}
		}
		if (networkIndex >= OpenEnds.Count || networkIndex < 0)
		{
			return null;
		}
		return OpenEnds[networkIndex].GetCable()?.CableNetwork;
	}

	public static double BatchRead(LogicBatchMethod method, LogicType logicType, int deviceHash, List<ILogicable> devices)
	{
		int num = 0;
		double num2 = 0.0;
		switch (method)
		{
		case LogicBatchMethod.Count:
		{
			int count4 = devices.Count;
			while (count4-- > 0)
			{
				if (devices[count4].GetPrefabHash() == deviceHash)
				{
					num++;
				}
			}
			num2 = num;
			break;
		}
		case LogicBatchMethod.Average:
		case LogicBatchMethod.Sum:
		{
			num2 = 0.0;
			num = 0;
			int count2 = devices.Count;
			while (count2-- > 0)
			{
				ILogicable logicable2 = devices[count2];
				if (logicable2 != null && logicable2.GetPrefabHash() == deviceHash)
				{
					num2 += logicable2.GetLogicValue(logicType);
					num++;
				}
			}
			if (method == LogicBatchMethod.Average)
			{
				num2 /= (double)num;
			}
			break;
		}
		case LogicBatchMethod.Minimum:
		{
			num2 = double.PositiveInfinity;
			int count3 = devices.Count;
			while (count3-- > 0)
			{
				ILogicable logicable3 = devices[count3];
				if (logicable3 != null && logicable3.GetPrefabHash() == deviceHash)
				{
					double logicValue2 = logicable3.GetLogicValue(logicType);
					if (!(logicValue2 >= num2))
					{
						num2 = logicValue2;
					}
				}
			}
			if (num2 >= double.PositiveInfinity)
			{
				num2 = 0.0;
			}
			break;
		}
		case LogicBatchMethod.Maximum:
		{
			num2 = double.NegativeInfinity;
			int count = devices.Count;
			while (count-- > 0)
			{
				ILogicable logicable = devices[count];
				if (logicable != null && logicable.GetPrefabHash() == deviceHash)
				{
					double logicValue = logicable.GetLogicValue(logicType);
					if (!(logicValue <= num2))
					{
						num2 = logicValue;
					}
				}
			}
			break;
		}
		}
		return num2;
	}

	public static double BatchRead(LogicBatchMethod method, LogicType logicType, int deviceHash, int nameHash, List<ILogicable> devices)
	{
		int num = 0;
		double num2 = 0.0;
		switch (method)
		{
		case LogicBatchMethod.Count:
		{
			int count4 = devices.Count;
			while (count4-- > 0)
			{
				if (devices[count4].GetPrefabHash() == deviceHash)
				{
					num++;
				}
			}
			num2 = num;
			break;
		}
		case LogicBatchMethod.Average:
		case LogicBatchMethod.Sum:
		{
			num2 = 0.0;
			num = 0;
			int count2 = devices.Count;
			while (count2-- > 0)
			{
				ILogicable logicable2 = devices[count2];
				if (logicable2 != null && logicable2.GetPrefabHash() == deviceHash && logicable2.GetNameHash() == nameHash)
				{
					num2 += logicable2.GetLogicValue(logicType);
					num++;
				}
			}
			if (method == LogicBatchMethod.Average)
			{
				num2 /= (double)num;
			}
			break;
		}
		case LogicBatchMethod.Minimum:
		{
			num2 = double.PositiveInfinity;
			int count3 = devices.Count;
			while (count3-- > 0)
			{
				ILogicable logicable3 = devices[count3];
				if (logicable3 != null && logicable3.GetPrefabHash() == deviceHash && logicable3.GetNameHash() == nameHash)
				{
					double logicValue2 = logicable3.GetLogicValue(logicType);
					if (!(logicValue2 >= num2))
					{
						num2 = logicValue2;
					}
				}
			}
			if (num2 >= double.PositiveInfinity)
			{
				num2 = 0.0;
			}
			break;
		}
		case LogicBatchMethod.Maximum:
		{
			num2 = double.NegativeInfinity;
			int count = devices.Count;
			while (count-- > 0)
			{
				ILogicable logicable = devices[count];
				if (logicable != null && logicable.GetPrefabHash() == deviceHash && logicable.GetNameHash() == nameHash)
				{
					double logicValue = logicable.GetLogicValue(logicType);
					if (!(logicValue <= num2))
					{
						num2 = logicValue;
					}
				}
			}
			break;
		}
		}
		return num2;
	}

	public static double BatchRead(LogicBatchMethod method, LogicSlotType logicType, int slotIndex, int deviceHash, List<ILogicable> devices)
	{
		int num = 0;
		double num2 = 0.0;
		switch (method)
		{
		case LogicBatchMethod.Count:
		{
			int count4 = devices.Count;
			while (count4-- > 0)
			{
				if (devices[count4].GetPrefabHash() == deviceHash)
				{
					num++;
				}
			}
			num2 = num;
			break;
		}
		case LogicBatchMethod.Average:
		case LogicBatchMethod.Sum:
		{
			num2 = 0.0;
			num = 0;
			int count2 = devices.Count;
			while (count2-- > 0)
			{
				ILogicable logicable2 = devices[count2];
				if (logicable2 != null && logicable2.GetPrefabHash() == deviceHash)
				{
					num2 += logicable2.GetLogicValue(logicType, slotIndex);
					num++;
				}
			}
			if (method == LogicBatchMethod.Average)
			{
				num2 /= (double)num;
			}
			break;
		}
		case LogicBatchMethod.Minimum:
		{
			num2 = double.PositiveInfinity;
			int count3 = devices.Count;
			while (count3-- > 0)
			{
				ILogicable logicable3 = devices[count3];
				if (logicable3 != null && logicable3.GetPrefabHash() == deviceHash)
				{
					double logicValue2 = logicable3.GetLogicValue(logicType, slotIndex);
					if (!(logicValue2 >= num2))
					{
						num2 = logicValue2;
					}
				}
			}
			if (num2 >= double.PositiveInfinity)
			{
				num2 = 0.0;
			}
			break;
		}
		case LogicBatchMethod.Maximum:
		{
			num2 = double.NegativeInfinity;
			int count = devices.Count;
			while (count-- > 0)
			{
				ILogicable logicable = devices[count];
				if (logicable != null && logicable.GetPrefabHash() == deviceHash)
				{
					double logicValue = logicable.GetLogicValue(logicType, slotIndex);
					if (!(logicValue <= num2))
					{
						num2 = logicValue;
					}
				}
			}
			break;
		}
		}
		return num2;
	}

	public static double BatchRead(LogicBatchMethod method, LogicSlotType logicType, int slotIndex, int deviceHash, int nameHash, List<ILogicable> devices)
	{
		int num = 0;
		double num2 = 0.0;
		switch (method)
		{
		case LogicBatchMethod.Count:
		{
			int count4 = devices.Count;
			while (count4-- > 0)
			{
				if (devices[count4].GetPrefabHash() == deviceHash)
				{
					num++;
				}
			}
			num2 = num;
			break;
		}
		case LogicBatchMethod.Average:
		case LogicBatchMethod.Sum:
		{
			num2 = 0.0;
			num = 0;
			int count2 = devices.Count;
			while (count2-- > 0)
			{
				ILogicable logicable2 = devices[count2];
				if (logicable2 != null && logicable2.GetPrefabHash() == deviceHash && logicable2.GetNameHash() == nameHash)
				{
					num2 += logicable2.GetLogicValue(logicType, slotIndex);
					num++;
				}
			}
			if (method == LogicBatchMethod.Average)
			{
				num2 /= (double)num;
			}
			break;
		}
		case LogicBatchMethod.Minimum:
		{
			num2 = double.PositiveInfinity;
			int count3 = devices.Count;
			while (count3-- > 0)
			{
				ILogicable logicable3 = devices[count3];
				if (logicable3 != null && logicable3.GetPrefabHash() == deviceHash && logicable3.GetNameHash() == nameHash)
				{
					double logicValue2 = logicable3.GetLogicValue(logicType, slotIndex);
					if (!(logicValue2 >= num2))
					{
						num2 = logicValue2;
					}
				}
			}
			if (num2 >= double.PositiveInfinity)
			{
				num2 = 0.0;
			}
			break;
		}
		case LogicBatchMethod.Maximum:
		{
			num2 = double.NegativeInfinity;
			int count = devices.Count;
			while (count-- > 0)
			{
				ILogicable logicable = devices[count];
				if (logicable != null && logicable.GetPrefabHash() == deviceHash && logicable.GetNameHash() == nameHash)
				{
					double logicValue = logicable.GetLogicValue(logicType, slotIndex);
					if (!(logicValue <= num2))
					{
						num2 = logicValue;
					}
				}
			}
			break;
		}
		}
		return num2;
	}

	public static IRobotInput GetNearestRobotInput(Vector3 worldPosition, float maxDistance = -1f)
	{
		IRobotInput result = null;
		float num = float.MaxValue;
		for (int i = 0; i < AllRobotInputs.Count; i++)
		{
			IRobotInput robotInput = AllRobotInputs[i];
			float num2 = Vector3.Distance(robotInput.Position, worldPosition);
			if (num2 < num && (maxDistance < 0f || num2 < maxDistance))
			{
				num = num2;
				result = robotInput;
			}
		}
		return result;
	}

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None && !IsCursor)
		{
			if (this is IRobotInput item)
			{
				AllRobotInputs.Add(item);
			}
			if (this is ISolarRadiator radiator)
			{
				SolarRadiators.Register(radiator);
			}
			if (this is ICircuitHolder iCircuitHolder)
			{
				CircuitHolders.Register(iCircuitHolder);
			}
		}
	}

	public override void OnDestroy()
	{
		if (Singleton<GameManager>.IsQuitting)
		{
			return;
		}
		base.OnDestroy();
		if (!IsCursor)
		{
			if (this is IRobotInput item)
			{
				AllRobotInputs.Remove(item);
			}
			if (this is ISolarRadiator radiator)
			{
				SolarRadiators.Deregister(radiator);
			}
			if (this is ICircuitHolder iCircuitHolder)
			{
				CircuitHolders.Deregister(iCircuitHolder);
			}
		}
	}

	protected virtual void CheckConnections()
	{
	}

	public virtual bool IsLogicSlotReadable()
	{
		return HasAnySlots;
	}

	public override Slot GetSlot(int slotIndex)
	{
		if (Slots == null || slotIndex < 0 || slotIndex >= Slots.Count)
		{
			return null;
		}
		return Slots[slotIndex];
	}

	public virtual int GetNextSlotId(int slotIndex, bool isForward)
	{
		if (Slots == null)
		{
			return -1;
		}
		if (Slots.Count == 0)
		{
			return 0;
		}
		int num = slotIndex;
		num += (isForward ? 1 : (-1));
		num %= Slots.Count;
		if (num < 0)
		{
			num += Slots.Count;
		}
		return num;
	}

	public virtual bool IsLogicReadable()
	{
		for (int i = 0; i < EnumCollections.LogicTypes.Values.Length; i++)
		{
			LogicType logicType = EnumCollections.LogicTypes.Values[i];
			if (CanLogicRead(logicType))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool IsLogicWritable()
	{
		for (int i = 0; i < EnumCollections.LogicTypes.Values.Length; i++)
		{
			LogicType logicType = EnumCollections.LogicTypes.Values[i];
			if (CanLogicWrite(logicType))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool CanLogicRead(LogicType logicType)
	{
		if (!base.IsStructureCompleted && GameManager.GameState == GameState.Running)
		{
			return false;
		}
		switch (logicType)
		{
		case LogicType.Color:
			return HasColorState;
		case LogicType.Activate:
			return HasActivateState;
		case LogicType.Power:
			return HasPowerState;
		case LogicType.Open:
			return HasOpenState;
		case LogicType.Mode:
			return HasModeState;
		case LogicType.Error:
			return HasErrorState;
		case LogicType.Lock:
			return HasLockState;
		case LogicType.On:
			return HasOnOffState;
		case LogicType.Pressure:
		case LogicType.Temperature:
		case LogicType.RatioOxygen:
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioNitrogen:
		case LogicType.RatioPollutant:
		case LogicType.RatioMethane:
		case LogicType.RatioWater:
		case LogicType.TotalMoles:
		case LogicType.RatioNitrousOxide:
		case LogicType.Combustion:
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
			return HasReadableAtmosphere;
		case LogicType.RequiredPower:
			if (UsedPower > 0f)
			{
				return HasPowerState;
			}
			return false;
		case LogicType.Reagents:
			return HasReadableReagentMixture;
		case LogicType.PrefabHash:
		case LogicType.ReferenceId:
		case LogicType.NameHash:
			return true;
		case LogicType.StackSize:
			return this is IMemory;
		default:
			return false;
		}
	}

	public virtual bool CanLogicWrite(LogicType logicType)
	{
		if (!base.IsStructureCompleted && GameManager.GameState == GameState.Running)
		{
			return false;
		}
		return logicType switch
		{
			LogicType.Color => HasColorState, 
			LogicType.Activate => HasActivateState, 
			LogicType.Open => HasOpenState, 
			LogicType.Mode => HasModeState, 
			LogicType.Lock => HasLockState, 
			LogicType.On => HasOnOffState, 
			_ => false, 
		};
	}

	public virtual void SetLogicValue(LogicType logicType, double value)
	{
		if (!base.IsStructureCompleted)
		{
			return;
		}
		int state = (int)Mathf.Clamp((float)value, 0f, 1f);
		switch (logicType)
		{
		case LogicType.Color:
		{
			int num = (int)value.Clamp(0.0, GameManager.ColorCount - 1);
			if (GameManager.IsLogicSelectableColor(num))
			{
				OnServer.Interact(base.InteractColor, num);
			}
			break;
		}
		case LogicType.Activate:
			if (OnOff)
			{
				OnServer.Interact(base.InteractActivate, state);
			}
			break;
		case LogicType.Open:
			OnServer.Interact(base.InteractOpen, state);
			break;
		case LogicType.Mode:
		{
			int state2 = (int)value.Clamp(0.0, ModeStrings.Length - 1);
			OnServer.Interact(base.InteractMode, state2);
			break;
		}
		case LogicType.Lock:
			OnServer.Interact(base.InteractLock, state);
			break;
		case LogicType.On:
			OnServer.Interact(base.InteractOnOff, state);
			break;
		}
	}

	public virtual double GetLogicValue(LogicType logicType)
	{
		if (!base.IsStructureCompleted)
		{
			return 0.0;
		}
		switch (logicType)
		{
		case LogicType.ReferenceId:
			return base.ReferenceId;
		case LogicType.Color:
			return ColorState;
		case LogicType.Activate:
			return Activate;
		case LogicType.On:
			return OnOff ? 1 : 0;
		case LogicType.Power:
			return Powered ? 1 : 0;
		case LogicType.Open:
			return IsOpen ? 1 : 0;
		case LogicType.Mode:
			return Mode;
		case LogicType.Error:
			return Error;
		case LogicType.Lock:
			return IsLocked ? 1 : 0;
		case LogicType.Combustion:
		{
			Atmosphere internalAtmosphere = base.InternalAtmosphere;
			if (internalAtmosphere == null || !internalAtmosphere.Sparked)
			{
				return 0.0;
			}
			return 1.0;
		}
		case LogicType.TotalMoles:
			return base.InternalAtmosphere?.TotalMoles.ToDouble() ?? 0.0;
		case LogicType.Pressure:
			return base.InternalAtmosphere?.PressureGassesAndLiquids.ToDouble() ?? 0.0;
		case LogicType.Temperature:
			return base.InternalAtmosphere?.Temperature.ToDouble() ?? 0.0;
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
			return GasRatio(logicType);
		case LogicType.RequiredPower:
			return GetUsedPower(PowerCableNetwork);
		case LogicType.Reagents:
			return (ReadableReagentMixture != null) ? ((float)ReadableReagentMixture.TotalReagents) : 0f;
		case LogicType.PrefabHash:
			return PrefabHash;
		case LogicType.NameHash:
			return GetNameHash();
		case LogicType.StackSize:
			return (this is IMemory memory) ? ((float)memory.GetStackSize()) : 0f;
		default:
			return 0.0;
		}
	}

	public virtual bool CanLogicWrite(LogicSlotType logicSlotType, int slotId)
	{
		if (!HasAnySlots || !base.IsStructureCompleted)
		{
			return false;
		}
		if (slotId < 0 || slotId >= Slots.Count)
		{
			return false;
		}
		Slot slot = Slots[slotId];
		switch (logicSlotType)
		{
		case LogicSlotType.On:
		{
			Slot.Class type = slot.Type;
			return type == Slot.Class.Helmet || type == Slot.Class.Tool || type == Slot.Class.Appliance;
		}
		case LogicSlotType.Lock:
			return slot.Type == Slot.Class.Helmet;
		case LogicSlotType.Open:
		{
			Slot.Class type = slot.Type;
			return type == Slot.Class.Helmet || type == Slot.Class.GasCanister || type == Slot.Class.LiquidCanister || type == Slot.Class.LiquidBottle;
		}
		default:
			return false;
		}
	}

	public virtual void SetLogicValue(LogicSlotType logicSlotType, int slotId, double value)
	{
		if (!HasAnySlots || !base.IsStructureCompleted || slotId < 0 || slotId >= Slots.Count)
		{
			return;
		}
		Slot slot = Slots[slotId];
		if ((object)slot.Occupant == null)
		{
			return;
		}
		switch (logicSlotType)
		{
		case LogicSlotType.Open:
			if (slot.Occupant.HasOpenState)
			{
				OnServer.Interact(slot.Occupant.InteractOpen, (int)value);
			}
			break;
		case LogicSlotType.On:
			if (slot.Occupant.HasOnOffState)
			{
				OnServer.Interact(slot.Occupant.InteractOnOff, (int)value);
			}
			break;
		case LogicSlotType.Lock:
			if (slot.Occupant.HasLockState)
			{
				OnServer.Interact(slot.Occupant.InteractLock, (int)value);
			}
			break;
		}
	}

	public virtual bool CanLogicRead(LogicSlotType logicSlotType, int slotId)
	{
		if (!base.IsStructureCompleted && GameManager.GameState == GameState.Running)
		{
			return false;
		}
		Slot slot = GetSlot(slotId);
		if (slot == null)
		{
			return false;
		}
		switch (logicSlotType)
		{
		case LogicSlotType.Occupied:
		case LogicSlotType.OccupantHash:
		case LogicSlotType.Quantity:
		case LogicSlotType.Damage:
		case LogicSlotType.Class:
		case LogicSlotType.MaxQuantity:
		case LogicSlotType.PrefabHash:
		case LogicSlotType.SortingClass:
		case LogicSlotType.ReferenceId:
		case LogicSlotType.FreeSlots:
		case LogicSlotType.TotalSlots:
			return true;
		case LogicSlotType.On:
		{
			Slot.Class type = slot.Type;
			return type == Slot.Class.Helmet || type == Slot.Class.Tool || type == Slot.Class.Appliance;
		}
		case LogicSlotType.Lock:
			return slot.Type == Slot.Class.Helmet;
		case LogicSlotType.Open:
		{
			Slot.Class type = slot.Type;
			return type == Slot.Class.Helmet || type == Slot.Class.GasCanister || type == Slot.Class.LiquidCanister || type == Slot.Class.LiquidBottle;
		}
		case LogicSlotType.FilterType:
			return slot.Type == Slot.Class.GasFilter;
		case LogicSlotType.Pressure:
		case LogicSlotType.Temperature:
		case LogicSlotType.Volume:
		{
			Slot.Class type = slot.Type;
			return type == Slot.Class.GasCanister || type == Slot.Class.LiquidCanister || type == Slot.Class.LiquidBottle;
		}
		case LogicSlotType.Charge:
		case LogicSlotType.ChargeRatio:
			return slot.Type == Slot.Class.Battery;
		default:
			return false;
		}
	}

	public virtual double GetLogicValue(LogicSlotType logicSlotType, int slotId)
	{
		if (!base.IsStructureCompleted)
		{
			return 0.0;
		}
		Slot slot = GetSlot(slotId);
		if (slot == null)
		{
			return 0.0;
		}
		switch (logicSlotType)
		{
		case LogicSlotType.ReferenceId:
			return slot.Occupant?.ReferenceId ?? 0;
		case LogicSlotType.Occupied:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return 1.0;
		case LogicSlotType.Open:
			if (!slot.Occupant || !slot.Occupant.IsOpen)
			{
				return 0.0;
			}
			return 1.0;
		case LogicSlotType.Lock:
			if (!slot.Occupant || !slot.Occupant.IsLocked)
			{
				return 0.0;
			}
			return 1.0;
		case LogicSlotType.On:
			if (!slot.Occupant || !slot.Occupant.OnOff)
			{
				return 0.0;
			}
			return 1.0;
		case LogicSlotType.OccupantHash:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return slot.Occupant.PrefabHash;
		case LogicSlotType.Quantity:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return ((double?)(slot.Occupant as IQuantity)?.GetQuantity) ?? 1.0;
		case LogicSlotType.MaxQuantity:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return ((double?)(slot.Occupant as IQuantity)?.GetMaxQuantity) ?? 1.0;
		case LogicSlotType.Damage:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return slot.Occupant.DamageState.TotalRatio;
		case LogicSlotType.Pressure:
			if (!slot.Occupant || slot.Occupant.InternalAtmosphere == null)
			{
				return 0.0;
			}
			return slot.Occupant.InternalAtmosphere.PressureGassesAndLiquids.ToDouble();
		case LogicSlotType.Volume:
			if (!slot.Occupant || slot.Occupant.InternalAtmosphere == null)
			{
				return 0.0;
			}
			return slot.Occupant.InternalAtmosphere.Volume.ToDouble();
		case LogicSlotType.Temperature:
			if (!slot.Occupant || slot.Occupant.InternalAtmosphere == null)
			{
				return 0.0;
			}
			return slot.Occupant.InternalAtmosphere.Temperature.ToDouble();
		case LogicSlotType.Charge:
		case LogicSlotType.ChargeRatio:
			return BatteryCell.GetLogicValue(slot.Occupant, logicSlotType);
		case LogicSlotType.Class:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return (int)slot.Occupant.SlotType;
		case LogicSlotType.SortingClass:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return (int)slot.Occupant.SortingClass;
		case LogicSlotType.PrefabHash:
			if (!slot.Occupant)
			{
				return 0.0;
			}
			return slot.Occupant.PrefabHash;
		case LogicSlotType.FilterType:
			if (!(slot.Occupant is GasFilter gasFilter))
			{
				return 0.0;
			}
			return (double)gasFilter.FilterType;
		case LogicSlotType.TotalSlots:
		{
			if (!slot.Contains<DynamicThing>(out var occupant2))
			{
				return 0.0;
			}
			return occupant2.TotalSlots;
		}
		case LogicSlotType.FreeSlots:
		{
			if (!slot.Contains<DynamicThing>(out var occupant))
			{
				return 0.0;
			}
			return occupant.GetFreeSlotCount();
		}
		default:
			return 0.0;
		}
	}

	public virtual bool IsProviderToDevice(Device device, ref List<long> evaluatedDeviceReferences)
	{
		return false;
	}

	public virtual float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (PowerCable == null || PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		return 0f;
	}

	public virtual float GetUsedPower(CableNetwork cableNetwork)
	{
		if (PowerCable == null || PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		if (!OnOff || !base.IsStructureCompleted)
		{
			return 0f;
		}
		return UsedPower;
	}

	public virtual void UsePower(CableNetwork cableNetwork, float powerUsed)
	{
	}

	public virtual void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
	}

	public virtual bool AllowSetPower(CableNetwork cableNetwork)
	{
		return PowerCableNetwork == cableNetwork;
	}

	public bool OnControlLost()
	{
		if ((bool)this && !GameManager.RunSimulation)
		{
			return true;
		}
		OnServer.Interact(this, InteractableType.Lock, 0);
		OnServer.Interact(base.InteractOnOff, 0);
		return true;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		foreach (Connection openEnd in OpenEnds)
		{
			if (!(hitCollider != openEnd.Collider) && !(hitCollider == null))
			{
				return new PassiveTooltip(true).Populate(openEnd);
			}
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override void OnRenamed()
	{
		base.OnRenamed();
		if (DataCableNetwork != null)
		{
			DataCableNetwork.OnDeviceNameChanged(this);
		}
	}

	public void FindDataCable()
	{
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected<Cable>(NetworkType.Data, span, ref count);
		DataCables = new Cable[count];
		if (count == 0)
		{
			DataCable = null;
			return;
		}
		Span<SmallCellRef> span2 = span;
		Span<SmallCellRef> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			DataCables[i] = span3[i].Get<Cable>();
		}
		DataCable = DataCables[0];
	}

	private void FindPowerCable()
	{
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected<Cable>(NetworkType.Power, span, ref count);
		PowerCables = new Cable[count];
		if (count == 0)
		{
			PowerCable = null;
			AssessPower(null, OnOff);
			return;
		}
		Span<SmallCellRef> span2 = span;
		Span<SmallCellRef> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			PowerCables[i] = span3[i].Get<Cable>();
		}
		PowerCable = PowerCables[0];
	}

	public void InitializeDataConnection()
	{
		FindDataCable();
		FindPowerCable();
		DataCableNetwork?.DirtyPowerAndDataDeviceLists();
		PowerCableNetwork?.DirtyPowerAndDataDeviceLists();
	}

	public override CanConstructInfo CanConstruct()
	{
		Span<SmallCellRef> buf = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected<Device>(buf, ref count);
		for (int num = count - 1; num >= 0; num--)
		{
			ElevatorShaft found2;
			if (buf[num].TryGet<INetworkedPipe>(out var found) && !found.ProhibitConnection(this))
			{
				count--;
			}
			else if (this is ElevatorShaft && buf[num].TryGet<ElevatorShaft>(out found2))
			{
				count--;
			}
		}
		if (count > 0 && buf[0].TryGet<Device>(out var found3))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedByAdjacentDevice.AsString(found3.DisplayName));
		}
		if (count > 0)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedByUnknownDevice.DisplayString);
		}
		return base.CanConstruct();
	}

	protected virtual void SetPower(CableNetwork cableNetwork, bool hasPower)
	{
		if (Powered != hasPower && GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractPowered, hasPower ? 1 : 0);
		}
	}

	public async UniTaskVoid SetPowerFromThread(CableNetwork cableNetwork, bool hasPower)
	{
		await UniTask.SwitchToMainThread();
		SetPower(cableNetwork, hasPower);
	}

	protected virtual void AssessPower(CableNetwork cableNetwork, bool isOn)
	{
		if (cableNetwork == null || !isOn)
		{
			if (Powered)
			{
				SetPower(cableNetwork, hasPower: false);
			}
			return;
		}
		float usedPower = GetUsedPower(cableNetwork);
		if (usedPower <= 0f)
		{
			return;
		}
		if (usedPower > cableNetwork.EstimatedRemainingLoad)
		{
			cableNetwork.DuringTickLoad += Mathf.Min(usedPower, cableNetwork.EstimatedRemainingLoad);
			if (Powered)
			{
				SetPower(cableNetwork, hasPower: false);
			}
		}
		else
		{
			cableNetwork.DuringTickLoad += usedPower;
			if (!Powered)
			{
				SetPower(cableNetwork, hasPower: true);
			}
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.GameState == GameState.Running)
		{
			if (GameManager.RunSimulation && interactable.Action == InteractableType.OnOff && HasPowerState)
			{
				AssessPower(PowerCable ? PowerCable.CableNetwork : null, interactable.State == 1);
			}
			_ = IsOperable;
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (_infoScreen != null)
		{
			_infoScreen.RefreshState(this);
		}
	}

	public virtual void OnLinkWithBoard(Motherboard motherboard)
	{
	}

	public virtual void OnUnlinkWithBoard(Motherboard motherboard)
	{
	}

	public virtual void OnAddChuteNetwork(ChuteNetwork newNetwork)
	{
	}

	public virtual void OnRemoveChuteNetwork(ChuteNetwork oldNetwork)
	{
	}

	public virtual void OnAddPipeNetwork(PipeNetwork newNetwork)
	{
	}

	public virtual void OnRemovePipeNetwork(PipeNetwork oldNetwork)
	{
	}

	public virtual void OnAddCableNetwork(CableNetwork newNetwork)
	{
		CheckConnections();
	}

	public virtual void OnRemoveCableNetwork(CableNetwork oldNetwork)
	{
		if (Powered)
		{
			AssessPower(PowerCable ? PowerCable.CableNetwork : null, OnOff);
		}
		CheckConnections();
	}

	public virtual void OnDeviceConnectToNetwork(Device device)
	{
		if (OnDeviceConnectToNetworkEvent != null)
		{
			OnDeviceConnectToNetworkEvent();
		}
	}

	public virtual void OnNetworkedDeviceNameChanged(Device device)
	{
	}

	public virtual void OnNetworkedRefresh(Device device)
	{
	}

	public virtual void OnInputTriggerEnter(DynamicThing dynamicThing, MachineInputTrigger trigger)
	{
	}

	public virtual void OnInputTriggerExit(DynamicThing dynamicThing, MachineInputTrigger trigger)
	{
	}

	public virtual void OnDeviceDisconnectFromNetwork(Device device)
	{
	}

	public virtual void InitializeDevice()
	{
		Span<SmallCellRef> span = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected<Cable>(span, ref count);
		Span<SmallCellRef> span2 = span;
		Span<SmallCellRef> span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			SmallCellRef smallCellRef = span3[i];
			if (smallCellRef.TryGet<Cable>(out var found))
			{
				found.CableNetwork.AddDevice(found, this);
			}
		}
		count = 0;
		FillConnected<INetworkedPipe>(span, ref count);
		span2 = span;
		span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			SmallCellRef smallCellRef2 = span3[i];
			if (smallCellRef2.TryGet<INetworkedPipe>(out var found2))
			{
				found2.PipeNetwork.AddDevice(found2, this);
			}
		}
		count = 0;
		FillConnected<INetworkedChute>(span, ref count);
		span2 = span;
		span3 = span2.Slice(0, count);
		for (int i = 0; i < span3.Length; i++)
		{
			SmallCellRef smallCellRef3 = span3[i];
			if (smallCellRef3.TryGet<INetworkedChute>(out var found3))
			{
				found3.ChuteNetwork.AddDevice(found3, this);
			}
		}
		CheckConnections();
	}

	public virtual void OnPowerTick()
	{
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (OnOff && Powered && (object)PowerCable != null && EnergyToHeatRatio > 0f && base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid).IsAboveArmstrong())
		{
			Atmosphere atmosphere = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			float usedPower = GetUsedPower(PowerCable.CableNetwork);
			if (usedPower > 0f)
			{
				atmosphere.GasMixture.AddEnergy(new MoleEnergy(usedPower * EnergyToHeatRatio));
			}
		}
	}

	public override void OnRegistered(Cell cell)
	{
		_centerPosition = base.ThingTransformPosition + ThingTransform.rotation * Bounds.center;
		base.OnRegistered(cell);
		LocalGrid = base.GridController.WorldToLocalGrid(_centerPosition);
		AtmosphericsManager.Instance.Register(this);
		AllDevices.Add(this);
		if (GameManager.GameState != GameState.Loading)
		{
			InitializeDataConnection();
			InitializeDevice();
		}
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		if (GameManager.GameState != GameState.None)
		{
			int count = ConnectedCableNetworks.Count;
			while (count-- > 0)
			{
				ConnectedCableNetworks[count].RemoveDevice(this);
			}
			int count2 = ConnectedPipeNetworks.Count;
			while (count2-- > 0)
			{
				ConnectedPipeNetworks[count2].RemoveDevice(this);
			}
			int count3 = ConnectedChuteNetworks.Count;
			while (count3-- > 0)
			{
				ConnectedChuteNetworks[count3].RemoveDevice(this);
			}
			AtmosphericsManager.Instance.Deregister(this);
			AllDevices.Remove(this);
		}
	}

	public override void OnNeighborPlaced(SmallGrid neighbor)
	{
		base.OnNeighborPlaced(neighbor);
		if (GameManager.GameState != GameState.Loading)
		{
			InitializeDataConnection();
		}
	}

	public override void OnNeighborRemoved(SmallGrid neighbor)
	{
		base.OnNeighborRemoved(neighbor);
		InitializeDataConnection();
	}

	public static void InitAllDevices()
	{
		AllDevices.ForEach(InitializeDeviceAction);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return null;
		}
		Labeller labeller = sourceItem as Labeller;
		if ((bool)labeller)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = ActionStrings.Rename
			};
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
				return delayedActionInstance;
			}
			labeller.Rename(this);
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}

	public override bool OnAddToPool(object densePool, int slot)
	{
		if (_deviceDensePool.CanAddToPool(densePool))
		{
			return _deviceDensePool.AddToPool(densePool, slot);
		}
		if (_solarRadiatorPool.CanAddToPool(densePool))
		{
			return _solarRadiatorPool.AddToPool(densePool, slot);
		}
		if (_circuitHolderPool.CanAddToPool(densePool))
		{
			return _circuitHolderPool.AddToPool(densePool, slot);
		}
		return base.OnAddToPool(densePool, slot);
	}

	public override void OnRemoveFromPool(object densePool)
	{
		base.OnRemoveFromPool(densePool);
		_deviceDensePool.OnRemovedFrom(densePool);
		_solarRadiatorPool.OnRemovedFrom(densePool);
		_circuitHolderPool.OnRemovedFrom(densePool);
	}
}
