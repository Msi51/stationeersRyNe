using System.Xml.Linq;
using Assets.Scripts.Atmospherics;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public static class GameConstants
{
	public static class Trade
	{
		public static float BaseSuperAlloy = 1f;

		public static float BaseAlloy = 0.7f;

		public static float BaseIngot = 0.1f;

		public static float PlantScale = 20f;

		public static float CreditPerNutrition = 1f;
	}

	public static class SpaceMap
	{
		public const float SPACE_ICE_YIELD_MULTIPLIER = 4f;

		public const float SPACE_ORE_YIELD_MULTIPLIER = 1.25f;

		public const float SPACE_GAS_YIELD_MULTIPLIER = 1f;
	}

	public static class FoodQuality
	{
		public const float NONE = 0f;

		public const float SPOILED = 0f;

		public const float RAW = 0.25f;

		public const float COOKED = 0.5f;

		public const float CANNED = 0.75f;

		public const float COMPLEX = 1f;

		public const float STARTING_FOOD_QUALITY = 0.75f;

		public const float COOKED_THRESHOLD = 0.45f;

		public const float CANNED_THRESHOLD = 0.7f;

		public const float COMPLEX_THRESHOLD = 0.9f;

		public const float RAW_MULTIPLIER = 0.75f;

		public const float COOKED_MULTIPLIER = 1f;

		public const float CANNED_MULTIPLIER = 1.25f;

		public const float COMPLEX_MULTIPLIER = 1.75f;

		public const float MAX_NUTRITION_MULTIPLIER = 1.75f;
	}

	public static XNamespace XML_NAMESPACE = "http://www.w3.org/2001/XMLSchema-instance";

	public const int MILLISECONDS_IN_SECOND = 1000;

	public const int SECONDS_IN_MINUTE = 60;

	public const int SECONDS_IN_HOUR = 3600;

	public const int SECONDS_IN_DAY = 86400;

	public const int SECONDS_IN_WEEK = 604800;

	public const int SECONDS_IN_MONTH = 2592000;

	public const int SECONDS_IN_YEAR = 31104000;

	public const int ERROR_FLASH_DELAY = 250;

	public const int WIND_AUDIO_UPDATE_MS = 33;

	public const int PRESSURE_AUDIO_UPDATE_MS = 300;

	public const int TRACKER_UPDATE_INTERVAL = 1000;

	public const int FRAMES_PER_TOOLTIP_UPDATE = 30;

	public const float SUN_SHADOW_BIAS = 0.2f;

	public const float SUN_SHADOW_NORMAL_BIAS = 0f;

	public const float THING_SHADOW_BIAS = 0.1f;

	public const int HUMAN_COGNITION_THRESHOLD = 100;

	public const float MAX_DEPENETRATION_VELOCITY = 10f;

	public const int MAX_STRING_LENGTH = 200;

	public const int MAX_ROCKETS = 100;

	public const float SPACE_MAP_DISTANCE_MULTIPLIER = 2f;

	public const float SITE_DISTANCE = 100f;

	public const double MIN_STATE_CHANGE_MOLES_RATIO = 0.1;

	public const double MAX_STATE_CHANGE_MOLES_RATIO = 0.5;

	public const double THRESHOLD_MOLES_STATE_CHANGE_ALL = 1.0;

	public const float MINIMUM_MOLES_FOR_CONDENSATION_EFFECT = 0.01f;

	public const int TERRAIN_MAX_HEIGHT = 1023;

	public const int TERRAIN_MIN_HEIGHT = 0;

	public const int BED_ROCK_THICKNESS = 2;

	public const int BED_ROCK = 2;

	public const int VEIN_REGISTRATION_SIZE = 32;

	public static Vector3Int MINABLES_GENERATION_RANGE = new Vector3Int(192, 96, 192);

	public static Vector3Int MINABLES_GENERATION_RANGE_AIMEE = new Vector3Int(32, 32, 32);

	public static Vector3Int MINABLES_GENERATION_RANGE_DEEP_MINER = new Vector3Int(32, 32, 32);

	public static Vector3Int MINABLES_GENERATION_RANGE_EXPLOSIVE = new Vector3Int(64, 64, 64);

	public static Vector3Int MINABLES_GENERATION_RANGE_CLIENT = new Vector3Int(64, 64, 64);

	public static readonly MoleQuantity MinimumMolesForCondensationEffect = new MoleQuantity(0.009999999776482582);

	public const float MAX_ALLOWED_RATIO_VOLUME_LIQUIDS_GAS_PIPE = 0.02f;

	public const int WORLD_ATMOSPHERE_SOFT_CAP = 2000;

	public const int WORLD_ATMOSPHERE_MAX = 20000;

	public const int ATMOSPHERES_MAX = 32768;

	public const float MAX_ROCKET_GRAVITY = -5.5f;

	public const float MIN_ROCKET_GRAVITY = -1f;

	public const double DECI = 10.0;
}
