using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Trading;

namespace Assets.Scripts.GridSystem;

[XmlRoot]
public class Room : IEvaluable
{
	public static List<Room> AllRooms = new List<Room>();

	public List<WorldGrid> Grids = new List<WorldGrid>();

	public long RoomId;

	[XmlIgnore]
	public bool IsDeletionCandidate;

	public bool WillSave = true;

	public GasMixture FrozenContents = GasMixtureHelper.Create();

	public object FrozenContentsLock = new object();

	[XmlIgnore]
	public List<DynamicThing> DynamicContents = new List<DynamicThing>();

	[XmlIgnore]
	private readonly object _lockDynamicThing = new object();

	[XmlIgnore]
	public RoomType RoomType;

	public GasMixture GasMixture;

	public GasMixture AverageGasMixture;

	public VolumeLitres Volume;

	public PressurekPa Pressure;

	public TemperatureKelvin Temperature;

	public Room()
	{
	}

	private Room(RoomData dataRoom)
	{
		RoomId = dataRoom.RoomId;
		WillSave = dataRoom.WillSave;
	}

	public bool IsValid()
	{
		if (Grids != null && Grids.Count > 0)
		{
			return RoomController.World.GetRoom(Grids[0]) == this;
		}
		return false;
	}

	public void CacheRoomData()
	{
		GasMixture gasMixture = GasMixtureHelper.Create();
		VolumeLitres zero = VolumeLitres.Zero;
		for (int num = Grids.Count - 1; num >= 0; num--)
		{
			Atmosphere atmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(Grids[num]);
			gasMixture.Add(atmosphere.GasMixture);
			zero += atmosphere.Volume;
		}
		Pressure = IdealGas.Pressure(gasMixture.GetTotalMolesGasses, gasMixture.Temperature, zero);
		Temperature = gasMixture.Temperature;
		Volume = zero;
		GasMixture = new GasMixture(gasMixture);
		gasMixture.Divide((Volume / Chemistry.GridVolume).ToFloat());
		AverageGasMixture = new GasMixture(gasMixture);
	}

	public static void RunCacheRoomDataJobs()
	{
		AtmosphericsWorker.ClearScores();
		int count = AllRooms.Count;
		while (count-- > 0)
		{
			Room room = AllRooms[count];
			if (room != null && !room.IsDeletionCandidate)
			{
				AtmosphericsWorker.Assign(room);
			}
		}
		AtmosphericsWorker.Execute(AtmosphericsWorker.Job.CacheRoomData);
		AtmosphericsWorker.WaitForCompletion();
	}

	public PressurekPa PartialPressure(Chemistry.GasType gasType)
	{
		return gasType switch
		{
			Chemistry.GasType.Undefined => PressurekPa.Zero, 
			Chemistry.GasType.Oxygen => IdealGas.Pressure(GasMixture.Oxygen.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.Nitrogen => IdealGas.Pressure(GasMixture.Nitrogen.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.CarbonDioxide => IdealGas.Pressure(GasMixture.CarbonDioxide.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.Methane => IdealGas.Pressure(GasMixture.Methane.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.Pollutant => IdealGas.Pressure(GasMixture.Pollutant.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.Water => PressurekPa.Zero, 
			Chemistry.GasType.PollutedWater => PressurekPa.Zero, 
			Chemistry.GasType.NitrousOxide => IdealGas.Pressure(GasMixture.NitrousOxide.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.LiquidNitrogen => PressurekPa.Zero, 
			Chemistry.GasType.LiquidOxygen => PressurekPa.Zero, 
			Chemistry.GasType.LiquidMethane => PressurekPa.Zero, 
			Chemistry.GasType.Steam => IdealGas.Pressure(GasMixture.Steam.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.Hydrogen => IdealGas.Pressure(GasMixture.Hydrogen.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.LiquidHydrogen => PressurekPa.Zero, 
			Chemistry.GasType.LiquidCarbonDioxide => PressurekPa.Zero, 
			Chemistry.GasType.LiquidPollutant => PressurekPa.Zero, 
			Chemistry.GasType.LiquidNitrousOxide => PressurekPa.Zero, 
			Chemistry.GasType.Hydrazine => IdealGas.Pressure(GasMixture.Hydrazine.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.LiquidHydrazine => PressurekPa.Zero, 
			Chemistry.GasType.LiquidAlcohol => PressurekPa.Zero, 
			Chemistry.GasType.Helium => IdealGas.Pressure(GasMixture.Helium.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.LiquidSodiumChloride => PressurekPa.Zero, 
			Chemistry.GasType.Silanol => IdealGas.Pressure(GasMixture.Silanol.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.LiquidSilanol => PressurekPa.Zero, 
			Chemistry.GasType.HydrochloricAcid => IdealGas.Pressure(GasMixture.HydrochloricAcid.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.LiquidHydrochloricAcid => PressurekPa.Zero, 
			Chemistry.GasType.Ozone => IdealGas.Pressure(GasMixture.Ozone.Quantity, GasMixture.Temperature, Volume), 
			Chemistry.GasType.LiquidOzone => PressurekPa.Zero, 
			_ => throw new ArgumentOutOfRangeException("gasType", gasType, null), 
		};
	}

	public bool AddGrid(WorldGrid grid)
	{
		if (Grids.Contains(grid))
		{
			return false;
		}
		Grids.Add(grid);
		RoomController.World.AddOrUpdateRoomLookup(grid, this);
		Atmosphere atmosphereLocal = GridController.World.AtmosphericsController.GetAtmosphereLocal(grid);
		if (atmosphereLocal != null)
		{
			atmosphereLocal.Room = this;
		}
		RoomManager.EvaluateRoomTypeRules(this);
		return true;
	}

	public void RemoveGrid(WorldGrid grid)
	{
		if (Grids.Contains(grid))
		{
			Grids.Remove(grid);
			RoomController.World.RemoveRoomLookup(grid);
			Atmosphere atmosphereLocal = GridController.World.AtmosphericsController.GetAtmosphereLocal(grid);
			if (atmosphereLocal != null)
			{
				atmosphereLocal.Room = null;
			}
		}
		RoomManager.EvaluateRoomTypeRules(this);
	}

	public void Register(DynamicThing dynamicThing)
	{
		lock (_lockDynamicThing)
		{
			DynamicContents.Add(dynamicThing);
		}
	}

	public void Deregister(DynamicThing dynamicThing)
	{
		lock (_lockDynamicThing)
		{
			DynamicContents.Remove(dynamicThing);
		}
	}

	public object GetDynamicThingLock()
	{
		return _lockDynamicThing;
	}

	public static List<RoomData> SerializeSave()
	{
		List<RoomData> list = new List<RoomData>();
		foreach (Room allRoom in AllRooms)
		{
			if (allRoom != null && allRoom.IsValid() && allRoom.WillSave)
			{
				list.Add(new RoomData(allRoom));
			}
		}
		return list;
	}

	public static List<Room> DeserializeSave(List<RoomData> dataRooms)
	{
		List<Room> list = new List<Room>();
		HashSet<WorldGrid> hashSet = new HashSet<WorldGrid>();
		List<Grid3> list2 = new List<Grid3>();
		foreach (RoomData dataRoom in dataRooms)
		{
			Room room = new Room(dataRoom);
			bool flag = false;
			foreach (Grid3 grid in dataRoom.Grids)
			{
				WorldGrid item = new WorldGrid(grid);
				if (hashSet.Contains(item))
				{
					flag = true;
					list2.Add((room.Grids.Count > 0) ? room.Grids[0].Value : item.Value);
					break;
				}
				hashSet.Add(item);
				room.Grids.Add(item);
			}
			if (!flag)
			{
				list.Add(room);
			}
			RoomManager.EvaluateRoomTypeRules(room);
		}
		if (list2.Count > 0)
		{
			ConsoleWindow.PrintAction("Bad room data found in save - if any suspicious room behaviour is found please run the 'regeneraterooms' command.");
		}
		return list;
	}
}
