using System;
using UnityEngine;

namespace Assets.Scripts.Util;

public static class Defines
{
	public static class Animator
	{
		public static readonly int Vertical = UnityEngine.Animator.StringToHash("Vertical");

		public static readonly int Horizontal = UnityEngine.Animator.StringToHash("Horizontal");

		public static readonly int TargetVertical = UnityEngine.Animator.StringToHash("TargetVertical");

		public static readonly int TargetHorizontal = UnityEngine.Animator.StringToHash("TargetHorizontal");

		public static readonly int On = UnityEngine.Animator.StringToHash("On");

		public static readonly int Off = UnityEngine.Animator.StringToHash("Off");

		public static readonly int NotPowered = UnityEngine.Animator.StringToHash("NotPowered");

		public static readonly int Activate = UnityEngine.Animator.StringToHash("Activate");

		public static readonly int OnPowered = UnityEngine.Animator.StringToHash("OnPowered");

		public static readonly int OffPowered = UnityEngine.Animator.StringToHash("OffPowered");

		public static readonly int Error0 = UnityEngine.Animator.StringToHash("Error0");

		public static readonly int Error1 = UnityEngine.Animator.StringToHash("Error1");

		public static readonly int StartUp = UnityEngine.Animator.StringToHash("StartUp");

		public static readonly int Lock = UnityEngine.Animator.StringToHash("Lock");

		public static readonly int Active = UnityEngine.Animator.StringToHash("Active");

		public static readonly int Powered = UnityEngine.Animator.StringToHash("Powered");

		public static readonly int Activate0 = UnityEngine.Animator.StringToHash("Activate0");

		public static readonly int Activate1 = UnityEngine.Animator.StringToHash("Activate1");

		public static readonly int Activate2 = UnityEngine.Animator.StringToHash("Activate2");

		public static readonly int Activate3 = UnityEngine.Animator.StringToHash("Activate3");

		public static readonly int Armed = UnityEngine.Animator.StringToHash("Armed");

		public static readonly int Disarmed = UnityEngine.Animator.StringToHash("Disarmed");

		public static readonly int Outward = UnityEngine.Animator.StringToHash("Outward");

		public static readonly int Inward = UnityEngine.Animator.StringToHash("Inward");

		public static readonly int Normal = UnityEngine.Animator.StringToHash("Normal");

		public static readonly int Critical = UnityEngine.Animator.StringToHash("Critical");

		public static readonly int Rpm = UnityEngine.Animator.StringToHash("Rpm");

		public static readonly int Stress = UnityEngine.Animator.StringToHash("Stress");

		public static readonly int InvalidSmelt = UnityEngine.Animator.StringToHash("InvalidSmelt");

		public static readonly int ValidSmelt = UnityEngine.Animator.StringToHash("ValidSmelt");

		public static readonly int Idle = UnityEngine.Animator.StringToHash("Idle");

		public static readonly int Charging = UnityEngine.Animator.StringToHash("Charging");

		public static readonly int Charged = UnityEngine.Animator.StringToHash("Charged");

		public static readonly int Discharged = UnityEngine.Animator.StringToHash("Discharged");
	}

	public static class Prefabs
	{
		public static readonly int ItemCrowbar = UnityEngine.Animator.StringToHash("ItemCrowbar");

		public static readonly int ItemAngleGrinder = UnityEngine.Animator.StringToHash("ItemAngleGrinder");
	}

	public static class Color
	{
		public static readonly string Gray = "gray";

		public static readonly string White = "white";

		public static readonly string Red = "red";

		public static readonly string Orange = "orange";

		public static readonly string Green = "green";

		public static readonly string Blue = "blue";
	}

	public static class SoundChannel
	{
		public static readonly int Huge = UnityEngine.Animator.StringToHash("Huge");

		public static readonly int Large = UnityEngine.Animator.StringToHash("Large");

		public static readonly int Medium = UnityEngine.Animator.StringToHash("Medium");

		public static readonly int Small = UnityEngine.Animator.StringToHash("Small");

		public static readonly int SmallInternal = UnityEngine.Animator.StringToHash("SmallInternal");
	}

	public static class Sounds
	{
		public static readonly int WrenchOneShot = UnityEngine.Animator.StringToHash("WrenchOneShot");

		public static readonly int MovingSoundVerticalHash = UnityEngine.Animator.StringToHash("MovingSoundVertical");

		public static readonly int MovingSoundVerticalStartHash = UnityEngine.Animator.StringToHash("MovingSoundVerticalStart");

		public static readonly int MovingSoundVerticalFinishHash = UnityEngine.Animator.StringToHash("MovingSoundVerticalFinish");

		public static readonly int DialTurnHash = UnityEngine.Animator.StringToHash("DialTurn");

		public static readonly int MovingSoundHorizontalHash = UnityEngine.Animator.StringToHash("MovingSoundHorizontal");

		public static readonly int MovingSoundHorizontalStartHash = UnityEngine.Animator.StringToHash("MovingSoundHorizontalStart");

		public static readonly int MovingSoundHorizontalFinishHash = UnityEngine.Animator.StringToHash("MovingSoundHorizontalFinish");

		public static readonly int RobotArmAnimate = UnityEngine.Animator.StringToHash("RobotArmAnimate");

		public static int RobotArmMoving = UnityEngine.Animator.StringToHash("RobotArmMoving");

		public static int RobotArmStop = UnityEngine.Animator.StringToHash("RobotArmStop");

		public static readonly int ToiletFlush = UnityEngine.Animator.StringToHash("ToiletFlush");

		public static readonly int SanitationBag = UnityEngine.Animator.StringToHash("SanitationBag");

		public static readonly int InjectorUse = UnityEngine.Animator.StringToHash("InjectorUse");

		public static readonly int LogicOnBeep = UnityEngine.Animator.StringToHash("LogicOnBeep");

		public static readonly int LogicOffBeep = UnityEngine.Animator.StringToHash("LogicOffBeep");

		public static readonly int LogicErrorBeep = UnityEngine.Animator.StringToHash("LogicError");

		public static readonly int LogicOn = UnityEngine.Animator.StringToHash("LogicOn");

		public static readonly int LogicOff = UnityEngine.Animator.StringToHash("LogicOff");

		public static readonly int PipeDeviceOn = UnityEngine.Animator.StringToHash("PipeDeviceOn");

		public static readonly int PipeDeviceOff = UnityEngine.Animator.StringToHash("PipeDeviceOff");

		public static readonly int LogicRead = UnityEngine.Animator.StringToHash("LogicRead");

		public static readonly int LogicWrite = UnityEngine.Animator.StringToHash("LogicWrite");

		public static readonly int LogicMath = UnityEngine.Animator.StringToHash("LogicMath");

		public static readonly int SwitchOn = UnityEngine.Animator.StringToHash("SwitchOn");

		public static readonly int SwitchOff = UnityEngine.Animator.StringToHash("SwitchOff");

		public static readonly int Error = UnityEngine.Animator.StringToHash("Error");

		public static readonly int DialTurn = UnityEngine.Animator.StringToHash("DialTurn");

		public static readonly int GrinderActiveOff = UnityEngine.Animator.StringToHash("GrinderActiveOff");

		public static readonly int Label = UnityEngine.Animator.StringToHash("Label");

		public static readonly int LabelConfirm = UnityEngine.Animator.StringToHash("LabelConfirm");

		public static readonly int LabelCancel = UnityEngine.Animator.StringToHash("LabelCancel");

		public static readonly int AtmosphereFireStart = UnityEngine.Animator.StringToHash("AtmosphereFireStart");

		public static readonly int AtmosphereFire = UnityEngine.Animator.StringToHash("AtmosphereFire");

		public static readonly int ScrewdriverSound = UnityEngine.Animator.StringToHash("ScrewDriverOneShot");

		public static readonly int ThrottleLeverDown = UnityEngine.Animator.StringToHash("ThrottleLeverDown");

		public static readonly int ThrottleLeverUp = UnityEngine.Animator.StringToHash("ThrottleLeverUp");

		public static readonly int LeverDown = UnityEngine.Animator.StringToHash("LeverDown");

		public static readonly int LeverUp = UnityEngine.Animator.StringToHash("LeverUp");

		public static readonly int HarvestPlantActive = UnityEngine.Animator.StringToHash("HarvestPlantActive");

		public static readonly int HarvestPlant = UnityEngine.Animator.StringToHash("HarvestPlant");

		public static readonly int PlantingPlant = UnityEngine.Animator.StringToHash("PlantingPlant");

		public static readonly int PlantingFinished = UnityEngine.Animator.StringToHash("PlantingFinished");

		public static readonly int ActivateButton = UnityEngine.Animator.StringToHash("ActivateButton");

		public static readonly int DirectionNextButton = UnityEngine.Animator.StringToHash("DirectionNextButton");

		public static readonly int DirectionPreviousButton = UnityEngine.Animator.StringToHash("DirectionPreviousButton");

		public static readonly int CompletedChime = UnityEngine.Animator.StringToHash("CompletedChime");

		public static readonly int Splice = UnityEngine.Animator.StringToHash("splice");

		public static readonly int VolumePumpRunnningHash = UnityEngine.Animator.StringToHash("VolumePumpRunning");

		public static readonly int PipeFailHash = UnityEngine.Animator.StringToHash("PipeFail");

		public static readonly int ShuttleSmallSonicBoom = UnityEngine.Animator.StringToHash("ShuttleSmallSonicBoom");

		public static readonly int ApcOpen = UnityEngine.Animator.StringToHash("ApcOpen");

		public static readonly int ApcClose = UnityEngine.Animator.StringToHash("ApcClose");

		public static readonly int ApcOn = UnityEngine.Animator.StringToHash("ApcOn");

		public static readonly int ApcOff = UnityEngine.Animator.StringToHash("ApcOff");

		public static readonly int EggCartonOpen = UnityEngine.Animator.StringToHash("EggCartonOpen");

		public static readonly int EggCartonClose = UnityEngine.Animator.StringToHash("EggCartonClose");

		public static readonly int ShuttleSmallMainEngine = UnityEngine.Animator.StringToHash("ShuttleSmallMainEngine");

		public static readonly int ShuttleSmallMainEngineDepart = UnityEngine.Animator.StringToHash("ShuttleSmallMainEngineDepart");

		public static readonly int ShuttleSmallMainEngineStart = UnityEngine.Animator.StringToHash("ShuttleSmallMainEngineStart");

		public static readonly int ShuttleSmallMainEngineEnd = UnityEngine.Animator.StringToHash("ShuttleSmallMainEngineEnd");

		public static readonly int ShuttleSmallMainEngineDistant = UnityEngine.Animator.StringToHash("ShuttleSmallMainEngineDistant");

		public static readonly int ShuttleSmallMainEngineDistantDepart = UnityEngine.Animator.StringToHash("ShuttleSmallMainEngineDistantDepart");

		public static readonly int ShuttleMediumMainEngine = UnityEngine.Animator.StringToHash("ShuttleMediumMainEngine");

		public static readonly int ShuttleMediumMainEngineDepart = UnityEngine.Animator.StringToHash("ShuttleMediumMainEngineDepart");

		public static readonly int ShuttleMediumMainEngineStart = UnityEngine.Animator.StringToHash("ShuttleMediumMainEngineStart");

		public static readonly int ShuttleMediumMainEngineEnd = UnityEngine.Animator.StringToHash("ShuttleMediumMainEngineEnd");

		public static readonly int ShuttleMediumMainEngineDistant = UnityEngine.Animator.StringToHash("ShuttleMediumMainEngineDistant");

		public static readonly int ShuttleMediumMainEngineDistantDepart = UnityEngine.Animator.StringToHash("ShuttleMediumMainEngineDistantDepart");

		public static readonly int ShuttleLargeMainEngine = UnityEngine.Animator.StringToHash("ShuttleLargeMainEngine");

		public static readonly int ShuttleLargeMainEngineDepart = UnityEngine.Animator.StringToHash("ShuttleLargeMainEngineDepart");

		public static readonly int ShuttleLargeMainEngineL = UnityEngine.Animator.StringToHash("ShuttleLargeMainEngineL");

		public static readonly int ShuttleLargeMainEngineLDepart = UnityEngine.Animator.StringToHash("ShuttleLargeMainEngineLDepart");

		public static readonly int ShuttleLargeMainEngineR = UnityEngine.Animator.StringToHash("ShuttleLargeMainEngineR");

		public static readonly int ShuttleLargeMainEngineRDepart = UnityEngine.Animator.StringToHash("ShuttleLargeMainEngineRDepart");

		public static readonly int ShuttleLargeMainEngineDistant = UnityEngine.Animator.StringToHash("ShuttleLargeMainEngineDistant");

		public static readonly int ShuttleLargeMainEngineDistantDepart = UnityEngine.Animator.StringToHash("ShuttleLargeMainEngineDistantDepart");

		public static readonly int DisposableBatteryChargerHash = UnityEngine.Animator.StringToHash("DisposableBatteryCharger");

		public static readonly int DisposableBatteryChargerFinishedHash = UnityEngine.Animator.StringToHash("DisposableBatteryChargerFinished");

		public static readonly int HemDroidRepairKitHash = UnityEngine.Animator.StringToHash("HemDroidRepairKit");

		public static readonly int HemDroidRepairKitFinishedHash = UnityEngine.Animator.StringToHash("HemDroidRepairKitFinished");

		public static readonly int DeviceExportHash = UnityEngine.Animator.StringToHash("DeviceExport");

		public static readonly int DeviceImportHash = UnityEngine.Animator.StringToHash("DeviceImport");

		public static readonly int ChuteBinOpenHash = UnityEngine.Animator.StringToHash("ChuteBinOpen");

		public static readonly int ChuteBinCloseHash = UnityEngine.Animator.StringToHash("ChuteBinClose");

		public static readonly int PipeValveOnHash = UnityEngine.Animator.StringToHash("PipeValveOn");

		public static readonly int PipeValveOffHash = UnityEngine.Animator.StringToHash("PipeValveOff");

		public static readonly int StorageLockerOpenHash = UnityEngine.Animator.StringToHash("StorageLockerOpen");

		public static readonly int StorageLockerCloseHash = UnityEngine.Animator.StringToHash("StorageLockerClose");

		public static readonly int StorageLockerSmallOpenHash = UnityEngine.Animator.StringToHash("StorageLockerSmallOpen");

		public static readonly int StorageLockerSmallCloseHash = UnityEngine.Animator.StringToHash("StorageLockerSmallClose");

		public static readonly int CornerLockerOpenHash = UnityEngine.Animator.StringToHash("CornerLockerOpen");

		public static readonly int CornerLockerCloseHash = UnityEngine.Animator.StringToHash("CornerLockerClose");

		public static readonly int PipeLeakHash = UnityEngine.Animator.StringToHash("PipeLeak");

		public static readonly int PipeDamageHash = UnityEngine.Animator.StringToHash("PipeDamage");

		public static readonly int RocketCloseHash = UnityEngine.Animator.StringToHash("RocketClose");

		public static readonly int RocketDistantHash = UnityEngine.Animator.StringToHash("RocketDistant");

		public static readonly int RocketStartHash = UnityEngine.Animator.StringToHash("RocketStart");

		public static readonly int RocketAmbienceHash = UnityEngine.Animator.StringToHash("RocketAmbience");

		public static readonly int DryFireHash = UnityEngine.Animator.StringToHash("DryFire");

		public static readonly int FlareAirBurstHash = UnityEngine.Animator.StringToHash("FlareAirBurst");

		public static readonly int EnginesInternal = UnityEngine.Animator.StringToHash("EnginesInternal");

		public static readonly int EnginesExternal = UnityEngine.Animator.StringToHash("EnginesExternal");

		public static readonly int CapsuleImpact = UnityEngine.Animator.StringToHash("CapsuleImpact");

		public static readonly int ShutterOpen = UnityEngine.Animator.StringToHash("ShutterOpen");

		public static readonly int ShutterClose = UnityEngine.Animator.StringToHash("ShutterClose");

		public static readonly int ShutterOpenStop = UnityEngine.Animator.StringToHash("ShutterOpenStop");

		public static readonly int ShutterCloseStop = UnityEngine.Animator.StringToHash("ShutterCloseStop");

		public static readonly int BobbleHead = UnityEngine.Animator.StringToHash("BobbleHead");

		public static readonly int CollectorInwardsLoop = UnityEngine.Animator.StringToHash("CollectorInwardsLoop");

		public static readonly int CollectorOutwardsLoop = UnityEngine.Animator.StringToHash("CollectorOutwardsLoop");

		public static readonly int CollectorInwardsStart = UnityEngine.Animator.StringToHash("CollectorInwardsStart");

		public static readonly int CollectorOutwardsStart = UnityEngine.Animator.StringToHash("CollectorOutwardsStart");

		public static readonly int CollectorIngest = UnityEngine.Animator.StringToHash("CollectorIngest");

		public static readonly int RobotArmAnimateDown = UnityEngine.Animator.StringToHash("RobotArmAnimateDown");

		public static readonly int RobotArmAnimateUp = UnityEngine.Animator.StringToHash("RobotArmAnimateUp");

		public static readonly int VentInwards = UnityEngine.Animator.StringToHash("VentInwards");

		public static readonly int VentOutwards = UnityEngine.Animator.StringToHash("VentOutwards");

		public static readonly int EquipHeavyMiningTool = UnityEngine.Animator.StringToHash("EquipHeavyMiningTool");

		public static readonly int UnEquipHeavyMiningTool = UnityEngine.Animator.StringToHash("UnEquipHeavyMiningTool");

		public static readonly int FootstepWater = UnityEngine.Animator.StringToHash("FootstepWater");

		public static readonly int ElectricalFailure = UnityEngine.Animator.StringToHash("ElectricalFailure");

		public static readonly int ApcCharged = UnityEngine.Animator.StringToHash("ApcCharged");

		public static readonly int ApcDischarged = UnityEngine.Animator.StringToHash("ApcDischarged");

		public static readonly int ApcCharging = UnityEngine.Animator.StringToHash("ApcCharging");

		public static readonly int ApcDischarging = UnityEngine.Animator.StringToHash("ApcDischarging");
	}

	public class Paths
	{
		private static string _baseFolder = "\\My Games\\Stationeers\\";

		public static string LocalData = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + _baseFolder;
	}

	public class Trading
	{
		public static readonly int GasTrader = UnityEngine.Animator.StringToHash("GasTrader");

		public static readonly int LiquidTrader = UnityEngine.Animator.StringToHash("LiquidTrader");
	}

	public class Version
	{
		public const int ROCKET_PART_BUILD_STATES_ADDED = 25553;

		public const int STOMACHS_ADDED = 27309;
	}
}
