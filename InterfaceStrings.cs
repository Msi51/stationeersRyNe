using Assets.Scripts;
using Assets.Scripts.Objects;
using UnityEngine;

public static class InterfaceStrings
{
	private static readonly int RenameThingHash = Animator.StringToHash("RenameThing");

	private static readonly int SetThingValueHash = Animator.StringToHash("SetThingValue");

	private static readonly int MothershipConstructionHash = Animator.StringToHash("MothershipConstruction");

	private static readonly int MulticonstructorItemHash = Animator.StringToHash("MulticonstructorItem");

	private static readonly int NeedMoreKitHash = Animator.StringToHash("TooltipNeedMoreKit");

	private static readonly int TooltipRotateLeftRightHash = Animator.StringToHash("TooltipRotateLeftRight");

	private static readonly int TooltipRotateUpDownHash = Animator.StringToHash("TooltipRotateUpDown");

	private static readonly int TooltipRollLeftRightHash = Animator.StringToHash("TooltipRollLeftRight");

	private static readonly int UnableToConstructHash = Animator.StringToHash("UnableToConstruct");

	private static readonly int TooltipNumberofBuildStateHash = Animator.StringToHash("BuildStateOfHash");

	private static readonly int TooltipUpgrade2Hash = Animator.StringToHash("TooltipUpgrade2");

	private static readonly int TooltipUpgradeHash = Animator.StringToHash("TooltipUpgrade");

	private static readonly int TooltipAllowsMountingHash = Animator.StringToHash("TooltipAllowsMounting");

	private static readonly int TooltipPlacementSnapGridHash = Animator.StringToHash("TooltipPlacementSnapGrid");

	private static readonly int TooltipPlacementSnapFaceHash = Animator.StringToHash("TooltipPlacementSnapFace");

	private static readonly int TooltipPlacementSnapFaceMountHash = Animator.StringToHash("TooltipPlacementSnapFaceMount");

	private static readonly int TooltipPlacementSnapFaceMountMissingFrameHash = Animator.StringToHash("TooltipPlacementSnapFaceMountMissingFrame");

	private static readonly int TooltipPlacementSnapFaceMountMissingSupportHash = Animator.StringToHash("TooltipPlacementSnapFaceMountMissingSupport");

	private static readonly int TooltipPlacementSnapFaceMountBlockedDynamicHash = Animator.StringToHash("TooltipPlacementSnapFaceMountBlockedDynamic");

	private static readonly int TooltipPlacementSnapFaceMountBlockedFallbackHash = Animator.StringToHash("TooltipPlacementSnapFaceMountBlockedFallback");

	private static readonly int TooltipPlacementSnapFaceNotMountableDynamicHash = Animator.StringToHash("TooltipPlacementSnapFaceNotMountableDynamic");

	private static readonly int TooltipPlacementSnapFaceNotMountableFallbackHash = Animator.StringToHash("TooltipPlacementSnapFaceNotMountableFallback");

	private static readonly int TooltipPlacementSnapFaceWrongFaceDynamicHash = Animator.StringToHash("TooltipPlacementSnapFaceWrongFaceDynamic");

	private static readonly int TooltipPlacementSnapFaceWrongFaceFallbackHash = Animator.StringToHash("TooltipPlacementSnapFaceWrongFaceFallback");

	private static readonly int FurnaceChamberHash = Animator.StringToHash("FurnaceChamber");

	private static readonly int FurnaceChamberTextHash = Animator.StringToHash("FurnaceChamberText");

	private static readonly int FurnaceSmeltingHash = Animator.StringToHash("FurnaceSmelting");

	private static readonly int LogicStateHash = Animator.StringToHash("LogicState");

	private static readonly int LogicNoSettingHash = Animator.StringToHash("LogicNoSetting");

	private static readonly int LogicNoAdditionalSettingsForHash = Animator.StringToHash("LogicNoAdditionalSettingsFor");

	private static readonly int LogicNoSlotHash = Animator.StringToHash("LogicNoSlot");

	private static readonly int LogicNoAvailableSlotsHash = Animator.StringToHash("LogicNoAvailableSlots");

	private static readonly int LogicNoAdditionalSlotsForHash = Animator.StringToHash("LogicNoAdditionalSlotsFor");

	private static readonly int LogicNoDeviceHash = Animator.StringToHash("LogicNoDevice");

	private static readonly int RequiresScrewdriverHash = Animator.StringToHash("RequiresScrewdriver");

	private static readonly int LogicNoReadableDevicesHash = Animator.StringToHash("LogicNoReadableDevices");

	private static readonly int LogicNoWritableDevicesHash = Animator.StringToHash("LogicNoWritableDevices");

	private static readonly int LogicNoReadableTypesHash = Animator.StringToHash("LogicNoReadableTypes");

	private static readonly int ChangeSettingToHash = Animator.StringToHash("ChangeSettingTo");

	private static readonly int ChangeSettingToAllHash = Animator.StringToHash("ChangeSettingToAll");

	private static readonly int HoldForPreviousObjectHash = Animator.StringToHash("HoldForPreviousObject");

	private static readonly int ChangeSettingToForHash = Animator.StringToHash("ChangeSettingToFor");

	private static readonly int ChangeSettingToForInHash = Animator.StringToHash("ChangeSettingToForIn");

	private static readonly int ChangeReagentSettingToHash = Animator.StringToHash("ChangeReagentSettingTo");

	private static readonly int CrowbarToFlipHash = Animator.StringToHash("CrowbarToFlip");

	private static readonly int ChangeSlotSettingToForHash = Animator.StringToHash("ChangeSlotSettingToFor");

	private static readonly int ChangeSettingToForAllHash = Animator.StringToHash("ChangeSettingToForAll");

	private static readonly int InteractionTemporarilyDisabledOnThisObjectHash = Animator.StringToHash("InteractionTemporarilyDisabledOnThisObject");

	private static readonly int UnableToInteractAsYouDoNotHaveTheRequiredAccessCardHash = Animator.StringToHash("UnableToInteractAsYouDoNotHaveTheRequiredAccessCard");

	private static readonly int ConnectionFilteredHash = Animator.StringToHash("ConnectionFiltered");

	private static readonly int ConnectionUnfilteredHash = Animator.StringToHash("ConnectionUnfiltered");

	private static readonly int ConnectionInputHash = Animator.StringToHash("ConnectionInput");

	private static readonly int ConnectionInput2Hash = Animator.StringToHash("ConnectionInput2");

	private static readonly int ConnectionOutputHash = Animator.StringToHash("ConnectionOutput");

	private static readonly int ConnectionOutput2Hash = Animator.StringToHash("ConnectionOutput2");

	private static readonly int ConnectionWasteHash = Animator.StringToHash("ConnectionWaste");

	private static readonly int ConnectionBreathingHash = Animator.StringToHash("ConnectionBreathing");

	private static readonly int ConnectionPropellantHash = Animator.StringToHash("ConnectionPropellant");

	private static readonly int LogicTypeNoneHash = Animator.StringToHash("LogicTypeNone");

	private static readonly int LogicTypePowerHash = Animator.StringToHash("LogicTypePower");

	private static readonly int LogicTypeOpenHash = Animator.StringToHash("LogicTypeOpen");

	private static readonly int LogicTypeModeHash = Animator.StringToHash("LogicTypeMode");

	private static readonly int LogicTypeErrorHash = Animator.StringToHash("LogicTypeError");

	private static readonly int LogicTypeLockHash = Animator.StringToHash("LogicTypeLock");

	private static readonly int LogicTypePressureHash = Animator.StringToHash("LogicTypePressure");

	private static readonly int LogicTypeTemperatureHash = Animator.StringToHash("LogicTypeTemperature");

	private static readonly int LogicTypePressureExternalHash = Animator.StringToHash("LogicTypePressureExternal");

	private static readonly int LogicTypePressureInternalHash = Animator.StringToHash("LogicTypePressureInternal");

	private static readonly int LogicTypeActivateHash = Animator.StringToHash("LogicTypeActivate");

	private static readonly int LogicTypeChargeHash = Animator.StringToHash("LogicTypeCharge");

	private static readonly int LogicTypeSettingHash = Animator.StringToHash("LogicTypeSetting");

	private static readonly int LogicTypeReagentsHash = Animator.StringToHash("LogicTypeReagents");

	private static readonly int LogicTypeRatioOxygenHash = Animator.StringToHash("LogicTypeRatioOxygen");

	private static readonly int LogicTypeRatioCarbonDioxideHash = Animator.StringToHash("LogicTypeRatioCarbonDioxide");

	private static readonly int LogicTypeRatioNitrogenHash = Animator.StringToHash("LogicTypeRatioNitrogen");

	private static readonly int LogicTypeRatioPollutantHash = Animator.StringToHash("LogicTypeRatioPollutant");

	private static readonly int LogicTypeRatioMethaneHash = Animator.StringToHash("LogicTypeRatioMethane");

	private static readonly int LogicTypeRatioWaterHash = Animator.StringToHash("LogicTypeRatioWater");

	private static readonly int LogicTypeRatioPollutedWaterHash = Animator.StringToHash("LogicTypeRatioPollutedWater");

	private static readonly int LogicTypeRatioLiquidNitrogenHash = Animator.StringToHash("LogicTypeRatioLiquidNitrogen");

	private static readonly int LogicTypeCombustionHash = Animator.StringToHash("LogicTypeCombustion");

	private static readonly int LogicTypeRatioNitrousOxideHash = Animator.StringToHash("LogicTypeRatioNitrousOxide");

	private static readonly int LogicTypeHorizontalHash = Animator.StringToHash("LogicTypeHorizontal");

	private static readonly int LogicTypeVerticalHash = Animator.StringToHash("LogicTypeVertical");

	private static readonly int LogicTypeSolarAngleHash = Animator.StringToHash("LogicTypeSolarAngle");

	private static readonly int LogicTypeSolarIrradianceHash = Animator.StringToHash("LogicTypeSolarIrradiance");

	private static readonly int LogicTypeMaximumHash = Animator.StringToHash("LogicTypeMaximum");

	private static readonly int LogicTypeRatioHash = Animator.StringToHash("LogicTypeRatio");

	private static readonly int LogicTypePowerPotentialHash = Animator.StringToHash("LogicTypePowerPotential");

	private static readonly int LogicTypePowerActualHash = Animator.StringToHash("LogicTypePowerActual");

	private static readonly int LogicTypeQuantityHash = Animator.StringToHash("LogicTypeQuantity");

	private static readonly int LogicTypeOnHash = Animator.StringToHash("LogicTypeOn");

	private static readonly int LogicTypeImportQuantityHash = Animator.StringToHash("LogicTypeImportQuantity");

	private static readonly int LogicTypeImportSlotOccupantHash = Animator.StringToHash("LogicTypeImportSlotOccupant");

	private static readonly int LogicTypeExportQuantityHash = Animator.StringToHash("LogicTypeExportQuantity");

	private static readonly int LogicTypeExportSlotOccupantHash = Animator.StringToHash("LogicTypeExportSlotOccupant");

	private static readonly int LogicTypeRequiredPowerHash = Animator.StringToHash("LogicTypeRequiredPower");

	private static readonly int LogicTypeHorizontalRatioHash = Animator.StringToHash("LogicTypeHorizontalRatio");

	private static readonly int LogicTypeVerticalRatioHash = Animator.StringToHash("LogicTypeVerticalRatio");

	private static readonly int LogicTypePowerRequiredHash = Animator.StringToHash("LogicTypePowerRequired");

	private static readonly int LogicTypeIdleHash = Animator.StringToHash("LogicTypeIdle");

	private static readonly int LogicTypeColorHash = Animator.StringToHash("LogicTypeColor");

	private static readonly int LogicTypeElevatorSpeedHash = Animator.StringToHash("LogicTypeElevatorSpeed");

	private static readonly int LogicTypeElevatorLevelHash = Animator.StringToHash("LogicTypeElevatorLevel");

	private static readonly int LogicTypeRecipeHashHash = Animator.StringToHash("LogicTypeRecipeHash");

	private static readonly int LogicTypeExportSlotHashHash = Animator.StringToHash("LogicTypeExportSlotHash");

	private static readonly int LogicTypeImportSlotHashHash = Animator.StringToHash("LogicTypeImportSlotHash");

	private static readonly int LogicTypePlantHealth1Hash = Animator.StringToHash("LogicTypePlantHealth1");

	private static readonly int LogicTypePlantHealth2Hash = Animator.StringToHash("LogicTypePlantHealth2");

	private static readonly int LogicTypePlantHealth3Hash = Animator.StringToHash("LogicTypePlantHealth3");

	private static readonly int LogicTypePlantHealth4Hash = Animator.StringToHash("LogicTypePlantHealth4");

	private static readonly int LogicTypePlantGrowth1Hash = Animator.StringToHash("LogicTypePlantGrowth1");

	private static readonly int LogicTypePlantGrowth2Hash = Animator.StringToHash("LogicTypePlantGrowth2");

	private static readonly int LogicTypePlantGrowth3Hash = Animator.StringToHash("LogicTypePlantGrowth3");

	private static readonly int LogicTypePlantGrowth4Hash = Animator.StringToHash("LogicTypePlantGrowth4");

	private static readonly int LogicTypePlantEfficiency1Hash = Animator.StringToHash("LogicTypePlantEfficiency1");

	private static readonly int LogicTypePlantEfficiency2Hash = Animator.StringToHash("LogicTypePlantEfficiency2");

	private static readonly int LogicTypePlantEfficiency3Hash = Animator.StringToHash("LogicTypePlantEfficiency3");

	private static readonly int LogicTypePlantEfficiency4Hash = Animator.StringToHash("LogicTypePlantEfficiency4");

	private static readonly int LogicTypePlantHash1Hash = Animator.StringToHash("LogicTypePlantHash1");

	private static readonly int LogicTypePlantHash2Hash = Animator.StringToHash("LogicTypePlantHash2");

	private static readonly int LogicTypePlantHash3Hash = Animator.StringToHash("LogicTypePlantHash3");

	private static readonly int LogicTypePlantHash4Hash = Animator.StringToHash("LogicTypePlantHash4");

	private static readonly int LogicTypeRequestHashHash = Animator.StringToHash("LogicTypeRequestHash");

	private static readonly int LogicTypeCompletionRatioHash = Animator.StringToHash("LogicTypeCompletionRatio");

	private static readonly int LogicTypeClearMemoryHash = Animator.StringToHash("LogicTypeClearMemory");

	private static readonly int LogicTypeExportCountHash = Animator.StringToHash("LogicTypeExportCount");

	private static readonly int LogicTypeImportCountHash = Animator.StringToHash("LogicTypeImportCount");

	private static readonly int LogicTypePowerGenerationHash = Animator.StringToHash("LogicTypePowerGeneration");

	private static readonly int LogicTypeTotalMolesHash = Animator.StringToHash("LogicTypeTotalMoles");

	private static readonly int LogicTypeVolumeHash = Animator.StringToHash("LogicTypeVolume");

	private static readonly int LogicTypePlantHash = Animator.StringToHash("LogicTypePlant");

	private static readonly int LogicTypeHarvestHash = Animator.StringToHash("LogicTypeHarvest");

	private static readonly int LogicTypeOutputHash = Animator.StringToHash("LogicTypeOutput");

	private static readonly int LogicTypePressureSettingHash = Animator.StringToHash("LogicTypePressureSetting");

	private static readonly int LogicTypeTemperatureSettingHash = Animator.StringToHash("LogicTypeTemperatureSetting");

	private static readonly int LogicTypeTemperatureExternalHash = Animator.StringToHash("LogicTypeTemperatureExternal");

	private static readonly int LogicTypeFiltrationHash = Animator.StringToHash("LogicTypeFiltration");

	private static readonly int LogicTypeAirReleaseHash = Animator.StringToHash("LogicTypeAirRelease");

	private static readonly int LogicTypePositionXHash = Animator.StringToHash("LogicTypePositionX");

	private static readonly int LogicTypePositionYHash = Animator.StringToHash("LogicTypePositionY");

	private static readonly int LogicTypePositionZHash = Animator.StringToHash("LogicTypePositionZ");

	private static readonly int LogicTypeTargetXHash = Animator.StringToHash("LogicTypeTargetX");

	private static readonly int LogicTypeTargetYHash = Animator.StringToHash("LogicTypeTargetY");

	private static readonly int LogicTypeTargetZHash = Animator.StringToHash("LogicTypeTargetZ");

	private static readonly int LogicTypeSettingInputHash = Animator.StringToHash("LogicTypeSettingInput");

	private static readonly int LogicTypeSettingOutputHash = Animator.StringToHash("LogicTypeSettingOutput");

	private static readonly int LogicTypeVelocityMagnitudeHash = Animator.StringToHash("LogicTypeVelocityMagnitude");

	private static readonly int LogicTypeVelocityRelativeXHash = Animator.StringToHash("LogicTypeVelocityRelativeX");

	private static readonly int LogicTypeVelocityRelativeYHash = Animator.StringToHash("LogicTypeVelocityRelativeY");

	private static readonly int LogicTypeVelocityRelativeZHash = Animator.StringToHash("LogicTypeVelocityRelativeZ");

	private static readonly int LogicTypePrefabHashHash = Animator.StringToHash("LogicTypePrefabHash");

	private static readonly int LogicTypeForceWriteHash = Animator.StringToHash("LogicTypeForceWrite");

	private static readonly int LogicTypeSignalStrengthHash = Animator.StringToHash("LogicTypeSignalStrength");

	private static readonly int LogicTypeSignalIDHash = Animator.StringToHash("LogicTypeSignalID");

	private static readonly int LogicSlotTypeNoneHash = Animator.StringToHash("LogicSlotTypeNone");

	private static readonly int LogicSlotTypeOccupiedHash = Animator.StringToHash("LogicSlotTypeOccupied");

	private static readonly int LogicSlotTypeOccupantHashHash = Animator.StringToHash("LogicSlotTypeOccupantHash");

	private static readonly int LogicSlotTypeQuantityHash = Animator.StringToHash("LogicSlotTypeQuantity");

	private static readonly int LogicSlotTypeDamageHash = Animator.StringToHash("LogicSlotTypeDamage");

	private static readonly int LogicSlotTypeEfficiencyHash = Animator.StringToHash("LogicSlotTypeEfficiency");

	private static readonly int LogicSlotTypeMatureHash = Animator.StringToHash("LogicSlotTypeMature");

	private static readonly int LogicSlotTypeSeedingHash = Animator.StringToHash("LogicSlotTypeSeeding");

	private static readonly int LogicSlotTypeHealthHash = Animator.StringToHash("LogicSlotTypeHealth");

	private static readonly int LogicSlotTypeGrowthHash = Animator.StringToHash("LogicSlotTypeGrowth");

	private static readonly int LogicSlotTypePressureHash = Animator.StringToHash("LogicSlotTypePressure");

	private static readonly int LogicSlotTypeTemperatureHash = Animator.StringToHash("LogicSlotTypeTemperature");

	private static readonly int LogicSlotTypeChargeHash = Animator.StringToHash("LogicSlotTypeCharge");

	private static readonly int LogicSlotTypeChargeRatioHash = Animator.StringToHash("LogicSlotTypeChargeRatio");

	private static readonly int LogicSlotTypeClassHash = Animator.StringToHash("LogicSlotTypeClass");

	private static readonly int LogicSlotTypePressureWasteHash = Animator.StringToHash("LogicSlotTypePressureWaste");

	private static readonly int LogicSlotTypePressureAirHash = Animator.StringToHash("LogicSlotTypePressureAir");

	private static readonly int LogicSlotTypeMaxQuantityHash = Animator.StringToHash("LogicSlotTypeMaxQuantity");

	private static readonly int LogicSlotTypePrefabHashHash = Animator.StringToHash("LogicSlotTypePrefabHash");

	private static readonly int MoleOxygenDescriptionHash = Animator.StringToHash("MoleOxygenDescription");

	private static readonly int MoleNitrogenDescriptionHash = Animator.StringToHash("MoleNitrogenDescription");

	private static readonly int MoleCarbonDioxideDescriptionHash = Animator.StringToHash("MoleCarbonDioxideDescription");

	private static readonly int MoleMethaneDescriptionHash = Animator.StringToHash("MoleMethaneDescription");

	private static readonly int MolePollutantDescriptionHash = Animator.StringToHash("MolePollutantDescription");

	private static readonly int MoleWaterDescriptionHash = Animator.StringToHash("MoleWaterDescription");

	private static readonly int MolePollutedWaterDescriptionHash = Animator.StringToHash("MolePollutedWaterDescription");

	private static readonly int MoleNitrousOxideDescriptionHash = Animator.StringToHash("MoleNitrousOxideDescription");

	private static readonly int MoleLiquidNitrogenDescriptionHash = Animator.StringToHash("MoleLiquidNitrogenDescription");

	private static readonly int MoleLiquidOxygenDescriptionHash = Animator.StringToHash("MoleLiquidOxygenDescription");

	private static readonly int MoleLiquidMethaneDescriptionHash = Animator.StringToHash("MoleLiquidMethaneDescription");

	private static readonly int MoleSteamDescriptionHash = Animator.StringToHash("MoleSteamDescription");

	private static readonly int MoleLiquidCarbonDioxideDescriptionHash = Animator.StringToHash("MoleLiquidCarbonDioxideDescription");

	private static readonly int MoleLiquidPollutantDescriptionHash = Animator.StringToHash("MoleLiquidPollutantDescription");

	private static readonly int MoleLiquidNitrousOxideDescriptionHash = Animator.StringToHash("MoleLiquidNitrousOxideDescription");

	private static readonly int MoleLiquidHydrogenDescriptionHash = Animator.StringToHash("MoleLiquidHydrogenDescription");

	private static readonly int MoleHydrogenDescriptionHash = Animator.StringToHash("MoleHydrogenDescription");

	private static readonly int MoleHydrazineDescriptionHash = Animator.StringToHash("MoleHydrazineDescription");

	private static readonly int MoleLiquidHydrazineDescriptionHash = Animator.StringToHash("MoleLiquidHydrazineDescription");

	private static readonly int MoleLiquidAlcoholDescriptionHash = Animator.StringToHash("MoleLiquidAlcoholDescription");

	private static readonly int MoleHeliumDescriptionHash = Animator.StringToHash("MoleHeliumDescription");

	private static readonly int MoleLiquidSodiumChlorideDescriptionHash = Animator.StringToHash("MoleLiquidSodiumChlorideDescription");

	private static readonly int MoleSilanolDescriptionHash = Animator.StringToHash("MoleSilanolDescription");

	private static readonly int MoleLiquidSilanolDescriptionHash = Animator.StringToHash("MoleLiquidSilanolDescription");

	private static readonly int MoleHydrochloricAcidDescriptionHash = Animator.StringToHash("MoleHydrochloricAcidDescription");

	private static readonly int MoleLiquidHydrochloricAcidDescriptionHash = Animator.StringToHash("MoleLiquidHydrochloricAcidDescription");

	private static readonly int MoleOzoneDescriptionHash = Animator.StringToHash("MoleOzoneDescription");

	private static readonly int MoleLiquidOzoneDescriptionHash = Animator.StringToHash("MoleLiquidOzoneDescription");

	private static readonly int TraderInUseHash = Animator.StringToHash("TraderInUse");

	private static readonly int TraderIsLockedHash = Animator.StringToHash("Locked");

	private static readonly int ResearchPodTypeHash = Animator.StringToHash("ResearchPodTypeMessage");

	private static readonly int GetAutoResearchPodsRequiredHash = Animator.StringToHash("AutoGetResearchPodsRequired");

	private static readonly int GetManualResearchPodsRequiredHash = Animator.StringToHash("ManualGetResearchPodsRequired");

	private static readonly int Activate_Research = Animator.StringToHash("ActivateResearch");

	private static readonly int CurrentStoredPodType = Animator.StringToHash("CurrentResearchPodType");

	private static readonly int MineablesInVicinity = Animator.StringToHash("MineablesInVicinity");

	private static readonly int MineablesInQueue = Animator.StringToHash("MineablesInQueue");

	private static readonly int NextWeatherEventTime = Animator.StringToHash("NextWeatherEventTime");

	private static readonly int FuelInTankRemaining = Animator.StringToHash("FuelInTankRemaining");

	private static readonly int FuelReturnCost = Animator.StringToHash("FuelReturnCost");

	private static readonly int CollectableGoods = Animator.StringToHash("CollectableGoods");

	private static readonly int LogicTypeTimeHash = Animator.StringToHash("LogicTypeTime");

	private static readonly int LogicTypeBpmHash = Animator.StringToHash("LogicTypeBpm");

	private static readonly int LogicTypeEnvironmentEfficiencyHash = Animator.StringToHash("LogicTypeEnvironmentEfficiency");

	private static readonly int LogicTypeWorkingGasEfficiencyHash = Animator.StringToHash("LogicTypeWorkingGasEfficiency");

	private static readonly int LogicNoAudioReceiverDevicesHash = Animator.StringToHash("NoAudioReceiverDevices");

	public static readonly int PitchHash = Animator.StringToHash("Pitch");

	public static readonly int VolumeHash = Animator.StringToHash("Volume");

	public static readonly int SpeedHash = Animator.StringToHash("Speed");

	public static readonly int AttackHash = Animator.StringToHash("Attack");

	public static readonly int ReleaseHash = Animator.StringToHash("Release");

	public static readonly int CurrentInstrumentHash = Animator.StringToHash("CurrentInstrument");

	public static readonly int NoSoundCartridgeHash = Animator.StringToHash("NoSoundCartridge");

	public static readonly int NoNoteHash = Animator.StringToHash("NoNote");

	public static readonly int IntensityHash = Animator.StringToHash("Intensity");

	public static readonly int WaveformHash = Animator.StringToHash("Waveform");

	private static readonly int LogicTypePressureInputHash = Animator.StringToHash("LogicTypePressureInput");

	public static readonly int RadiusHash = Animator.StringToHash("Radius");

	private static readonly int LogicTypeTemperatureInputHash = Animator.StringToHash("LogicTypeTemperatureInput");

	private static readonly int LogicTypeRatioOxygenInputHash = Animator.StringToHash("LogicTypeRatioOxygenInput");

	private static readonly int LogicTypeRatioCarbonDioxideInputHash = Animator.StringToHash("LogicTypeRatioCarbonDioxideInput");

	private static readonly int LogicTypeRatioNitrogenInputHash = Animator.StringToHash("LogicTypeRatioNitrogenInput");

	private static readonly int LogicTypeRatioPollutantInputHash = Animator.StringToHash("LogicTypeRatioPollutantInput");

	private static readonly int LogicTypeRatioMethaneInputHash = Animator.StringToHash("LogicTypeRatioMethaneInput");

	private static readonly int LogicTypeRatioWaterInputHash = Animator.StringToHash("LogicTypeRatioWaterInput");

	private static readonly int LogicTypeRatioNitrousOxideInputHash = Animator.StringToHash("LogicTypeRatioNitrousOxideInput");

	private static readonly int LogicTypeRatioLiquidNitrogenInputHash = Animator.StringToHash("LogicTypeRatioLiquidNitrogenInput");

	private static readonly int LogicTypeCombustionInputHash = Animator.StringToHash("LogicTypeCombustionInput");

	private static readonly int LogicTypeTotalMolesInputHash = Animator.StringToHash("LogicTypeTotalMolesInput");

	private static readonly int LogicTypePressureInput2Hash = Animator.StringToHash("LogicTypePressureInput2");

	private static readonly int LogicTypeTemperatureInput2Hash = Animator.StringToHash("LogicTypeTemperatureInput2");

	private static readonly int LogicTypeRatioOxygenInput2Hash = Animator.StringToHash("LogicTypeRatioOxygenInput2");

	private static readonly int LogicTypeRatioCarbonDioxideInput2Hash = Animator.StringToHash("LogicTypeRatioCarbonDioxideInput2");

	private static readonly int LogicTypeRatioNitrogenInput2Hash = Animator.StringToHash("LogicTypeRatioNitrogenInput2");

	private static readonly int LogicTypeRatioPollutantInput2Hash = Animator.StringToHash("LogicTypeRatioPollutantInput2");

	private static readonly int LogicTypeRatioMethaneInput2Hash = Animator.StringToHash("LogicTypeRatioMethaneInput2");

	private static readonly int LogicTypeRatioWaterInput2Hash = Animator.StringToHash("LogicTypeRatioWaterInput2");

	private static readonly int LogicTypeRatioNitrousOxideInput2Hash = Animator.StringToHash("LogicTypeRatioNitrousOxideInput2");

	private static readonly int LogicTypeRatioLiquidNitrogenInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidNitrogenInput2");

	private static readonly int LogicTypeCombustionInput2Hash = Animator.StringToHash("LogicTypeCombustionInput2");

	private static readonly int LogicTypeTotalMolesInput2Hash = Animator.StringToHash("LogicTypeTotalMolesInput2");

	private static readonly int LogicTypePressureOutputHash = Animator.StringToHash("LogicTypePressureOutput");

	private static readonly int LogicTypeTemperatureOutputHash = Animator.StringToHash("LogicTypeTemperatureOutput");

	private static readonly int LogicTypeRatioOxygenOutputHash = Animator.StringToHash("LogicTypeRatioOxygenOutput");

	private static readonly int LogicTypeRatioCarbonDioxideOutputHash = Animator.StringToHash("LogicTypeRatioCarbonDioxideOutput");

	private static readonly int LogicTypeRatioNitrogenOutputHash = Animator.StringToHash("LogicTypeRatioNitrogenOutput");

	private static readonly int LogicTypeRatioPollutantOutputHash = Animator.StringToHash("LogicTypeRatioPollutantOutput");

	private static readonly int LogicTypeRatioMethaneOutputHash = Animator.StringToHash("LogicTypeRatioMethaneOutput");

	private static readonly int LogicTypeRatioWaterOutputHash = Animator.StringToHash("LogicTypeRatioWaterOutput");

	private static readonly int LogicTypeRatioNitrousOxideOutputHash = Animator.StringToHash("LogicTypeRatioNitrousOxideOutput");

	private static readonly int LogicTypeRatioLiquidNitrogenOutputHash = Animator.StringToHash("LogicTypeRatioLiquidNitrogenOutput");

	private static readonly int LogicTypeCombustionOutputHash = Animator.StringToHash("LogicTypeCombustionOutput");

	private static readonly int LogicTypeTotalMolesOutputHash = Animator.StringToHash("LogicTypeTotalMolesOutput");

	private static readonly int LogicTypePressureOutput2Hash = Animator.StringToHash("LogicTypePressureOutput2");

	private static readonly int LogicTypeTemperatureOutput2Hash = Animator.StringToHash("LogicTypeTemperatureOutput2");

	private static readonly int LogicTypeRatioOxygenOutput2Hash = Animator.StringToHash("LogicTypeRatioOxygenOutput2");

	private static readonly int LogicTypeRatioCarbonDioxideOutput2Hash = Animator.StringToHash("LogicTypeRatioCarbonDioxideOutput2");

	private static readonly int LogicTypeRatioNitrogenOutput2Hash = Animator.StringToHash("LogicTypeRatioNitrogenOutput2");

	private static readonly int LogicTypeRatioPollutantOutput2Hash = Animator.StringToHash("LogicTypeRatioPollutantOutput2");

	private static readonly int LogicTypeRatioMethaneOutput2Hash = Animator.StringToHash("LogicTypeRatioMethaneOutput2");

	private static readonly int LogicTypeRatioWaterOutput2Hash = Animator.StringToHash("LogicTypeRatioWaterOutput2");

	private static readonly int LogicTypeRatioNitrousOxideOutput2Hash = Animator.StringToHash("LogicTypeRatioNitrousOxideOutput2");

	private static readonly int LogicTypeRatioLiquidNitrogenOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidNitrogenOutput2");

	private static readonly int LogicTypeCombustionOutput2Hash = Animator.StringToHash("LogicTypeCombustionOutput2");

	private static readonly int LogicTypeTotalMolesOutput2Hash = Animator.StringToHash("LogicTypeTotalMolesOutput2");

	private static readonly int LogicTypeOperationalTemperatureEfficiencyHash = Animator.StringToHash("LogicTypeOperationalTemperatureEfficiency");

	private static readonly int LogicTypeTemperatureDifferentialEfficiencyHash = Animator.StringToHash("LogicTypeTemperatureDifferentialEfficiency");

	private static readonly int LogicTypePressureEfficiencyHash = Animator.StringToHash("LogicTypePressureEfficiency");

	private static readonly int LogicTypeCombustionLimiterHash = Animator.StringToHash("LogicTypeCombustionLimiter");

	private static readonly int LogicTypeThrottleHash = Animator.StringToHash("LogicTypeThrottle");

	private static readonly int LogicTypeRpmHash = Animator.StringToHash("LogicTypeRpm");

	private static readonly int LogicTypeStressHash = Animator.StringToHash("LogicTypeStress");

	private static readonly int LogicTypeInterrogationProgressHash = Animator.StringToHash("LogicTypeInterrogationProgress");

	private static readonly int LogicTypeTargetPadIndexHash = Animator.StringToHash("LogicTypeTargetPadIndex");

	private static readonly int LogicTypeSizeXHash = Animator.StringToHash("LogicTypeSizeX");

	private static readonly int LogicTypeSizeYHash = Animator.StringToHash("LogicTypeSizeY");

	private static readonly int LogicTypeSizeZHash = Animator.StringToHash("LogicTypeSizeZ");

	private static readonly int LogicTypeMinWattsToContactHash = Animator.StringToHash("LogicTypeMinWattsToContact");

	private static readonly int LogicTypeWattsReachingContactHash = Animator.StringToHash("LogicTypeWattsReachingContact");

	private static readonly int LogicTypeLogicTypeChannelHash = Animator.StringToHash("LogicTypeChannel");

	private static readonly int LogicTypeLogicTypeFlushHash = Animator.StringToHash("LogicTypeFlush");

	private static readonly int LogicTypeLogicTypeSoundAlertHash = Animator.StringToHash("LogicTypeSoundAlert");

	private static readonly int LogicTypeVolumeOfLiquidHash = Animator.StringToHash("LogicTypeVolumeOfLiquid");

	private static readonly int RpmHash = Animator.StringToHash("Rpm");

	private static readonly int StressHash = Animator.StringToHash("Stress");

	private static readonly int ThrottleHash = Animator.StringToHash("Throttle");

	private static readonly int CombustionLimiterHash = Animator.StringToHash("CombustionLimiter");

	private static readonly int LogicTypeLineNumberHash = Animator.StringToHash("LogicTypeLineNumber");

	private static readonly int LogicTypeUnknownHash = Animator.StringToHash("LogicTypeUnknown");

	private static readonly int LogicTypeRatioLiquidOxygenHash = Animator.StringToHash("LogicTypeRatioLiquidOxygen");

	private static readonly int LogicTypeRatioLiquidOxygenOutputHash = Animator.StringToHash("LogicTypeRatioLiquidOxygenOutput");

	private static readonly int LogicTypeRatioLiquidOxygenOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidOxygenOutput2");

	private static readonly int LogicTypeRatioLiquidOxygenInputHash = Animator.StringToHash("LogicTypeRatioLiquidOxygenInput");

	private static readonly int LogicTypeRatioLiquidOxygenInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidOxygenInput2");

	private static readonly int LogicTypeRatioLiquidMethaneHash = Animator.StringToHash("LogicTypeRatioLiquidMethane");

	private static readonly int LogicTypeRatioLiquidMethaneOutputHash = Animator.StringToHash("LogicTypeRatioLiquidMethaneOutput");

	private static readonly int LogicTypeRatioLiquidMethaneOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidMethaneOutput2");

	private static readonly int LogicTypeRatioLiquidMethaneInputHash = Animator.StringToHash("LogicTypeRatioLiquidMethaneInput");

	private static readonly int LogicTypeRatioLiquidMethaneInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidMethaneInput2");

	private static readonly int LogicTypeRatioSteamHash = Animator.StringToHash("LogicTypeRatioSteam");

	private static readonly int LogicTypeRatioSteamOutputHash = Animator.StringToHash("LogicTypeRatioSteamOutput");

	private static readonly int LogicTypeRatioSteamOutput2Hash = Animator.StringToHash("LogicTypeRatioSteamOutput2");

	private static readonly int LogicTypeRatioSteamInputHash = Animator.StringToHash("LogicTypeRatioSteamInput");

	private static readonly int LogicTypeRatioSteamInput2Hash = Animator.StringToHash("LogicTypeRatioSteamInput2");

	private static readonly int LogicTypeContactTypeIdHash = Animator.StringToHash("LogicTypeContactTypeId");

	private static readonly int LogicTypeRatioLiquidCarbonDioxideHash = Animator.StringToHash("LogicTypeRatioLiquidCarbonDioxide");

	private static readonly int LogicTypeRatioLiquidCarbonDioxideOutputHash = Animator.StringToHash("LogicTypeRatioLiquidCarbonDioxideOutput");

	private static readonly int LogicTypeRatioLiquidCarbonDioxideOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidCarbonDioxideOutput2");

	private static readonly int LogicTypeRatioLiquidCarbonDioxideInputHash = Animator.StringToHash("LogicTypeRatioLiquidCarbonDioxideInput");

	private static readonly int LogicTypeRatioLiquidCarbonDioxideInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidCarbonDioxideInput2");

	private static readonly int LogicTypeRatioLiquidPollutantHash = Animator.StringToHash("LogicTypeRatioLiquidPollutant");

	private static readonly int LogicTypeRatioLiquidPollutantOutputHash = Animator.StringToHash("LogicTypeRatioLiquidPollutantOutput");

	private static readonly int LogicTypeRatioLiquidPollutantOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidPollutantOutput2");

	private static readonly int LogicTypeRatioLiquidPollutantInputHash = Animator.StringToHash("LogicTypeRatioLiquidPollutantInput");

	private static readonly int LogicTypeRatioLiquidPollutantInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidPollutantInput2");

	private static readonly int LogicTypeRatioLiquidNitrousOxideHash = Animator.StringToHash("LogicTypeRatioLiquidNitrousOxide");

	private static readonly int LogicTypeRatioLiquidNitrousOxideOutputHash = Animator.StringToHash("LogicTypeRatioLiquidNitrousOxideOutput");

	private static readonly int LogicTypeRatioLiquidNitrousOxideOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidNitrousOxideOutput2");

	private static readonly int LogicTypeRatioLiquidNitrousOxideInputHash = Animator.StringToHash("LogicTypeRatioLiquidNitrousOxideInput");

	private static readonly int LogicTypeRatioLiquidNitrousOxideInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidNitrousOxideInput2");

	private static readonly int LogicTypeRatioHydrogenHash = Animator.StringToHash("LogicTypeRatioHydrogen");

	private static readonly int LogicTypeRatioLiquidHydrogenHash = Animator.StringToHash("LogicTypeRatioLiquidHydrogen");

	private static readonly int LogicTypeProgressHash = Animator.StringToHash("LogicTypeProgress");

	private static readonly int LogicTypeDestinationCodeHash = Animator.StringToHash("LogicTypeDestinationCode");

	private static readonly int LogicTypeAccelerationHash = Animator.StringToHash("LogicTypeAcceleration");

	private static readonly int LogicTypeAutoShutOffHash = Animator.StringToHash("LogicTypeAutoShutOff");

	private static readonly int LogicTypeThrustHash = Animator.StringToHash("LogicTypeThrust");

	private static readonly int LogicTypeThrustToWeightHash = Animator.StringToHash("LogicTypeThrustToWeight");

	private static readonly int LogicTypeOverShootTargetHash = Animator.StringToHash("LogicTypeOverShootTarget");

	private static readonly int LogicTypeTimeToDestinationHash = Animator.StringToHash("LogicTypeTimeToDestination");

	private static readonly int LogicTypeBurnTimeRemainingHash = Animator.StringToHash("LogicTypeBurnTimeRemaining");

	private static readonly int LogicTypeDryMassHash = Animator.StringToHash("LogicTypeDryMass");

	private static readonly int LogicTypeMassHash = Animator.StringToHash("LogicTypeMass");

	private static readonly int LogicTypeWeightHash = Animator.StringToHash("LogicTypeWeight");

	private static readonly int LogicTypeAutoLandHash = Animator.StringToHash("LogicTypeAutoLand");

	private static readonly int LogicTypeContactSlotIndexHash = Animator.StringToHash("LogicTypeContactSlotIndex");

	private static readonly int LogicTypeRatioHydrazineHash = Animator.StringToHash("LogicTypeRatioHydrazine");

	private static readonly int LogicTypeRatioLiquidHydrazineHash = Animator.StringToHash("LogicTypeRatioLiquidHydrazine");

	private static readonly int LogicTypeRatioLiquidAlcoholHash = Animator.StringToHash("LogicTypeRatioLiquidAlcohol");

	private static readonly int LogicTypeRatioHeliumHash = Animator.StringToHash("LogicTypeRatioHelium");

	private static readonly int LogicTypeRatioLiquidSodiumChlorideHash = Animator.StringToHash("LogicTypeRatioLiquidSodiumChloride");

	private static readonly int LogicTypeRatioSilanolHash = Animator.StringToHash("LogicTypeRatioSilanol");

	private static readonly int LogicTypeRatioLiquidSilanolHash = Animator.StringToHash("LogicTypeRatioLiquidSilanol");

	private static readonly int LogicTypeRatioHydrochloricAcidHash = Animator.StringToHash("LogicTypeRatioHydrochloricAcid");

	private static readonly int LogicTypeRatioLiquidHydrochloricAcidHash = Animator.StringToHash("LogicTypeRatioLiquidHydrochloricAcid");

	private static readonly int LogicTypeRatioOzoneHash = Animator.StringToHash("LogicTypeRatioOzone");

	private static readonly int LogicTypeRatioLiquidOzoneHash = Animator.StringToHash("LogicTypeRatioLiquidOzone");

	private static readonly int LogicTypeRatioHydrogenInputHash = Animator.StringToHash("LogicTypeRatioHydrogenInput");

	private static readonly int LogicTypeRatioHydrogenInput2Hash = Animator.StringToHash("LogicTypeRatioHydrogenInput2");

	private static readonly int LogicTypeRatioHydrogenOutputHash = Animator.StringToHash("LogicTypeRatioHydrogenOutput");

	private static readonly int LogicTypeRatioHydrogenOutput2Hash = Animator.StringToHash("LogicTypeRatioHydrogenOutput2");

	private static readonly int LogicTypeRatioLiquidHydrogenInputHash = Animator.StringToHash("LogicTypeRatioLiquidHydrogenInput");

	private static readonly int LogicTypeRatioLiquidHydrogenInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidHydrogenInput2");

	private static readonly int LogicTypeRatioLiquidHydrogenOutputHash = Animator.StringToHash("LogicTypeRatioLiquidHydrogenOutput");

	private static readonly int LogicTypeRatioLiquidHydrogenOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidHydrogenOutput2");

	private static readonly int LogicTypeRatioPollutedWaterInputHash = Animator.StringToHash("LogicTypeRatioPollutedWaterInput");

	private static readonly int LogicTypeRatioPollutedWaterInput2Hash = Animator.StringToHash("LogicTypeRatioPollutedWaterInput2");

	private static readonly int LogicTypeRatioPollutedWaterOutputHash = Animator.StringToHash("LogicTypeRatioPollutedWaterOutput");

	private static readonly int LogicTypeRatioPollutedWaterOutput2Hash = Animator.StringToHash("LogicTypeRatioPollutedWaterOutput2");

	private static readonly int LogicTypeRatioHydrazineInputHash = Animator.StringToHash("LogicTypeRatioHydrazineInput");

	private static readonly int LogicTypeRatioHydrazineInput2Hash = Animator.StringToHash("LogicTypeRatioHydrazineInput2");

	private static readonly int LogicTypeRatioHydrazineOutputHash = Animator.StringToHash("LogicTypeRatioHydrazineOutput");

	private static readonly int LogicTypeRatioHydrazineOutput2Hash = Animator.StringToHash("LogicTypeRatioHydrazineOutput2");

	private static readonly int LogicTypeRatioLiquidHydrazineInputHash = Animator.StringToHash("LogicTypeRatioLiquidHydrazineInput");

	private static readonly int LogicTypeRatioLiquidHydrazineInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidHydrazineInput2");

	private static readonly int LogicTypeRatioLiquidHydrazineOutputHash = Animator.StringToHash("LogicTypeRatioLiquidHydrazineOutput");

	private static readonly int LogicTypeRatioLiquidHydrazineOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidHydrazineOutput2");

	private static readonly int LogicTypeRatioLiquidAlcoholInputHash = Animator.StringToHash("LogicTypeRatioLiquidAlcoholInput");

	private static readonly int LogicTypeRatioLiquidAlcoholInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidAlcoholInput2");

	private static readonly int LogicTypeRatioLiquidAlcoholOutputHash = Animator.StringToHash("LogicTypeRatioLiquidAlcoholOutput");

	private static readonly int LogicTypeRatioLiquidAlcoholOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidAlcoholOutput2");

	private static readonly int LogicTypeRatioHeliumInputHash = Animator.StringToHash("LogicTypeRatioHeliumInput");

	private static readonly int LogicTypeRatioHeliumInput2Hash = Animator.StringToHash("LogicTypeRatioHeliumInput2");

	private static readonly int LogicTypeRatioHeliumOutputHash = Animator.StringToHash("LogicTypeRatioHeliumOutput");

	private static readonly int LogicTypeRatioHeliumOutput2Hash = Animator.StringToHash("LogicTypeRatioHeliumOutput2");

	private static readonly int LogicTypeRatioLiquidSodiumChlorideInputHash = Animator.StringToHash("LogicTypeRatioLiquidSodiumChlorideInput");

	private static readonly int LogicTypeRatioLiquidSodiumChlorideInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidSodiumChlorideInput2");

	private static readonly int LogicTypeRatioLiquidSodiumChlorideOutputHash = Animator.StringToHash("LogicTypeRatioLiquidSodiumChlorideOutput");

	private static readonly int LogicTypeRatioLiquidSodiumChlorideOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidSodiumChlorideOutput2");

	private static readonly int LogicTypeRatioSilanolInputHash = Animator.StringToHash("LogicTypeRatioSilanolInput");

	private static readonly int LogicTypeRatioSilanolInput2Hash = Animator.StringToHash("LogicTypeRatioSilanolInput2");

	private static readonly int LogicTypeRatioSilanolOutputHash = Animator.StringToHash("LogicTypeRatioSilanolOutput");

	private static readonly int LogicTypeRatioSilanolOutput2Hash = Animator.StringToHash("LogicTypeRatioSilanolOutput2");

	private static readonly int LogicTypeRatioLiquidSilanolInputHash = Animator.StringToHash("LogicTypeRatioLiquidSilanolInput");

	private static readonly int LogicTypeRatioLiquidSilanolInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidSilanolInput2");

	private static readonly int LogicTypeRatioLiquidSilanolOutputHash = Animator.StringToHash("LogicTypeRatioLiquidSilanolOutput");

	private static readonly int LogicTypeRatioLiquidSilanolOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidSilanolOutput2");

	private static readonly int LogicTypeRatioHydrochloricAcidInputHash = Animator.StringToHash("LogicTypeRatioHydrochloricAcidInput");

	private static readonly int LogicTypeRatioHydrochloricAcidInput2Hash = Animator.StringToHash("LogicTypeRatioHydrochloricAcidInput2");

	private static readonly int LogicTypeRatioHydrochloricAcidOutputHash = Animator.StringToHash("LogicTypeRatioHydrochloricAcidOutput");

	private static readonly int LogicTypeRatioHydrochloricAcidOutput2Hash = Animator.StringToHash("LogicTypeRatioHydrochloricAcidOutput2");

	private static readonly int LogicTypeRatioLiquidHydrochloricAcidInputHash = Animator.StringToHash("LogicTypeRatioLiquidHydrochloricAcidInput");

	private static readonly int LogicTypeRatioLiquidHydrochloricAcidInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidHydrochloricAcidInput2");

	private static readonly int LogicTypeRatioLiquidHydrochloricAcidOutputHash = Animator.StringToHash("LogicTypeRatioLiquidHydrochloricAcidOutput");

	private static readonly int LogicTypeRatioLiquidHydrochloricAcidOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidHydrochloricAcidOutput2");

	private static readonly int LogicTypeRatioOzoneInputHash = Animator.StringToHash("LogicTypeRatioOzoneInput");

	private static readonly int LogicTypeRatioOzoneInput2Hash = Animator.StringToHash("LogicTypeRatioOzoneInput2");

	private static readonly int LogicTypeRatioOzoneOutputHash = Animator.StringToHash("LogicTypeRatioOzoneOutput");

	private static readonly int LogicTypeRatioOzoneOutput2Hash = Animator.StringToHash("LogicTypeRatioOzoneOutput2");

	private static readonly int LogicTypeRatioLiquidOzoneInputHash = Animator.StringToHash("LogicTypeRatioLiquidOzoneInput");

	private static readonly int LogicTypeRatioLiquidOzoneInput2Hash = Animator.StringToHash("LogicTypeRatioLiquidOzoneInput2");

	private static readonly int LogicTypeRatioLiquidOzoneOutputHash = Animator.StringToHash("LogicTypeRatioLiquidOzoneOutput");

	private static readonly int LogicTypeRatioLiquidOzoneOutput2Hash = Animator.StringToHash("LogicTypeRatioLiquidOzoneOutput2");

	public static string RenameThing => Localization.GetInterface(RenameThingHash);

	public static string SetThingValue => Localization.GetInterface(SetThingValueHash);

	public static string MothershipConstruction => Localization.GetInterface(MothershipConstructionHash);

	public static string MulticonstructorItem => Localization.GetInterface(MulticonstructorItemHash);

	public static string NeedMoreKit => Localization.GetInterface(NeedMoreKitHash);

	public static string TooltipRotateLeftRight => Localization.GetInterface(TooltipRotateLeftRightHash);

	public static string TooltipRotateUpDown => Localization.GetInterface(TooltipRotateUpDownHash);

	public static string TooltipRollLeftRight => Localization.GetInterface(TooltipRollLeftRightHash);

	public static string UnableToConstruct => Localization.GetInterface(UnableToConstructHash);

	public static string TooltipNumberofBuildState => Localization.GetInterface(TooltipNumberofBuildStateHash);

	public static string TooltipUpgrade2 => Localization.GetInterface(TooltipUpgrade2Hash);

	public static string TooltipUpgrade => Localization.GetInterface(TooltipUpgradeHash);

	public static string TooltipAllowsMounting => Localization.GetInterface(TooltipAllowsMountingHash);

	public static string TooltipPlacementSnapGrid => Localization.GetInterface(TooltipPlacementSnapGridHash);

	public static string TooltipPlacementSnapFace => Localization.GetInterface(TooltipPlacementSnapFaceHash);

	public static string TooltipPlacementSnapFaceMount => Localization.GetInterface(TooltipPlacementSnapFaceMountHash);

	public static string TooltipPlacementSnapFaceMountMissingFrame => Localization.GetInterface(TooltipPlacementSnapFaceMountMissingFrameHash);

	public static string TooltipPlacementSnapFaceMountMissingSupport => Localization.GetInterface(TooltipPlacementSnapFaceMountMissingSupportHash);

	public static string FurnaceChamber => Localization.GetInterface(FurnaceChamberHash);

	public static string FurnaceChamberText => Localization.GetInterface(FurnaceChamberTextHash);

	public static string FurnaceSmelting => Localization.GetInterface(FurnaceSmeltingHash);

	public static string LogicState => Localization.GetInterface(LogicStateHash);

	public static string LogicNoSetting => Localization.GetInterface(LogicNoSettingHash);

	public static string LogicNoAdditionalSettingsFor => Localization.GetInterface(LogicNoAdditionalSettingsForHash);

	public static string LogicNoSlot => Localization.GetInterface(LogicNoSlotHash);

	public static string LogicNoAvailableSlots => Localization.GetInterface(LogicNoAvailableSlotsHash);

	public static string LogicNoAdditionalSlotsFor => Localization.GetInterface(LogicNoAdditionalSlotsForHash);

	public static string LogicNoDevice => Localization.GetInterface(LogicNoDeviceHash);

	public static string RequiresScrewdriver => Localization.GetInterface(RequiresScrewdriverHash);

	public static string LogicNoReadableDevices => Localization.GetInterface(LogicNoReadableDevicesHash);

	public static string LogicNoWritableDevices => Localization.GetInterface(LogicNoWritableDevicesHash);

	public static string LogicNoReadableTypes => Localization.GetInterface(LogicNoReadableTypesHash);

	public static string ChangeSettingTo => Localization.GetInterface(ChangeSettingToHash);

	public static string ChangeSettingToAll => Localization.GetInterface(ChangeSettingToAllHash);

	public static string HoldForPreviousObject => Localization.GetInterface(HoldForPreviousObjectHash);

	public static string ChangeSettingToFor => Localization.GetInterface(ChangeSettingToForHash);

	public static string ChangeSettingToForIn => Localization.GetInterface(ChangeSettingToForInHash);

	public static string ChangeReagentSettingTo => Localization.GetInterface(ChangeReagentSettingToHash);

	public static string CrowbarToFlip => Localization.GetInterface(CrowbarToFlipHash);

	public static string ChangeSlotSettingToFor => Localization.GetInterface(ChangeSlotSettingToForHash);

	public static string ChangeSettingToForAll => Localization.GetInterface(ChangeSettingToForAllHash);

	public static string InteractionTemporarilyDisabledOnThisObject => Localization.GetInterface(InteractionTemporarilyDisabledOnThisObjectHash);

	public static string UnableToInteractAsYouDoNotHaveTheRequiredAccessCard => Localization.GetInterface(UnableToInteractAsYouDoNotHaveTheRequiredAccessCardHash);

	public static string ConnectionFiltered => Localization.GetInterface(ConnectionFilteredHash);

	public static string ConnectionUnfiltered => Localization.GetInterface(ConnectionUnfilteredHash);

	public static string ConnectionInput => Localization.GetInterface(ConnectionInputHash);

	public static string ConnectionInput2 => Localization.GetInterface(ConnectionInput2Hash);

	public static string ConnectionOutput => Localization.GetInterface(ConnectionOutputHash);

	public static string ConnectionOutput2 => Localization.GetInterface(ConnectionOutput2Hash);

	public static string ConnectionWaste => Localization.GetInterface(ConnectionWasteHash);

	public static string ConnectionBreathing => Localization.GetInterface(ConnectionBreathingHash);

	public static string ConnectionPropellant => Localization.GetInterface(ConnectionPropellantHash);

	public static string LogicTypeNone => Localization.GetInterface(LogicTypeNoneHash);

	public static string LogicTypePower => Localization.GetInterface(LogicTypePowerHash);

	public static string LogicTypeOpen => Localization.GetInterface(LogicTypeOpenHash);

	public static string LogicTypeMode => Localization.GetInterface(LogicTypeModeHash);

	public static string LogicTypeError => Localization.GetInterface(LogicTypeErrorHash);

	public static string LogicTypeLock => Localization.GetInterface(LogicTypeLockHash);

	public static string LogicTypePressure => Localization.GetInterface(LogicTypePressureHash);

	public static string LogicTypeTemperature => Localization.GetInterface(LogicTypeTemperatureHash);

	public static string LogicTypePressureExternal => Localization.GetInterface(LogicTypePressureExternalHash);

	public static string LogicTypePressureInternal => Localization.GetInterface(LogicTypePressureInternalHash);

	public static string LogicTypeActivate => Localization.GetInterface(LogicTypeActivateHash);

	public static string LogicTypeCharge => Localization.GetInterface(LogicTypeChargeHash);

	public static string LogicTypeSetting => Localization.GetInterface(LogicTypeSettingHash);

	public static string LogicTypeReagents => Localization.GetInterface(LogicTypeReagentsHash);

	public static string LogicTypeRatioOxygen => Localization.GetInterface(LogicTypeRatioOxygenHash);

	public static string LogicTypeRatioCarbonDioxide => Localization.GetInterface(LogicTypeRatioCarbonDioxideHash);

	public static string LogicTypeRatioNitrogen => Localization.GetInterface(LogicTypeRatioNitrogenHash);

	public static string LogicTypeRatioPollutant => Localization.GetInterface(LogicTypeRatioPollutantHash);

	public static string LogicTypeRatioMethane => Localization.GetInterface(LogicTypeRatioMethaneHash);

	public static string LogicTypeRatioWater => Localization.GetInterface(LogicTypeRatioWaterHash);

	public static string LogicTypeRatioPollutedWater => Localization.GetInterface(LogicTypeRatioPollutedWaterHash);

	public static string LogicTypeRatioLiquidNitrogen => Localization.GetInterface(LogicTypeRatioLiquidNitrogenHash);

	public static string LogicTypeCombustion => Localization.GetInterface(LogicTypeCombustionHash);

	public static string LogicTypeRatioNitrousOxide => Localization.GetInterface(LogicTypeRatioNitrousOxideHash);

	public static string LogicTypeHorizontal => Localization.GetInterface(LogicTypeHorizontalHash);

	public static string LogicTypeVertical => Localization.GetInterface(LogicTypeVerticalHash);

	public static string LogicTypeSolarAngle => Localization.GetInterface(LogicTypeSolarAngleHash);

	public static string LogicTypeSolarIrradiance => Localization.GetInterface(LogicTypeSolarIrradianceHash);

	public static string LogicTypeMaximum => Localization.GetInterface(LogicTypeMaximumHash);

	public static string LogicTypeRatio => Localization.GetInterface(LogicTypeRatioHash);

	public static string LogicTypePowerPotential => Localization.GetInterface(LogicTypePowerPotentialHash);

	public static string LogicTypePowerActual => Localization.GetInterface(LogicTypePowerActualHash);

	public static string LogicTypeQuantity => Localization.GetInterface(LogicTypeQuantityHash);

	public static string LogicTypeOn => Localization.GetInterface(LogicTypeOnHash);

	public static string LogicTypeImportQuantity => Localization.GetInterface(LogicTypeImportQuantityHash);

	public static string LogicTypeImportSlotOccupant => Localization.GetInterface(LogicTypeImportSlotOccupantHash);

	public static string LogicTypeExportQuantity => Localization.GetInterface(LogicTypeExportQuantityHash);

	public static string LogicTypeExportSlotOccupant => Localization.GetInterface(LogicTypeExportSlotOccupantHash);

	public static string LogicTypeRequiredPower => Localization.GetInterface(LogicTypeRequiredPowerHash);

	public static string LogicTypeHorizontalRatio => Localization.GetInterface(LogicTypeHorizontalRatioHash);

	public static string LogicTypeVerticalRatio => Localization.GetInterface(LogicTypeVerticalRatioHash);

	public static string LogicTypePowerRequired => Localization.GetInterface(LogicTypePowerRequiredHash);

	public static string LogicTypeIdle => Localization.GetInterface(LogicTypeIdleHash);

	public static string LogicTypeColor => Localization.GetInterface(LogicTypeColorHash);

	public static string LogicTypeElevatorSpeed => Localization.GetInterface(LogicTypeElevatorSpeedHash);

	public static string LogicTypeElevatorLevel => Localization.GetInterface(LogicTypeElevatorLevelHash);

	public static string LogicTypeRecipeHash => Localization.GetInterface(LogicTypeRecipeHashHash);

	public static string LogicTypeExportSlotHash => Localization.GetInterface(LogicTypeExportSlotHashHash);

	public static string LogicTypeImportSlotHash => Localization.GetInterface(LogicTypeImportSlotHashHash);

	public static string LogicTypePlantHealth1 => Localization.GetInterface(LogicTypePlantHealth1Hash);

	public static string LogicTypePlantHealth2 => Localization.GetInterface(LogicTypePlantHealth2Hash);

	public static string LogicTypePlantHealth3 => Localization.GetInterface(LogicTypePlantHealth3Hash);

	public static string LogicTypePlantHealth4 => Localization.GetInterface(LogicTypePlantHealth4Hash);

	public static string LogicTypePlantGrowth1 => Localization.GetInterface(LogicTypePlantGrowth1Hash);

	public static string LogicTypePlantGrowth2 => Localization.GetInterface(LogicTypePlantGrowth2Hash);

	public static string LogicTypePlantGrowth3 => Localization.GetInterface(LogicTypePlantGrowth3Hash);

	public static string LogicTypePlantGrowth4 => Localization.GetInterface(LogicTypePlantGrowth4Hash);

	public static string LogicTypePlantEfficiency1 => Localization.GetInterface(LogicTypePlantEfficiency1Hash);

	public static string LogicTypePlantEfficiency2 => Localization.GetInterface(LogicTypePlantEfficiency2Hash);

	public static string LogicTypePlantEfficiency3 => Localization.GetInterface(LogicTypePlantEfficiency3Hash);

	public static string LogicTypePlantEfficiency4 => Localization.GetInterface(LogicTypePlantEfficiency4Hash);

	public static string LogicTypePlantHash1 => Localization.GetInterface(LogicTypePlantHash1Hash);

	public static string LogicTypePlantHash2 => Localization.GetInterface(LogicTypePlantHash2Hash);

	public static string LogicTypePlantHash3 => Localization.GetInterface(LogicTypePlantHash3Hash);

	public static string LogicTypePlantHash4 => Localization.GetInterface(LogicTypePlantHash4Hash);

	public static string LogicTypeRequestHash => Localization.GetInterface(LogicTypeRequestHashHash);

	public static string LogicTypeCompletionRatio => Localization.GetInterface(LogicTypeCompletionRatioHash);

	public static string LogicTypeClearMemory => Localization.GetInterface(LogicTypeClearMemoryHash);

	public static string LogicTypeExportCount => Localization.GetInterface(LogicTypeExportCountHash);

	public static string LogicTypeImportCount => Localization.GetInterface(LogicTypeImportCountHash);

	public static string LogicTypePowerGeneration => Localization.GetInterface(LogicTypePowerGenerationHash);

	public static string LogicTypeTotalMoles => Localization.GetInterface(LogicTypeTotalMolesHash);

	public static string LogicTypeVolume => Localization.GetInterface(LogicTypeVolumeHash);

	public static string LogicTypePlant => Localization.GetInterface(LogicTypePlantHash);

	public static string LogicTypeHarvest => Localization.GetInterface(LogicTypeHarvestHash);

	public static string LogicTypeOutput => Localization.GetInterface(LogicTypeOutputHash);

	public static string LogicTypePressureSetting => Localization.GetInterface(LogicTypePressureSettingHash);

	public static string LogicTypeTemperatureSetting => Localization.GetInterface(LogicTypeTemperatureSettingHash);

	public static string LogicTypeTemperatureExternal => Localization.GetInterface(LogicTypeTemperatureExternalHash);

	public static string LogicTypeFiltration => Localization.GetInterface(LogicTypeFiltrationHash);

	public static string LogicTypeAirRelease => Localization.GetInterface(LogicTypeAirReleaseHash);

	public static string LogicTypePositionX => Localization.GetInterface(LogicTypePositionXHash);

	public static string LogicTypePositionY => Localization.GetInterface(LogicTypePositionYHash);

	public static string LogicTypePositionZ => Localization.GetInterface(LogicTypePositionZHash);

	public static string LogicTypeTargetX => Localization.GetInterface(LogicTypeTargetXHash);

	public static string LogicTypeTargetY => Localization.GetInterface(LogicTypeTargetYHash);

	public static string LogicTypeTargetZ => Localization.GetInterface(LogicTypeTargetZHash);

	public static string LogicTypeSettingInput => Localization.GetInterface(LogicTypeSettingInputHash);

	public static string LogicTypeSettingOutput => Localization.GetInterface(LogicTypeSettingOutputHash);

	public static string LogicTypeVelocityMagnitude => Localization.GetInterface(LogicTypeVelocityMagnitudeHash);

	public static string LogicTypeVelocityRelativeX => Localization.GetInterface(LogicTypeVelocityRelativeXHash);

	public static string LogicTypeVelocityRelativeY => Localization.GetInterface(LogicTypeVelocityRelativeYHash);

	public static string LogicTypeVelocityRelativeZ => Localization.GetInterface(LogicTypeVelocityRelativeZHash);

	public static string LogicTypePrefabHash => Localization.GetInterface(LogicTypePrefabHashHash);

	public static string LogicTypeForceWrite => Localization.GetInterface(LogicTypeForceWriteHash);

	public static string LogicTypeSignalStrength => Localization.GetInterface(LogicTypeSignalStrengthHash);

	public static string LogicTypeSignalID => Localization.GetInterface(LogicTypeSignalIDHash);

	public static string LogicSlotTypeNone => Localization.GetInterface(LogicSlotTypeNoneHash);

	public static string LogicSlotTypeOccupied => Localization.GetInterface(LogicSlotTypeOccupiedHash);

	public static string LogicSlotTypeOccupantHash => Localization.GetInterface(LogicSlotTypeOccupantHashHash);

	public static string LogicSlotTypeQuantity => Localization.GetInterface(LogicSlotTypeQuantityHash);

	public static string LogicSlotTypeDamage => Localization.GetInterface(LogicSlotTypeDamageHash);

	public static string LogicSlotTypeEfficiency => Localization.GetInterface(LogicSlotTypeEfficiencyHash);

	public static string LogicSlotTypeMature => Localization.GetInterface(LogicSlotTypeMatureHash);

	public static string LogicSlotTypeSeeding => Localization.GetInterface(LogicSlotTypeSeedingHash);

	public static string LogicSlotTypeHealth => Localization.GetInterface(LogicSlotTypeHealthHash);

	public static string LogicSlotTypeGrowth => Localization.GetInterface(LogicSlotTypeGrowthHash);

	public static string LogicSlotTypePressure => Localization.GetInterface(LogicSlotTypePressureHash);

	public static string LogicSlotTypeTemperature => Localization.GetInterface(LogicSlotTypeTemperatureHash);

	public static string LogicSlotTypeCharge => Localization.GetInterface(LogicSlotTypeChargeHash);

	public static string LogicSlotTypeChargeRatio => Localization.GetInterface(LogicSlotTypeChargeRatioHash);

	public static string LogicSlotTypeClass => Localization.GetInterface(LogicSlotTypeClassHash);

	public static string LogicSlotTypePressureWaste => Localization.GetInterface(LogicSlotTypePressureWasteHash);

	public static string LogicSlotTypePressureAir => Localization.GetInterface(LogicSlotTypePressureAirHash);

	public static string LogicSlotTypeMaxQuantity => Localization.GetInterface(LogicSlotTypeMaxQuantityHash);

	public static string LogicSlotTypePrefabHash => Localization.GetInterface(LogicSlotTypePrefabHashHash);

	public static string MoleOxygenDescription => Localization.GetInterface(MoleOxygenDescriptionHash);

	public static string MoleNitrogenDescription => Localization.GetInterface(MoleNitrogenDescriptionHash);

	public static string MoleCarbonDioxideDescription => Localization.GetInterface(MoleCarbonDioxideDescriptionHash);

	public static string MoleMethaneDescription => Localization.GetInterface(MoleMethaneDescriptionHash);

	public static string MolePollutantDescription => Localization.GetInterface(MolePollutantDescriptionHash);

	public static string MoleWaterDescription => Localization.GetInterface(MoleWaterDescriptionHash);

	public static string MolePollutedWaterDescription => Localization.GetInterface(MolePollutedWaterDescriptionHash);

	public static string MoleNitrousOxideDescription => Localization.GetInterface(MoleNitrousOxideDescriptionHash);

	public static string MoleLiquidNitrogenDescription => Localization.GetInterface(MoleLiquidNitrogenDescriptionHash);

	public static string MoleLiquidOxygenDescription => Localization.GetInterface(MoleLiquidOxygenDescriptionHash);

	public static string MoleLiquidMethaneDescription => Localization.GetInterface(MoleLiquidMethaneDescriptionHash);

	public static string MoleSteamDescription => Localization.GetInterface(MoleSteamDescriptionHash);

	public static string MoleLiquidCarbonDioxideDescription => Localization.GetInterface(MoleLiquidCarbonDioxideDescriptionHash);

	public static string MoleLiquidPollutantDescription => Localization.GetInterface(MoleLiquidPollutantDescriptionHash);

	public static string MoleLiquidNitrousOxideDescription => Localization.GetInterface(MoleLiquidNitrousOxideDescriptionHash);

	public static string MoleLiquidHydrogenDescription => Localization.GetInterface(MoleLiquidHydrogenDescriptionHash);

	public static string MoleHydrogenDescription => Localization.GetInterface(MoleHydrogenDescriptionHash);

	public static string MoleHydrazineDescription => Localization.GetInterface(MoleHydrazineDescriptionHash);

	public static string MoleLiquidHydrazineDescription => Localization.GetInterface(MoleLiquidHydrazineDescriptionHash);

	public static string MoleLiquidAlcoholDescription => Localization.GetInterface(MoleLiquidAlcoholDescriptionHash);

	public static string MoleHeliumDescription => Localization.GetInterface(MoleHeliumDescriptionHash);

	public static string MoleLiquidSodiumChlorideDescription => Localization.GetInterface(MoleLiquidSodiumChlorideDescriptionHash);

	public static string MoleSilanolDescription => Localization.GetInterface(MoleSilanolDescriptionHash);

	public static string MoleLiquidSilanolDescription => Localization.GetInterface(MoleLiquidSilanolDescriptionHash);

	public static string MoleHydrochloricAcidDescription => Localization.GetInterface(MoleHydrochloricAcidDescriptionHash);

	public static string MoleLiquidHydrochloricAcidDescription => Localization.GetInterface(MoleLiquidHydrochloricAcidDescriptionHash);

	public static string MoleOzoneDescription => Localization.GetInterface(MoleOzoneDescriptionHash);

	public static string MoleLiquidOzoneDescription => Localization.GetInterface(MoleLiquidOzoneDescriptionHash);

	public static string TraderInUseMessage => Localization.GetInterface(TraderInUseHash);

	public static string TraderIsLockedMessage => Localization.GetInterface(TraderIsLockedHash);

	public static string ResearchPodTypeMessage => Localization.GetInterface(ResearchPodTypeHash);

	public static string GetAutoResearchPodsRequired => Localization.GetInterface(GetAutoResearchPodsRequiredHash);

	public static string GetManualResearchPodsRequired => Localization.GetInterface(GetManualResearchPodsRequiredHash);

	public static string GetActivateResearch => Localization.GetInterface(Activate_Research);

	public static string GetResearchPodTypeMessage => Localization.GetInterface(CurrentStoredPodType);

	public static string GetMineablesInVicinity => Localization.GetInterface(MineablesInVicinity);

	public static string GetRobotMineablesInQueue => Localization.GetInterface(MineablesInQueue);

	public static string GetNextWeatherEventTime => Localization.GetInterface(NextWeatherEventTime);

	public static string GetFuelInTankRemaining => Localization.GetInterface(FuelInTankRemaining);

	public static string GetFuelReturnCost => Localization.GetInterface(FuelReturnCost);

	public static string GetCollectableGoods => Localization.GetInterface(FuelReturnCost);

	public static string LogicTypeTime => Localization.GetInterface(LogicTypeTimeHash);

	public static string LogicTypeBpm => Localization.GetInterface(LogicTypeBpmHash);

	public static string LogicTypeEnvironmentEfficiency => Localization.GetInterface(LogicTypeEnvironmentEfficiencyHash);

	public static string LogicTypeWorkingGasEfficiency => Localization.GetInterface(LogicTypeWorkingGasEfficiencyHash);

	public static string NoAudioReceiverDevices => Localization.GetInterface(LogicNoAudioReceiverDevicesHash);

	public static string Pitch => Localization.GetInterface(PitchHash);

	public static string Volume => Localization.GetInterface(VolumeHash);

	public static string Speed => Localization.GetInterface(SpeedHash);

	public static string Attack => Localization.GetInterface(AttackHash);

	public static string Release => Localization.GetInterface(ReleaseHash);

	public static string CurrentInstrument => Localization.GetInterface(CurrentInstrumentHash);

	public static string NoSoundCartridge => Localization.GetInterface(NoSoundCartridgeHash);

	public static string NoNote => Localization.GetInterface(NoNoteHash);

	public static string Intensity => Localization.GetInterface(IntensityHash);

	public static string Waveform => Localization.GetInterface(WaveformHash);

	public static string Radius => Localization.GetInterface(RadiusHash);

	public static string LogicTypePressureInput => Localization.GetInterface(LogicTypePressureInputHash);

	public static string LogicTypeTemperatureInput => Localization.GetInterface(LogicTypeTemperatureInputHash);

	public static string LogicTypeRatioOxygenInput => Localization.GetInterface(LogicTypeRatioOxygenInputHash);

	public static string LogicTypeRatioCarbonDioxideInput => Localization.GetInterface(LogicTypeRatioCarbonDioxideInputHash);

	public static string LogicTypeRatioNitrogenInput => Localization.GetInterface(LogicTypeRatioNitrogenInputHash);

	public static string LogicTypeRatioPollutantInput => Localization.GetInterface(LogicTypeRatioPollutantInputHash);

	public static string LogicTypeRatioMethaneInput => Localization.GetInterface(LogicTypeRatioMethaneInputHash);

	public static string LogicTypeRatioWaterInput => Localization.GetInterface(LogicTypeRatioWaterInputHash);

	public static string LogicTypeRatioNitrousOxideInput => Localization.GetInterface(LogicTypeRatioNitrousOxideInputHash);

	public static string LogicTypeRatioLiquidNitrogenInput => Localization.GetInterface(LogicTypeRatioLiquidNitrogenInputHash);

	public static string LogicTypeCombustionInput => Localization.GetInterface(LogicTypeCombustionInputHash);

	public static string LogicTypeTotalMolesInput => Localization.GetInterface(LogicTypeTotalMolesInputHash);

	public static string LogicTypePressureInput2 => Localization.GetInterface(LogicTypePressureInput2Hash);

	public static string LogicTypeTemperatureInput2 => Localization.GetInterface(LogicTypeTemperatureInput2Hash);

	public static string LogicTypeRatioOxygenInput2 => Localization.GetInterface(LogicTypeRatioOxygenInput2Hash);

	public static string LogicTypeRatioCarbonDioxideInput2 => Localization.GetInterface(LogicTypeRatioCarbonDioxideInput2Hash);

	public static string LogicTypeRatioNitrogenInput2 => Localization.GetInterface(LogicTypeRatioNitrogenInput2Hash);

	public static string LogicTypeRatioPollutantInput2 => Localization.GetInterface(LogicTypeRatioPollutantInput2Hash);

	public static string LogicTypeRatioMethaneInput2 => Localization.GetInterface(LogicTypeRatioMethaneInput2Hash);

	public static string LogicTypeRatioWaterInput2 => Localization.GetInterface(LogicTypeRatioWaterInput2Hash);

	public static string LogicTypeRatioNitrousOxideInput2 => Localization.GetInterface(LogicTypeRatioNitrousOxideInput2Hash);

	public static string LogicTypeRatioLiquidNitrogenInput2 => Localization.GetInterface(LogicTypeRatioLiquidNitrogenInput2Hash);

	public static string LogicTypeCombustionInput2 => Localization.GetInterface(LogicTypeCombustionInput2Hash);

	public static string LogicTypeTotalMolesInput2 => Localization.GetInterface(LogicTypeTotalMolesInput2Hash);

	public static string LogicTypePressureOutput => Localization.GetInterface(LogicTypePressureOutputHash);

	public static string LogicTypeTemperatureOutput => Localization.GetInterface(LogicTypeTemperatureOutputHash);

	public static string LogicTypeRatioOxygenOutput => Localization.GetInterface(LogicTypeRatioOxygenOutputHash);

	public static string LogicTypeRatioCarbonDioxideOutput => Localization.GetInterface(LogicTypeRatioCarbonDioxideOutputHash);

	public static string LogicTypeRatioNitrogenOutput => Localization.GetInterface(LogicTypeRatioNitrogenOutputHash);

	public static string LogicTypeRatioPollutantOutput => Localization.GetInterface(LogicTypeRatioPollutantOutputHash);

	public static string LogicTypeRatioMethaneOutput => Localization.GetInterface(LogicTypeRatioMethaneOutputHash);

	public static string LogicTypeRatioWaterOutput => Localization.GetInterface(LogicTypeRatioWaterOutputHash);

	public static string LogicTypeRatioNitrousOxideOutput => Localization.GetInterface(LogicTypeRatioNitrousOxideOutputHash);

	public static string LogicTypeRatioLiquidNitrogenOutput => Localization.GetInterface(LogicTypeRatioLiquidNitrogenOutputHash);

	public static string LogicTypeCombustionOutput => Localization.GetInterface(LogicTypeCombustionOutputHash);

	public static string LogicTypeTotalMolesOutput => Localization.GetInterface(LogicTypeTotalMolesOutputHash);

	public static string LogicTypePressureOutput2 => Localization.GetInterface(LogicTypePressureOutput2Hash);

	public static string LogicTypeTemperatureOutput2 => Localization.GetInterface(LogicTypeTemperatureOutput2Hash);

	public static string LogicTypeRatioOxygenOutput2 => Localization.GetInterface(LogicTypeRatioOxygenOutput2Hash);

	public static string LogicTypeRatioCarbonDioxideOutput2 => Localization.GetInterface(LogicTypeRatioCarbonDioxideOutput2Hash);

	public static string LogicTypeRatioNitrogenOutput2 => Localization.GetInterface(LogicTypeRatioNitrogenOutput2Hash);

	public static string LogicTypeRatioPollutantOutput2 => Localization.GetInterface(LogicTypeRatioPollutantOutput2Hash);

	public static string LogicTypeRatioMethaneOutput2 => Localization.GetInterface(LogicTypeRatioMethaneOutput2Hash);

	public static string LogicTypeRatioWaterOutput2 => Localization.GetInterface(LogicTypeRatioWaterOutput2Hash);

	public static string LogicTypeRatioNitrousOxideOutput2 => Localization.GetInterface(LogicTypeRatioNitrousOxideOutput2Hash);

	public static string LogicTypeRatioLiquidNitrogenOutput2 => Localization.GetInterface(LogicTypeRatioLiquidNitrogenOutput2Hash);

	public static string LogicTypeCombustionOutput2 => Localization.GetInterface(LogicTypeCombustionOutput2Hash);

	public static string LogicTypeTotalMolesOutput2 => Localization.GetInterface(LogicTypeTotalMolesOutput2Hash);

	public static string LogicTypeOperationalTemperatureEfficiency => Localization.GetInterface(LogicTypeOperationalTemperatureEfficiencyHash);

	public static string LogicTypeTemperatureDifferentialEfficiency => Localization.GetInterface(LogicTypeTemperatureDifferentialEfficiencyHash);

	public static string LogicTypePressureEfficiency => Localization.GetInterface(LogicTypePressureEfficiencyHash);

	public static string LogicTypeCombustionLimiter => Localization.GetInterface(LogicTypeCombustionLimiterHash);

	public static string LogicTypeThrottle => Localization.GetInterface(LogicTypeThrottleHash);

	public static string LogicTypeRpm => Localization.GetInterface(LogicTypeRpmHash);

	public static string LogicTypeStress => Localization.GetInterface(LogicTypeStressHash);

	public static string LogicTypeInterrogationProgress => Localization.GetInterface(LogicTypeInterrogationProgressHash);

	public static string LogicTypeTargetPadIndex => Localization.GetInterface(LogicTypeTargetPadIndexHash);

	public static string LogicTypeSizeX => Localization.GetInterface(LogicTypeSizeXHash);

	public static string LogicTypeSizeY => Localization.GetInterface(LogicTypeSizeYHash);

	public static string LogicTypeSizeZ => Localization.GetInterface(LogicTypeSizeZHash);

	public static string LogicTypeMinWattsToContact => Localization.GetInterface(LogicTypeMinWattsToContactHash);

	public static string LogicTypeWattsReachingContact => Localization.GetInterface(LogicTypeWattsReachingContactHash);

	public static string LogicTypeChannel => Localization.GetInterface(LogicTypeLogicTypeChannelHash);

	public static string LogicTypeFlush => Localization.GetInterface(LogicTypeLogicTypeFlushHash);

	public static string LogicTypeSoundAlert => Localization.GetInterface(LogicTypeLogicTypeSoundAlertHash);

	public static string LogicTypeVolumeOfLiquid => Localization.GetInterface(LogicTypeVolumeOfLiquidHash);

	public static string Rpm => Localization.GetInterface(RpmHash);

	public static string Stress => Localization.GetInterface(StressHash);

	public static string Throttle => Localization.GetInterface(ThrottleHash);

	public static string CombustionLimiter => Localization.GetInterface(CombustionLimiterHash);

	public static string LogicTypeLineNumber => Localization.GetInterface(LogicTypeLineNumberHash);

	public static string LogicTypeUnknown => Localization.GetInterface(LogicTypeUnknownHash);

	public static string LogicTypeRatioLiquidOxygen => Localization.GetInterface(LogicTypeRatioLiquidOxygenHash);

	public static string LogicTypeRatioLiquidOxygenOutput => Localization.GetInterface(LogicTypeRatioLiquidOxygenOutputHash);

	public static string LogicTypeRatioLiquidOxygenOutput2 => Localization.GetInterface(LogicTypeRatioLiquidOxygenOutput2Hash);

	public static string LogicTypeRatioLiquidOxygenInput => Localization.GetInterface(LogicTypeRatioLiquidOxygenInputHash);

	public static string LogicTypeRatioLiquidOxygenInput2 => Localization.GetInterface(LogicTypeRatioLiquidOxygenInput2Hash);

	public static string LogicTypeRatioLiquidMethane => Localization.GetInterface(LogicTypeRatioLiquidMethaneHash);

	public static string LogicTypeRatioLiquidMethaneOutput => Localization.GetInterface(LogicTypeRatioLiquidMethaneOutputHash);

	public static string LogicTypeRatioLiquidMethaneOutput2 => Localization.GetInterface(LogicTypeRatioLiquidMethaneOutput2Hash);

	public static string LogicTypeRatioLiquidMethaneInput => Localization.GetInterface(LogicTypeRatioLiquidMethaneInputHash);

	public static string LogicTypeRatioLiquidMethaneInput2 => Localization.GetInterface(LogicTypeRatioLiquidMethaneInput2Hash);

	public static string LogicTypeRatioSteam => Localization.GetInterface(LogicTypeRatioSteamHash);

	public static string LogicTypeRatioSteamOutput => Localization.GetInterface(LogicTypeRatioSteamOutputHash);

	public static string LogicTypeRatioSteamOutput2 => Localization.GetInterface(LogicTypeRatioSteamOutput2Hash);

	public static string LogicTypeRatioSteamInput => Localization.GetInterface(LogicTypeRatioSteamInputHash);

	public static string LogicTypeRatioSteamInput2 => Localization.GetInterface(LogicTypeRatioSteamInput2Hash);

	public static string LogicTypeContactTypeId => Localization.GetInterface(LogicTypeContactTypeIdHash);

	public static string LogicTypeRatioLiquidCarbonDioxide => Localization.GetInterface(LogicTypeRatioLiquidCarbonDioxideHash);

	public static string LogicTypeRatioLiquidCarbonDioxideOutput => Localization.GetInterface(LogicTypeRatioLiquidCarbonDioxideOutputHash);

	public static string LogicTypeRatioLiquidCarbonDioxideOutput2 => Localization.GetInterface(LogicTypeRatioLiquidCarbonDioxideOutput2Hash);

	public static string LogicTypeRatioLiquidCarbonDioxideInput => Localization.GetInterface(LogicTypeRatioLiquidCarbonDioxideInputHash);

	public static string LogicTypeRatioLiquidCarbonDioxideInput2 => Localization.GetInterface(LogicTypeRatioLiquidCarbonDioxideInput2Hash);

	public static string LogicTypeRatioLiquidPollutant => Localization.GetInterface(LogicTypeRatioLiquidPollutantHash);

	public static string LogicTypeRatioLiquidPollutantOutput => Localization.GetInterface(LogicTypeRatioLiquidPollutantOutputHash);

	public static string LogicTypeRatioLiquidPollutantOutput2 => Localization.GetInterface(LogicTypeRatioLiquidPollutantOutput2Hash);

	public static string LogicTypeRatioLiquidPollutantInput => Localization.GetInterface(LogicTypeRatioLiquidPollutantInputHash);

	public static string LogicTypeRatioLiquidPollutantInput2 => Localization.GetInterface(LogicTypeRatioLiquidPollutantInput2Hash);

	public static string LogicTypeRatioLiquidNitrousOxide => Localization.GetInterface(LogicTypeRatioLiquidNitrousOxideHash);

	public static string LogicTypeRatioLiquidNitrousOxideOutput => Localization.GetInterface(LogicTypeRatioLiquidNitrousOxideOutputHash);

	public static string LogicTypeRatioLiquidNitrousOxideOutput2 => Localization.GetInterface(LogicTypeRatioLiquidNitrousOxideOutput2Hash);

	public static string LogicTypeRatioLiquidNitrousOxideInput => Localization.GetInterface(LogicTypeRatioLiquidNitrousOxideInputHash);

	public static string LogicTypeRatioLiquidNitrousOxideInput2 => Localization.GetInterface(LogicTypeRatioLiquidNitrousOxideInput2Hash);

	public static string LogicTypeRatioHydrogen => Localization.GetInterface(LogicTypeRatioHydrogenHash);

	public static string LogicTypeRatioLiquidHydrogen => Localization.GetInterface(LogicTypeRatioLiquidHydrogenHash);

	public static string LogicTypeProgress => Localization.GetInterface(LogicTypeProgressHash);

	public static string LogicTypeDestinationCode => Localization.GetInterface(LogicTypeDestinationCodeHash);

	public static string LogicTypeAcceleration => Localization.GetInterface(LogicTypeAccelerationHash);

	public static string LogicTypeAutoShutOff => Localization.GetInterface(LogicTypeAutoShutOffHash);

	public static string LogicTypeThrust => Localization.GetInterface(LogicTypeThrustHash);

	public static string LogicTypeThrustToWeight => Localization.GetInterface(LogicTypeThrustToWeightHash);

	public static string LogicTypeOverShootTarget => Localization.GetInterface(LogicTypeOverShootTargetHash);

	public static string LogicTypeTimeToDestination => Localization.GetInterface(LogicTypeTimeToDestinationHash);

	public static string LogicTypeBurnTimeRemaining => Localization.GetInterface(LogicTypeBurnTimeRemainingHash);

	public static string LogicTypeDryMass => Localization.GetInterface(LogicTypeDryMassHash);

	public static string LogicTypeMass => Localization.GetInterface(LogicTypeMassHash);

	public static string LogicTypeWeight => Localization.GetInterface(LogicTypeWeightHash);

	public static string LogicTypeAutoLand => Localization.GetInterface(LogicTypeAutoLandHash);

	public static string LogicTypeContactSlotIndex => Localization.GetInterface(LogicTypeContactSlotIndexHash);

	public static string LogicTypeRatioHydrazine => Localization.GetInterface(LogicTypeRatioHydrazineHash);

	public static string LogicTypeRatioLiquidHydrazine => Localization.GetInterface(LogicTypeRatioLiquidHydrazineHash);

	public static string LogicTypeRatioLiquidAlcohol => Localization.GetInterface(LogicTypeRatioLiquidAlcoholHash);

	public static string LogicTypeRatioHelium => Localization.GetInterface(LogicTypeRatioHeliumHash);

	public static string LogicTypeRatioLiquidSodiumChloride => Localization.GetInterface(LogicTypeRatioLiquidSodiumChlorideHash);

	public static string LogicTypeRatioSilanol => Localization.GetInterface(LogicTypeRatioSilanolHash);

	public static string LogicTypeRatioLiquidSilanol => Localization.GetInterface(LogicTypeRatioLiquidSilanolHash);

	public static string LogicTypeRatioHydrochloricAcid => Localization.GetInterface(LogicTypeRatioHydrochloricAcidHash);

	public static string LogicTypeRatioLiquidHydrochloricAcid => Localization.GetInterface(LogicTypeRatioLiquidHydrochloricAcidHash);

	public static string LogicTypeRatioOzone => Localization.GetInterface(LogicTypeRatioOzoneHash);

	public static string LogicTypeRatioLiquidOzone => Localization.GetInterface(LogicTypeRatioLiquidOzoneHash);

	public static string LogicTypeRatioHydrogenInput => Localization.GetInterface(LogicTypeRatioHydrogenInputHash);

	public static string LogicTypeRatioHydrogenInput2 => Localization.GetInterface(LogicTypeRatioHydrogenInput2Hash);

	public static string LogicTypeRatioHydrogenOutput => Localization.GetInterface(LogicTypeRatioHydrogenOutputHash);

	public static string LogicTypeRatioHydrogenOutput2 => Localization.GetInterface(LogicTypeRatioHydrogenOutput2Hash);

	public static string LogicTypeRatioLiquidHydrogenInput => Localization.GetInterface(LogicTypeRatioLiquidHydrogenInputHash);

	public static string LogicTypeRatioLiquidHydrogenInput2 => Localization.GetInterface(LogicTypeRatioLiquidHydrogenInput2Hash);

	public static string LogicTypeRatioLiquidHydrogenOutput => Localization.GetInterface(LogicTypeRatioLiquidHydrogenOutputHash);

	public static string LogicTypeRatioLiquidHydrogenOutput2 => Localization.GetInterface(LogicTypeRatioLiquidHydrogenOutput2Hash);

	public static string LogicTypeRatioPollutedWaterInput => Localization.GetInterface(LogicTypeRatioPollutedWaterInputHash);

	public static string LogicTypeRatioPollutedWaterInput2 => Localization.GetInterface(LogicTypeRatioPollutedWaterInput2Hash);

	public static string LogicTypeRatioPollutedWaterOutput => Localization.GetInterface(LogicTypeRatioPollutedWaterOutputHash);

	public static string LogicTypeRatioPollutedWaterOutput2 => Localization.GetInterface(LogicTypeRatioPollutedWaterOutput2Hash);

	public static string LogicTypeRatioHydrazineInput => Localization.GetInterface(LogicTypeRatioHydrazineInputHash);

	public static string LogicTypeRatioHydrazineInput2 => Localization.GetInterface(LogicTypeRatioHydrazineInput2Hash);

	public static string LogicTypeRatioHydrazineOutput => Localization.GetInterface(LogicTypeRatioHydrazineOutputHash);

	public static string LogicTypeRatioHydrazineOutput2 => Localization.GetInterface(LogicTypeRatioHydrazineOutput2Hash);

	public static string LogicTypeRatioLiquidHydrazineInput => Localization.GetInterface(LogicTypeRatioLiquidHydrazineInputHash);

	public static string LogicTypeRatioLiquidHydrazineInput2 => Localization.GetInterface(LogicTypeRatioLiquidHydrazineInput2Hash);

	public static string LogicTypeRatioLiquidHydrazineOutput => Localization.GetInterface(LogicTypeRatioLiquidHydrazineOutputHash);

	public static string LogicTypeRatioLiquidHydrazineOutput2 => Localization.GetInterface(LogicTypeRatioLiquidHydrazineOutput2Hash);

	public static string LogicTypeRatioLiquidAlcoholInput => Localization.GetInterface(LogicTypeRatioLiquidAlcoholInputHash);

	public static string LogicTypeRatioLiquidAlcoholInput2 => Localization.GetInterface(LogicTypeRatioLiquidAlcoholInput2Hash);

	public static string LogicTypeRatioLiquidAlcoholOutput => Localization.GetInterface(LogicTypeRatioLiquidAlcoholOutputHash);

	public static string LogicTypeRatioLiquidAlcoholOutput2 => Localization.GetInterface(LogicTypeRatioLiquidAlcoholOutput2Hash);

	public static string LogicTypeRatioHeliumInput => Localization.GetInterface(LogicTypeRatioHeliumInputHash);

	public static string LogicTypeRatioHeliumInput2 => Localization.GetInterface(LogicTypeRatioHeliumInput2Hash);

	public static string LogicTypeRatioHeliumOutput => Localization.GetInterface(LogicTypeRatioHeliumOutputHash);

	public static string LogicTypeRatioHeliumOutput2 => Localization.GetInterface(LogicTypeRatioHeliumOutput2Hash);

	public static string LogicTypeRatioLiquidSodiumChlorideInput => Localization.GetInterface(LogicTypeRatioLiquidSodiumChlorideInputHash);

	public static string LogicTypeRatioLiquidSodiumChlorideInput2 => Localization.GetInterface(LogicTypeRatioLiquidSodiumChlorideInput2Hash);

	public static string LogicTypeRatioLiquidSodiumChlorideOutput => Localization.GetInterface(LogicTypeRatioLiquidSodiumChlorideOutputHash);

	public static string LogicTypeRatioLiquidSodiumChlorideOutput2 => Localization.GetInterface(LogicTypeRatioLiquidSodiumChlorideOutput2Hash);

	public static string LogicTypeRatioSilanolInput => Localization.GetInterface(LogicTypeRatioSilanolInputHash);

	public static string LogicTypeRatioSilanolInput2 => Localization.GetInterface(LogicTypeRatioSilanolInput2Hash);

	public static string LogicTypeRatioSilanolOutput => Localization.GetInterface(LogicTypeRatioSilanolOutputHash);

	public static string LogicTypeRatioSilanolOutput2 => Localization.GetInterface(LogicTypeRatioSilanolOutput2Hash);

	public static string LogicTypeRatioLiquidSilanolInput => Localization.GetInterface(LogicTypeRatioLiquidSilanolInputHash);

	public static string LogicTypeRatioLiquidSilanolInput2 => Localization.GetInterface(LogicTypeRatioLiquidSilanolInput2Hash);

	public static string LogicTypeRatioLiquidSilanolOutput => Localization.GetInterface(LogicTypeRatioLiquidSilanolOutputHash);

	public static string LogicTypeRatioLiquidSilanolOutput2 => Localization.GetInterface(LogicTypeRatioLiquidSilanolOutput2Hash);

	public static string LogicTypeRatioHydrochloricAcidInput => Localization.GetInterface(LogicTypeRatioHydrochloricAcidInputHash);

	public static string LogicTypeRatioHydrochloricAcidInput2 => Localization.GetInterface(LogicTypeRatioHydrochloricAcidInput2Hash);

	public static string LogicTypeRatioHydrochloricAcidOutput => Localization.GetInterface(LogicTypeRatioHydrochloricAcidOutputHash);

	public static string LogicTypeRatioHydrochloricAcidOutput2 => Localization.GetInterface(LogicTypeRatioHydrochloricAcidOutput2Hash);

	public static string LogicTypeRatioLiquidHydrochloricAcidInput => Localization.GetInterface(LogicTypeRatioLiquidHydrochloricAcidInputHash);

	public static string LogicTypeRatioLiquidHydrochloricAcidInput2 => Localization.GetInterface(LogicTypeRatioLiquidHydrochloricAcidInput2Hash);

	public static string LogicTypeRatioLiquidHydrochloricAcidOutput => Localization.GetInterface(LogicTypeRatioLiquidHydrochloricAcidOutputHash);

	public static string LogicTypeRatioLiquidHydrochloricAcidOutput2 => Localization.GetInterface(LogicTypeRatioLiquidHydrochloricAcidOutput2Hash);

	public static string LogicTypeRatioOzoneInput => Localization.GetInterface(LogicTypeRatioOzoneInputHash);

	public static string LogicTypeRatioOzoneInput2 => Localization.GetInterface(LogicTypeRatioOzoneInput2Hash);

	public static string LogicTypeRatioOzoneOutput => Localization.GetInterface(LogicTypeRatioOzoneOutputHash);

	public static string LogicTypeRatioOzoneOutput2 => Localization.GetInterface(LogicTypeRatioOzoneOutput2Hash);

	public static string LogicTypeRatioLiquidOzoneInput => Localization.GetInterface(LogicTypeRatioLiquidOzoneInputHash);

	public static string LogicTypeRatioLiquidOzoneInput2 => Localization.GetInterface(LogicTypeRatioLiquidOzoneInput2Hash);

	public static string LogicTypeRatioLiquidOzoneOutput => Localization.GetInterface(LogicTypeRatioLiquidOzoneOutputHash);

	public static string LogicTypeRatioLiquidOzoneOutput2 => Localization.GetInterface(LogicTypeRatioLiquidOzoneOutput2Hash);

	private static string TooltipDynamicThing(Thing thing, int dynamicHash, int staticHash)
	{
		if (thing != null)
		{
			Localization.Variable1 = "{THING:" + thing.PrefabName + "}";
			return Localization.GetInterface(dynamicHash);
		}
		return Localization.GetInterface(staticHash);
	}

	public static string TooltipPlacementSnapFaceMountBlocked(Thing blockingThing)
	{
		return TooltipDynamicThing(blockingThing, TooltipPlacementSnapFaceMountBlockedDynamicHash, TooltipPlacementSnapFaceMountBlockedFallbackHash);
	}

	public static string TooltipPlacementSnapFaceNotMountable(Thing targetThing)
	{
		return TooltipDynamicThing(targetThing, TooltipPlacementSnapFaceNotMountableDynamicHash, TooltipPlacementSnapFaceNotMountableFallbackHash);
	}

	public static string TooltipPlacementSnapFaceWrongFace(Thing targetThing)
	{
		return TooltipDynamicThing(targetThing, TooltipPlacementSnapFaceWrongFaceDynamicHash, TooltipPlacementSnapFaceWrongFaceFallbackHash);
	}
}
