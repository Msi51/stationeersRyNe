using System;
using System.IO;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Serialization;
using Objects.Rockets;
using Objects.Rockets.Mining;
using Objects.Rockets.Scanning;

namespace Assets.Scripts;

public class Network
{
	public const int JOIN_BUFFER_SIZE = 67108864;

	public const int FRAGMENT_BUFFER_SIZE = 16777216;

	private const byte TYPE_BYTE = 0;

	private const byte TYPE_UINT16 = 1;

	private const byte TYPE_UINT32 = 2;

	private const byte TYPE_INT64 = 3;

	private const byte TYPE_FP16 = 4;

	private const byte TYPE_FP32 = 5;

	private const byte TYPE_FP64 = 6;

	private const byte TYPE_INT32 = 7;

	private const byte TYPE_SBYTE = 8;

	private const byte TYPE_RATIO_CLAMP01 = 9;

	private const byte TYPE_UINT64 = 10;

	public static void WritePackedId(RocketBinaryWriter writer, IReferencable referencable)
	{
		WritePackedId(writer, referencable?.ReferenceId ?? 0);
	}

	public static void ReadPackedId(RocketBinaryReader reader, out long referenceId)
	{
		byte b = reader.ReadByte();
		referenceId = b switch
		{
			0 => reader.ReadByte(), 
			1 => reader.ReadUInt16(), 
			2 => reader.ReadUInt32(), 
			3 => reader.ReadInt64(), 
			_ => throw new InvalidDataException($"Unknown type indicator: {b}"), 
		};
	}

	public static void WriteDateTime(RocketBinaryWriter writer, DateTime dateTime)
	{
		writer.WriteInt64(dateTime.Ticks);
	}

	public static void ReadDateTime(RocketBinaryReader reader, out DateTime dateTime)
	{
		long ticks = reader.ReadInt64();
		dateTime = new DateTime(ticks);
	}

	public static void WriteLogicValue(RocketBinaryWriter writer, LogicType logicType, double value)
	{
		writer.WriteUInt16((ushort)logicType);
		byte dataTypeForNetworkSend = GetDataTypeForNetworkSend(logicType);
		switch (dataTypeForNetworkSend)
		{
		case 0:
			writer.WriteByte((byte)value);
			break;
		case 1:
			writer.WriteUInt16((ushort)value);
			break;
		case 2:
			writer.WriteUInt32((uint)value);
			break;
		case 3:
			writer.WriteInt64((long)value);
			break;
		case 4:
			writer.WriteFloatHalf((float)value);
			break;
		case 5:
			writer.WriteSingle((float)value);
			break;
		case 6:
			writer.WriteDouble(value);
			break;
		case 7:
			writer.WriteInt32((int)value);
			break;
		case 8:
			writer.WriteSByte((sbyte)value);
			break;
		case 9:
			writer.WriteUInt16((ushort)(Math.Clamp(value, 0.0, 1.0) * 10000.0));
			break;
		case 10:
			writer.WriteUInt64((ulong)value);
			break;
		default:
			throw new InvalidDataException($"Unknown type indicator: {dataTypeForNetworkSend}");
		}
	}

	public static void ReadLogicValue(RocketBinaryReader reader, out LogicType logicType, out double value)
	{
		logicType = (LogicType)reader.ReadUInt16();
		byte dataTypeForNetworkSend = GetDataTypeForNetworkSend(logicType);
		switch (dataTypeForNetworkSend)
		{
		case 0:
			value = (int)reader.ReadByte();
			break;
		case 1:
			value = (int)reader.ReadUInt16();
			break;
		case 2:
			value = reader.ReadUInt32();
			break;
		case 3:
			value = reader.ReadInt64();
			break;
		case 4:
			value = reader.ReadFloatHalf();
			break;
		case 5:
			value = reader.ReadSingle();
			break;
		case 6:
			value = reader.ReadDouble();
			break;
		case 7:
			value = reader.ReadInt32();
			break;
		case 8:
			value = reader.ReadSByte();
			break;
		case 9:
			value = (float)(int)reader.ReadUInt16() / 10000f;
			break;
		case 10:
			value = reader.ReadUInt64();
			break;
		default:
			throw new InvalidDataException($"Unknown type indicator: {dataTypeForNetworkSend}");
		}
	}

	public static void WriteIndex<T>(RocketBinaryWriter writer, out T count, out int bufferIndex) where T : struct, IComparable, IFormattable, IConvertible, IComparable<T>, IEquatable<T>
	{
		bufferIndex = writer.Position;
		Type typeFromHandle = typeof(T);
		if (typeFromHandle == typeof(byte))
		{
			writer.WriteByte(0);
		}
		else if (typeFromHandle == typeof(ushort))
		{
			writer.WriteUInt16(0);
		}
		else
		{
			if (!(typeFromHandle == typeof(uint)))
			{
				throw new NotImplementedException();
			}
			writer.WriteUInt32(0u);
		}
		count = default(T);
	}

	public static void WriteIndex<T>(RocketBinaryWriter writer, T number, int bufferIndex) where T : struct, IComparable, IFormattable, IConvertible, IComparable<T>, IEquatable<T>
	{
		writer.Position = bufferIndex;
		Type typeFromHandle = typeof(T);
		if (typeFromHandle == typeof(byte))
		{
			writer.WriteByte(__refvalue(__makeref(number), byte));
		}
		else if (typeFromHandle == typeof(ushort))
		{
			writer.WriteUInt16(__refvalue(__makeref(number), ushort));
		}
		else
		{
			if (!(typeFromHandle == typeof(uint)))
			{
				throw new NotImplementedException();
			}
			writer.WriteUInt32(__refvalue(__makeref(number), uint));
		}
		writer.Seek(0, SeekOrigin.End);
	}

	public static void WritePackedId(RocketBinaryWriter writer, long referenceId)
	{
		if (referenceId <= 65535)
		{
			if (referenceId <= 255)
			{
				writer.WriteByte(0);
				writer.WriteByte((byte)referenceId);
			}
			else
			{
				writer.WriteByte(1);
				writer.WriteUInt16((ushort)referenceId);
			}
		}
		else if (referenceId <= uint.MaxValue)
		{
			writer.WriteByte(2);
			writer.WriteUInt32((uint)referenceId);
		}
		else
		{
			writer.WriteByte(3);
			writer.WriteInt64(referenceId);
		}
	}

	public static byte GetDataTypeForNetworkSend(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.None:
		case LogicType.Power:
		case LogicType.Open:
		case LogicType.Mode:
		case LogicType.Error:
		case LogicType.Lock:
			return 0;
		case LogicType.Pressure:
		case LogicType.Temperature:
		case LogicType.PressureExternal:
		case LogicType.PressureInternal:
			return 5;
		case LogicType.Activate:
			return 0;
		case LogicType.Charge:
			return 5;
		case LogicType.OrbitPeriod:
		case LogicType.SemiMajorAxis:
			return 6;
		case LogicType.AlignmentError:
		case LogicType.Inclination:
		case LogicType.Eccentricity:
			return 5;
		case LogicType.Setting:
			return 6;
		case LogicType.Reagents:
			return 5;
		case LogicType.RatioOxygen:
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioNitrogen:
		case LogicType.RatioPollutant:
		case LogicType.RatioMethane:
		case LogicType.RatioWater:
			return 9;
		case LogicType.HealthDamage:
		case LogicType.StunDamage:
			return 5;
		case LogicType.Horizontal:
		case LogicType.Vertical:
		case LogicType.SolarAngle:
		case LogicType.Maximum:
			return 5;
		case LogicType.Ratio:
			return 9;
		case LogicType.PowerPotential:
		case LogicType.PowerActual:
			return 5;
		case LogicType.Quantity:
			return 5;
		case LogicType.On:
			return 0;
		case LogicType.ImportQuantity:
		case LogicType.Index:
			return 2;
		case LogicType.ExportQuantity:
			return 2;
		case LogicType.RequiredPower:
			return 5;
		case LogicType.HorizontalRatio:
		case LogicType.VerticalRatio:
			return 9;
		case LogicType.PowerRequired:
			return 5;
		case LogicType.Idle:
			return 0;
		case LogicType.Color:
			return 0;
		case LogicType.ElevatorSpeed:
			return 5;
		case LogicType.ElevatorLevel:
			return 0;
		case LogicType.RecipeHash:
		case LogicType.ExportSlotHash:
		case LogicType.ImportSlotHash:
			return 7;
		case LogicType.RequestHash:
			return 7;
		case LogicType.CompletionRatio:
			return 9;
		case LogicType.ClearMemory:
			return 0;
		case LogicType.ExportCount:
		case LogicType.ImportCount:
			return 2;
		case LogicType.PowerGeneration:
			return 5;
		case LogicType.TotalMoles:
			return 5;
		case LogicType.Volume:
			return 5;
		case LogicType.Plant:
			return 0;
		case LogicType.Harvest:
			return 0;
		case LogicType.Output:
			return 8;
		case LogicType.PressureSetting:
		case LogicType.TemperatureSetting:
		case LogicType.TemperatureExternal:
			return 5;
		case LogicType.Filtration:
		case LogicType.AirRelease:
			return 0;
		case LogicType.PositionX:
		case LogicType.PositionY:
		case LogicType.PositionZ:
			return 5;
		case LogicType.VelocityMagnitude:
		case LogicType.VelocityRelativeX:
		case LogicType.VelocityRelativeY:
		case LogicType.VelocityRelativeZ:
			return 4;
		case LogicType.RatioNitrousOxide:
			return 9;
		case LogicType.PrefabHash:
		case LogicType.CelestialHash:
		case LogicType.NameHash:
		case LogicType.NextWeatherHash:
			return 7;
		case LogicType.ForceWrite:
			return 0;
		case LogicType.SignalStrength:
			return 5;
		case LogicType.SignalID:
			return 7;
		case LogicType.TargetX:
		case LogicType.TargetY:
		case LogicType.TargetZ:
			return 5;
		case LogicType.SettingInput:
		case LogicType.SettingOutput:
			return 5;
		case LogicType.CurrentResearchPodType:
			return 0;
		case LogicType.ManualResearchRequiredPod:
			return 0;
		case LogicType.MineablesInVicinity:
		case LogicType.MineablesInQueue:
			return 1;
		case LogicType.NextWeatherEventTime:
			return 5;
		case LogicType.Combustion:
			return 0;
		case LogicType.Fuel:
		case LogicType.ReturnFuelCost:
			return 5;
		case LogicType.CollectableGoods:
			return 0;
		case LogicType.Time:
			return 5;
		case LogicType.Bpm:
			return 5;
		case LogicType.EnvironmentEfficiency:
		case LogicType.WorkingGasEfficiency:
			return 4;
		case LogicType.PressureInput:
		case LogicType.TemperatureInput:
			return 5;
		case LogicType.RatioOxygenInput:
		case LogicType.RatioCarbonDioxideInput:
		case LogicType.RatioNitrogenInput:
		case LogicType.RatioPollutantInput:
		case LogicType.RatioMethaneInput:
		case LogicType.RatioWaterInput:
		case LogicType.RatioNitrousOxideInput:
		case LogicType.RatioHydrogenInput:
		case LogicType.RatioLiquidHydrogenInput:
		case LogicType.RatioPollutedWaterInput:
		case LogicType.RatioHydrazineInput:
		case LogicType.RatioLiquidHydrazineInput:
		case LogicType.RatioLiquidAlcoholInput:
		case LogicType.RatioHeliumInput:
		case LogicType.RatioLiquidSodiumChlorideInput:
		case LogicType.RatioSilanolInput:
		case LogicType.RatioLiquidSilanolInput:
		case LogicType.RatioHydrochloricAcidInput:
		case LogicType.RatioLiquidHydrochloricAcidInput:
		case LogicType.RatioOzoneInput:
		case LogicType.RatioLiquidOzoneInput:
			return 9;
		case LogicType.TotalMolesInput:
		case LogicType.PressureInput2:
		case LogicType.TemperatureInput2:
			return 5;
		case LogicType.RatioOxygenInput2:
		case LogicType.RatioCarbonDioxideInput2:
		case LogicType.RatioNitrogenInput2:
		case LogicType.RatioPollutantInput2:
		case LogicType.RatioMethaneInput2:
		case LogicType.RatioWaterInput2:
		case LogicType.RatioNitrousOxideInput2:
		case LogicType.RatioHydrogenInput2:
		case LogicType.RatioLiquidHydrogenInput2:
		case LogicType.RatioPollutedWaterInput2:
		case LogicType.RatioHydrazineInput2:
		case LogicType.RatioLiquidHydrazineInput2:
		case LogicType.RatioLiquidAlcoholInput2:
		case LogicType.RatioHeliumInput2:
		case LogicType.RatioLiquidSodiumChlorideInput2:
		case LogicType.RatioSilanolInput2:
		case LogicType.RatioLiquidSilanolInput2:
		case LogicType.RatioHydrochloricAcidInput2:
		case LogicType.RatioLiquidHydrochloricAcidInput2:
		case LogicType.RatioOzoneInput2:
		case LogicType.RatioLiquidOzoneInput2:
			return 9;
		case LogicType.TotalMolesInput2:
		case LogicType.PressureOutput:
		case LogicType.TemperatureOutput:
			return 5;
		case LogicType.RatioOxygenOutput:
		case LogicType.RatioCarbonDioxideOutput:
		case LogicType.RatioNitrogenOutput:
		case LogicType.RatioPollutantOutput:
		case LogicType.RatioMethaneOutput:
		case LogicType.RatioWaterOutput:
		case LogicType.RatioNitrousOxideOutput:
		case LogicType.RatioHydrogenOutput:
		case LogicType.RatioLiquidHydrogenOutput:
		case LogicType.RatioPollutedWaterOutput:
		case LogicType.RatioHydrazineOutput:
		case LogicType.RatioLiquidHydrazineOutput:
		case LogicType.RatioLiquidAlcoholOutput:
		case LogicType.RatioHeliumOutput:
		case LogicType.RatioLiquidSodiumChlorideOutput:
		case LogicType.RatioSilanolOutput:
		case LogicType.RatioLiquidSilanolOutput:
		case LogicType.RatioHydrochloricAcidOutput:
		case LogicType.RatioLiquidHydrochloricAcidOutput:
		case LogicType.RatioOzoneOutput:
		case LogicType.RatioLiquidOzoneOutput:
			return 9;
		case LogicType.TotalMolesOutput:
		case LogicType.PressureOutput2:
		case LogicType.TemperatureOutput2:
			return 5;
		case LogicType.RatioOxygenOutput2:
		case LogicType.RatioCarbonDioxideOutput2:
		case LogicType.RatioNitrogenOutput2:
		case LogicType.RatioPollutantOutput2:
		case LogicType.RatioMethaneOutput2:
		case LogicType.RatioWaterOutput2:
		case LogicType.RatioNitrousOxideOutput2:
		case LogicType.RatioHydrogenOutput2:
		case LogicType.RatioLiquidHydrogenOutput2:
		case LogicType.RatioPollutedWaterOutput2:
		case LogicType.RatioHydrazineOutput2:
		case LogicType.RatioLiquidHydrazineOutput2:
		case LogicType.RatioLiquidAlcoholOutput2:
		case LogicType.RatioHeliumOutput2:
		case LogicType.RatioLiquidSodiumChlorideOutput2:
		case LogicType.RatioSilanolOutput2:
		case LogicType.RatioLiquidSilanolOutput2:
		case LogicType.RatioHydrochloricAcidOutput2:
		case LogicType.RatioLiquidHydrochloricAcidOutput2:
		case LogicType.RatioOzoneOutput2:
		case LogicType.RatioLiquidOzoneOutput2:
			return 9;
		case LogicType.TotalMolesOutput2:
			return 5;
		case LogicType.CombustionInput:
		case LogicType.CombustionInput2:
		case LogicType.CombustionOutput:
		case LogicType.CombustionOutput2:
			return 0;
		case LogicType.OperationalTemperatureEfficiency:
		case LogicType.TemperatureDifferentialEfficiency:
		case LogicType.PressureEfficiency:
			return 4;
		case LogicType.CombustionLimiter:
		case LogicType.Throttle:
			return 5;
		case LogicType.Rpm:
		case LogicType.StackSize:
			return 1;
		case LogicType.Stress:
			return 5;
		case LogicType.InterrogationProgress:
			return 9;
		case LogicType.TargetPadIndex:
			return 0;
		case LogicType.SizeX:
		case LogicType.SizeY:
		case LogicType.SizeZ:
			return 5;
		case LogicType.MinimumWattsToContact:
		case LogicType.WattsReachingContact:
			return 5;
		case LogicType.Channel0:
		case LogicType.Channel1:
		case LogicType.Channel2:
		case LogicType.Channel3:
		case LogicType.Channel4:
		case LogicType.Channel5:
		case LogicType.Channel6:
		case LogicType.Channel7:
			return 6;
		case LogicType.LineNumber:
			return 1;
		case LogicType.Flush:
			return 0;
		case LogicType.SoundAlert:
			return 0;
		case LogicType.SolarIrradiance:
			return 5;
		case LogicType.RatioLiquidNitrogen:
		case LogicType.RatioLiquidNitrogenInput:
		case LogicType.RatioLiquidNitrogenInput2:
		case LogicType.RatioLiquidNitrogenOutput:
		case LogicType.RatioLiquidNitrogenOutput2:
			return 9;
		case LogicType.VolumeOfLiquid:
			return 5;
		case LogicType.RatioLiquidOxygen:
		case LogicType.RatioLiquidOxygenInput:
		case LogicType.RatioLiquidOxygenInput2:
		case LogicType.RatioLiquidOxygenOutput:
		case LogicType.RatioLiquidOxygenOutput2:
		case LogicType.RatioLiquidMethane:
		case LogicType.RatioLiquidMethaneInput:
		case LogicType.RatioLiquidMethaneInput2:
		case LogicType.RatioLiquidMethaneOutput:
		case LogicType.RatioLiquidMethaneOutput2:
		case LogicType.RatioSteam:
		case LogicType.RatioSteamInput:
		case LogicType.RatioSteamInput2:
		case LogicType.RatioSteamOutput:
		case LogicType.RatioSteamOutput2:
			return 9;
		case LogicType.ContactTypeId:
			return 7;
		case LogicType.RatioLiquidCarbonDioxide:
		case LogicType.RatioLiquidCarbonDioxideInput:
		case LogicType.RatioLiquidCarbonDioxideInput2:
		case LogicType.RatioLiquidCarbonDioxideOutput:
		case LogicType.RatioLiquidCarbonDioxideOutput2:
		case LogicType.RatioLiquidPollutant:
		case LogicType.RatioLiquidPollutantInput:
		case LogicType.RatioLiquidPollutantInput2:
		case LogicType.RatioLiquidPollutantOutput:
		case LogicType.RatioLiquidPollutantOutput2:
		case LogicType.RatioLiquidNitrousOxide:
		case LogicType.RatioLiquidNitrousOxideInput:
		case LogicType.RatioLiquidNitrousOxideInput2:
		case LogicType.RatioLiquidNitrousOxideOutput:
		case LogicType.RatioLiquidNitrousOxideOutput2:
			return 9;
		case LogicType.Progress:
			return 9;
		case LogicType.DestinationCode:
			return 10;
		case LogicType.Acceleration:
			return 5;
		case LogicType.ReferenceId:
			return 10;
		case LogicType.AutoShutOff:
		case LogicType.TargetSlotIndex:
			return 0;
		case LogicType.Mass:
		case LogicType.DryMass:
		case LogicType.Thrust:
		case LogicType.Weight:
		case LogicType.ThrustToWeight:
			return 5;
		case LogicType.TimeToDestination:
		case LogicType.BurnTimeRemaining:
			return 5;
		case LogicType.AutoLand:
			return 0;
		case LogicType.ForwardX:
		case LogicType.ForwardY:
		case LogicType.ForwardZ:
		case LogicType.Orientation:
			return 5;
		case LogicType.VelocityX:
		case LogicType.VelocityY:
		case LogicType.VelocityZ:
			return 5;
		case LogicType.PassedMoles:
		case LogicType.ExhaustVelocity:
			return 5;
		case LogicType.RatioHydrogen:
		case LogicType.RatioLiquidHydrogen:
		case LogicType.RatioPollutedWater:
			return 9;
		case LogicType.BestContactFilter:
			return 7;
		case LogicType.ContactSlotIndex:
			return 7;
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
			return 9;
		default:
			return 6;
		case LogicType.ImportSlotOccupant:
		case LogicType.ExportSlotOccupant:
		case LogicType.PlantHealth1:
		case LogicType.PlantHealth2:
		case LogicType.PlantHealth3:
		case LogicType.PlantHealth4:
		case LogicType.PlantGrowth1:
		case LogicType.PlantGrowth2:
		case LogicType.PlantGrowth3:
		case LogicType.PlantGrowth4:
		case LogicType.PlantEfficiency1:
		case LogicType.PlantEfficiency2:
		case LogicType.PlantEfficiency3:
		case LogicType.PlantEfficiency4:
		case LogicType.PlantHash1:
		case LogicType.PlantHash2:
		case LogicType.PlantHash3:
		case LogicType.PlantHash4:
			return 6;
		}
	}

	public static void WriteNullable<T>(RocketBinaryWriter writer, T value) where T : INetworkNullable
	{
		bool flag = value != null;
		writer.WriteBoolean(flag);
		if (flag)
		{
			value.Write(writer);
		}
	}

	public static void ReadNullable<T>(RocketBinaryReader reader, ref T value) where T : class, INetworkNullable
	{
		if (!reader.ReadBoolean())
		{
			return;
		}
		Type typeFromHandle = typeof(T);
		if (typeFromHandle == typeof(MineableDeposit))
		{
			value = new MineableDeposit(reader) as T;
			return;
		}
		if (typeFromHandle == typeof(SurveyData))
		{
			value = new SurveyData(reader) as T;
			return;
		}
		if (typeFromHandle == typeof(ChartData))
		{
			value = new ChartData(reader) as T;
			return;
		}
		if (typeFromHandle == typeof(SurfaceScanData))
		{
			value = new SurfaceScanData(reader) as T;
			return;
		}
		if (typeFromHandle == typeof(DiscoverSiteData))
		{
			value = new DiscoverSiteData(reader) as T;
			return;
		}
		if (typeFromHandle == typeof(SpaceMapNodeMineData))
		{
			value = new SpaceMapNodeMineData(reader) as T;
			return;
		}
		if (typeFromHandle == typeof(NodeConnection))
		{
			value = new NodeConnection(reader) as T;
			return;
		}
		if (typeFromHandle == typeof(NodeTransit))
		{
			value = new NodeTransit(reader) as T;
			return;
		}
		throw new NotImplementedException("type " + typeFromHandle.Name + " is not implemented for ReadNullable<T>");
	}

	public static void ReadIndex<T>(RocketBinaryReader reader, out T value)
	{
		Type typeFromHandle = typeof(T);
		if (typeFromHandle == typeof(byte))
		{
			byte b = reader.ReadByte();
			value = __refvalue(__makeref(b), T);
			return;
		}
		if (typeFromHandle == typeof(ushort))
		{
			ushort num = reader.ReadUInt16();
			value = __refvalue(__makeref(num), T);
			return;
		}
		if (typeFromHandle == typeof(uint))
		{
			uint num2 = reader.ReadUInt32();
			value = __refvalue(__makeref(num2), T);
			return;
		}
		throw new NotImplementedException();
	}
}
