using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Cysharp.Threading.Tasks;
using Objects.Rockets;
using Sound;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace Assets.Scripts.Util;

public static class Achievements
{
	public enum Stat : byte
	{
		CelestialTrackingTime,
		TotalCobaltMined,
		TotalPlantsHarvested,
		TotalCreditsFromSales,
		TotalRocketLaunches,
		TotalRocketLandings
	}

	public enum Kind : byte
	{
		AchievementFurnaceApprentice,
		AchievementFurnaceJourneyman,
		AchievementFurnaceMaster,
		AchievementMyNextForm,
		AchievementPowerOverwhelming,
		AchievementItsGonnaBlow,
		AchievementABitWindy,
		AchievementHurryUp,
		AchievementBlastOff,
		AchievementThatllBe350,
		AchievementClearedToLand,
		AchievementYouStillOpen,
		AchievementMarkWatney,
		AchievementStillMarkWatney,
		AchievementMuffinMan,
		AchievementGottaGrowFast,
		AchievementGoodMorning,
		AchievementJetpackCritical,
		AchievementTakeALoadOff,
		AchievementZzzt,
		AchievementTidy,
		Achievement121Gigawatts,
		AchievementAimeeDoesNotUnderstand,
		AchievementOops,
		AchievementWelcomeAboard,
		AchievementMedic,
		AchievementBrutalLegend,
		AchievementSomeHighQualityH2O,
		AchievementSomeAssemblyRequired,
		AchievementStackOverflow,
		AchievementAndYetItMoves,
		AchievementSootHappens,
		AchievementWhyIsItStillMarkWatney,
		AchievementKhajiitHasWares,
		AchievementHaltAndCatchFire,
		AchievementDidYouForgetSomething,
		AchievementWellThatWasQuick,
		AchievementYesItExplodedMark,
		AchievementJupiterAscending,
		AchievementTheCassiniJob,
		AchievementEuropaUniversalis,
		AchievementItsNotMadeOfCheese,
		AchievementGoAtThrottleUp,
		AchievementFrequentFlyer,
		AchievementImStillAlive,
		AchievementJovianWinter,
		AchievementHoustonStillNoProblem,
		AchievementFastAndFurious,
		AchievementComeOnTars,
		AchievementMajorTomToGroundControl,
		AchievementFinallyRealFood
	}

	public class Steam : IAchievementStore
	{
		public static readonly Steam Instance = new Steam();

		private static readonly Dictionary<Kind, string> Keys = new Dictionary<Kind, string>();

		private static readonly Dictionary<int, Kind> Lookup = new Dictionary<int, Kind>();

		private static readonly Dictionary<Stat, string> StatKeys = new Dictionary<Stat, string>();

		private static readonly Dictionary<int, Stat> StatLookup = new Dictionary<int, Stat>();

		private static readonly int MaxIterations = 10;

		public static void InitializeSteamNames()
		{
			Kind[] values = EnumCollections.Achievements.Values;
			foreach (Kind kind in values)
			{
				string text = Regex.Replace(EnumCollections.Achievements.GetName(kind), "([a-z])([A-Z])", "$1_$2").Replace(' ', '_').ToUpper();
				Keys.Add(kind, text);
				Lookup.Add(Animator.StringToHash(text), kind);
			}
			Stat[] values2 = EnumCollections.AchievementStats.Values;
			foreach (Stat stat in values2)
			{
				string name = EnumCollections.AchievementStats.GetName(stat);
				StatKeys.Add(stat, name);
				StatLookup.Add(Animator.StringToHash(name), stat);
			}
		}

		public static Kind GetKindFromName(string name)
		{
			if (Lookup.TryGetValue(Animator.StringToHash(name), out var value))
			{
				return value;
			}
			throw new KeyNotFoundException(name);
		}

		public static Stat GetStatFromName(string name)
		{
			if (StatLookup.TryGetValue(Animator.StringToHash(name), out var value))
			{
				return value;
			}
			throw new KeyNotFoundException(name);
		}

		public static void InitializeAsAchievementStore()
		{
			InitializeWithStore(Instance);
		}

		public void PopulateCache(Dictionary<Kind, bool> cache)
		{
			if (SteamUserStats.StatsRecieved)
			{
				IEnumerator<Achievement> enumerator = SteamUserStats.Achievements.GetEnumerator();
				while (enumerator.MoveNext())
				{
					Achievement current = enumerator.Current;
					Kind kindFromName = GetKindFromName(current.Identifier);
					cache[kindFromName] = current.State;
				}
				enumerator.Dispose();
			}
		}

		public void PopulateCache(Dictionary<Stat, bool> cache)
		{
			if (SteamUserStats.StatsRecieved)
			{
				IEnumerator<Achievement> enumerator = SteamUserStats.Achievements.GetEnumerator();
				while (enumerator.MoveNext())
				{
					Achievement current = enumerator.Current;
					Stat statFromName = GetStatFromName(current.Identifier);
					cache[statFromName] = current.State;
				}
				enumerator.Dispose();
			}
		}

		public void Achieve(Kind kind)
		{
			string text = Keys[kind];
			if (Cache[kind] || !SteamUserStats.StatsRecieved)
			{
				return;
			}
			Cache[kind] = true;
			IEnumerator<Achievement> enumerator = SteamUserStats.Achievements.GetEnumerator();
			using (enumerator)
			{
				while (enumerator.MoveNext())
				{
					Achievement current = enumerator.Current;
					if (current.Identifier == text)
					{
						if (!current.State)
						{
							current.Trigger();
							string name = EnumCollections.Achievements.GetName(kind);
							name = name.Substring("Achievement ".Length);
							ConsoleWindow.PrintAction(GameStrings.AchievementUnlocked.AsString(name));
						}
						break;
					}
				}
			}
		}

		public void Add(Stat stat, int value)
		{
			if (SteamUserStats.StatsRecieved)
			{
				SteamUserStats.AddStat(EnumCollections.AchievementStats.GetName(stat), value);
				TryForceAchievementUpdate().Forget();
			}
		}

		public void Add(Stat stat, float value)
		{
			if (SteamUserStats.StatsRecieved)
			{
				SteamUserStats.AddStat(EnumCollections.AchievementStats.GetName(stat), value);
				TryForceAchievementUpdate().Forget();
			}
		}

		public void SetAchievementValue(Stat stat, int value)
		{
			if (SteamUserStats.StatsRecieved)
			{
				SteamUserStats.SetStat(EnumCollections.AchievementStats.GetName(stat), value);
				TryForceAchievementUpdate().Forget();
			}
		}

		public void SetAchievementValue(Stat stat, float value)
		{
			if (SteamUserStats.StatsRecieved)
			{
				SteamUserStats.SetStat(EnumCollections.AchievementStats.GetName(stat), value);
				TryForceAchievementUpdate().Forget();
			}
		}

		public void Clear(Kind kind)
		{
			string text = Keys[kind];
			if (!SteamUserStats.StatsRecieved)
			{
				return;
			}
			foreach (Achievement achievement in SteamUserStats.Achievements)
			{
				if (achievement.Identifier == text)
				{
					achievement.Clear();
					break;
				}
			}
		}

		public void Clear(Stat stat)
		{
			string text = StatKeys[stat];
			if (!SteamUserStats.StatsRecieved)
			{
				return;
			}
			foreach (Achievement achievement in SteamUserStats.Achievements)
			{
				if (achievement.Identifier == text)
				{
					achievement.Clear();
					break;
				}
			}
		}

		public static async UniTaskVoid TryForceAchievementUpdate()
		{
			bool result = false;
			int iterations = 0;
			while (!result)
			{
				iterations++;
				ConsoleWindow.PrintAction($"Update attempt {iterations}", aged: true);
				result = SteamUserStats.StoreStats();
				if (iterations >= MaxIterations && !result)
				{
					ConsoleWindow.PrintError("Failed to force achievement update after 10 attempts.");
					return;
				}
				await UniTask.Delay(1000);
			}
			ConsoleWindow.PrintAction("Achievement updated successfully.", aged: true);
		}

		public static string GetKey(Kind achievement)
		{
			return Keys[achievement];
		}

		public static string GetStatKey(Stat achievement)
		{
			return StatKeys[achievement];
		}
	}

	private static IAchievementStore store;

	private static readonly Dictionary<Kind, bool> Cache = new Dictionary<Kind, bool>();

	private static readonly Dictionary<Stat, bool> StatCache = new Dictionary<Stat, bool>();

	public static readonly SyncList<AchieveServer> AchievedEvents = new SyncList<AchieveServer>(AchieveServer.DeserializeNew);

	private const int STEEL_INGOT_PREFAB_HASH = -654790771;

	private const int INVAR_INGOT_PREFAB_HASH = -297990285;

	private const int HASTELLOY_INGOT_PREFAB_HASH = 1579842814;

	private const int STATION_BATTERY_PREFAB_HASH = -400115994;

	private const int REVERSE_STACKER_PREFAB_HASH = 1585641623;

	private const int STACKER_PREFAB_HASH = -2020231820;

	private const int SUIT_STORAGE_PREFAB_HASH = 255034731;

	private const int SUIT_STORAGE_FRAME_PREFAB_HASH = -504802150;

	private const int SUIT_STORAGE_LOCKER_PREFAB_HASH = -346183425;

	private const int ELECTRONICS_PRINTER_PREFAB_HASH = 1307165496;

	private const int MUFFIN_PREFAB_HASH = -1864982322;

	private const int NUCLEAR_BATTERY_PREFAB_HASH = 544617306;

	private const int POTATO_PLANT_PREFAB_HASH = 1929046963;

	private const float GO_AT_THROTTLE_UP_EV = 6000f;

	private const float LIFE_TIME_REMAINING_SECONDS = 60f;

	private static readonly int StationeerHash = Animator.StringToHash("Stationeer");

	public static void Initialize()
	{
		Cache.Clear();
		Kind[] values = EnumCollections.Achievements.Values;
		foreach (Kind key in values)
		{
			Cache.Add(key, value: false);
		}
		Stat[] values2 = EnumCollections.AchievementStats.Values;
		foreach (Stat key2 in values2)
		{
			StatCache.Add(key2, value: false);
		}
		ConsoleWindow.Print($"initialized {Cache.Count + StatCache.Count} achievement records");
		Steam.InitializeSteamNames();
		AchievementChainData.InitializeOnStart();
	}

	public static void InitializeWithStore(IAchievementStore store)
	{
		Achievements.store = store;
		foreach (KeyValuePair<Kind, bool> item in Cache)
		{
			if (item.Value)
			{
				store?.Achieve(item.Key);
			}
		}
		store?.PopulateCache(Cache);
		store?.PopulateCache(StatCache);
	}

	public static void Achieve(string id, bool sendToAll = false)
	{
		if (!Enum.TryParse<Kind>(id, out var result))
		{
			ConsoleWindow.PrintError("Achievement '" + id + "' not found");
		}
		else
		{
			Achieve(result, sendToAll);
		}
	}

	public static void Achieve(Kind kind, bool sendToAll = false)
	{
		if (Enabled() && !Cache[kind])
		{
			if (!GameManager.IsBatchMode)
			{
				store?.Achieve(kind);
			}
			if (sendToAll && NetworkManager.IsServer && NetworkServer.HasClients())
			{
				AchievedEvents.Add(new AchieveServer
				{
					Kind = kind,
					ParentHuman = null
				});
			}
		}
	}

	public static void Increment(Stat stat, int value)
	{
		if (Enabled() && !GameManager.IsBatchMode)
		{
			store?.Add(stat, value);
		}
	}

	public static void Increment(Stat stat, float value)
	{
		if (Enabled() && !GameManager.IsBatchMode)
		{
			store?.Add(stat, value);
		}
	}

	public static void Clear(Kind kind)
	{
		if (!GameManager.IsBatchMode && Cache[kind])
		{
			Cache[kind] = false;
			store?.Clear(kind);
		}
	}

	public static void ClearAll()
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		foreach (Kind item in new List<Kind>(Cache.Keys))
		{
			Clear(item);
		}
	}

	public static void AchieveItsGonnaBlow(Pipe pipe)
	{
		if (Enabled() && pipe.IsBurst != PipeBurst.None && (pipe.DamageRecord & PipeBurst.Pressure) != PipeBurst.None)
		{
			Achieve(Kind.AchievementItsGonnaBlow);
		}
	}

	public static void AchieveTidy(Structure prefab)
	{
		if (!Enabled())
		{
			return;
		}
		bool flag;
		if ((object)prefab != null)
		{
			int prefabHash = prefab.PrefabHash;
			if (prefabHash == -2020231820 || prefabHash == 1585641623)
			{
				flag = true;
				goto IL_0029;
			}
		}
		flag = false;
		goto IL_0029;
		IL_0029:
		if (flag && prefab.IsStructureCompleted)
		{
			Achieve(Kind.AchievementTidy);
		}
	}

	public static void AchievePowerOverwhelming(Structure prefab)
	{
		if (Enabled() && (object)prefab != null && prefab.PrefabHash == -400115994 && prefab.IsStructureCompleted)
		{
			Achieve(Kind.AchievementPowerOverwhelming);
		}
	}

	public static void AssessTakeALoadOff(Structure prefab)
	{
		if (Enabled() && (((object)prefab != null && prefab.PrefabHash == 255034731) || ((object)prefab != null && prefab.PrefabHash == -504802150) || ((object)prefab != null && prefab.PrefabHash == -346183425)) && prefab.IsStructureCompleted)
		{
			Achieve(Kind.AchievementTakeALoadOff);
		}
	}

	private static bool IsExhaustVelocity(Rocket rocket)
	{
		foreach (IRocketEngine engine in rocket.RocketNetwork.Engines)
		{
			if (!(engine.ExhaustVelocity < 6000f))
			{
				return true;
			}
		}
		return false;
	}

	public static void AssessGoAtThrottleUp(Rocket rocket)
	{
		if (Enabled() && rocket != null && IsExhaustVelocity(rocket))
		{
			Achieve(Kind.AchievementGoAtThrottleUp);
		}
	}

	public static void AssessMyNextForm(Structure prefab, int newStateIndex)
	{
		if (Enabled() && (object)prefab != null && prefab.PrefabHash == 1307165496 && newStateIndex == prefab.BuildStates.Count - 1)
		{
			Achieve(Kind.AchievementMyNextForm);
		}
	}

	public static void AssessMarkWatney(Plant prefab)
	{
		if (Enabled() && (object)prefab != null && prefab.PrefabHash == 1929046963)
		{
			Achieve(Kind.AchievementMarkWatney);
		}
	}

	public static void AssessStillMarkWatney(Plant prefab)
	{
		if (Enabled() && (object)prefab != null && prefab.PrefabHash == 1929046963 && prefab.IsDead)
		{
			Achieve(Kind.AchievementStillMarkWatney);
		}
	}

	public static void AssessAimeeDoesNotUnderstand(Interactable interactable, bool powered)
	{
		if (Enabled() && powered && interactable.Action == InteractableType.OnOff)
		{
			Achieve(Kind.AchievementAimeeDoesNotUnderstand);
		}
	}

	public static void AssessGoodMorning(Human human)
	{
		if (Enabled() && (object)InventoryManager.ParentHuman != null && human.HasAuthority && !(InventoryManager.ParentHuman != human))
		{
			Achieve(Kind.AchievementGoodMorning);
		}
	}

	public static void AssessMuffinMan(DynamicThing dynamicThing)
	{
		if (Enabled() && (bool)dynamicThing && dynamicThing.PrefabHash == -1864982322)
		{
			Achieve(Kind.AchievementMuffinMan);
		}
	}

	public static void AssessGottaGrowFast(float currentValue, float maxValue)
	{
		if (Enabled() && !(currentValue < maxValue * 0.99f))
		{
			Achieve(Kind.AchievementGottaGrowFast);
		}
	}

	public static void AchieveHurryUp()
	{
		if (Enabled())
		{
			Achieve(Kind.AchievementHurryUp);
		}
	}

	public static void AchieveSootHappens()
	{
		if (Enabled())
		{
			Achieve(Kind.AchievementSootHappens, sendToAll: true);
		}
	}

	public static void AssessClearedToLand(TraderContact contact)
	{
		if (Enabled() && contact != null && contact.IsPlane)
		{
			Achieve(Kind.AchievementClearedToLand);
		}
	}

	public static void AssessYouStillOpen(TraderContact contact)
	{
		if (Enabled())
		{
			float num = (GameManager.RunSimulation ? GameManager.GameTime : NetworkTime.time);
			if (contact != null && contact.EndLifetime < num + 60f)
			{
				Achieve(Kind.AchievementYouStillOpen);
			}
		}
	}

	public static void AssessWelcomeAboard()
	{
		if (Enabled(mustBeRunning: false) && NetworkManager.TotalPlayersInGame > 3)
		{
			Achieve(Kind.AchievementWelcomeAboard);
		}
	}

	public static void AchieveBlastOff()
	{
		if (Enabled())
		{
			Achieve(Kind.AchievementBlastOff);
		}
	}

	public static void AssessBrutalLegend()
	{
		if (!Enabled() || !WorldSetting.Current.StartConditionData.IsBrutal)
		{
			return;
		}
		DifficultySetting difficultySetting = DifficultySetting.Find(StationeerHash);
		if (DifficultySetting.Current != difficultySetting)
		{
			return;
		}
		foreach (Human allHuman in Human.AllHumans)
		{
			if (allHuman.DaysLived < 30)
			{
				continue;
			}
			EntityState state = allHuman.State;
			if (state != EntityState.Dead && state != EntityState.Decay)
			{
				if (InventoryManager.ParentHuman == allHuman)
				{
					Achieve(Kind.AchievementBrutalLegend);
				}
				else if (NetworkManager.IsServer && NetworkServer.HasClients())
				{
					AchievedEvents.Add(new AchieveServer
					{
						Kind = Kind.AchievementBrutalLegend,
						ParentHuman = allHuman
					});
				}
			}
		}
	}

	public static void AssessFurnaceMaster(IQuantity prefab)
	{
		if (Enabled() && prefab is Thing { PrefabHash: 1579842814 })
		{
			Achieve(Kind.AchievementFurnaceMaster, sendToAll: true);
		}
	}

	public static void AssessFurnaceJourneyman(IQuantity prefab)
	{
		if (Enabled() && prefab is Thing { PrefabHash: -297990285 })
		{
			Achieve(Kind.AchievementFurnaceJourneyman, sendToAll: true);
		}
	}

	public static void AssessFurnaceApprentice(IQuantity prefab)
	{
		if (Enabled() && prefab is Thing { PrefabHash: -654790771 })
		{
			Achieve(Kind.AchievementFurnaceApprentice, sendToAll: true);
		}
	}

	public static void AssessSomeHighQualityH2O(Entity entity)
	{
		if (!Enabled() || !(entity is Human human))
		{
			return;
		}
		float num = 8.75f;
		if (!(human.Hydration < num * 0.99f))
		{
			if (human == InventoryManager.ParentHuman)
			{
				Achieve(Kind.AchievementSomeHighQualityH2O);
			}
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				AchievedEvents.Add(new AchieveServer
				{
					Kind = Kind.AchievementSomeHighQualityH2O,
					ParentHuman = human
				});
			}
		}
	}

	public static void AssessOops(DynamicThing prefab)
	{
		if (Enabled() && !(prefab is Ore) && !(prefab is Ingot))
		{
			Achieve(Kind.AchievementOops, sendToAll: true);
		}
	}

	public static void AssessMedic(Item item, Human source, Human target)
	{
		if (Enabled() && (object)target != null && target.Unconscious && item is HealthPill)
		{
			if (source == InventoryManager.ParentHuman)
			{
				Achieve(Kind.AchievementMedic);
			}
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				AchievedEvents.Add(new AchieveServer
				{
					Kind = Kind.AchievementMedic,
					ParentHuman = source
				});
			}
		}
	}

	public static void AchieveSomeAssemblyRequired()
	{
		if (Enabled())
		{
			Achieve(Kind.AchievementSomeAssemblyRequired);
		}
	}

	public static void AchieveHaltAndCatchFire()
	{
		if (Enabled())
		{
			Achieve(Kind.AchievementHaltAndCatchFire, sendToAll: true);
		}
	}

	public static void AchieveABitWindy()
	{
		if (Enabled())
		{
			Achieve(Kind.AchievementABitWindy, sendToAll: true);
		}
	}

	public static void AssessZzzt(Human attackSource)
	{
		if (Enabled())
		{
			if (attackSource == InventoryManager.ParentHuman)
			{
				Achieve(Kind.AchievementZzzt);
			}
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				AchievedEvents.Add(new AchieveServer
				{
					Kind = Kind.AchievementZzzt,
					ParentHuman = attackSource
				});
			}
		}
	}

	public static void Achieve121Gigawatts(DynamicThing prefab)
	{
		if (Enabled() && (object)prefab != null && prefab.PrefabHash == 544617306)
		{
			Achieve(Kind.Achievement121Gigawatts, sendToAll: true);
		}
	}

	public static void AchieveStackOverflow(ulong lastEditedBy)
	{
		if (!Enabled())
		{
			return;
		}
		if (!NetworkManager.IsServer)
		{
			Achieve(Kind.AchievementStackOverflow);
		}
		else
		{
			if (lastEditedBy == 0L)
			{
				return;
			}
			ulong num = InventoryManager.ParentBrain?.ClientId ?? 0;
			if (!NetworkServer.HasClients())
			{
				if (num == lastEditedBy)
				{
					Achieve(Kind.AchievementStackOverflow);
				}
				return;
			}
			if (num == lastEditedBy)
			{
				Achieve(Kind.AchievementStackOverflow);
				return;
			}
			Human human = Human.Find(lastEditedBy);
			if ((object)human != null)
			{
				AchievedEvents.Add(new AchieveServer
				{
					Kind = Kind.AchievementStackOverflow,
					ParentHuman = human
				});
			}
		}
	}

	public static void AchieveJetpackCritical(Jetpack jetpack)
	{
		if (!Enabled() || jetpack.PropellentSlot == null || !jetpack.PropellentSlot.Contains<DynamicThing>(out var occupant))
		{
			return;
		}
		Human rootParentHuman = jetpack.RootParentHuman;
		if (rootParentHuman == null)
		{
			return;
		}
		PressurekPa pressurekPa = AtmosphericsController.World.SampleGlobalAtmosphere(rootParentHuman.WorldGrid).PressureGassesAndLiquids + new PressurekPa(100.0);
		if (!(occupant.InternalAtmosphere.PressureGassesAndLiquids > pressurekPa))
		{
			if (rootParentHuman == InventoryManager.ParentHuman)
			{
				Achieve(Kind.AchievementJetpackCritical);
			}
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				AchievedEvents.Add(new AchieveServer
				{
					Kind = Kind.AchievementJetpackCritical,
					ParentHuman = rootParentHuman
				});
			}
		}
	}

	private static bool Enabled(bool mustBeRunning = true)
	{
		if (mustBeRunning && GameManager.GameState != GameState.Running)
		{
			return false;
		}
		return DifficultySetting.Current.Achievements;
	}

	public static void AssessWhyIsItStillMarkWatney()
	{
		if (!Enabled())
		{
			return;
		}
		Human parentHuman = InventoryManager.ParentHuman;
		if ((object)parentHuman != null && parentHuman.DaysLived >= 30)
		{
			EntityState state = parentHuman.State;
			if (state != EntityState.Dead && state != EntityState.Decay && parentHuman.GetPotatoDays() >= 30)
			{
				Achieve(Kind.AchievementWhyIsItStillMarkWatney);
			}
		}
	}

	public static bool Check(Kind achievement)
	{
		return Cache[achievement];
	}

	public static bool Check(Stat achievement)
	{
		return StatCache[achievement];
	}

	public static void AchieveDidYouForgetSomething()
	{
		if (Enabled())
		{
			Achieve(Kind.AchievementDidYouForgetSomething, sendToAll: true);
		}
	}

	public static void AssessWellThankWasQuick(Human human)
	{
		if (Enabled() && !GameManager.IsBatchMode && !WorldManager.IsCreative() && EnvironmentalAudioHandler.Instance.WorldType == WorldManager.WorldType.Vulcan && human.DaysLived < 1)
		{
			Achieve(Kind.AchievementWellThatWasQuick);
		}
	}

	public static void AchieveYesItExplodedMark()
	{
		if (Enabled())
		{
			Achieve(Kind.AchievementYesItExplodedMark, sendToAll: true);
		}
	}

	public static void AssessDaysLived(ushort daysLived)
	{
		WorldSetting current = WorldSetting.Current;
		if (current == null)
		{
			return;
		}
		string currentDifficulty = DifficultySetting.Current?.Id ?? string.Empty;
		foreach (AchievementSurvivalData survivalAchievement in current.Data.SurvivalAchievements)
		{
			survivalAchievement.Evaluate(daysLived, currentDifficulty);
		}
	}
}
