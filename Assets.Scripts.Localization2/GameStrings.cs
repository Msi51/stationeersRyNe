namespace Assets.Scripts.Localization2;

public static class GameStrings
{
	public static readonly GameString CrewChairHeading = GameString.Create("CrewChairHeading", "Screen Connection");

	public static readonly GameString CrewChairDescription = GameString.Create("CrewChairDescription", "Currently connected to {LOCAL:Thing}", "Thing");

	public static readonly GameString LogicTypeCurrentNodeType = GameString.Create("LogicTypeCurrentNodeType", "Returns the NodeType as an integer for the current location for a Rocket");

	public static readonly GameString LogicTypeTargetNodeType = GameString.Create("LogicTypeTargetNodeType", "Returns the NodeType as an integer for the target location for a Rocket");

	public static readonly GameString LogicTypeEnergyConvected = GameString.Create("LogicTypeEnergyConvected", "The energy convected with the surroundings on the last atmospheric tick, in Joules. Positive values remove heat from the internal atmosphere (cooling); negative values add heat.");

	public static readonly GameString LogicTypeEnergyRadiated = GameString.Create("LogicTypeEnergyRadiated", "The energy radiated to the surroundings on the last atmospheric tick, in Joules, net of solar heating. Positive values remove heat from the internal atmosphere (cooling); negative values add heat.");

	public static readonly GameString Inject = GameString.Create("Inject", "Inject");

	public static readonly GameString ForceDefecate = GameString.Create("ForceDefecate", "Force Defecate");

	public static readonly GameString Hydration = GameString.Create("Hydration", "Hydration");

	public static readonly GameString Nutrition = GameString.Create("Nutrition", "Nutrition");

	public static readonly GameString Oxygenation = GameString.Create("Oxygenation", "Oxygenation");

	public static readonly GameString GForce = GameString.Create("GForce", "G-Force");

	public static readonly GameString BreathingEfficiency = GameString.Create("BreathingEfficiency", "Breathing");

	public static readonly GameString Waste = GameString.Create("Waste", "Waste");

	public static readonly GameString Stun = GameString.Create("Stun", "Stun");

	public static readonly GameString UndergoingAcceleration = GameString.Create("UndergoingAcceleration", "Undergoing heavy acceleration.");

	public static readonly GameString Use = GameString.Create("Use", "Use");

	public static readonly GameString MedicalEffectsDescription = GameString.Create("MedicalEffectsDescription", "Effects have been inducted artificially");

	public static readonly GameString CryoStatusHeading = GameString.Create("CryoStatusHeading", "Status");

	public static readonly GameString HealingHeading = GameString.Create("HealingHeading", "Medically Induced Healing");

	public static readonly GameString CryogenicHealingHeading = GameString.Create("CryogenicHealingHeading", "Cryogenic Healing");

	public static readonly GameString StimulantHeading = GameString.Create("StimulantHeading", "Medically Induced Stimulant");

	public static readonly GameString LifeSuspendedHeader = GameString.Create("LifeSuspendedHeader", "Life Suspended");

	public static readonly GameString LifeSuspendedDescription = GameString.Create("LifeSuspendedDescription", "Metabolic processes are suspended.");

	public static readonly GameString StunnedHeading = GameString.Create("StunnedHeading", "Medically Induced Stun");

	public static readonly GameString SanitationNeed = GameString.Create("SanitationNeed", "Sanitation Need");

	public static readonly GameString SanitationDescription = GameString.Create("SanitationDescription", "You need to go to the bathroom soon, find a <color=yellow>toilet</color>\nor use a <color=yellow>waste bag</color> to relieve yourself, otherwise you might soil your <color=yellow>suit</color>.");

	public static readonly GameString SoiledHeading = GameString.Create("SoiledHeading", "Soiled Environment");

	public static readonly GameString SoiledDescription = GameString.Create("SoiledDescription", "The current environment is exposing you to <color=orange>polluted water</color>.\nYou need to move away or flush your <color=yellow>suit</color> to remove.");

	public static readonly GameString NextWeatherHashDescription = GameString.Create("NextWeatherHashDescription", "NextWeatherHash provides the hash value for the name of the next weather event as a 32 bit integer.");

	public static readonly GameString WeatherEventComing = GameString.Create("WeatherEventComing", "A weather event is incoming");

	public static readonly GameString DeviceMustBeOutside = GameString.Create("DeviceMustBeOutside", "The {LOCAL:Device} will not function indoors", "Device");

	public static readonly GameString DeviceConfigMode = GameString.Create("DeviceConfigMode", "Set to Configure");

	public static readonly GameString DeviceOperateMode = GameString.Create("DeviceOperateMode", "Set to Operate");

	public static readonly GameString SlotItemProvidesPower = GameString.Create("SlotItemProvidesPower", "The {LOCAL:Item} provide {LOCAL:Power} of power", "Item", "Power");

	public static readonly GameString CanReplaceFuselage = GameString.Create("CanReplaceFuselage", "Can replace other fuselages when holding a {LOCAL:Tool}", "Tool");

	public static readonly GameString ShowerWillClean = GameString.Create("ShowerWillClean", "Will clean {LOCAL:Occupant}", "Occupant");

	public static readonly GameString DeviceNotInPosition = GameString.Create("DeviceNotInPosition", "{LOCAL:Occupant} is not close enough to use", "Occupant");

	public static readonly GameString DeviceCannotWhenWearing = GameString.Create("DeviceCannotWhenWearing", "Cannot use wearing {LOCAL:Thing}", "Thing");

	public static readonly GameString NoSanitationNeed = GameString.Create("NoSanitationNeed", "{LOCAL:Human} does not need to go to the toilet right now", "Human");

	public static readonly GameString SanitationPacketFull = GameString.Create("SanitationPacketFull", "{LOCAL:Thing} is full", "Thing");

	public static readonly GameString LandingGear = GameString.Create("LandingGear", "Landing Gear");

	public static readonly GameString Activate = GameString.Create("Activate", "Activate");

	public static readonly GameString Deactivate = GameString.Create("Deactivate", "Deactivate");

	public static readonly GameString ButtonSearch = GameString.Create("ButtonSearch", "Search");

	public static readonly GameString HeaderCreativeSpawnMenu = GameString.Create("HeaderCreativeSpawnMenu", "Creative Spawn Menu");

	public static readonly GameString EnabledLower = GameString.Create("EnabledLower", "enabled");

	public static readonly GameString DisabledLower = GameString.Create("DisabledLower", "disabled");

	public static readonly GameString LogicDescriptionNameHash = GameString.Create("LogicDescriptionNameHash", "Provides the hash value for the name of the object as a 32 bit integer.");

	public static readonly GameString LogicDescriptionStackSize = GameString.Create("LogicDescriptionStackSize", "Returns the stack size of the devices internal memory.");

	public static readonly GameString CodeEditorFileSize = GameString.Create("CodeEditorFileSize", "Size {LOCAL:Size} / {LOCAL:MaxSize} bytes", "Size", "MaxSize");

	public static readonly GameString UplinkConnectedTo = GameString.Create("UplinkConnectedTo", "Connected to {LOCAL:Downlink}", "Downlink");

	public static readonly GameString UplinkNotConnected = GameString.Create("UplinkNotConnected", "Not connected to any downlink");

	public static readonly GameString UplinkConnectedToRocket = GameString.Create("UplinkConnectedToRocket", "Accessing {LOCAL:Rocket} via {LOCAL:RocketAvionics}", "Rocket", "RocketAvionics");

	public static readonly GameString CelestialTrackerSetTo = GameString.Create("CelestialTrackerSetTo", "Tracker set to {LOCAL:Celestial}", "Celestial");

	public static readonly GameString CelestialTrackerRequiresOrbit = GameString.Create("CelestialTrackerRequiresOrbit", "Cannot track this celestial while not in orbit");

	public static readonly GameString DeviceShortCircuited = GameString.Create("DeviceShortCircuited", "Short circuited");

	public static readonly GameString DeviceIndexSetTo = GameString.Create("DeviceIndexSetTo", "Index {LOCAL:Index}", "Index");

	public static readonly GameString NoCelestialFound = GameString.Create("NoCelestialFound", "No Celestial Objects found");

	public static readonly GameString WorldInfoPlanetaryBody = GameString.Create("WorldInfoPlanetaryBody", "Orbiting {LOCAL:ParentBody}", "ParentBody");

	public static readonly GameString WorldInfoZeroGravity = GameString.Create("WorldInfoZeroGravity", "Has <color=red>zero gravity</color>");

	public static readonly GameString WorldInfoGravity = GameString.Create("WorldInfoGravity", "<color=yellow>Gravity</color> is {LOCAL:Gravity} of <color=white>Earth</color>", "Gravity");

	public static readonly GameString WorldInfoRotationPeriod = GameString.Create("WorldInfoRotationPeriod", "{LOCAL:PrimaryBody} transits <color=yellow>sky</color> every {LOCAL:RotationPeriod} at {LOCAL:SolarAngle}", "PrimaryBody", "RotationPeriod", "SolarAngle");

	public static readonly GameString WorldInfoSiderealPeriod = GameString.Create("WorldInfoSiderealPeriod", "Orbits {LOCAL:PrimaryBody} every {LOCAL:SiderealPeriod}", "PrimaryBody", "SiderealPeriod");

	public static readonly GameString WorldInfoSolarEnergy = GameString.Create("WorldInfoSolarEnergy", "{LOCAL:PrimaryBody} provides {LOCAL:Value}", "PrimaryBody", "Value");

	public static readonly GameString WorldInfoSolarEnergyRange = GameString.Create("WorldInfoSolarEnergyRange", "{LOCAL:PrimaryBody} provides {LOCAL:LowValue} to {LOCAL:HighValue}", "PrimaryBody", "LowValue", "HighValue");

	public static readonly GameString WorldInfoNoSolarEnergy = GameString.Create("WorldInfoNoSolarEnergy", "{LOCAL:PrimaryBody} provides <color=red>no sunlight</color>", "PrimaryBody");

	public static readonly GameString WorldInfoIsVacuum = GameString.Create("WorldInfoIsVacuum", "Has <color=red>no atmosphere</color>");

	public static readonly GameString WorldInfoTemperatureRange = GameString.Create("WorldInfoTemperatureRange", "<color=yellow>Temperature</color> from {LOCAL:LowValue} to {LOCAL:HighValue}", "LowValue", "HighValue");

	public static readonly GameString WorldInfoTemperature = GameString.Create("WorldInfoTemperature", "<color=yellow>Temperature</color> is {LOCAL:Value}", "Value");

	public static readonly GameString WorldInfoPressureRange = GameString.Create("WorldInfoPressureRange", "<color=yellow>Pressure</color> from {LOCAL:LowValue} to {LOCAL:HighValue}", "LowValue", "HighValue");

	public static readonly GameString WorldInfoPressure = GameString.Create("WorldInfoPressure", "<color=yellow>Pressure</color> is {LOCAL:Value}", "Value");

	public static readonly GameString WorldInfoPlanetWeather = GameString.Create("WorldInfoPlanetWeather", "Will experience {LOCAL:Weather} events", "Weather");

	public static readonly GameString ThingDamageIsAt = GameString.Create("ThingDamageIsAt", "{LOCAL:DamageType} is at {LOCAL:Percent}", "DamageType", "Percent");

	public static readonly GameString NamedThingDamageIsAt = GameString.Create("NamedThingDamageIsAt", "{LOCAL:Thing} {LOCAL:DamageType} is at {LOCAL:Percent}", "Thing", "DamageType", "Percent");

	public static readonly GameString DirtCanisterDirtIsAt = GameString.Create("DirtCanisterDirtIsAt", "Currently contains {LOCAL:Quantity} of dirt", "Quantity");

	public static readonly GameString IsCurrentlyMelting = GameString.Create("IsCurrentlyMelting", "Is currently <color=yellow>melting</color>");

	public static readonly GameString ContainsItemInSlotAtQuantity = GameString.Create("ContainsItemInSlotAtQuantity", "Contains {LOCAL:Item} in {LOCAL:Slot} at {LOCAL:QuantityState}", "Item", "Slot", "QuantityState");

	public static readonly GameString DamageTypeDecay = GameString.Create("DamageTypeDecay", "Decay");

	public static readonly GameString DamageTypeDamage = GameString.Create("DamageTypeDamage", "Damage");

	public static readonly GameString ConnectionGeneric = GameString.Create("ConnectionGeneric", "Connection");

	public static readonly GameString CanConnectTo = GameString.Create("CanConnectTo", "Provides {LOCAL:NetworkType} connection", "NetworkType");

	public static readonly GameString ConnectionForThing = GameString.Create("ConnectionForThing", "Connection for {LOCAL:ParentThing}", "ParentThing");

	public static readonly GameString ConnectionToThing = GameString.Create("ConnectionToThing", "Connected to {LOCAL:ConnectedThing}", "ConnectedThing");

	public static readonly GameString ThingIsHeating = GameString.Create("ThingIsHeating", "The {LOCAL:Thing} is <color=green>heating</color> its atmosphere to {LOCAL:Temperature}", "Thing", "Temperature");

	public static readonly GameString ScrubberVentOpen = GameString.Create("ScrubberVentOpen", "<color=yellow>Vent</color> is <color=green>open</color>, internal atmosphere will be released");

	public static readonly GameString ScrubberNoFilters = GameString.Create("ScrubberNoFilters", "No <color=yellow>Filters</color> loaded, will pass all atmosphere");

	public static readonly GameString ScrubberConnectToPipe = GameString.Create("ScrubberConnectToPipe", "Connect to pipe network to release pressure.");

	public static readonly GameString CanBeUnfastenedWithWrench = GameString.Create("CanBeUnfastenedWithWrench", "A <color=green>Wrench</color> can be used to unfasten");

	public static readonly GameString ThingCanBeDestroyedByWeather = GameString.Create("ThingCanBeDestroyedByWeather", "The {LOCAL:Thing} will be damaged by {LOCAL:WeatherEvent} events", "Thing", "WeatherEvent");

	public static readonly GameString HeatingEnergyAmount = GameString.Create("HeatingEnergyAmount", "The {LOCAL:Thing} is <color=green>heating</color> at {LOCAL:Temperature}", "Thing", "Temperature");

	public static readonly GameString CoolingEnergyAmount = GameString.Create("CoolingEnergyAmount", "The {LOCAL:Thing} is <color=green>cooling</color> at {LOCAL:Temperature}", "Thing", "Temperature");

	public static readonly GameString CantEquipThisBackpack = GameString.Create("CantEquipThisBackpack", "{LOCAL:InvalidItem} can not be equipped when wearing {LOCAL:CurrentlyEquipped}", "InvalidItem", "CurrentlyEquipped");

	public static readonly GameString DefecateSuitFail = GameString.Create("DefecateSuitFail", "Failed: Wearing Suit");

	public static readonly GameString DefecateNeedFail = GameString.Create("DefecateNeedFail", "Failed: No Need");

	public static readonly GameString DefecatePacketFull = GameString.Create("DefecatePacketFull", "Failed: Packet Full");

	public static readonly GameString DifficultyCannotDefecateThroughSuit = GameString.Create("DifficultyCannotDefecateThroughSuit", "<color=yellow>Defecating</color> through a <color=white>Suit</color> is <color=red>disabled</color>");

	public static readonly GameString DifficultyCannotEatThroughHelmet = GameString.Create("DifficultyCannotEatThroughHelmet", "<color=yellow>Eating</color> through <color=white>Helmet</color> is <color=red>disabled</color>");

	public static readonly GameString DifficultyCannotDrinkThroughHelmet = GameString.Create("DifficultyCannotDrinkThroughHelmet", "<color=yellow>Drinking</color> through <color=white>Helmet</color> is <color=red>disabled</color>");

	public static readonly GameString DifficultyCannotEatOrDrinkThroughHelmet = GameString.Create("DifficultyCannotEatOrDrinkThroughHelmet", "<color=yellow>Eating</color> and <color=yellow>drinking</color> through <color=white>Helmet</color> is <color=red>disabled</color>");

	public static readonly GameString DifficultyValuePeriod = GameString.Create("DifficultyValuePeriod", "{LOCAL:Type} period {LOCAL:Value}", "Type", "Value");

	public static readonly GameString DifficultyValueRate = GameString.Create("DifficultyValueRate", "{LOCAL:Type} rate {LOCAL:Value}", "Type", "Value");

	public static readonly GameString DifficultyValueYield = GameString.Create("DifficultyValueYield", "{LOCAL:Type} yield {LOCAL:Value}", "Type", "Value");

	public static readonly GameString DifficultyValueStatus = GameString.Create("DifficultyValueStatus", "{LOCAL:Type} is {LOCAL:Status}", "Type", "Status");

	public static readonly GameString DifficultyValueStatusAre = GameString.Create("DifficultyValueStatusAre", "{LOCAL:Type} are {LOCAL:Status}", "Type", "Status");

	public static readonly GameString DifficultyThingStatus = GameString.Create("DifficultyThingStatus", "{LOCAL:Type} to {LOCAL:Thing} is {LOCAL:Status}", "Type", "Thing", "Status");

	public static readonly GameString DifficultyThingValueRate = GameString.Create("DifficultyThingValueRate", "{LOCAL:Type} to {LOCAL:Thing} rate {LOCAL:Value}", "Type", "Thing", "Value");

	public static readonly GameString ProportionalGainDescription = GameString.Create("ProportionalGainDescription", "The proportional gain of the PID controller. This value determines how aggressively the controller responds to the error between the setpoint and the process variable. A higher value results in a faster response but may lead to overshoot or instability.");

	public static readonly GameString IntegralGainDescription = GameString.Create("IntegralGainDescription", "The integral gain of the PID controller. This value determines how much the controller responds to the accumulated error over time. A higher value can help eliminate steady-state errors but may also lead to oscillations or instability.");

	public static readonly GameString DerivativeGainDescription = GameString.Create("DerivativeGainDescription", "The derivative gain of the PID controller. This value determines how much the controller responds to the rate of change of the error. A higher value can help dampen oscillations and improve stability, but may also lead to noise amplification.");

	public static readonly GameString ResetDescription = GameString.Create("ResetDescription", "Resets the PID controller's internal state, clearing any accumulated error and resetting the output to zero. This is useful for starting fresh or recovering from an unstable state.");

	public static readonly GameString SetpointDescription = GameString.Create("SetpointDescription", "The desired value that the PID controller aims to achieve. This is the target value for the process variable, and the controller will adjust its output to minimize the difference between the setpoint and the process variable.");

	public static readonly GameString MinimumDescription = GameString.Create("MinimumDescription", "Minimum value for provided logic device.");

	public static readonly GameString PassedMolesDescription = GameString.Create("PassedMolesDescription", "The number of moles that passed through this device on the previous simulation tick");

	public static readonly GameString ExhaustVelocityDescription = GameString.Create("ExhaustVelocityDescription", "The velocity of the exhaust gas in m/s");

	public static readonly GameString FlightControlRuleDescription = GameString.Create("FlightControlRuleDescription", "Flight control rule of rocket. None = 0, No AutoPilot. Normal = 1, Target Decent Apex of 60m. Alternate = 2, Velocity to High - Full throttle. Alternate2 = 3, Target an appropriate decent velocity as velocity is too low. FinalApproach = 4, Descend towards launch mount in a controlled manner.");

	public static readonly GameString ReEntryAltitudeDescription = GameString.Create("ReEntryAltitudeDescription", "The altitude that the rocket will begin its decent to the pad. Must be between 25km and 120km");

	public static readonly GameString AltitudeDescription = GameString.Create("AltitudeDescription", "The altitude that the rocket above the planet's surface. -1 if the rocket is in space.");

	public static readonly GameString GravityDescription = GameString.Create("GravityDescription", "The gravitational acceleration acting on the rocket, in m/s.");

	public static readonly GameString NetworkFaultDescription = GameString.Create("NetworkFaultDescription", "Attached network is experiencing a fault, such as a pipe burst or other failure.");

	public static readonly GameString OperationalTemperatureEfficiency = GameString.Create("OperationalTemperatureEfficiency", "Operational Temperature Efficiency {LOCAL:Value}", "Value");

	public static readonly GameString TemperatureDifferentialEfficiency = GameString.Create("TemperatureDifferentialEfficiency", "Temperature Differential Efficiency {LOCAL:Value}", "Value");

	public static readonly GameString PressureEfficiency = GameString.Create("PressureEfficiency", "Pressure Efficiency {LOCAL:Value}", "Value");

	public static readonly GameString ConversionEfficiency = GameString.Create("ConversionEfficiency", "Conversion Efficiency {LOCAL:Value}", "Value");

	public static readonly GameString CoolingAmount = GameString.Create("CoolingAmount", "Cooling {LOCAL:Value}", "Value");

	public static readonly GameString HeatingAmount = GameString.Create("HeatingAmount", "Heating {LOCAL:Value}", "Value");

	public static readonly GameString LogicDescriptionFreeSlots = GameString.Create("LogicDescriptionFreeSlots", "The number of free slots available in this object.");

	public static readonly GameString LogicDescriptionTotalSlots = GameString.Create("LogicDescriptionTotalSlots", "The total number of slots available in this object.");

	public static readonly GameString LogicDescriptionMaturityRatio = GameString.Create("LogicDescriptionMaturityRatio", "How far the plant is towards maturity represented as a fraction between 0 and 1, with 1 being mature and ready for harvest.");

	public static readonly GameString LogicDescriptionSeedingRatio = GameString.Create("LogicDescriptionSeedingRatio", "How far the plant is towards seeding represented as a fraction between 0 and 1, with 1 being seeding and will supply seeds when harvested.");

	public static readonly GameString LogicDescriptionSemiMajorAxis = GameString.Create("LogicDescriptionSemiMajorAxis", "The longest radius of an elliptical orbit in astronomical units, measuring half the major axis. Determines the size of the orbit.");

	public static readonly GameString LogicDescriptionEccentricity = GameString.Create("LogicDescriptionEccentricity", "A measure of how elliptical (oval) an orbit is. Ranges from 0 (a perfect circle) to 1 (a parabolic trajectory).");

	public static readonly GameString LogicDescriptionInclination = GameString.Create("LogicDescriptionInclination", "The tilt of an orbit's plane relative to the equatorial plane, measured in degrees. Defines the orbital plane's angle.");

	public static readonly GameString LogicDescriptionPeriod = GameString.Create("LogicDescriptionPeriod", "The time it takes for an object to complete one full orbit around another object, measured in days. Indicates the duration of the orbital cycle.");

	public static readonly GameString LogicDescriptionDistanceAu = GameString.Create("LogicDescriptionDistanceAu", "The current distance to the celestial object, measured in astronomical units.");

	public static readonly GameString LogicDescriptionDistanceKm = GameString.Create("LogicDescriptionDistanceKm", "The current distance to the celestial object, measured in kilometers.");

	public static readonly GameString LogicDescriptionAlignmentError = GameString.Create("LogicDescriptionAlignmentError", "The angular discrepancy between the telescope's current orientation and the target. Indicates how 'off target' the telescope is. Returns NaN when no target.");

	public static readonly GameString LogicDescriptionCelestialParentHash = GameString.Create("LogicDescriptionCelestialParentHash", "The hash for the name of the parent the celestial is orbiting, 0 if there is no parent celestial.");

	public static readonly GameString LogicDescriptionTrueAnomaly = GameString.Create("LogicDescriptionTrueAnomaly", "An angular parameter that defines the position of a body moving along a Keplerian orbit. It is the angle between the direction of periapsis and the current position of the body, as seen from the main focus of the ellipse (the point around which the object orbits).");

	public static readonly GameString LogicDescriptionHealthDamage = GameString.Create("LogicDescriptionHealthDamage", "The total amount of health damage on the entity");

	public static readonly GameString LogicDescriptionStunDamage = GameString.Create("LogicDescriptionStunDamage", "The amount of stun damage on the entity");

	public static readonly GameString LogicDescriptionDiscover = GameString.Create("LogicDescriptionDiscover", "Progress status of Discovery scan at the rocket's target Space Map Location. Returns a clamped normalised value. If Discovery scan is not available returns -1.");

	public static readonly GameString LogicDescriptionChart = GameString.Create("LogicDescriptionChart", "Progress status of Chart scan at the rocket's target Space Map Location. Returns a clamped normalised value. If Chart scan is not available returns -1.");

	public static readonly GameString LogicDescriptionSurvey = GameString.Create("LogicDescriptionSurvey", "Progress status of Survey scan at the rocket's target Space Map Location. Returns a normalised value where 100% surveyed is equal to 1. If Survey scan is not available returns -1.");

	public static readonly GameString LogicDescriptionNavPoints = GameString.Create("LogicDescriptionNavPoints", "The number of NavPoints at the rocket's target Space Map Location.");

	public static readonly GameString LogicDescriptionChartedNavPoints = GameString.Create("LogicDescriptionChartedNavPoints", "The number of charted NavPoints at the rocket's target Space Map Location.");

	public static readonly GameString LogicDescriptionSites = GameString.Create("LogicDescriptionSites", "The number of Sites that have been discovered at the rockets target Space Map location.");

	public static readonly GameString LogicDescriptionCurrentCode = GameString.Create("LogicDescriptionCurrentCode", "The Space Map Address of the rockets current Space Map Location");

	public static readonly GameString LogicDescriptionDestinationCode = GameString.Create("LogicDescriptionDestinationCode", "The Space Map Address of the rockets target Space Map Location");

	public static readonly GameString LogicDescriptionSize = GameString.Create("LogicDescriptionSize", "The size of the rocket's target site's mine-able deposit.");

	public static readonly GameString LogicDescriptionRichness = GameString.Create("LogicDescriptionRichness", "The richness of the rocket's target site's mine-able deposit.");

	public static readonly GameString LogicDescriptionDensity = GameString.Create("LogicDescriptionDensity", "The density of the rocket's target site's mine-able deposit.");

	public static readonly GameString LogicDescriptionTotalQuantity = GameString.Create("LogicDescriptionTotalQuantity", "The estimated total quantity of resources available to mine at the rocket's target Space Map Site.");

	public static readonly GameString LogicDescriptionMinedQuantity = GameString.Create("LogicDescriptionMinedQuantity", "The total number of resources that have been mined at the rocket's target Space Map Site.");

	public static readonly GameString LogicDescriptionBestContactFilter = GameString.Create("LogicDescriptionBestContactFilter", "Filters the satellite's auto selection of targets to a single reference ID.");

	public static readonly GameString LogicDescriptionTargetSlotIndex = GameString.Create("LogicDescriptionTargetSlotIndex", "The slot index that the target device that this device will try to interact with");

	public static readonly GameString LogicDescriptionTargetPrefabHash = GameString.Create("LogicDescriptionTargetPrefabHash", "The prefab");

	public static readonly GameString LogicDescriptionExtended = GameString.Create("LogicDescriptionExtended", "Extended");

	public static readonly GameString LogicDescriptionCelestialHash = GameString.Create("LogicDescriptionCelestialHash", "The current hash of the targeted celestial object.");

	public static readonly GameString LogicDescriptionEntityState = GameString.Create("LogicDescriptionEntityState", "The current entity state, such as whether it is dead, unconscious or alive, expressed as a state integer.");

	public static readonly GameString LogicDescriptionApex = GameString.Create("LogicDescriptionApex", "The lowest altitude that the rocket will reach before it starts travelling upwards again.");

	public static readonly GameString LogicDescriptionIndex = GameString.Create("LogicDescriptionIndex", "The current index for the device.");

	public static readonly GameString LogicDescriptionDrillCondition = GameString.Create("LogicDescriptionDrillCondition", "The current condition of the drill head in this devices drill slot. Expressed as a ratio between 0 and 1.");

	public static readonly GameString LogicDescriptionVelocityX = GameString.Create("LogicDescriptionVelocityX", "The world velocity of the entity in the X axis");

	public static readonly GameString LogicDescriptionVelocityY = GameString.Create("LogicDescriptionVelocityY", "The world velocity of the entity in the Y axis");

	public static readonly GameString LogicDescriptionVelocityZ = GameString.Create("LogicDescriptionVelocityZ", "The world velocity of the entity in the Z axis");

	public static readonly GameString LogicDescriptionOrientation = GameString.Create("LogicDescriptionOrientation", "The orientation of the entity in degrees in a plane relative towards the north origin");

	public static readonly GameString LogicDescriptionForward = GameString.Create("LogicDescriptionForward", "The direction the entity is facing expressed as a normalized vector");

	public static readonly GameString LogicDescriptionReferenceId = GameString.Create("LogicDescriptionReferenceId", "Unique Reference Identifier for this object");

	public static readonly GameString LogicDescriptionDispense = GameString.Create("LogicDescriptionDispense", "The device will dispense once, used in logic mode for export devices.");

	public static readonly GameString LogicDescriptionDispenseSlot = GameString.Create("LogicDescriptionDispenseSlot", "Set to the index of a stored item to dispense that specific item once. The device dispenses it then resets this back to -1. A value of -1 means idle.");

	public static readonly GameString ScriptDescriptionSrl = GameString.Create("ScriptDescriptionSrl", "Performs a bitwise logical right shift operation on the binary representation of a value. It shifts the bits to the right and fills the vacated leftmost bits with zeros");

	public static readonly GameString ScriptDescriptionSra = GameString.Create("ScriptDescriptionSra", "Performs a bitwise arithmetic right shift operation on the binary representation of a value. It shifts the bits to the right and fills the vacated leftmost bits with a copy of the sign bit (the most significant bit).");

	public static readonly GameString ScriptDescriptionSll = GameString.Create("ScriptDescriptionSll", "Performs a bitwise logical left shift operation on the binary representation of a value. It shifts the bits to the left and fills the vacated rightmost bits with zeros.");

	public static readonly GameString ScriptDescriptionSla = GameString.Create("ScriptDescriptionSla", "Performs a bitwise arithmetic left shift operation on the binary representation of a value. It shifts the bits to the left and fills the vacated rightmost bits with a copy of the sign bit (the most significant bit).");

	public static readonly GameString ScriptDescriptionNot = GameString.Create("ScriptDescriptionNot", "Performs a bitwise logical NOT operation flipping each bit of the input value, resulting in a binary complement. If a bit is 1, it becomes 0, and if a bit is 0, it becomes 1.");

	public static readonly GameString ScriptDescriptionAnd = GameString.Create("ScriptDescriptionAnd", "Performs a bitwise logical AND operation on the binary representation of two values. Each bit of the result is determined by evaluating the corresponding bits of the input values. If both bits are 1, the resulting bit is set to 1. Otherwise the resulting bit is set to 0.");

	public static readonly GameString ScriptDescriptionOr = GameString.Create("ScriptDescriptionOr", "Performs a bitwise logical OR operation on the binary representation of two values. Each bit of the result is determined by evaluating the corresponding bits of the input values. If either bit is 1, the resulting bit is set to 1. If both bits are 0, the resulting bit is set to 0.");

	public static readonly GameString ScriptDescriptionXor = GameString.Create("ScriptDescriptionXor", "Performs a bitwise logical XOR (exclusive OR) operation on the binary representation of two values. Each bit of the result is determined by evaluating the corresponding bits of the input values. If the bits are different (one bit is 0 and the other is 1), the resulting bit is set to 1. If the bits are the same (both 0 or both 1), the resulting bit is set to 0.");

	public static readonly GameString ScriptDescriptionNor = GameString.Create("ScriptDescriptionNor", "Performs a bitwise logical NOR (NOT OR) operation on the binary representation of two values. Each bit of the result is determined by evaluating the corresponding bits of the input values. If both bits are 0, the resulting bit is set to 1. Otherwise, if at least one bit is 1, the resulting bit is set to 0.");

	public static readonly GameString ScriptDescriptionSbn = GameString.Create("ScriptDescriptionSbn", "Stores register value to LogicType on all output network devices with provided type hash and name.");

	public static readonly GameString ScriptDescriptionGet = GameString.Create("ScriptDescriptionGet", "Using the provided device, attempts to read the stack value at the provided address, and places it in the register.");

	public static readonly GameString ScriptDescriptionClr = GameString.Create("ScriptDescriptionClr", "Clears the stack memory for the provided device.");

	public static readonly GameString ScriptDescriptionClrD = GameString.Create("ScriptDescriptionClrD", "Seeks directly for the provided device id and clears the stack memory of that device");

	public static readonly GameString ScriptDescriptionPut = GameString.Create("ScriptDescriptionPut", "Using the provided device, attempts to write the provided value to the stack at the provided address.");

	public static readonly GameString ScriptDescriptionGetD = GameString.Create("ScriptDescriptionGetD", "Seeks directly for the provided device id, attempts to read the stack value at the provided address, and places it in the register.");

	public static readonly GameString ScriptDescriptionPutD = GameString.Create("ScriptDescriptionPutD", "Seeks directly for the provided device id, attempts to write the provided value to the stack at the provided address.");

	public static readonly GameString ScriptDescriptionPoke = GameString.Create("ScriptDescriptionPoke", "Stores the provided value at the provided address in the stack.");

	public static readonly GameString ScriptDescriptionRMap = GameString.Create("ScriptDescriptionRMap", "Given a reagent hash, store the corresponding prefab hash that the device expects to fulfill the reagent requirement. For example, on an autolathe, the hash for Iron will store the hash for ItemIronIngot.");

	public static readonly GameString ScriptDescriptionBdnvl = GameString.Create("ScriptDescriptionBdnvl", "Will branch to line a if the provided device not valid for a load instruction for the provided logic type.");

	public static readonly GameString ScriptDescriptionBdnvs = GameString.Create("ScriptDescriptionBdnvs", "Will branch to line a if the provided device not valid for a store instruction for the provided logic type.");

	public static readonly GameString ScriptDescriptionPow = GameString.Create("ScriptDescriptionPow", "Stores the result of raising a to the power of b in the register. Follows IEEE-754 standard for floating point arithmetic.");

	public static readonly GameString ScriptDescriptionExt = GameString.Create("ScriptDescriptionExt", "Extracts a bit field from a, beginning at b for c length and placed in the provided register. Payload cannot exceed 53 bits in final length.");

	public static readonly GameString ScriptDescriptionIns = GameString.Create("ScriptDescriptionIns", "Inserts a bit field of a into the provided register, beginning at b for c length. Payload cannot exceed 53 bits in final length.");

	public static readonly GameString ScriptDescriptionLerp = GameString.Create("ScriptDescriptionLerp", "Linearly interpolates between a and b by the ratio c, and places the result in the register provided. The ratio c will be clamped between 0 and 1.");

	public static readonly GameString ScriptDescriptionSgn = GameString.Create("ScriptDescriptionSgn", "Stores the sign of a in the register: -1 if a is negative, 1 if positive, and 0 if a is zero (or not a number).");

	public static readonly GameString ScriptDescriptionClamp = GameString.Create("ScriptDescriptionClamp", "Stores a clamped to the inclusive range [min, max] in the register provided.");

	public static readonly GameString ScriptDescriptionRol = GameString.Create("ScriptDescriptionRol", "Performs a bitwise left rotation on the binary representation of a by b places, wrapping the bits shifted out of the most significant position back into the least significant position.");

	public static readonly GameString ScriptDescriptionRor = GameString.Create("ScriptDescriptionRor", "Performs a bitwise right rotation on the binary representation of a by b places, wrapping the bits shifted out of the least significant position back into the most significant position.");

	public static readonly GameString AchievementUnlocked = GameString.Create("AchievementUnlocked", "Achievement '{LOCAL:Name}' unlocked", "Name");

	public static readonly GameString FurnaceMeltThing = GameString.Create("FurnaceMeltThing", "{LOCAL:Thing} can be melted by pressing <color=yellow>Activate</color>", "Thing");

	public static readonly GameString UnitSeconds = GameString.Create("UnitSeconds", "{LOCAL:Value} seconds", "Value");

	public static readonly GameString UnitSecond = GameString.Create("UnitSecond", "{LOCAL:Value} second", "Value");

	public static readonly GameString UnitMinutes = GameString.Create("UnitMinutes", "{LOCAL:Value} minutes", "Value");

	public static readonly GameString UnitMinute = GameString.Create("UnitMinute", "{LOCAL:Value} minute", "Value");

	public static readonly GameString UnitHours = GameString.Create("UnitHours", "{LOCAL:Value} hours", "Value");

	public static readonly GameString UnitHour = GameString.Create("UnitHour", "{LOCAL:Value} hour", "Value");

	public static readonly GameString UnitDays = GameString.Create("UnitDays", "{LOCAL:Value} days", "Value");

	public static readonly GameString UnitDay = GameString.Create("UnitDay", "{LOCAL:Value} day", "Value");

	public static readonly GameString UnitWeeks = GameString.Create("UnitWeeks", "{LOCAL:Value} weeks", "Value");

	public static readonly GameString UnitWeek = GameString.Create("UnitWeek", "{LOCAL:Value} week", "Value");

	public static readonly GameString UnitMonths = GameString.Create("UnitMonths", "{LOCAL:Value} months", "Value");

	public static readonly GameString UnitMonth = GameString.Create("UnitMonth", "{LOCAL:Value} month", "Value");

	public static readonly GameString UnitYears = GameString.Create("UnitYears", "{LOCAL:Value} years", "Value");

	public static readonly GameString UnitYear = GameString.Create("UnitYear", "{LOCAL:Value} year", "Value");

	public static readonly GameString ManufacturesAppliesMultiplier = GameString.Create("ManufacturesAppliesMultiplier", "<color=orange>{LOCAL:ModifierName}</color> operates at <color=yellow>{LOCAL:Percent}</color>", "ModifierName", "Percent");

	public static readonly GameString ManufacturesAtTier = GameString.Create("ManufacturesAtTier", "Manufactures at <color=yellow>{LOCAL:MachineTier}</color>", "MachineTier");

	public static readonly GameString StationpediaBuildStateUpgrade = GameString.Create("StationpediaBuildStateUpgrade", "<color=yellow>Device Upgrade</color>");

	public static readonly GameString WaterWrongTemp = GameString.Create("WaterWrongTemp", "<color=red>Error!</color> Water must be between <color=yellow>0 and 100 degrees C</color>");

	public static readonly GameString ToxicLiquidsInPipe = GameString.Create("ToxicLiquidsInPipe", "<color=red>Error!</color> Toxins present in pipe network");

	public static readonly GameString NoWaterAvailable = GameString.Create("NoWaterAvailable", "<color=red>Error!</color> there is no <color=yellow>Water Pressure</color>");

	public static readonly GameString PowerControlSetTo = GameString.Create("PowerControlSetTo", "Set <b>{LOCAL:Category}</b> {LOCAL:OnState}", "Category", "OnState");

	public static readonly GameString InternalPressureIs = GameString.Create("InternalPressureIs", "Pressure is <color=yellow>{LOCAL:Value}</color>", "Value");

	public static readonly GameString ThingOverPressure = GameString.Create("ThingOverPressure", "The {LOCAL:Thing} is <color=red>Over Pressure</color>!", "Thing");

	public static readonly GameString ThingOverTemperature = GameString.Create("ThingOverTemperature", "The {LOCAL:Thing} is <color=red>Over Heating</color>!", "Thing");

	public static readonly GameString HeatingFromInternal = GameString.Create("HeatingFromInternal", "The {LOCAL:Thing} is <color=white>Heating</color> from {LOCAL:Source} at {LOCAL:Energy}", "Thing", "Source", "Energy");

	public static readonly GameString HeatingFromElement = GameString.Create("HeatingFromElement", "The {LOCAL:Thing} is <color=white>Heating</color> from <color=yellow>Element</color> at {LOCAL:Energy}", "Thing", "Energy");

	public static readonly GameString CoolantFrozen = GameString.Create("CoolantFrozen", "The {LOCAL:Thing} is <color=blue>Frozen</color>", "Thing");

	public static readonly GameString WorldEnvironmentTooHot = GameString.Create("WorldEnvironmentTooHot", "The operating environment is <color=orange>too hot</color>");

	public static readonly GameString WorldEnvironmentTooCold = GameString.Create("WorldEnvironmentTooCold", "The operating environment is <color=blue>too cold</color>");

	public static readonly GameString CoolingFromInternal = GameString.Create("CoolingFromInternal", "The {LOCAL:Thing} is <color=white>Cooling</color> from {LOCAL:Source} at {LOCAL:Energy}", "Thing", "Source", "Energy");

	public static readonly GameString PressureSafteySetTo = GameString.Create("PressureSafteySetTo", "The <color=green>{LOCAL:Location} Safety</color> is set to <color=yellow>{LOCAL:Value}</color>", "Location", "Value");

	public static readonly GameString DeviceWorldGridBlocked = GameString.Create("DeviceWorldGridBlocked", "The <color=white>Output</color> of {LOCAL:Thing} is <color=red>Blocked</color>", "Thing");

	public static readonly GameString DeviceOutputCrewModule = GameString.Create("DeviceOutputCrewModule", "Will <color=white>Output</color> into {LOCAL:Thing}", "Thing");

	public static readonly GameString TemperatureTooHotForThing = GameString.Create("TemperatureTooHotForThing", "The temperature is too hot for {LOCAL:Thing} to operate", "Thing");

	public static readonly GameString TemperatureTooColdForThing = GameString.Create("TemperatureTooColdForThing", "The temperature is too cold for {LOCAL:Thing} to operate", "Thing");

	public static readonly GameString AtmosphereExternal = GameString.Create("AtmosphereExternal", "External");

	public static readonly GameString AtmosphereInternal = GameString.Create("AtmosphereInternal", "Internal");

	public static readonly GameString IngotIsType = GameString.Create("IngotIsType", "This is a <color=yellow>{LOCAL:Type}</color>", "Type");

	public static readonly GameString CannotBePickedUpRightNow = GameString.Create("CannotBePickedUpRightNow", "Cannot be picked up right now");

	public static readonly GameString CurrentlyOnFire = GameString.Create("CurrentlyOnFire", "Currently <color=red>On Fire</color>");

	public static readonly GameString IsDestroyed = GameString.Create("IsDestroyed", "Is <color=red>Destroyed</color>");

	public static readonly GameString InputChatMessage = GameString.Create("InputChatMessage", "Enter Message to Send");

	public static readonly GameString TypeOfSlot = GameString.Create("TypeOfSlot", "Inside {LOCAL:SlotType} slot", "SlotType");

	public static readonly GameString ItemInSlot = GameString.Create("ItemInSlot", "Stack of <color=yellow>{LOCAL:StackSize}</color> x {LOCAL:Thing}", "Thing", "StackSize");

	public static readonly GameString ItemInSlotStack = GameString.Create("ItemInSlotStack", "Stack of <color=yellow>{LOCAL:StackSize}</color> x {LOCAL:Thing}", "Thing", "StackSize");

	public static readonly GameString ItemInSlotValue = GameString.Create("ItemInSlotValue", "The {LOCAL:Thing} is at <color=yellow>{LOCAL:Value}</color>", "Thing", "Value");

	public static readonly GameString DoorIsWelded = GameString.Create("DoorIsWelded", "The {LOCAL:Thing} cannot be operated while it is <color=yellow>welded</color>", "Thing");

	public static readonly GameString MoleMixtureMustBePure = GameString.Create("MoleMixtureMustBePure", "Requires a <color=#44AD83>pure gas mixture</color> of only the listed types");

	public static readonly GameString ToolRequiredToDeconstruct = GameString.Create("ToolRequiredToDeconstruct", "{LOCAL:Tool} required to <color=red>deconstruct</color>", "Tool");

	public static readonly GameString GridBlockedByStructure = GameString.Create("GridBlockedByStructure", "Placement is blocked by <color=green>{LOCAL:Structure}</color>", "Structure");

	public static readonly GameString FaceBlockedByStructure = GameString.Create("FaceBlockedByStructure", "Placement is blocked by <color=green>{LOCAL:Structure}</color>", "Structure");

	public static readonly GameString PlacementBlockedByStructure = GameString.Create("PlacementBlockedByStructure", "Placement is blocked by <color=green>{LOCAL:Structure}</color>", "Structure");

	public static readonly GameString PlacementRequiresHydroponicsTray = GameString.Create("PlacementRequiresHydroponicsTray", "Placement requires a Hydroponics tray below");

	public static readonly GameString CannotMergeTwoOperationalElevatorShafts = GameString.Create("CannotMergeTwoOperationalElevatorShafts", "Can not merge two operational elevator shafts");

	public static readonly GameString ElevatorShaftRotationFail = GameString.Create("ElevatorShaftRotationFail", "All Elevator building pieces must be rotated the same direction");

	public static readonly GameString PlacementBlockedByAdjacentDevice = GameString.Create("PlacementBlockedByAdjacentDevice", "Cannot place adjacent to <color=green>{LOCAL:Device}</color>", "Device");

	public static readonly GameString PlacementBlockedByUnknownDevice = GameString.Create("PlacementBlockedByUnknownDevice", "Cannot place adjacent to another Device");

	public static readonly GameString PlacementBlockedBySmallGrid = GameString.Create("PlacementBlockedBySmallGrid", "Placement is blocked by <color=green>{LOCAL:SmallGrid}</color>", "SmallGrid");

	public static readonly GameString PlacementRequiresFrame = GameString.Create("PlacementRequiresFrame", "Placement requires a <color=green>Frame</color> below for support");

	public static readonly GameString PlacementRequiresFrameOrTower = GameString.Create("PlacementRequiresFrameOrTower", "Placement requires a <color=green>Frame</color> or <color=green>Launch Tower</color> below for support");

	public static readonly GameString PlacementMustBeOnTerrain = GameString.Create("PlacementMustBeOnTerrain", "Placement requires <color=yellow>Terrain</color> below");

	public static readonly GameString PlacementRequiresFrameOrGround = GameString.Create("PlacementRequiresFrameOrGround", "Placement requires a <color=green>Frame</color> or solid ground below for support");

	public static readonly GameString PlacementRequiresPipeValve = GameString.Create("PlacementRequiresPipeValve", "Must be placed after a pipe valve (or any other atmospherics device)");

	public static readonly GameString PipeContentTypeIncorrect = GameString.Create("PipeContentTypeIncorrect", "Pipe content type does not match content type of <color=green>{LOCAL:DevicePipeMounted}</color>", "DevicePipeMounted");

	public static readonly GameString CannotMergeWithSmallGrid = GameString.Create("CannotMergeWithSmallGrid", "Cannot merge with <color=green>{LOCAL:SmallGrid}</color>", "SmallGrid");

	public static readonly GameString CannotPlaceOnBrokenCable = GameString.Create("CannotPlaceOnBrokenCable", "Cannot Place on broken cable");

	public static readonly GameString CannotPlaceOnBurstPipe = GameString.Create("CannotPlaceOnBurstPipe", "Cannot Place on burst pipe");

	public static readonly GameString MustMountToCable = GameString.Create("MustMountToCable", "Must be mounted to a straight cable");

	public static readonly GameString MustMountToPipe = GameString.Create("MustMountToPipe", "Must be mounted to a straight pipe");

	public static readonly GameString CannotMergeIMergeable = GameString.Create("CannotMergeIMergeable", "This {LOCAL:IMergeable} cannot be merged", "IMergeable");

	public static readonly GameString CannotMergeIMergeableOfDifferentType = GameString.Create("CannotMergeIMergeableOfDifferentType", "Cannot merge with {LOCAL:IMergeable} as it is a different type", "IMergeable");

	public static readonly GameString MergeRequiresTool = GameString.Create("MergeRequiresTool", "Merging requires <color=green>{LOCAL:Tool}</color> in other hand", "Tool");

	public static readonly GameString CableRunTooLong = GameString.Create("CableRunTooLong", "Cable run is too long");

	public static readonly GameString CableRunNoValidTarget = GameString.Create("CableRunNoValidTarget", "No valid target for cable run");

	public static readonly GameString CableRunNotEnoughCable = GameString.Create("CableRunNotEnoughCable", "Not enough <color=green>{LOCAL:Coil}</color> (<color=yellow>{LOCAL:Count}</color>) for this run", "Coil", "Count");

	public static readonly GameString CableRunLength = GameString.Create("CableRunLength", "Placing <color=yellow>{LOCAL:Count}</color> x {LOCAL:Cable} (costs <color=yellow>{LOCAL:Cost}</color> x <color=green>{LOCAL:Coil}</color>)", "Count", "Cable", "Cost", "Coil");

	public static readonly GameString EntityIsCurrentlyState = GameString.Create("EntityIsCurrentlyState", "{LOCAL:Thing} is currently <color=yellow>{LOCAL:State}</color>", "Thing", "State");

	public static readonly GameString EntityIsDead = GameString.Create("EntityIsDead", "{LOCAL:Thing} is <color=red>Dead</color>", "Thing");

	public static readonly GameString PlacementRequiresModularRocket = GameString.Create("PlacementRequiresModularRocket", "Placement requires a modular rocket piece below");

	public static readonly GameString CannotPlaceOnCommandModule = GameString.Create("CannotPlaceOnCommandModule", "Cannot place this rocket piece above a command module piece");

	public static readonly GameString CouplerBetweenTwoRocketPieces = GameString.Create("CouplerBetweenTwoRocketPieces", "Coupling Unit must be placed at the intersection of two rocket pieces");

	public static readonly GameString RocketRotationFail = GameString.Create("RocketRotationFail", "All rocket pieces must be rotated the same direction");

	public static readonly GameString PlacementRequiresLaunchPad = GameString.Create("PlacementRequiresLaunchPad", "Placement requires Launchpad below");

	public static readonly GameString WeldingWillLockDoor = GameString.Create("WeldingWillLockDoor", "<color=yellow>Welding</color> the {LOCAL:Thing} will lock it in place", "Thing");

	public static readonly GameString UnweldingWillUnLockDoor = GameString.Create("UnweldingWillUnLockDoor", "<color=yellow>Unwelding</color> the {LOCAL:Thing} will unlock it", "Thing");

	public static readonly GameString FurnaceCurrentlySmelting = GameString.Create("FurnaceCurrentlySmelting", "Currently Smelting {LOCAL:OreName} x <color=yellow>{LOCAL:Quantity}</color>", "OreName", "Quantity");

	public static readonly GameString FurnaceNothingToSmelt = GameString.Create("FurnaceNothingToSmelt", "<color=red>Nothing to Smelt</color>");

	public static readonly GameString DeviceAlreadyExporting = GameString.Create("DeviceAlreadyExporting", "Already exporting {LOCAL:ItemName}", "ItemName");

	public static readonly GameString SuitContextAirConState = GameString.Create("SuitContextAirConState", "A/C {LOCAL:IsTurnedOn}", "IsTurnedOn");

	public static readonly GameString SuitContextAirPumpState = GameString.Create("SuitContextAirPumpState", "Air {LOCAL:IsTurnedOn}", "IsTurnedOn");

	public static readonly GameString SuitContextFilterState = GameString.Create("SuitContextFilterState", "Filter {LOCAL:IsTurnedOn}", "IsTurnedOn");

	public static readonly GameString SaveButtonGameString = GameString.Create("SaveButtonGameString", "Save <i>{LOCAL:WorldName}</i>", "WorldName");

	public static readonly GameString SaveAsButtonGameString = GameString.Create("SaveAsButtonGameString", "Save As...");

	public static readonly GameString InputPasswordTitle = GameString.Create("InputPasswordTitle", "Password");

	public static readonly GameString InputPasswordPlaceholder = GameString.Create("InputPasswordPlaceholder", "Input Password");

	public static readonly GameString InputSaveNameTitle = GameString.Create("InputSaveNameTitle", "Save Name");

	public static readonly GameString InputSaveNamePlaceholder = GameString.Create("InputSaveNamePlaceholder", "Enter Save Name");

	public static readonly GameString InteractableAction = GameString.Create("InteractableAction", "Set {LOCAL:InteractableAction}", "InteractableAction");

	public static readonly GameString InteractableActionToState = GameString.Create("InteractableActionToState", "Set {LOCAL:InteractableAction} {LOCAL:State}", "InteractableAction", "State");

	public static readonly GameString DeleteSavePrompt = GameString.Create("DeleteSavePrompt", "Are you sure you want to delete:\n<color=red>delete {LOCAL:WorldSaveName}</color>", "WorldSaveName");

	public static readonly GameString DeleteBackSavePrompt = GameString.Create("DeleteBackSavePrompt", "Are you sure?\nThis will <color=red>delete {LOCAL:WorldSaveName}</color> backup only.", "WorldSaveName");

	public static readonly GameString DaysPassedPlayerMessage = GameString.Create("DaysPassedPlayerMessage", "{LOCAL:DaysPassed} days have passed. Your character has survived {LOCAL:DaysLived} without dying.", "DaysPassed", "DaysLived");

	public static readonly GameString DaysPassedHudText = GameString.Create("DaysPassedHudText", "DAY {LOCAL:DaysPassed}", "DaysPassed");

	public static readonly GameString CustomSkeletonName = GameString.Create("CustomSkeletonName", "Human Skeleton({LOCAL:UserName})", "UserName");

	public static readonly GameString CustomSkullName = GameString.Create("CustomSkullName", "Human Skull({LOCAL:UserName})", "UserName");

	public static readonly GameString ClearStacker = GameString.Create("ClearStacker", "Clear Stacker");

	public static readonly GameString QuantityOfGasTypeInMix = GameString.Create("QuantityOfGasTypeInMix", "<color=yellow>{LOCAL:Quantity}</color> x {LOCAL:GasType}", "Quantity", "GasType");

	public static readonly GameString InvalidAttachmentsDeconstruct = GameString.Create("InvalidAttachmentsDeconstruct", "Cannot deconstruct while {LOCAL:Device} is attached to it", "Device");

	public static readonly GameString PlacementIsNotUmbilicalConnector = GameString.Create("PlacementIsNotUmbilicalConnector", "Only another <color=yellow>Umbilical</color> can connect to this side of {LOCAL:Device}", "Device");

	public static readonly GameString CannotDeconstructLaunchMount = GameString.Create("CannotDeconstructLaunchMount", "Cannot deconstruct {LOCAL:Mount} as {LOCAL:Rocket} is interacting with it", "Mount", "Rocket");

	public static readonly GameString StructureIsCompleted = GameString.Create("StructureIsCompleted", "Must be fully constructed");

	public static readonly GameString StructureCanManufacture = GameString.Create("StructureCanManufacture", "BuildState can manufacture");

	public static readonly GameString PlantRecordCondition = GameString.Create("PlantRecordCondition", "{LOCAL:StatusType} record must be {LOCAL:ComparisonOperator} {LOCAL:Value}", "StatusType", "ComparisonOperator", "Value");

	public static readonly GameString PlantStatusCondition = GameString.Create("PlantStatusCondition", "{LOCAL:StatusType} status must be {LOCAL:Value}", "StatusType", "Value");

	public static readonly GameString GrowthStateCondition = GameString.Create("GrowthStateCondition", "Growth state must be {LOCAL:ComparisonOperator} {LOCAL:Value}", "ComparisonOperator", "Value");

	public static readonly GameString PreSpawnedCondition = GameString.Create("PreSpawnedCondition", "Was pre-spawned: {LOCAL:Value}", "Value");

	public static readonly GameString HumanCondition = GameString.Create("HumanCondition", "Player is {LOCAL:IsOnline} {LOCAL:State}", "IsOnline", "State");

	public static readonly GameString TemperatureComparableCondition = GameString.Create("TemperatureComparableCondition", "Temperature must be {LOCAL:ComparisonOperator} {LOCAL:Value}", "ComparisonOperator", "Value");

	public static readonly GameString RatioTooltip = GameString.Create("RatioTooltip", "Ratio <color=green>{LOCAL:RatioValue1}:{LOCAL:RatioValue2}</color>", "RatioValue1", "RatioValue2");

	public static readonly GameString QuantityAndRatioTooltip = GameString.Create("QuantityAndRatioTooltip", "Quantity <color=green>{LOCAL:QuantityValue}\n<color=white>Mode <color=green>{LOCAL:RatioValue1}:{LOCAL:RatioValue2}", "QuantityValue", "RatioValue1", "RatioValue2");

	public static readonly GameString ToggleMode = GameString.Create("ToggleMode", "Toggle Mode");

	public static readonly GameString Ratio = GameString.Create("Ratio", "Ratio");

	public static readonly GameString CloseThreshold = GameString.Create("CloseThreshold", "Close Threshold");

	public static readonly GameString CloseThresholdToolTip = GameString.Create("CloseThresholdToolTip", "Close Threshold <color=green>{LOCAL:ThresholdValue}", "ThresholdValue");

	public static readonly GameString IceCrusherState = GameString.Create("IceCrusherState", "Ice Crusher Status");

	public static readonly GameString InternalAtmosphere = GameString.Create("InternalAtmosphere", "Internal Atmosphere");

	public static readonly GameString IceCrusherSetting = GameString.Create("IceCrusherSetting", "Will Heat if below {LOCAL:Temperature}", "Temperature");

	public static readonly GameString IceCrusherHeater = GameString.Create("IceCrusherHeater", "Heating Element");

	public static readonly GameString IceCrusherHeating = GameString.Create("IceCrusherHeating", "Operation slowed while heating to {LOCAL:Temperature} target", "Temperature");

	public static readonly GameString IceCrusherIndicatorHeat = GameString.Create("IceCrusherIndicatorHeat", "Heating");

	public static readonly GameString IceCrusherIndicatorIdle = GameString.Create("IceCrusherIndicatorIdle", "Idle");

	public static readonly GameString IceCrusherAtmosFull = GameString.Create("IceCrusherAtmosFull", "Internal Atmosphere is Full");

	public static readonly GameString BreathingAtmosphere = GameString.Create("BreathingAtmosphere", "Occupant Atmosphere");

	public static readonly GameString CryogenicLiquid = GameString.Create("CryogenicLiquid", "Cryogenic Liquid");

	public static readonly GameString NotPureLiquidNitrogen = GameString.Create("NotPureLiquidNitrogen", "Liquid is not pure Liquid Nitrogen");

	public static readonly GameString CryoLiquidVolumeTooLow = GameString.Create("CryoLiquidVolumeTooLow", "Cryogenic Liquid volume must be above {LOCAL:Volume}", "Volume");

	public static readonly GameString CryoLiquidTemperatureTooHigh = GameString.Create("CryoLiquidTemperatureTooHigh", "Cryogenic liquid must be below {LOCAL:Temperature}", "Temperature");

	public static readonly GameString CryoConditionsNotMet = GameString.Create("CryoConditionsNotMet", "Cryogenic conditions not met");

	public static readonly GameString CryoWillRevive = GameString.Create("CryoWillRevive", "Will regenerate deceased patients");

	public static readonly GameString CryoState = GameString.Create("CryoState", "Cryogenic Status");

	public static readonly GameString SleeperState = GameString.Create("SleeperState", "Sleeper Status");

	public static readonly GameString SleeperMetabolicSuspension = GameString.Create("SleeperMetabolicSuspension", "Metabolic Suspension Active");

	public static readonly GameString SleeperDoorIsOpen = GameString.Create("SleeperDoorIsOpen", "Operation disabled while door open");

	public static readonly GameString CryoWillHeal = GameString.Create("CryoWillHeal", "Will heal damaged patients");

	public static readonly GameString UnidentifiedPlayer = GameString.Create("UnidentifiedPlayer", "Unidentified player");

	public static readonly GameString PlayerHasAlreadyRespawned = GameString.Create("PlayerHasAlreadyRespawned", "<color=green>{LOCAL:UserName}</color> has <color=yellow>respawned</color> and cannot be revived.", "UserName");

	public static readonly GameString PlayerBodyBagDead = GameString.Create("PlayerBodyBagDead", "<color=green>{LOCAL:UserName}</color> is <color=red>dead</color> in the body bag and waiting to be revived.", "UserName");

	public static readonly GameString PlayerIsOnline = GameString.Create("PlayerIsOnline", "<color=green>{LOCAL:UserName}</color> is <color=green>online</color>.", "UserName");

	public static readonly GameString PlayerIsOffline = GameString.Create("PlayerIsOffline", "<color=green>{LOCAL:UserName}</color> is <color=yellow>offline</color>.", "UserName");

	public static readonly GameString BodyBagName = GameString.Create("BodyBagName", "Body Bag(<color=green>{LOCAL:UserName}</color>)", "UserName");

	public static readonly GameString TrackablePlayerBodyBag = GameString.Create("TrackablePlayerBodyBag", "{LOCAL:UserName}'s Body Bag", "UserName");

	public static readonly GameString PlayerBelongings = GameString.Create("PlayerBelongings", "Belongings");

	public static readonly GameString PlayerReviveInCryotube = GameString.Create("PlayerReviveInCryotube", "Place <color=green>{LOCAL:UserName}</color> in a <color=green>Cryotube</color> to revive.", "UserName");

	public static readonly GameString PlayerRequestHelpUnconscious = GameString.Create("PlayerRequestHelpUnconscious", "{LOCAL:UserName} is in trouble and needs help.", "UserName");

	public static readonly GameString PlayerRequestHelpDead = GameString.Create("PlayerRequestHelpDead", "{LOCAL:UserName} is dead. They have requested they be revived in a Cryotube.", "UserName");

	public static readonly GameString LoadingScreenDeserializeChunks = GameString.Create("LoadingScreenDeserializeChunks", "Deserializing Chunks");

	public static readonly GameString LoadingScreenDeserializingAtmospherics = GameString.Create("LoadingScreenDeserializingAtmospherics", "Deserializing Atmospherics");

	public static readonly GameString LoadingScreenDeserializeCableNetworks = GameString.Create("LoadingScreenDeserializeCableNetworks", "Deserializing Cable Networks");

	public static readonly GameString LoadingScreenDeserializeTerraformingValues = GameString.Create("LoadingScreenDeserializeTerraformingValues", "Deserializing Terraforming values");

	public static readonly GameString LoadingScreenPleaseWait = GameString.Create("LoadingScreenPleaseWait", "Please Wait");

	public static readonly GameString LoadingScreenClientJoining = GameString.Create("LoadingScreenClientJoining", "Client Joining");

	public static readonly GameString LoadingScreenReceivingJoinData = GameString.Create("LoadingScreenReceivingJoinData", "Receiving Join Data");

	public static readonly GameString LoadingScreenRegisteringPrefabs = GameString.Create("LoadingScreenRegisteringPrefabs", "Registering Prefabs");

	public static readonly GameString LoadingScreenConnectingToServer = GameString.Create("LoadingScreenConnectingToServer", "Connecting to Server");

	public static readonly GameString LoadingScreenReadyToJoin = GameString.Create("LoadingScreenReadyToJoin", "Ready To Join");

	public static readonly GameString LoadingScreenGeneratingTerrain = GameString.Create("LoadingScreenGeneratingTerrain", "Generating Terrain");

	public static readonly GameString LoadingScreenFinishingGeneratingTerrain = GameString.Create("LoadingScreenFinishingGeneratingTerrain", "Finished Generating Terrain");

	public static readonly GameString LoadingScreenDeserializingChuteNetworks = GameString.Create("LoadingScreenDeserializingChuteNetworks", "Deserializing Chute Networks");

	public static readonly GameString LoadingScreenProcessingThings = GameString.Create("LoadingScreenProcessingThings", "Processing Things");

	public static readonly GameString LoadingScreenDeserializingNetworks = GameString.Create("LoadingScreenDeserializingNetworks", "Deserializing Networks");

	public static readonly GameString LoadingScreenDeserializingPipeNetworks = GameString.Create("LoadingScreenDeserializingPipeNetworks", "Deserializing Pipe Networks");

	public static readonly GameString LoadingScreenDeserializeRocketNetworks = GameString.Create("LoadingScreenDeserializeRocketNetworks", "Deserializing Rocket Networks");

	public static readonly GameString LoadingScreenCalculatingMeshes = GameString.Create("LoadingScreenCalculatingMeshes", "Calculating Meshes");

	public static readonly GameString LoadingScreenDeserializingStationContacts = GameString.Create("LoadingScreenDeserializingStationContacts", "Deserializing Station Contacts");

	public static readonly GameString LoadingScreenDeserializingSpaceMap = GameString.Create("LoadingScreenDeserializingSpaceMap", "Deserializing Space Map");

	public static readonly GameString LoadingScreenDeserializingRockets = GameString.Create("LoadingScreenDeserializingRockets", "Deserializing Rockets");

	public static readonly GameString LoadingScreenDeserializingRocketLog = GameString.Create("LoadingScreenDeserializingRocketLog", "Deserializing Rocket Log");

	public static readonly GameString LoadingScreenDeserializingWorldObjectives = GameString.Create("LoadingScreenDeserializingWorldObjectives", "Deserializing World Objectives");

	public static readonly GameString LoadingScreenDeserializingPylons = GameString.Create("LoadingScreenDeserializingPylons", "Deserializing Pylons");

	public static readonly GameString LoadingScreenInitializing = GameString.Create("LoadingScreenInitializing", "Initializing");

	public static readonly GameString LoadingScreenInitializingChunks = GameString.Create("LoadingScreenInitializingChunks", "Initializing Chunks");

	public static readonly GameString LoadingScreenLoadingChunks = GameString.Create("LoadingScreenLoadingChunks", "Loading Chunks");

	public static readonly GameString LoadingScreenLoadingTerrain = GameString.Create("LoadingScreenLoadingTerrain", "Loading Terrain Data");

	public static readonly GameString LoadingScreenSeedingMinables = GameString.Create("LoadingScreenSeedingMinables", "Seeding Minables");

	public static readonly GameString LoadingScreenGeneratingMinables = GameString.Create("LoadingScreenGeneratingMinables", "Generating Minables");

	public static readonly GameString LoadingScreenValidatingMinables = GameString.Create("LoadingScreenValidatingMinables", "Validating Minables");

	public static readonly GameString LoadingScreenRegisteringMinables = GameString.Create("LoadingScreenRegisteringMinables", "Registering Minables");

	public static readonly GameString LoadingScreenApplyingMinedMinables = GameString.Create("LoadingScreenApplyingMinedMinables", "Minable Deltas");

	public static readonly GameString LoadingScreenGeneratingRecipes = GameString.Create("LoadingScreenGeneratingRecipes", "Generating Recipes");

	public static readonly GameString LoadingScreenNewWorldMenu = GameString.Create("LoadingScreenNewWorldMenu", "New World Menu");

	public static readonly GameString LoadingScreenLoadingStationpedia = GameString.Create("LoadingScreenLoadingStationpedia", "Loading Stationpedia");

	public static readonly GameString LoadingScreenCalculatingChunks = GameString.Create("LoadingScreenCalculatingChunks", "Calculating Chunks");

	public static readonly GameString LoadingScreenLoadingRooms = GameString.Create("LoadingScreenLoadingRooms", "Loading Rooms");

	public static readonly GameString LoadingScreenLoadingAtmosphers = GameString.Create("LoadingScreenLoadingAtmosphers", "Loading Atmospheres");

	public static readonly GameString LoadingScreenRegeneratingTerrain = GameString.Create("LoadingScreenRegeneratingTerrain", "Regenerating Terrain");

	public static readonly GameString LoadingScreenPopulatingInventory = GameString.Create("LoadingScreenPopulatingInventory", "Populating Inventory");

	public static readonly GameString LoadingScreenSpawningThings = GameString.Create("LoadingScreenSpawningThings", "Spawning things");

	public static readonly GameString LoadingScreenPreparingTerrain = GameString.Create("LoadingScreenPreparingTerrain", "Preparing Terrain");

	public static readonly GameString LoadingScreenRequestingCharacter = GameString.Create("LoadingScreenRequestingCharacter", "Requesting Character");

	public static readonly GameString LoadingScreenInitializingDevices = GameString.Create("LoadingScreenInitializingDevices", "Initializing Devices");

	public static readonly GameString LoadingScreenValidatingPrefabs = GameString.Create("LoadingScreenValidatingPrefabs", "Validating Prefabs");

	public static readonly GameString LoadingScreenRenderingChunks = GameString.Create("LoadingScreenRenderingChunks", "Rendering Chunks");

	public static readonly GameString LoadingScreenLoadingThings = GameString.Create("LoadingScreenLoadingThings", "Loading Things ({LOCAL:ThingCount})", "ThingCount");

	public static readonly GameString LoadingScreenCleaningUpPrefabs = GameString.Create("LoadingScreenCleaningUpPrefabs", "Cleaning up prefabs");

	public static readonly GameString LoadingScreenDeserializeWorldLog = GameString.Create("LoadingScreenDeserializeWorldLog", "Deserializing World Log");

	public static readonly GameString LoadingScreenDeserializeRocketLog = GameString.Create("LoadingScreenDeserializeRocketLog", "Deserializing Rocket Log");

	public static readonly GameString SettingsFullScreenDisplayMode = GameString.Create("SettingsFullScreenDisplayMode", "Full Screen");

	public static readonly GameString SettingsWindowDisplayMode = GameString.Create("SettingsWindowDisplayMode", "Windowed");

	public static readonly GameString SetDifficultyTitleConfirmation = GameString.Create("SetDifficultyTitleConfirmation", "Set Difficulty");

	public static readonly GameString SetDifficultyBodyConfirmation = GameString.Create("SetDifficultyBodyConfirmation", "Difficulty settings not found. Please choose one");

	public static readonly GameString PlantSamplerAction = GameString.Create("PlantSamplerAction", "Sample {LOCAL:Plant}", "Plant");

	public static readonly GameString ActionFixLeak = GameString.Create("ActionFixLeak", "Fix Leak");

	public static readonly GameString ActionExtinguish = GameString.Create("ActionExtinguish", "Extinguish");

	public static readonly GameString ActionWeld = GameString.Create("ActionWeld", "Weld");

	public static readonly GameString ActionDrag = GameString.Create("ActionDrag", "Drag");

	public static readonly GameString ActionUnweld = GameString.Create("ActionUnweld", "Unweld");

	public static readonly GameString ActionClearPlant = GameString.Create("ActionClearPlant", "Clear {LOCAL:Plant}", "Plant");

	public static readonly GameString ActionHarvestPlant = GameString.Create("ActionHarvestPlant", "Harvest {LOCAL:Plant} Plant", "Plant");

	public static readonly GameString ActionHarvestSeed = GameString.Create("ActionHarvestSeed", "Harvest {LOCAL:Plant} Seed", "Plant");

	public static readonly GameString AlreadyFertalised = GameString.Create("AlreadyFertalised", "Slot is already fertilized");

	public static readonly GameString NotPlantable = GameString.Create("NotPlantable", "A {0} is not plantable");

	public static readonly GameString AddSeedsToSlot = GameString.Create("AddSeedsToSlot", "This will add one {0} to the tray");

	public static readonly GameString ClearFertiliser = GameString.Create("ClearFertiliser", "Clear fertiliser");

	public static readonly GameString NothingToSample = GameString.Create("NothingToSample", "Nothing to sample");

	public static readonly GameString NoPlantToLabel = GameString.Create("NoPlantToLabel", "No plant to label");

	public static readonly GameString NothingToWater = GameString.Create("NothingToWater", "Nothing to water");

	public static readonly GameString AlreadyFullOfWater = GameString.Create("AlreadyFullOfWater", "Already full of water");

	public static readonly GameString WaterPlantWith = GameString.Create("WaterPlantWith", "Water plant with {LOCAL:InHand}", "InHand");

	public static readonly GameString CanOnlyUseLiquidOnPlants = GameString.Create("CanOnlyUseLiquidOnPlants", "You can only use liquid gas canisters to water plants");

	public static readonly GameString CantHoldAnyMore = GameString.Create("CantHoldAnyMore", "Can't hold any more");

	public static readonly GameString SomethingPlantedHere = GameString.Create("SomethingPlantedHere", "There is already something planted here");

	public static readonly GameString SomethingInThisSlot = GameString.Create("SomethingInThisSlot", "There is already something in this slot");

	public static readonly GameString Fertiliser = GameString.Create("Fertiliser", "Fertiliser");

	public static readonly GameString AddFertiliserToSlot = GameString.Create("AddFertiliserToSlot", "Add fertiliser to slot");

	public static readonly GameString LightDeficient = GameString.Create("LightDeficient", "Light Deficient");

	public static readonly GameString DarknessDeficient = GameString.Create("DarknessDeficient", "Dark Deficient");

	public static readonly GameString NoDeficiency = GameString.Create("NoDeficiency", "No Deficiency");

	public static readonly GameString GrowthTemperatureRange = GameString.Create("GrowthTemperatureRange", "Growth temperature range");

	public static readonly GameString GrowthPressureRange = GameString.Create("GrowthPressureRange", "Growth pressure range");

	public static readonly GameString GrowthSpeedMultiplier = GameString.Create("GrowthSpeedMultiplier", "Growth speed multiplier");

	public static readonly GameString LightPerDay = GameString.Create("LightPerDay", "Light per day");

	public static readonly GameString DarknessPerDay = GameString.Create("DarknessPerDay", "Darkness per day");

	public static readonly GameString GasProduction = GameString.Create("GasProduction", "Gas production");

	public static readonly GameString WaterUsage = GameString.Create("WaterUsage", "Water usage");

	public static readonly GameString UndesiredGasResistance = GameString.Create("UndesiredGasResistance", "Undesired gas resistance");

	public static readonly GameString TimeUntilDehydrationDamage = GameString.Create("TimeUntilDehydrationDamage", "Time until drought damage");

	public static readonly GameString TimeUntilUndesiredGasDamage = GameString.Create("TimeUntilUndesiredGasDamage", "Time until toxin damage");

	public static readonly GameString TimeUntilFrozenDamage = GameString.Create("TimeUntilFrozenDamage", "Time until frozen damage");

	public static readonly GameString TimeUntilOverheatDamage = GameString.Create("TimeUntilOverheatDamage", "Time until overheat damage");

	public static readonly GameString TimeUntilSuffocateDamage = GameString.Create("TimeUntilSuffocateDamage", "Time until suffocate damage");

	public static readonly GameString TimeUntilLowPressureDamage = GameString.Create("TimeUntilLowPressureDamage", "Time until low pressure damage");

	public static readonly GameString TimeUntilHighPressureDamage = GameString.Create("TimeUntilHighPressureDamage", "Time until high pressure damage");

	public static readonly GameString TimeUntilLightDamage = GameString.Create("TimeUntilLightDamage", "Time until light damage");

	public static readonly GameString TimeUntilDarknessDamage = GameString.Create("TimeUntilDarknessDamage", "Time until darkness damage");

	public static readonly GameString MinGrowTemperature = GameString.Create("MinGrowTemperature", "Min grow temperature");

	public static readonly GameString MaxGrowTemperature = GameString.Create("MaxGrowTemperature", "Max grow temperature");

	public static readonly GameString MinIdealGrowTemperature = GameString.Create("MinIdealGrowTemperature", "Min ideal grow temperature");

	public static readonly GameString MaxIdealGrowTemperature = GameString.Create("MaxIdealGrowTemperature", "Max ideal grow temperature");

	public static readonly GameString MinGrowPressure = GameString.Create("MinGrowPressure", "Min grow pressure");

	public static readonly GameString MaxGrowPressure = GameString.Create("MaxGrowPressure", "Max grow pressure");

	public static readonly GameString MinIdealGrowPressure = GameString.Create("MinIdealGrowPressure", "Min ideal grow pressure");

	public static readonly GameString MaxIdealGrowPressure = GameString.Create("MaxIdealGrowPressure", "Max ideal grow pressure");

	public static readonly GameString NotEnoughSprayPaint = GameString.Create("NotEnoughSprayPaint", "Not enough paint in {LOCAL:SprayCanister}", "SprayCanister");

	public static readonly GameString CantPaintSameColour = GameString.Create("CantPaintSameColour", "The {LOCAL:Thing} is already painted {LOCAL:Color}", "Thing", "Color");

	public static readonly GameString ThingWillBeSprayed = GameString.Create("ThingWillBeSprayed", "The {LOCAL:Thing} will be painted {LOCAL:Color}", "Thing", "Color");

	public static readonly GameString GeneGrowthSpeedMultiplier = GameString.Create("GeneGrowthSpeedMultiplier", "Growth speed");

	public static readonly GameString GeneDescriptionGrowthSpeedMultiplier = GameString.Create("GeneDescriptionGrowthSpeedMultiplier", "Increases the growth speed of the plant. Plants with a higher Growth speed multiplier will take less time to grow.");

	public static readonly GameString GeneDarkPerDay = GameString.Create("GeneDarkPerDay", "Darkness per day");

	public static readonly GameString GeneDescriptionDarkPerDay = GameString.Create("GeneDescriptionDarkPerDay", "The length of time a plant needs to be in darkness each day-cycle to thrive. The standard length day-cycle is 20 minutes.");

	public static readonly GameString GeneLightPerDay = GameString.Create("GeneLightPerDay", "Light per day");

	public static readonly GameString GeneDescriptionLightPerDay = GameString.Create("GeneDescriptionLightPerDay", "The length of time a plant needs to be lit by the sun or a grow light each day-cycle to thrive. The standard length day-cycle is 20 minutes.");

	public static readonly GameString GeneDroughtTolerance = GameString.Create("GeneDroughtTolerance", "Drought tolerance");

	public static readonly GameString GeneDescriptionDroughtTolerance = GameString.Create("GeneDescriptionDroughtTolerance", "The length of time a plant can be dehydrated before it starts taking damage.");

	public static readonly GameString GeneWaterUsage = GameString.Create("GeneWaterUsage", "Water usage");

	public static readonly GameString GeneDescriptionWaterUsage = GameString.Create("GeneDescriptionWaterUsage", "The amount of water this plant consumes.");

	public static readonly GameString GeneGasProduction = GameString.Create("GeneGasProduction", "Gas production");

	public static readonly GameString GeneDescriptionGasProduction = GameString.Create("GeneDescriptionGasProduction", "The amount of gas the plant inhales and exhales.");

	public static readonly GameString GeneSuffocationTolerance = GameString.Create("GeneSuffocationTolerance", "Suffocation tolerance");

	public static readonly GameString GeneDescriptionSuffocationTolerance = GameString.Create("GeneDescriptionSuffocationTolerance", "The length of time a plant can be starved of its inhaled gas requirements before it starts taking damage.");

	public static readonly GameString GeneLowPressureResistance = GameString.Create("GeneLowPressureResistance", "Low pressure resistance");

	public static readonly GameString GeneDescriptionLowPressureResistance = GameString.Create("GeneDescriptionLowPressureResistance", "The lower limit of pressure that the plant is able to grow at.");

	public static readonly GameString GeneLowPressureTolerance = GameString.Create("GeneLowPressureTolerance", "Low pressure tolerance");

	public static readonly GameString GeneDescriptionLowPressureTolerance = GameString.Create("GeneDescriptionLowPressureTolerance", "The length of time a plant can stay below its low pressure limit before it starts taking damage.");

	public static readonly GameString GeneLowTemperatureResistance = GameString.Create("GeneLowTemperatureResistance", "Low temperature resistance");

	public static readonly GameString GeneDescriptionLowTemperatureResistance = GameString.Create("GeneDescriptionLowTemperatureResistance", "The lower limit of temperature that the plant is able to grow at.");

	public static readonly GameString GeneLowTemperatureTolerance = GameString.Create("GeneLowTemperatureTolerance", "Low temperature tolerance");

	public static readonly GameString GeneDescriptionLowTemperatureTolerance = GameString.Create("GeneDescriptionLowTemperatureTolerance", "The length of time a plant can stay below its low temperature limit before it starts taking damage.");

	public static readonly GameString GeneUndesiredGasResistance = GameString.Create("GeneUndesiredGasResistance", "Toxins resistance");

	public static readonly GameString GeneDescriptionUndesiredGasResistance = GameString.Create("GeneDescriptionUndesiredGasResistance", "The partial pressure threshold of toxic gases that the plant is able to grow in.");

	public static readonly GameString GeneUndesiredGasTolerance = GameString.Create("GeneUndesiredGasTolerance", "Toxins tolerance");

	public static readonly GameString GeneDescriptionUndesiredGasTolerance = GameString.Create("GeneDescriptionUndesiredGasTolerance", "The length of time a plant can stay above its toxins resistance threshold before taking damage.");

	public static readonly GameString GeneHighPressureResistance = GameString.Create("GeneHighPressureResistance", "High pressure resistance");

	public static readonly GameString GeneDescriptionHighPressureResistance = GameString.Create("GeneDescriptionHighPressureResistance", "The upper limit of pressure that the plant is able to grow at.");

	public static readonly GameString GeneHighPressureTolerance = GameString.Create("GeneHighPressureTolerance", "High pressure tolerance");

	public static readonly GameString GeneDescriptionHighPressureTolerance = GameString.Create("GeneDescriptionHighPressureTolerance", "The length of time a plant can stay above its high pressure limit before it starts taking damage.");

	public static readonly GameString GeneHighTemperatureResistance = GameString.Create("GeneHighTemperatureResistance", "High temperature resistance");

	public static readonly GameString GeneDescriptionHighTemperatureResistance = GameString.Create("GeneDescriptionHighTemperatureResistance", "The upper limit of temperature that the plant is able to grow at.");

	public static readonly GameString GeneHighTemperatureTolerance = GameString.Create("GeneHighTemperatureTolerance", "High temperature tolerance");

	public static readonly GameString GeneDescriptionHighTemperatureTolerance = GameString.Create("GeneDescriptionHighTemperatureTolerance", "The length of time a plant can stay above its high temperature limit before it starts taking damage.");

	public static readonly GameString GeneLightTolerance = GameString.Create("GeneLightTolerance", "Light tolerance");

	public static readonly GameString GeneDescriptionLightTolerance = GameString.Create("GeneDescriptionLightTolerance", "The length of time a plant can stay in light before it starts taking damage.");

	public static readonly GameString GeneDarknessTolerance = GameString.Create("GeneDarknessTolerance", "Darkness tolerance");

	public static readonly GameString GeneDescriptionDarknessTolerance = GameString.Create("GeneDescriptionDarknessTolerance", "The length of time a plant can stay in darkness before it starts taking damage.");

	public static readonly GameString ScriptEditorAuthorInfo = GameString.Create("ScriptEditorAuthorInfo", "Created by <color=white>{LOCAL:Author}</color>\n{LOCAL:Description}", "Author", "Description");

	public static readonly GameString ScriptEditorLoadTitle = GameString.Create("ScriptEditorLoadTitle", "Load Instructions");

	public static readonly GameString ScriptEditorLoadDescription = GameString.Create("ScriptEditorLoadDescription", "Are you sure you want to load this script into your computer? This will replace whatever is on screen.");

	public static readonly GameString ScriptEditorOverwriteTitle = GameString.Create("ScriptEditorOverwriteTitle", "Overwrite Instructions");

	public static readonly GameString ScriptEditorOverwriteDescription = GameString.Create("ScriptEditorOverwriteDescription", "Are you sure you want to overwrite this script with the data you have on the computer screen? This will replace what is in your source code library.");

	public static readonly GameString ScriptEditorPublishTitle = GameString.Create("ScriptEditorPublishTitle", "Publish Instructions");

	public static readonly GameString ScriptEditorPublishDescription = GameString.Create("ScriptEditorPublishDescription", "Are you sure you want to publish this instruction set to the Steam Workshop?");

	public static readonly GameString ScriptEditorDeleteTitle = GameString.Create("ScriptEditorDeleteTitle", "Delete Instructions");

	public static readonly GameString ScriptEditorDeleteDescription = GameString.Create("ScriptEditorDeleteDescription", "Are you sure you want to delete this script from the library? You cannot undo this.");

	public static readonly GameString ScriptEditorInstructionCancelled = GameString.Create("ScriptEditorInstructionCancelled", "Instructions list query cancelled");

	public static readonly GameString GeneCurrentSelected = GameString.Create("GeneCurrentSelected", "Currently selected gene <color=yellow>{LOCAL:Gene}</color>", "Gene");

	public static readonly GameString GeneDestabilisingOnly = GameString.Create("GeneDestabilisingOnly", "Destabilising Only");

	public static readonly GameString GlobalAttack = GameString.Create("GlobalAttack", "Attack <color=green>{0}</color>");

	public static readonly GameString GlobalRelease = GameString.Create("GlobalRelease", "Release <color=green>{0}</color>");

	public static readonly GameString GlobalSpeed = GameString.Create("GlobalSpeed", "Speed <color=green>{0}</color>");

	public static readonly GameString GlobalIntensity = GameString.Create("GlobalIntensity", "Intensity <color=green>{0}</color>");

	public static readonly GameString GlobalWaveForm = GameString.Create("GlobalWaveForm", "Waveform <color=green>{0}</color>");

	public static readonly GameString GlobalThrottle = GameString.Create("GlobalThrottle", "Throttle <color=green>{0}</color>");

	public static readonly GameString GlobalCombustionLimiter = GameString.Create("GlobalCombustionLimiter", "Combustion Limiter <color=green>{0}</color>");

	public static readonly GameString GlobalCurrentInstrument = GameString.Create("GlobalCurrentInstrument", "Current Instrument <color=green>{0}</color>");

	public static readonly GameString GlobalChangeSettingTo = GameString.Create("GlobalChangeSettingTo", "Cycle to {0}");

	public static readonly GameString GlobalChangeSettingToAll = GameString.Create("GlobalChangeSettingToAll", "Cycle to All {0}");

	public static readonly GameString GlobalChangeSettingToFor = GameString.Create("GlobalChangeSettingToFor", "Cycle to <color=yellow>{0} for {1}</color>");

	public static readonly GameString GlobalChangeSettingToForAll = GameString.Create("GlobalChangeSettingToForAll", "Cycle to <color=yellow>{0} for all {1}</color>");

	public static readonly GameString GlobalChangeSettingToForIn = GameString.Create("GlobalChangeSettingToForIn", "Cycle to <color=yellow>{0}</color> for {3} <color=yellow>({2})</color> in {1}");

	public static readonly GameString GlobalChangeSlotSettingToFor = GameString.Create("GlobalChangeSlotSettingToFor", "Cycle to {1}<color=yellow>({0}) for {2}");

	public static readonly GameString GlobalChangeReagentSettingTo = GameString.Create("GlobalChangeReagentSettingTo", "Cycle to {2}.<color=yellow>{0}.<color=yellow>{1}");

	public static readonly GameString GlobalAlreadyMax = GameString.Create("GlobalAlreadyMax", "Already Max");

	public static readonly GameString GlobalAlreadyMin = GameString.Create("GlobalAlreadyMin", "Already Min");

	public static readonly GameString GlobalStop = GameString.Create("GlobalStop", "Stop");

	public static readonly GameString GlobalStart = GameString.Create("GlobalStart", "Start");

	public static readonly GameString GlobalIncrease = GameString.Create("GlobalIncrease", "Increase");

	public static readonly GameString GlobalDecrease = GameString.Create("GlobalDecrease", "Decrease");

	public static readonly GameString GlobalStackSize = GameString.Create("GlobalStackSize", "Stacksize <color=green>{0}</color>");

	public static readonly GameString GlobalMaxMode = GameString.Create("GlobalMaxMode", "Max <color=green>{0}</color>");

	public static readonly GameString GlobalValue = GameString.Create("GlobalValue", "Value <color=green>{0}</color>");

	public static readonly GameString GlobalPitch = GameString.Create("GlobalPitch", "Pitch <color=green>{0}</color>");

	public static readonly GameString GlobalVolume = GameString.Create("GlobalVolume", "Volume <color=green>{0}</color>");

	public static readonly GameString GlobalVolumeLitre = GameString.Create("GlobalVolumeLitre", "Volume <color=green>{0}L</color>");

	public static readonly GameString GlobalRadius = GameString.Create("GlobalRadius", "Radius <color=green>{0}</color>");

	public static readonly GameString GlobalAlreadyInUse = GameString.Create("GlobalAlreadyInUse", "Already in use");

	public static readonly GameString Pressure = GameString.Create("Pressure", "Pressure");

	public static readonly GameString Temperature = GameString.Create("Temperature", "Temperature");

	public static readonly GameString Vacuum = GameString.Create("Vacuum", "Vacuum");

	public static readonly GameString Null = GameString.Create("Null", "Null");

	public static readonly GameString AtmosphereVolume = GameString.Create("AtmosphereVolume", "Capacity");

	public static readonly GameString LiquidsVolume = GameString.Create("LiquidsVolume", "Liquids Volume");

	public static readonly GameString StirlingTemperatureHot = GameString.Create("StirlingTemperatureHot", "Temperature Hot Side");

	public static readonly GameString GeneratingPower = GameString.Create("GeneratingPower", "Generating");

	public static readonly GameString StirlingTemperatureCold = GameString.Create("StirlingTemperatureCold", "Temperature Cold Side");

	public static readonly GameString StirlingOperatingEfficiency = GameString.Create("StirlingOperatingEfficiency", "Environment Operating Efficiency");

	public static readonly GameString StirlingPressureDifferential = GameString.Create("StirlingPressureDifferential", "Pressure Differential");

	public static readonly GameString StirlingGasEfficiency = GameString.Create("StirlingGasEfficiency", "Working Gas Efficiency");

	public static readonly GameString StirlingHelperText = GameString.Create("StirlingHelperText", "Connect input and output pipe networks and supply input network with hot high pressure gas");

	public static readonly GameString StirlingMissingCanister = GameString.Create("StirlingMissingCanister", "<color=red>Missing a <color=green>Gas Canister</color> for working gas</color>");

	public static readonly GameString ProgrammableChipErrorCode = GameString.Create("ProgrammableChipErrorCode", "Error <color=red>{LOCAL:ErrorType}</color> at line {LOCAL:LineNumber}", "ErrorType", "LineNumber");

	public static readonly GameString OutputLitres = GameString.Create("OutputLitres", "Output <color=green>{0} L</color>");

	public static readonly GameString InputLitres = GameString.Create("InputLitres", "Input <color=green>{0} L</color>");

	public static readonly GameString OutputKPA = GameString.Create("OutputKPA", "Output <color=green>{0} kPa</color>");

	public static readonly GameString InputKPA = GameString.Create("InputKPA", "Input <color=green>{0} kPa</color>");

	public static readonly GameString TargetPressureKPA = GameString.Create("TargetPressureKPA", "Target Pressure <color=green>{0} kPa</color>");

	public static readonly GameString OutputWatts = GameString.Create("OutputWatts", "Output <color=green>{0} W</color>");

	public static readonly GameString OutputVolumeRatio = GameString.Create("OutputVolumeRatio", "Output fullness <color=green>{0}%</color>");

	public static readonly GameString InputVolumeRatio = GameString.Create("InputVolumeRatio", "Input fullness <color=green>{0}%</color>");

	public static readonly GameString Input1Ratio = GameString.Create("Input1Ratio", "Input 1 <color=green>{0}%</color>");

	public static readonly GameString Input2Ratio = GameString.Create("Input2Ratio", "Input 2 <color=green>{0}%</color>");

	public static readonly GameString VerticalDegrees = GameString.Create("VerticalDegrees", "<color=yellow>Vertical</color> <color=green>{0} degrees</color>");

	public static readonly GameString HorizontalDegrees = GameString.Create("HorizontalDegrees", "<color=yellow>Horizontal</color> <color=green>{0} degrees</color>");

	public static readonly GameString StackerNoContentsToClear = GameString.Create("StackerNoContentsToClear", "<color=red>No contents to clear</color>");

	public static readonly GameString StackerWillForceChangeFromLogicToAutomatic = GameString.Create("StackerWillForceChangeFromLogicToAutomatic", "Will force change from <color=yellow>Logic</color> to <color=yellow>Automatic</color> mode");

	public static readonly GameString SlotContainsItem = GameString.Create("SlotContainsItem", "{LOCAL:Slot} contains {LOCAL:Item}", "Slot", "Item");

	public static readonly GameString StackerContainsQuantity = GameString.Create("StackerContainsQuantity", "Contains {LOCAL:Quantity} x {LOCAL:Item}", "Quantity", "Item");

	public static readonly GameString StackableNotEnoughInStackOf = GameString.Create("StackableNotEnoughInStackOf", "Not enough in stack of {LOCAL:Item} to complete, <color=yellow>{LOCAL:Quantity0}</color>/<color=yellow>{LOCAL:Quantity1}</color>", "Item", "Quantity0", "Quantity1");

	public static readonly GameString StackableNotEnoughInStack = GameString.Create("StackableNotEnoughInStack", "Not enough in stack!");

	public static readonly GameString InventoryStackIsFull = GameString.Create("InventoryStackIsFull", "Your stack of {LOCAL:Thing} is full", "Thing");

	public static readonly GameString InteractOpenResearchScreen = GameString.Create("InteractOpenResearchScreen", "Interact to open Research Unlock Screen");

	public static readonly GameString InteractCantClose = GameString.Create("InteractCantClose", "Something is preventing this from closing");

	public static readonly GameString ThingMoveToWorld = GameString.Create("ThingMoveToWorld", "Move {LOCAL:Thing} to world", "Thing");

	public static readonly GameString ThingCanNotEnterSlot = GameString.Create("ThingCanNotEnterSlot", "{LOCAL:Thing0} cannot enter {LOCAL:Thing1}", "Thing0", "Thing1");

	public static readonly GameString ThingIsFastenedIn = GameString.Create("ThingIsFastenedIn", "Fastened in {LOCAL:Slot} on {LOCAL:Parent}", "Slot", "Parent");

	public static readonly GameString ThingCanNotInsertInteractionsDisabled = GameString.Create("ThingCanNotInsertInteractionsDisabled", "Cannot insert {LOCAL:Thing0} interactions are disabled on {LOCAL:Thing1}", "Thing0", "Thing1");

	public static readonly GameString ThingCanNotInsertAlreadyContains = GameString.Create("ThingCanNotInsertAlreadyContains", "Cannot insert {LOCAL:Thing0} as it already contains {LOCAL:Thing1}", "Thing0", "Thing1");

	public static readonly GameString ThingCanNotSwap = GameString.Create("ThingCanNotSwap", "You cannot swap {LOCAL:Thing0} with {LOCAL:Thing1}", "Thing0", "Thing1");

	public static readonly GameString ThingCurrentlyNotInteractable = GameString.Create("ThingCurrentlyNotInteractable", "{LOCAL:Thing} is currently not interactable", "Thing");

	public static readonly GameString ThingCanNotPickUp = GameString.Create("ThingCanNotPickUp", "You cannot pick the {LOCAL:Thing} up", "Thing");

	public static readonly GameString ThingIsNotType = GameString.Create("ThingIsNotType", "{LOCAL:Thing0} is not type {LOCAL:Thing1}", "Thing0", "Thing1");

	public static readonly GameString ThingInteractionDisabled = GameString.Create("ThingInteractionDisabled", "Interaction temporary disabled on this object");

	public static readonly GameString ThingCurrentlyFlashingError = GameString.Create("ThingCurrentlyFlashingError", "Currently flashing an error");

	public static readonly GameString ThingCycleTo = GameString.Create("ThingCycleTo", "Cycle to {LOCAL:Thing}", "Thing");

	public static readonly GameString ThingNotEnoughReagents = GameString.Create("ThingNotEnoughReagents", "Not enough reagents for current plan");

	public static readonly GameString ThingSelectRecipeFromList = GameString.Create("ThingSelectRecipeFromList", "Select Recipe from List");

	public static readonly GameString ThingClearUnknownRecipe = GameString.Create("ThingClearUnknownRecipe", "Clear Unknown Recipe");

	public static readonly GameString ThingCreateThing = GameString.Create("ThingCreateThing", "Creating {LOCAL:Thing} <color=yellow>{1:F0}%</color>", "Thing");

	public static readonly GameString RequiredContinueConstruction = GameString.Create("RequiredContinueConstruction", "required to <color=yellow>continue construction</color>");

	public static readonly GameString RequiredUpgradeDevice = GameString.Create("RequiredUpgradeDevice", "required to <color=yellow>upgrade the device</color>");

	public static readonly GameString RequiredToRepair = GameString.Create("RequiredToRepair", "required to <color=yellow>repair</color>");

	public static readonly GameString ThingCanNotManufacture = GameString.Create("ThingCanNotManufacture", "Cannot manufacture {LOCAL:Thing} without all required reagents", "Thing");

	public static readonly GameString OnlyHumansCanDragThings = GameString.Create("OnlyHumansCanDragThings", "{LOCAL:Thing} does not allow dragging", "Thing");

	public static readonly GameString CannotDragWhileAlive = GameString.Create("CannotDragWhileAlive", "{LOCAL:Thing} cannot be dragged while <color=yellow>alive</color>", "Thing");

	public static readonly GameString ThingToFarAwayForDrag = GameString.Create("ThingToFarAwayForDrag", "{LOCAL:Thing} is too far away to be dragged", "Thing");

	public static readonly GameString ThingCanNotBeDraggedWithSomethingIn = GameString.Create("ThingCanNotBeDraggedWithSomethingIn", "Cannot be dragged with something in {LOCAL:Thing}", "Thing");

	public static readonly GameString ThingAlreadyDraggedBy = GameString.Create("ThingAlreadyDraggedBy", "{LOCAL:Thing0} is already being dragged by {LOCAL:Thing1}", "Thing0", "Thing1");

	public static readonly GameString ThingModeDoesNotSupportLinking = GameString.Create("ThingModeDoesNotSupportLinking", "Mode does not support linking");

	public static readonly GameString ThingWillDecayIn = GameString.Create("ThingWillDecayIn", "Will decay completely in <color=yellow>{0}</color>");

	public static readonly GameString FoodContainerDecayReduction = GameString.Create("FoodContainerDecayReduction", "Decay reduced to {LOCAL:Percent} inside {LOCAL:Container}", "Percent", "Container");

	public static readonly GameString ThingIsState = GameString.Create("ThingIsState", "The {LOCAL:Thing} is {LOCAL:State}", "Thing", "State");

	public static readonly GameString LaunchTowerAlreadyExists = GameString.Create("LaunchTowerAlreadyExists", "A <color=green>Launch Tower</color> already exists in this grid");

	public static readonly GameString CannotPlaceOutsideRocket = GameString.Create("CannotPlaceOutsideRocket", "Cannot place outside of a rocket");

	public static readonly GameString StructureYouRequireToCompleteTask = GameString.Create("StructureYouRequireToCompleteTask", "You require {0} in {1} to complete this task");

	public static readonly GameString CannotConstructWhenDamaged = GameString.Create("CannotConstructWhenDamaged", "Cannot continue construction of {LOCAL:Thing} while it is damaged", "Thing");

	public static readonly GameString StructureDeconstructionFailed = GameString.Create("StructureDeconstructionFailed", "Deconstruction failed");

	public static readonly GameString StructureIsNotDamaged = GameString.Create("StructureIsNotDamaged", "{LOCAL:Structure} is not damaged", "Structure");

	public static readonly GameString VehicleIsNotDamaged = GameString.Create("VehicleIsNotDamaged", "{LOCAL:Rover} is not damaged", "Rover");

	public static readonly GameString WeldingTorchNoFuel = GameString.Create("WeldingTorchNoFuel", "No fuel left!");

	public static readonly GameString PowerItemNoPower = GameString.Create("PowerItemNoPower", "No battery power!");

	public static readonly GameString SlotCanNotUseWith = GameString.Create("SlotCanNotUseWith", "You cannot use {LOCAL:Slot0} with {LOCAL:Slot1}", "Slot0", "Slot1");

	public static readonly GameString SlotAlreadyOccupied = GameString.Create("SlotAlreadyOccupied", "{0} is already occupied by {1}");

	public static readonly GameString SlotTakeThingFromSlot = GameString.Create("SlotTakeThingFromSlot", "Take {LOCAL:Item} from slot", "Item");

	public static readonly GameString CartridgeNoCartridgeInSlot = GameString.Create("CartridgeNoCartridgeInSlot", "No {LOCAL:Slot1} in {LOCAL:Slot0} slot to remove", "Slot0", "Slot1");

	public static readonly GameString HarvesterBusyDoing = GameString.Create("HarvesterBusyDoing", "Currently busy doing {0}");

	public static readonly GameString RoboticArmBusy = GameString.Create("RoboticArmBusy", "Currently busy");

	public static readonly GameString RoboticArmObstructed = GameString.Create("RoboticArmObstructed", "The arm is obstructed");

	public static readonly GameString RoboticArmJunction = GameString.Create("RoboticArmJunction", "Station {LOCAL:Index}", "Index");

	public static readonly GameString RoboticArmTargetIndex = GameString.Create("RoboticArmTargetIndex", "Target Index {LOCAL:Index}", "Index");

	public static readonly GameString RoboticArmCargoSetSlotIndex = GameString.Create("RoboticArmCargoSetSlotIndex", "Slot Index {LOCAL:Index}", "Index");

	public static readonly GameString RoboticArmSetStartingDock = GameString.Create("RoboticArmSetStartingDock", "Set starting dock");

	public static readonly GameString RoboticArmFull = GameString.Create("RoboticArmFull", "The arm's storage is full");

	public static readonly GameString RoboticArmEmpty = GameString.Create("RoboticArmEmpty", "The arm's storage is empty");

	public static readonly GameString RoboticArmInvalid = GameString.Create("RoboticArmInvalid", "Invalid");

	public static readonly GameString RoboticArmExtended = GameString.Create("RoboticArmExtended", "Extended");

	public static readonly GameString RoboticArmMoving = GameString.Create("RoboticArmMoving", "Moving");

	public static readonly GameString RoboticArmDocked = GameString.Create("RoboticArmDocked", "Docked");

	public static readonly GameString RoboticArmFaceBlocked = GameString.Create("RoboticArmFaceBlocked", "Arm action blocked");

	public static readonly GameString RoboticArmFaceBlockedInfo = GameString.Create("RoboticArmFaceBlockedInfo", "Arm action blocked. Not enough room for arm to extend");

	public static readonly GameString RoboticArmIdle = GameString.Create("RoboticArmIdle", "Idle");

	public static readonly GameString RoboticArmPosition = GameString.Create("RoboticArmPosition", "Position {LOCAL:Position}", "Position");

	public static readonly GameString RoboticArmIndex = GameString.Create("RoboticArmIndex", "Destination Index {LOCAL:Index}", "Index");

	public static readonly GameString RoboticArmState = GameString.Create("RoboticArmState", "State {LOCAL:State}", "State");

	public static readonly GameString RoboticArmVentDirection = GameString.Create("RoboticArmVentDirection", "Vent Direction {LOCAL:direction}", "direction");

	public static readonly GameString RoboticArmArmDisplayStateHasSpace = GameString.Create("RoboticArmArmDisplayStateHasSpace", "Storage Available");

	public static readonly GameString RoboticArmArmDisplayStateFull = GameString.Create("RoboticArmArmDisplayStateFull", "Storage Full");

	public static readonly GameString TargetSlotInfo = GameString.Create("TargetSlotInfo", "Targeting {LOCAL:SlotName} {LOCAL:Occupant}", "SlotName", "Occupant");

	public static readonly GameString TargetSlot = GameString.Create("TargetSlot", "Target Slot");

	public static readonly GameString Proxy = GameString.Create("Proxy", "Proxy");

	public static readonly GameString DeviceNotClosed = GameString.Create("DeviceNotClosed", "The {LOCAL:Device} must be closed to operate", "Device");

	public static readonly GameString MicrowaveOpenFailure = GameString.Create("MicrowaveOpenFailure", "Something is preventing this from closing");

	public static readonly GameString MicrowaveAddUnitsOf = GameString.Create("MicrowaveAddUnitsOf", "Add <color=green>{0}</color> units of {1}");

	public static readonly GameString ComposterNotEnoughProcessItems = GameString.Create("ComposterNotEnoughProcessItems", "No enough processed items...");

	public static readonly GameString ComposterItemsLeftToProcess = GameString.Create("ComposterItemsLeftToProcess", "Items left to process {0}");

	public static readonly GameString ComposterNotEnoughWater = GameString.Create("ComposterNotEnoughWater", "Not enough water...");

	public static readonly GameString ComposterCurrentlyProcessing = GameString.Create("ComposterCurrentlyProcessing", "Currently Processing...");

	public static readonly GameString ComposterCurrentlyNotProcessing = GameString.Create("ComposterCurrentlyNotProcessing", "Currently Not Processing...");

	public static readonly GameString ProcessingThing = GameString.Create("ProcessingThing", "Processing {LOCAL:Thing}", "Thing");

	public static readonly GameString Progress = GameString.Create("Progress", "Progress");

	public static readonly GameString DistanceToBedRock = GameString.Create("DistanceToBedRock", "Distance to Bedrock");

	public static readonly GameString ProduceQuantityOfResource = GameString.Create("ProduceQuantityOfResource", "Will produce {LOCAL:Quantity} x {LOCAL:Resource} of {LOCAL:Thing}", "Quantity", "Resource", "Thing");

	public static readonly GameString ProduceResource = GameString.Create("ProduceResource", "Will produce {LOCAL:Size} of {LOCAL:Resource}", "Size", "Resource");

	public static readonly GameString ProduceAnotherResource = GameString.Create("ProduceAnotherResource", "And {LOCAL:Size} of {LOCAL:Resource}", "Size", "Resource");

	public static readonly GameString AccessCardUnableToInteract = GameString.Create("AccessCardUnableToInteract", "Unable to interact as you do not have the required AccessCard");

	public static readonly GameString InteractionTemporarilyDisabledOnThisObject = GameString.Create("InteractionTemporarilyDisabledOnThisObject", "Interaction temporarily disabled on this object");

	public static readonly GameString FertiliserClearFromSlot = GameString.Create("FertiliserClearFromSlot", "Hold <color=yellow>{0}</color> to clear {1}");

	public static readonly GameString RegentMixString = GameString.Create("RegentMixString", "{0}");

	public static readonly GameString PatchingRecipe = GameString.Create("PatchingRecipe", "{LOCAL:ModName} is patching recipe for {LOCAL:PrefabName}", "ModName", "PrefabName");

	public static readonly GameString UnknownMod = GameString.Create("UnknownMod", "Unknown");

	public static readonly GameString ActionRepairRobot = GameString.Create("ActionRepairRobot", "Repair");

	public static readonly GameString ActionRepairRover = GameString.Create("ActionRepairRover", "Repair Vehicle");

	public static readonly GameString ActionPatchSolarPanels = GameString.Create("ActionPatchSolarPanels", "Patch");

	public static readonly GameString TooltipDeviceUnpowered = GameString.Create("TooltipDeviceUnpowered", "Unpowered");

	public static readonly GameString DeviceNoPower = GameString.Create("DeviceNoPower", "Cannot interact as the device is not powered");

	public static readonly GameString CannotInteract = GameString.Create("CannotInteract", "Cannot interact");

	public static readonly GameString DeviceError = GameString.Create("DeviceError", "Cannot interact as the device is in an error state");

	public static readonly GameString DeviceNotOpen = GameString.Create("DeviceNotOpen", "Cannot interact as device is not currently open");

	public static readonly GameString ThisDeviceNotOpen = GameString.Create("ThisDeviceNotOpen", "{LOCAL:Thing} must be open to operate", "Thing");

	public static readonly GameString DeviceIsOpen = GameString.Create("DeviceIsOpen", "Cannot interact as device is currently open");

	public static readonly GameString DeviceNotOn = GameString.Create("DeviceNotOn", "Cannot interact as the device is not turned on");

	public static readonly GameString CannotUseWhenNotOn = GameString.Create("CannotUseWhenNotOn", "Cannot use {LOCAL:Thing} as it is not <color=yellow>turned on</color>", "Thing");

	public static readonly GameString DeviceCanNotAdd = GameString.Create("DeviceCanNotAdd", "<color=red>You cannot add this to the device</color>");

	public static readonly GameString DeviceCanNotAddWhileProcessing = GameString.Create("DeviceCanNotAddWhileProcessing", "<color=red>Cannot add while the device is processing</color>");

	public static readonly GameString DeviceNoUseAbleIngredients = GameString.Create("DeviceNoUseAbleIngredients", "<color=red>There are no usable ingredients in this to add</color>");

	public static readonly GameString SlotVacant = GameString.Create("SlotVacant", "Vacant");

	public static readonly GameString SlotOccupant = GameString.Create("SlotOccupant", "Occupant");

	public static readonly GameString SlotOccupiedBy = GameString.Create("SlotOccupiedBy", "Currently occupied by {LOCAL:DynamicThing}", "DynamicThing");

	public static readonly GameString DeviceNotForSpecies = GameString.Create("DeviceNotForSpecies", "Device cannot be occupied by {LOCAL:Species}", "Species");

	public static readonly GameString DeviceDoesNotAllowInternals = GameString.Create("DeviceDoesNotAllowInternals", "Cannot enter device while wearing a {LOCAL:Thing}", "Thing");

	public static readonly GameString DeviceDoesNotAllowItemsInHands = GameString.Create("DeviceDoesNotAllowItemsInHands", "Cannot enter device while {LOCAL:Thing} is in {LOCAL:Slot}", "Thing", "Slot");

	public static readonly GameString DeviceLocked = GameString.Create("DeviceLocked", "Cannot interact as device is currently locked");

	public static readonly GameString DeviceBroken = GameString.Create("DeviceBroken", "Cannot interact as device is completely broken");

	public static readonly GameString DeviceEmpty = GameString.Create("DeviceEmpty", "This is to empty the device of all reagents");

	public static readonly GameString DeviceWillProduce = GameString.Create("DeviceWillProduce", "Will produce {LOCAL:Device}", "Device");

	public static readonly GameString DeviceAddReagents = GameString.Create("DeviceAddReagents", "Add <color=green>{LOCAL:Total}</color> units of {LOCAL:Thing}", "Total", "Thing");

	public static readonly GameString DeviceNothingToClear = GameString.Create("DeviceNothingToClear", "<color=red>There is nothing in this device to clear</color>");

	public static readonly GameString DeviceOnlyCookedItems = GameString.Create("DeviceOnlyCookedItems", "<color=red>This device only accepts cooked items and empty cans</color>");

	public static readonly GameString DeviceNoFuel = GameString.Create("DeviceNoFuel", "There is no fuel left in the device");

	public static readonly GameString DeviceNotHotEnough = GameString.Create("DeviceNotHotEnough", "The device is not hot enough to operate");

	public static readonly GameString DevicePortableConnectionFailureUnknown = GameString.Create("DevicePortableConnectionFailureUnknown", "Can only connect Unknown to this {LOCAL:Thing}", "Thing");

	public static readonly GameString DevicePortableConnectionFailureGas = GameString.Create("DevicePortableConnectionFailureGas", "Can only connect Gas to this {LOCAL:Thing}", "Thing");

	public static readonly GameString DevicePortableConnectionFailureGasLiquid = GameString.Create("DevicePortableConnectionFailureGasLiquid", "Can only connect Liquid to this {LOCAL:Thing}", "Thing");

	public static readonly GameString DeviceCannotDragCollision = GameString.Create("DeviceCannotDragCollision", "Cannot drag it will result in collision with structures");

	public static readonly GameString DeviceManualInputWindow = GameString.Create("DeviceManualInputWindow", "Will bring up a manual input window for you to enter a new setting");

	public static readonly GameString DeviceManualHeightWindow = GameString.Create("DeviceManualHeightWindow", "Opens an input window for a new height");

	public static readonly GameString DeviceNothingToChangeTo = GameString.Create("DeviceNothingToChangeTo", "Nothing inside to change to");

	public static readonly GameString DeviceNothingSelectedToDispense = GameString.Create("DeviceNothingSelectedToDispense", "Nothing selected to dispense");

	public static readonly GameString DeepMinerSomethingInTheWay = GameString.Create("DeepMinerSomethingInTheWay", "Something in the way {LOCAL:Thing}", "Thing");

	public static readonly GameString TorpedoTubeNothingLoadedInto = GameString.Create("TorpedoTubeNothingLoadedInto", "Nothing loaded into {LOCAL:Slot}", "Slot");

	public static readonly GameString TorpedoTubeNothingInThe = GameString.Create("TorpedoTubeNothingInThe", "Nothing in the {LOCAL:Slot}", "Slot");

	public static readonly GameString TorpedoTubeCanNotWhileExteriorDoorIsClosed = GameString.Create("TorpedoTubeCanNotWhileExteriorDoorIsClosed", "Cannot {LOCAL:Context} while exterior door is closed", "Context");

	public static readonly GameString TorpedoTubeCurrentlyObscuring = GameString.Create("TorpedoTubeCurrentlyObscuring", "{LOCAL:Slot0} is currently obscuring the {LOCAL:Slot1}", "Slot0", "Slot1");

	public static readonly GameString LogicNoDevice = GameString.Create("LogicNoDevice", "<color=red>No device</color>");

	public static readonly GameString LogicNoSetting = GameString.Create("LogicNoSetting", "<color=red>No setting</color>");

	public static readonly GameString LogicNoSlot = GameString.Create("LogicNoSlot", "<color=red>No slot</color>");

	public static readonly GameString LogicNoAvailableSlots = GameString.Create("LogicNoAvailableSlots", "<color=red>No available slots</color>");

	public static readonly GameString LogicNoWritableDevices = GameString.Create("LogicNoWritableDevices", "No other writable devices on network");

	public static readonly GameString LogicNoAdditionalSettingsFor = GameString.Create("LogicNoAdditionalSettingsFor", "<color=red>No additional settings for</color> {LOCAL:Thing}", "Thing");

	public static readonly GameString LogicNoAdditionalSlotsFor = GameString.Create("LogicNoAdditionalSlotsFor", "<color=red>No additional slots for</color> {LOCAL:Thing}", "Thing");

	public static readonly GameString LogicNoReadableDevices = GameString.Create("LogicNoReadableDevices", "No readable devices");

	public static readonly GameString LogicNoReadableTypes = GameString.Create("LogicNoReadableTypes", "No other readable types on network");

	public static readonly GameString LogicCurrentlySetTo = GameString.Create("LogicCurrentlySetTo", "The {LOCAL:Thing} is currently set to <color=yellow>Logic</color> mode", "Thing");

	public static readonly GameString AirLockIsCurrently = GameString.Create("AirLockIsCurrently", "The {LOCAL:Airlock} is currently <color=yellow>{LOCAL:AirLockState}</color>", "Airlock", "AirLockState");

	public static readonly GameString RequiresScrewdriver = GameString.Create("RequiresScrewdriver", "Requires screwdriver");

	public static readonly GameString RequiresScrewdriverOrLabeler = GameString.Create("RequiresScrewdriverOrLabeler", "You need a Screwdriver or Labeler to change settings");

	public static readonly GameString NoAudioReceiverDevices = GameString.Create("NoAudioReceiverDevices", "No audio receiver devices");

	public static readonly GameString ShaftNotOn = GameString.Create("ShaftNotOn", "Cannot interact as the current shaft is not turned on");

	public static readonly GameString ToolDoesNotHaveAnythingInSlot = GameString.Create("ToolDoesNotHaveAnythingInSlot", "{LOCAL:Tool} does not have anything in its {LOCAL:Slot} slot.", "Tool", "Slot");

	public static readonly GameString ToolDoesNotHaveEnoughCharge = GameString.Create("ToolDoesNotHaveEnoughCharge", "{LOCAL:Tool} does not have enough {LOCAL:Battery} charge.", "Tool", "Battery");

	public static readonly GameString ToolCanNotCompleteTask = GameString.Create("ToolCanNotCompleteTask", "{LOCAL:Tool} cannot complete this task.", "Tool");

	public static readonly GameString ToolNotCurrentlyOperableForTask = GameString.Create("ToolNotCurrentlyOperableForTask", "{LOCAL:Tool} is not currently operable for this task", "Tool");

	public static readonly GameString VolumeChangeDirection = GameString.Create("VolumeChangeDirection", "Change Direction");

	public static readonly GameString HoldForSmallIncrements = GameString.Create("HoldForSmallIncrements", "Hold {KEY:QuantityModifier} for smaller increments", "QuantityModifier");

	public static readonly GameString UseLabelerToSet = GameString.Create("UseLabelerToSet", "Use a <color=yellow>Labeler</color> to directly enter a value");

	public static readonly GameString PlantSamplerNoSample = GameString.Create("PlantSamplerNoSample", "No completed sample to view");

	public static readonly GameString PlantViewSampleInfo = GameString.Create("PlantViewSampleInfo", "<color=green>Left click</color> to view completed sample information");

	public static readonly GameString PlantHoldToClear = GameString.Create("PlantHoldToClear", "Hold <color=yellow>{KEY:QuantityModifier}</color> to clear {LOCAL:Plant}", "QuantityModifier", "Plant");

	public static readonly GameString PlantDead = GameString.Create("PlantDead", "The {LOCAL:Plant} is <color=red>dead</color>", "Plant");

	public static readonly GameString PlantNotGrowingMature = GameString.Create("PlantNotGrowingMature", "The {LOCAL:Plant} <color=red>is not growing</color> and will never fruit", "Plant");

	public static readonly GameString PlantBarelyGrowingMature = GameString.Create("PlantBarelyGrowingMature", "The {LOCAL:Plant} is barely growing toward fruiting", "Plant");

	public static readonly GameString PlantGrowingPoorlyMature = GameString.Create("PlantGrowingPoorlyMature", "The {LOCAL:Plant} is growing poorly toward fruiting", "Plant");

	public static readonly GameString PlantGrowingModeratelyMature = GameString.Create("PlantGrowingModeratelyMature", "The {LOCAL:Plant} is growing moderately well toward fruiting", "Plant");

	public static readonly GameString PlantThrivingMature = GameString.Create("PlantThrivingMature", "The {LOCAL:Plant} is thriving toward fruiting", "Plant");

	public static readonly GameString PlantNotGrowingSeed = GameString.Create("PlantNotGrowingSeed", "The {LOCAL:Plant} <color=red>is not growing</color> and will never seed", "Plant");

	public static readonly GameString PlantBarelyGrowingSeed = GameString.Create("PlantBarelyGrowingSeed", "The {LOCAL:Plant} is barely growing toward seeding", "Plant");

	public static readonly GameString PlantGrowingPoorlySeed = GameString.Create("PlantGrowingPoorlySeed", "The {LOCAL:Plant} is growing poorly toward seeding", "Plant");

	public static readonly GameString PlantGrowingModeratelySeed = GameString.Create("PlantGrowingModeratelySeed", "The {LOCAL:Plant} is growing moderately well toward seeding", "Plant");

	public static readonly GameString PlantThrivingSeed = GameString.Create("PlantThrivingSeed", "The {LOCAL:Plant} is thriving toward seeding", "Plant");

	public static readonly GameString PlantNotEnoughLight = GameString.Create("PlantNotEnoughLight", "The {LOCAL:Plant} <color=red>is not receiving the correct amount of light per day</color>", "Plant");

	public static readonly GameString PlantAtmosphereTooHot = GameString.Create("PlantAtmosphereTooHot", "The {LOCAL:Plant} <color=red>atmosphere is too hot</color>", "Plant");

	public static readonly GameString PlantAtmosphereTooCold = GameString.Create("PlantAtmosphereTooCold", "The {LOCAL:Plant} <color=red>atmosphere is too cold</color>", "Plant");

	public static readonly GameString PlantWaterTooHot = GameString.Create("PlantWaterTooHot", "The {LOCAL:Plant} <color=red>water is too hot</color>", "Plant");

	public static readonly GameString PlantWaterTooCold = GameString.Create("PlantWaterTooCold", "The {LOCAL:Plant} <color=red>water is too cold</color>", "Plant");

	public static readonly GameString PlantWaterNeeds = GameString.Create("PlantWaterNeeds", "The {LOCAL:Plant} <color=red>needs water</color>", "Plant");

	public static readonly GameString PlantNotCorrectAtmosphere = GameString.Create("PlantNotCorrectAtmosphere", "The {LOCAL:Plant} <color=red>does not have the correct atmosphere gas composition</color>", "Plant");

	public static readonly GameString PlantIsPollutedByAir = GameString.Create("PlantIsPollutedByAir", "The {LOCAL:Plant} <color=red>is being polluted from the air</color>", "Plant");

	public static readonly GameString PlantIsReceivingLight = GameString.Create("PlantIsReceivingLight", "The {LOCAL:Plant} is receiving light", "Plant");

	public static readonly GameString PlantIsInDarkness = GameString.Create("PlantIsInDarkness", "The {LOCAL:Plant} is in darkness", "Plant");

	public static readonly GameString PlantIsInLowPressureEnvironment = GameString.Create("PlantIsInLowPressureEnvironment", "The {LOCAL:Plant} <color=red>is in a low pressure environment</color>", "Plant");

	public static readonly GameString PlantIsInHighPressureEnvironment = GameString.Create("PlantIsInHighPressureEnvironment", "The {LOCAL:Plant} <color=red>is in a high pressure environment</color>", "Plant");

	public static readonly GameString PlantCreate = GameString.Create("PlantCreate", "This will create <color=yellow>{LOCAL:Count}</color> x {LOCAL:Plant}", "Count", "Plant");

	public static readonly GameString PlantCreateSeeds = GameString.Create("PlantCreateSeeds", "This will create <color=yellow>{LOCAL:Count}</color> x {LOCAL:Plant} seeds", "Count", "Plant");

	public static readonly GameString OreNoMiningBelt = GameString.Create("OreNoMiningBelt", "No mining belt to store in");

	public static readonly GameString OreNoSlotInMiningBelt = GameString.Create("OreNoSlotInMiningBelt", "No available slot in mining belt");

	public static readonly GameString ApcUnableToMoveLocked = GameString.Create("ApcUnableToMoveLocked", "Unable to move as the unit is locked");

	public static readonly GameString ApcUnableToMoveTool = GameString.Create("ApcUnableToMoveTool", "Unable to move as the tool is not in operable condition");

	public static readonly GameString ElevatorUnableToForceOpenLocked = GameString.Create("ElevatorUnableToForceOpenLocked", "Unable to force open, {LOCAL:Elevator} locked", "Elevator");

	public static readonly GameString ElevatorUnableToForceOpenPowered = GameString.Create("ElevatorUnableToForceOpenPowered", "Unable to force open, {LOCAL:Elevator} is currently powered", "Elevator");

	public static readonly GameString DoorUnableToForceOpenLocked = GameString.Create("DoorUnableToForceOpenLocked", "Unable to force open, door bolts are locked");

	public static readonly GameString DoorUnableToForceOpenPowered = GameString.Create("DoorUnableToForceOpenPowered", "Unable to force open, door is currently powered");

	public static readonly GameString ActivateExplosive = GameString.Create("ActivateExplosive", "Activates a {0} second timer. Caution is advised.");

	public static readonly GameString ArmExplosive = GameString.Create("ArmExplosive", "Arm");

	public static readonly GameString DisarmExplosive = GameString.Create("DisarmExplosive", "Disarm");

	public static readonly GameString RocketCanNotEnterCommand = GameString.Create("RocketCanNotEnterCommand", "Cannot enter command module is full");

	public static readonly GameString PlantStabilizerToggleModeToStabilize = GameString.Create("PlantStabilizerToggleModeToStabilize", "Set stabilize");

	public static readonly GameString PlantStabilizerToggleModeToDestabilize = GameString.Create("PlantStabilizerToggleModeToDestabilize", "Set destabilize");

	public static readonly GameString PlantStabilizerStabilizeMode = GameString.Create("PlantStabilizerStabilizeMode", "Stabilize");

	public static readonly GameString PlantStabilizerDestabilizeMode = GameString.Create("PlantStabilizerDestabilizeMode", "Destabilize");

	public static readonly GameString PlantStabilizerStabilizePlant = GameString.Create("PlantStabilizerStabilizePlant", "Stabilize all genes on {LOCAL:Plant}", "Plant");

	public static readonly GameString PlantStabilizerDestabilizePlant = GameString.Create("PlantStabilizerDestabilizePlant", "Destabilize <color=green>{LOCAL:Gene}</color> gene on {LOCAL:Plant} ", "Gene", "Plant");

	public static readonly GameString PlantStabilizerStabilizingPlant = GameString.Create("PlantStabilizerStabilizingPlant", "Stabilizing {LOCAL:Plant} <color=yellow>{LOCAL:Value}%</color>", "Plant", "Value");

	public static readonly GameString PlantStabilizerDestabilizingPlant = GameString.Create("PlantStabilizerDestabilizingPlant", "Destabilizing {LOCAL:Plant} <color=yellow>{LOCAL:Value}%</color>", "Plant", "Value");

	public static readonly GameString PlantStabilizerStartStabilizing = GameString.Create("PlantStabilizerStartStabilizing", "Start stabilizing");

	public static readonly GameString PlantStabilizerStartDestabilizing = GameString.Create("PlantStabilizerStartDestabilizing", "Start destabilizing");

	public static readonly GameString PlantStabilizerNoPlantInSlot = GameString.Create("PlantStabilizerNoPlantInSlot", "No plant in slot");

	public static readonly GameString TraderLocked = GameString.Create("TraderLocked", "Locked");

	public static readonly GameString TraderTrade = GameString.Create("TraderTrade", "Trade");

	public static readonly GameString CommunicateWithShuttle = GameString.Create("CommunicateWithShuttle", "Communicate");

	public static readonly GameString ContactTradeNoLandingPad = GameString.Create("ContactTradeNoLandingPad", "Trader cannot land as no active landing pad is available");

	public static readonly GameString ContactTradePadOff = GameString.Create("ContactTradePadOff", "Trader cannot land as the target Landing pad is turned off");

	public static readonly GameString ContactTradePadUnpowered = GameString.Create("ContactTradePadUnpowered", "Trader cannot land as the target Landing pad is unpowered");

	public static readonly GameString ContactTradeLandingObstructed = GameString.Create("ContactTradeLandingObstructed", "Trader cannot land as the path to the target landing pad is obstructed");

	public static readonly GameString ContactTradeDepartingObstructed = GameString.Create("ContactTradeDepartingObstructed", "Trader cannot depart as the path is obstructed");

	public static readonly GameString ContactTradeLandingPadError = GameString.Create("ContactTradeLandingPadError", "Trader cannot land as the target landing pad is incorrectly configured");

	public static readonly GameString ContactTradeLandingPadToSmall = GameString.Create("ContactTradeLandingPadToSmall", "Trader cannot land as the target landing pad is too small");

	public static readonly GameString ContactTradeLandingNoThreshold = GameString.Create("ContactTradeLandingNoThreshold", "Trader plane cannot land without a threshold");

	public static readonly GameString SizeTwoDCondition = GameString.Create("SizeTwoDCondition", "Size must be {LOCAL:Condition} {LOCAL:SizeX} x {LOCAL:SizeY}", "Condition", "SizeX", "SizeY");

	public static readonly GameString BuildNetworkCondition = GameString.Create("BuildNetworkCondition", "Build a {LOCAL:NetworkType} network", "NetworkType");

	public static readonly GameString TraderContactCondition = GameString.Create("TraderContactCondition", "{LOCAL:Action} a trader contact", "Action");

	public static readonly GameString ResolveAction = GameString.Create("ResolveAction", "Resolve");

	public static readonly GameString InterrogateAction = GameString.Create("InterrogateAction", "Interrogate");

	public static readonly GameString LandAction = GameString.Create("LandAction", "Land");

	public static readonly GameString AnimalHungry = GameString.Create("AnimalHungry", "Hungry");

	public static readonly GameString AnimalVeryHungry = GameString.Create("AnimalVeryHungry", "Very hungry");

	public static readonly GameString AnimalOld = GameString.Create("AnimalOld", "Getting old");

	public static readonly GameString AnimalVeryOld = GameString.Create("AnimalVeryOld", "Getting very old");

	public static readonly GameString UnviableEggNotice = GameString.Create("UnviableEggNotice", "The {LOCAL:Name} is unviable", "Name");

	public static readonly GameString WillHatchInto = GameString.Create("WillHatchInto", "Will hatch into a {LOCAL:Thing}", "Thing");

	public static readonly GameString ViableEggNotice = GameString.Create("ViableEggNotice", "Incubation requires {LOCAL:MinTemp} to {LOCAL:MaxTemp}", "MinTemp", "MaxTemp");

	public static readonly GameString ArmControlIdle = GameString.Create("ArmControlIdle", "Idle");

	public static readonly GameString ArmControlPlant = GameString.Create("ArmControlPlant", "Plant");

	public static readonly GameString ArmControlHarvest = GameString.Create("ArmControlHarvest", "Harvest");

	public static readonly GameString YouNeedAWrenchForOrientation = GameString.Create("YouNeedAWrenchForOrientation", "You need a wrench to change orientation");

	public static readonly GameString NotApplicableString = GameString.Create("NotApplicableString", "N/A");

	public static readonly GameString SlotFull = GameString.Create("SlotFull", "Slot is occupied");

	public static readonly GameString SplicerCurrentGene = GameString.Create("SplicerCurrentGene", "Current gene: <color=green>{LOCAL:Gene}</color>", "Gene");

	public static readonly GameString SplicerNextGene = GameString.Create("SplicerNextGene", "Change gene to: {LOCAL:Gene}", "Gene");

	public static readonly GameString SplicerDeviceIsOpen = GameString.Create("SplicerDeviceIsOpen", "Close device before starting splice");

	public static readonly GameString SplicerNeedSourceAndTargetPlant = GameString.Create("SplicerNeedSourceAndTargetPlant", "Need a source and target plant of the same type to start splice");

	public static readonly GameString SplicerStartSplice = GameString.Create("SplicerStartSplice", "Start splice");

	public static readonly GameString SplicerChangeGene = GameString.Create("SplicerChangeGene", "Change gene");

	public static readonly GameString SplicerPreviewSplice = GameString.Create("SplicerPreviewSplice", "Will splice <color=green>{LOCAL:Gene}</color> from {LOCAL:SourcePlant} into {LOCAL:TargetPlant}", "Gene", "SourcePlant", "TargetPlant");

	public static readonly GameString SplicerBusy = GameString.Create("SplicerBusy", "Currently splicing <color=green>{LOCAL:PercentComplete}%</color> complete", "PercentComplete");

	public static readonly GameString SplicerAddOneToSlot = GameString.Create("SplicerAddOneToSlot", "Add {LOCAL:Plant} to slot", "Plant");

	public static readonly GameString CableRunNoCoil = GameString.Create("CableRunNoCoil", "No cable coil to run");

	public static readonly GameString GasMixture = GameString.Create("GasMixture", "Gas mixture");

	public static readonly GameString ItemTransactionClientError = GameString.Create("ItemTransactionClientError", "Item transaction should only be called on Server");

	public static readonly GameString TransactionAtmosError = GameString.Create("TransactionAtmosError", "Landing pad atmosphere not found");

	public static readonly GameString TransactionTradableError = GameString.Create("TransactionTradableError", "Trader has spawned something that is not an ITradable");

	public static readonly GameString TransactionFailRequired = GameString.Create("TransactionFailRequired", "Cannot complete transaction. You are trying to sell more than the trader wants.");

	public static readonly GameString TransactionFailCreditCard = GameString.Create("TransactionFailCreditCard", "Cannot complete transaction. No credit card found. Credit card must be in hand or the creditcard slot in your uniform.");

	public static readonly GameString TransactionFailInsufficientAvailable = GameString.Create("TransactionFailInsufficientAvailable", "Cannot complete transaction. You are trying to sell more than you have stored.");

	public static readonly GameString TransactionErrorNonTradable = GameString.Create("TransactionErrorNonTradable", "Non tradable item in inventory:");

	public static readonly GameString TransactionFailInventory = GameString.Create("TransactionFailInventory", "Cannot complete transaction. Connect a <color=green>Vending Machine</color> or have a free <color=yellow>Left Hand</color> or <color=yellow>Right Hand</color>.");

	public static readonly GameString TransactionIncompleteTransaction = GameString.Create("TransactionIncompleteTransaction", "Incomplete transaction as there was not enough space. You have only been changed for what there was inventory space for. Connect a <color=green>Vending Machine</color> or have a free <color=yellow>Left Hand</color> or <color=yellow>Right Hand</color>.");

	public static readonly GameString TransactionFailItem = GameString.Create("TransactionFailItem", "Cannot complete transaction. Traded item is invalid.");

	public static readonly GameString TransactionFailInsufficientStock = GameString.Create("TransactionFailInsufficientStock", "The trader does not have enough stock to complete this transaction.");

	public static readonly GameString TransactionFailInsufficientFunds = GameString.Create("TransactionFailInsufficientFunds", "You do not have enough money to complete this transaction.");

	public static readonly GameString TransactionErrorTradableCast = GameString.Create("TransactionErrorTradableCast", "Cannot cast tradable item to thing");

	public static readonly GameString TradingToolTipNoInfo = GameString.Create("TradingToolTipNoInfo", "No additional information");

	public static readonly GameString TradingToolTipNoRequirements = GameString.Create("TradingToolTipNoRequirements", "No additional requirements");

	public static readonly GameString TradeGasQuantityComparison = GameString.Create("TradeGasQuantityComparison", "Contains {LOCAL:ComparisonOperator} {LOCAL:ValueToCompare}", "ComparisonOperator", "ValueToCompare");

	public static readonly GameString Quantity = GameString.Create("Quantity", "Quantity");

	public static readonly GameString Gene = GameString.Create("Gene", "Gene");

	public static readonly GameString OperatorAny = GameString.Create("OperatorAny", "Any");

	public static readonly GameString OperatorNone = GameString.Create("OperatorNone", "None");

	public static readonly GameString OperatorAll = GameString.Create("OperatorAll", "All");

	public static readonly GameString TradeOperatorAny = GameString.Create("TradeOperatorAny", "Any of the following");

	public static readonly GameString TradeOperatorNone = GameString.Create("TradeOperatorNone", "None of the following");

	public static readonly GameString TradeOperatorAll = GameString.Create("TradeOperatorAll", "All of the following");

	public static readonly GameString CompareLessThan = GameString.Create("CompareLessThan", "less than");

	public static readonly GameString CompareEqualOrLess = GameString.Create("CompareEqualOrLess", "less or equal to");

	public static readonly GameString CompareEqual = GameString.Create("CompareEqual", "equal to");

	public static readonly GameString CompareEqualOrGreater = GameString.Create("CompareEqualOrGreater", "greater or equal to");

	public static readonly GameString CompareGreater = GameString.Create("CompareGreater", "greater than");

	public static readonly GameString TradeItemStackChild = GameString.Create("TradeItemStackChild", "Includes {LOCAL:Quantity} x {LOCAL:Item}", "Item", "Quantity");

	public static readonly GameString TradeItemNutrition = GameString.Create("TradeItemNutrition", "Provides {LOCAL:Value} nutrition", "Value");

	public static readonly GameString TradeItemQuantity = GameString.Create("TradeItemQuantity", "Quantity is {LOCAL:ItemAsUnit}", "ItemAsUnit");

	public static readonly GameString TradeItemParent = GameString.Create("TradeItemParent", "{LOCAL:Item} with the following", "Item");

	public static readonly GameString TradeItemContains = GameString.Create("TradeItemContains", "The {LOCAL:OrePrefab} contains the following", "OrePrefab");

	public static readonly GameString TradeRatio = GameString.Create("TradeRatio", "{LOCAL:GasType} ratio must be {LOCAL:ComparisonOperator} {LOCAL:Value}", "GasType", "ComparisonOperator", "Value");

	public static readonly GameString PressureCondition = GameString.Create("PressureCondition", "Pressure must be {LOCAL:ComparisonOperator} {LOCAL:Value}", "ComparisonOperator", "Value");

	public static readonly GameString TradeQuantity = GameString.Create("TradeQuantity", "Quantity {LOCAL:ComparisonOperator} {LOCAL:Value}", "ComparisonOperator", "Value");

	public static readonly GameString TradeDecay = GameString.Create("TradeDecay", "Decay {LOCAL:ComparisonOperator} {LOCAL:Value}", "ComparisonOperator", "Value");

	public static readonly GameString TradeEnergyRatio = GameString.Create("TradeEnergyRatio", "Energy ratio {LOCAL:ComparisonOperator} {LOCAL:Value}", "ComparisonOperator", "Value");

	public static readonly GameString TradeEnergyState = GameString.Create("TradeEnergyState", "Energy state {LOCAL:ComparisonOperator} {LOCAL:Value}", "ComparisonOperator", "Value");

	public static readonly GameString DifficultyState = GameString.Create("DifficultyState", "Difficulty {LOCAL:ComparisonOperator} {LOCAL:Value}", "ComparisonOperator", "Value");

	public static readonly GameString SpeciesState = GameString.Create("SpeciesState", "Player Species {LOCAL:Value}", "Value");

	public static readonly GameString TradeChildItem = GameString.Create("TradeChildItem", "Contains a {LOCAL:Item}", "Item");

	public static readonly GameString CreateThingCondition = GameString.Create("CreateThingCondition", "Create a {LOCAL:Thing}", "Thing");

	public static readonly GameString TradeChildItemInSlot = GameString.Create("TradeChildItemInSlot", "Contains a {LOCAL:Item} in the {LOCAL:Slot} slot", "Item", "Slot");

	public static readonly GameString TradeTemperatureBetween = GameString.Create("TradeTemperatureBetween", "Temperature between {LOCAL:MinTemp} and {LOCAL:MaxTemp}", "MinTemp", "MaxTemp");

	public static readonly GameString TradingWith = GameString.Create("TradingWith", "Trading with");

	public static readonly GameString Buy = GameString.Create("Buy", "Buy");

	public static readonly GameString Sell = GameString.Create("Sell", "Sell");

	public static readonly GameString PartialPressureCondition = GameString.Create("PartialPressureCondition", "Partial pressure {LOCAL:GasType} must be {LOCAL:ComparisonOperator} {LOCAL:Value}", "GasType", "ComparisonOperator", "Value");

	public static readonly GameString HelperHintExpand = GameString.Create("HelperHintExpand", "Expand");

	public static readonly GameString HelperHintCollapse = GameString.Create("HelperHintCollapse", "Collapse");

	public static readonly GameString HelperHintDismiss = GameString.Create("HelperHintDismiss", "Dismiss");

	public static readonly GameString HelperHintRestore = GameString.Create("HelperHintRestore", "Restore");

	public static readonly GameString HelperHintOpenStationpedia = GameString.Create("HelperHintOpenStationpedia", "Open Stationpedia");

	public static readonly GameString HelperHintCreateThingCondition = GameString.Create("HelperHintCreateThingCondition", "Create a <link=\"{LOCAL:PrefabName}\"><color=green>{LOCAL:DisplayName}</color></link>", "PrefabName", "DisplayName");

	public static readonly GameString HelperHintContainsItem = GameString.Create("HelperHintContainsItem", "Contains a <link=\"{LOCAL:PrefabName}\"><color=green>{LOCAL:DisplayName}</color></link>", "PrefabName", "DisplayName");

	public static readonly GameString HelperHintContainsItemInSlot = GameString.Create("HelperHintContainsItemInSlot", "Contains a <link=\"{LOCAL:PrefabName}\"><color=green>{LOCAL:DisplayName}</color></link> in the <color=orange>{LOCAL:SlotName}</color> slot", "PrefabName", "DisplayName", "SlotName");

	public static readonly GameString HelperHintRoomState = GameString.Create("HelperHintRoomState", "In a <color=yellow>{LOCAL:RoomType}</color> room", "RoomType");

	public static readonly GameString HelperHintAnyRoomState = GameString.Create("HelperHintAnyRoomState", "Build a room");

	public static readonly GameString HelperHintQuantityComparison = GameString.Create("HelperHintQuantityComparison", "Quantity {LOCAL:ComparisonOperator} ", "ComparisonOperator");

	public static readonly GameString HelperHintPressureCondition = GameString.Create("HelperHintPressureCondition", "Pressure must be {LOCAL:ComparisonOperator} ", "ComparisonOperator");

	public static readonly GameString HelperHintRatioMustBeComparison = GameString.Create("HelperHintRatioMustBeComparison", " ratio must be {LOCAL:ComparisonOperator} ", "ComparisonOperator");

	public static readonly GameString HelperHintMustBeComparison = GameString.Create("HelperHintMustBeComparison", " must be {LOCAL:ComparisonOperator} ", "ComparisonOperator");

	public static readonly GameString HelperHintPartialPressure = GameString.Create("HelperHintPartialPressure", "Partial pressure ");

	public static readonly GameString HelperHintDecayComparison = GameString.Create("HelperHintDecayComparison", "Decay {LOCAL:ComparisonOperator} ", "ComparisonOperator");

	public static readonly GameString HelperHintSpeciesState = GameString.Create("HelperHintSpeciesState", "Player Species ");

	public static readonly GameString HelperHintContainsComparison = GameString.Create("HelperHintContainsComparison", "Contains {LOCAL:ComparisonOperator} ", "ComparisonOperator");

	public static readonly GameString HelperHintHumanCondition = GameString.Create("HelperHintHumanCondition", "todo");

	public static readonly GameString HelperHintInCellCondition = GameString.Create("HelperHintInCellCondition", "Thing is in cell: {LOCAL:Value}", "Value");

	public static readonly GameString Available = GameString.Create("Available", "Available");

	public static readonly GameString Wants = GameString.Create("Wants", "Wants");

	public static readonly GameString SoldOut = GameString.Create("SoldOut", "Sold Out");

	public static readonly GameString StockFull = GameString.Create("StockFull", "Stock Full");

	public static readonly GameString EnvironmentRequirementFail = GameString.Create("EnvironmentRequirementFail", "The Trader will not exit the shuttle until the atmosphere is safe to breathe.");

	public static readonly GameString CycleWaypointHeight = GameString.Create("CycleWaypointHeight", "Cycle height");

	public static readonly GameString CycleWaypoint = GameString.Create("CycleWaypoint", "Cycle waypoint");

	public static readonly GameString ToggleVisualiser = GameString.Create("ToggleVisualiser", "Toggle visualiser");

	public static readonly GameString CurrentWaypoint = GameString.Create("CurrentWaypoint", "Next waypoint: {LOCAL:NextWaypoint}", "NextWaypoint");

	public static readonly GameString CurrentHeight = GameString.Create("CurrentHeight", "Current height {LOCAL:Height}", "Height");

	public static readonly GameString VisualiserState = GameString.Create("VisualiserState", "Visualiser {LOCAL:State}", "State");

	public static readonly GameString TraderCrashedEvent = GameString.Create("TraderCrashedEvent", "The {LOCAL:Name} trader has crashed into {LOCAL:Collision} at position {LOCAL:Position}.", "Name", "Collision", "Position");

	public static readonly GameString TraderLeftRangeEvent = GameString.Create("TraderLeftRangeEvent", "The {LOCAL:Name} trader has left contact range.", "Name");

	public static readonly GameString TraderEnteredRangeEvent = GameString.Create("TraderEnteredRangeEvent", "The {LOCAL:Name} trader has entered contact range.", "Name");

	public static readonly GameString EvaporationChamberLiquidConnection = GameString.Create("EvaporationChamberLiquidConnection", "Liquid input connection");

	public static readonly GameString EvaporationChamberGasConnection = GameString.Create("EvaporationChamberGasConnection", "Gas output connection");

	public static readonly GameString EvaporationChamberHeatConnection = GameString.Create("EvaporationChamberHeatConnection", "Gas heat exchange connection");

	public static readonly GameString CondensationChamberLiquidConnection = GameString.Create("CondensationChamberLiquidConnection", "Liquid output connection");

	public static readonly GameString CondensationChamberGasConnection = GameString.Create("CondensationChamberGasConnection", "Gas input connection");

	public static readonly GameString Recharge = GameString.Create("Recharge", "Recharge");

	public static readonly GameString FullyCharged = GameString.Create("FullyCharged", "Fully Charged");

	public static readonly GameString Repair = GameString.Create("Repair", "Repair");

	public static readonly GameString HandSlotOccupied = GameString.Create("HandSlotOccupied", "Hand slot is occupied");

	public static readonly GameString Unpack = GameString.Create("Unpack", "Unpack");

	public static readonly GameString Defibrillate = GameString.Create("Defibrillate", "Defibrillate");

	public static readonly GameString DefibrillatorNotOperable = GameString.Create("DefibrillatorNotOperable", "Defibrillator is not operable for this action.");

	public static readonly GameString InputSetting = GameString.Create("InputSetting", "Setting");

	public static readonly GameString Thrust = GameString.Create("Thrust", "Thrust");

	public static readonly GameString FlowRate = GameString.Create("FlowRate", "Flow Rate");

	public static readonly GameString ExhaustVelocity = GameString.Create("ExhaustVelocity", "Exhaust Velocity");

	public static readonly GameString ExhaustTemperature = GameString.Create("ExhaustTemperature", "Exhaust Temperature");

	public static readonly GameString GasTemperature = GameString.Create("GasTemperature", "Temperature: {LOCAL:Value}K", "Value");

	public static readonly GameString CondensationInGasPipe = GameString.Create("CondensationInGasPipe", "Condensation is happening in the pipe");

	public static readonly GameString LiquidInGasPipe = GameString.Create("LiquidInGasPipe", "There is <color=red>Liquid</color> in the pipe");

	public static readonly GameString PipeStressed = GameString.Create("PipeStressed", "The pressure of the pipe is dangerously high");

	public static readonly GameString LiquidStress = GameString.Create("LiquidStress", "Liquid Stress: ");

	public static readonly GameString PipeFailModePressure = GameString.Create("PipeFailModePressure", "Destroyed due to an <color=red>overpressure</color> event");

	public static readonly GameString PipeFailModeLiquid = GameString.Create("PipeFailModeLiquid", "Destroyed due to the <color=red>presence of liquid</color>");

	public static readonly GameString PipeFailModeFrozen = GameString.Create("PipeFailModeFrozen", "Destroyed due to <color=red>liquids or gasses freezing</color>");

	public static readonly GameString PipeFail = GameString.Create("PipeFail", "<color=green>{LOCAL:DisplayName}</color> was destroyed due to {Local:Reason}", "DisplayName", "Reason");

	public static readonly GameString PipeDamageModePressure = GameString.Create("PipeDamageModePressure", "Damaged by an <color=red>overpressure</color> event");

	public static readonly GameString PipeDamageModeLiquid = GameString.Create("PipeDamageModeLiquid", "Damaged due the <color=red>presence of liquid</color>");

	public static readonly GameString PipeDamageModeFrozen = GameString.Create("PipeDamageModeFrozen", "Damaged due to <color=red>liquids or gasses freezing</color>");

	public static readonly GameString OverPressure = GameString.Create("OverPressure", "Over pressure");

	public static readonly GameString Frozen = GameString.Create("Frozen", "Liquids or gasses Freezing");

	public static readonly GameString PresenceOfLiquid = GameString.Create("PresenceOfLiquid", "Presence of liquid");

	public static readonly GameString SaveVersionWarning = GameString.Create("SaveVersionWarning", "WARNING. Old save version detected");

	public static readonly GameString StateChangePopup = GameString.Create("StateChangePopup", "Gas-to-Liquid State Change: This save is from version 0.2.4120.19431 or older and may not be be compatible with the new atmospherics state change system. Would you like Stationeers to attempt to modify the contents of atmospheric networks in the loaded game to prevent immediate explosions and overpressure events due to liquid-gas state change? (some gas/liquid may be deleted by and/or have its temperature modified by this process)");

	public static readonly GameString PipeVolumeChangePopup = GameString.Create("PipeVolumeChangePopup", "Pipe Volume Balance change: This save is from version 0.2.4120.19448 or older and may not be be compatible with new balance changes to pipe Volumes. Would you like Stationeers to attempt to modify the contents atmospheric networks in the loaded game to prevent immediate explosions and overpressure events due these changes? (some gas/liquid may be deleted by this process)");

	public static readonly GameString AttemptToFix = GameString.Create("AttemptToFix", "Attempt To Fix");

	public static readonly GameString DoNothing = GameString.Create("DoNothing", "Do Nothing");

	public static readonly GameString PlacementBlockedByRocket = GameString.Create("PlacementBlockedByRocket", "Placement is blocked by a <color=green>Rocket</color>");

	public static readonly GameString PlacementConnectingNeedsFuselage = GameString.Create("PlacementConnectingNeedsFuselage", "Placement that connects to a <color=green>Rocket</color> needs to be inside a <color=green>Fuselage</color> or via <color=green>Umbilical</color>");

	public static readonly GameString BulkheadMustBeCentered = GameString.Create("BulkheadMustBeCentered", "A <color=green>Crew Bulkhead</color> must be placed on the central column of the <color=green>Fuselage</color>");

	public static readonly GameString ProcessedMoles = GameString.Create("ProcessedMoles", "Input processed {LOCAL:Moles}", "Moles");

	public static readonly GameString MachinePressureDifferential = GameString.Create("MachinePressureDifferential", "Pressure Differential {LOCAL:PressureDelta}", "PressureDelta");

	public static readonly GameString PassedMolesInput1 = GameString.Create("PassedMolesInput1", "Input 1 throughput <color=green>{LOCAL:PressureDelta} mol</color>", "PressureDelta");

	public static readonly GameString PassedMolesInput2 = GameString.Create("PassedMolesInput2", "Input 2 throughput <color=green>{LOCAL:PressureDelta} mol</color>", "PressureDelta");

	public static readonly GameString RequireDlcToFabricate = GameString.Create("RequireDlcToFabricate", "You do not have the required DLC to print this item.");

	public static readonly GameString HeatExchangerEnergyTransfer = GameString.Create("HeatExchangerEnergyTransfer", "Heat Exchange Rate {LOCAL:EnergyResult}", "EnergyResult");

	public static readonly GameString EnergyTransfer = GameString.Create("EnergyTransfer", "Energy Transfer");

	public static readonly GameString HeatExchangeArea = GameString.Create("HeatExchangeArea", "Surface Area");

	public static readonly GameString HeatExchangeFromTo = GameString.Create("HeatExchangeFromTo", "Exchanging from {LOCAL:Input1} to {LOCAL:Input2}", "Input1", "Input2");

	public static readonly GameString FlowRateStatus = GameString.Create("FlowRateStatus", "Flow-rate is <color={LOCAL:color}>{LOCAL:flowRate}</color>", "color", "flowRate");

	public static readonly GameString IncreasePipeVolumePressure = GameString.Create("IncreasePipeVolumePressure", "Increase output pipe-network capacity or pressure");

	public static readonly GameString PressureCloseToTarget = GameString.Create("PressureCloseToTarget", "Pressure close to External target");

	public static readonly GameString PressureCloseToInternalTarget = GameString.Create("PressureCloseToInternalTarget", "Pressure close to Internal target");

	public static readonly GameString Rocket = GameString.Create("Rocket", "Rocket {LOCAL:id}", "id");

	public static readonly GameString InvalidFuselagePlacement = GameString.Create("InvalidFuselagePlacement", "Fuselage must be constructed on top of another fuselage");

	public static readonly GameString InvalidAdjacentFuselagePlacement = GameString.Create("InvalidAdjacentFuselagePlacement", "Rockets can not be placed next to each other");

	public static readonly GameString InvalidFuselageDeconstruct = GameString.Create("InvalidFuselageDeconstruct", "Fuselage cannot be deconstructed as it is supporting <color=green>{LOCAL:Other}</color>", "Other");

	public static readonly GameString Left = GameString.Create("Left", "Left");

	public static readonly GameString Center = GameString.Create("Center", "Center");

	public static readonly GameString Right = GameString.Create("Right", "Right");

	public static readonly GameString UmbilicalError = GameString.Create("UmbilicalError", "Cannot open when in error state");

	public static readonly GameString DestinationCode = GameString.Create("DestinationCode", "Destination Code: {LOCAL:Code}", "Code");

	public static readonly GameString None = GameString.Create("None", "None");

	public static readonly GameString TheGround = GameString.Create("TheGround", "the ground");

	public static readonly GameString Unknown = GameString.Create("Unknown", "Unknown");

	public static readonly GameString GroundVelocity = GameString.Create("GroundVelocity", "Ground Velocity");

	public static readonly GameString TransferVelocity = GameString.Create("TransferVelocity", "Transfer Velocity");

	public static readonly GameString Orbit = GameString.Create("Orbit", "Orbit");

	public static readonly GameString SpaceName = GameString.Create("SpaceName", "Space");

	public static readonly GameString TimeToImpact = GameString.Create("TimeToImpact", "<color=red>Impact</color>:");

	public static readonly GameString TimeToArrest = GameString.Create("TimeToArrest", "Decent Arrest:");

	public static readonly GameString TimeToArrival = GameString.Create("TimeToArrival", "ETA:");

	public static readonly GameString RocketMapIconTooltip = GameString.Create("RocketMapIconTooltip", "Rocket: <color=green>{LOCAL:name}</color>", "name");

	public static readonly GameString MediumRocketSize = GameString.Create("MediumRocketSize", "Medium");

	public static readonly GameString LargeRocketSize = GameString.Create("LargeRocketSize", "Large");

	public static readonly GameString RocketAdjacentFail = GameString.Create("RocketAdjacentFail", "Rockets can not be placed next to each other");

	public static readonly GameString FuselageRequiresLaunchMount = GameString.Create("FuselageRequiresLaunchMount", "Fuselage must be constructed on top of a fully constructed launch mount");

	public static readonly GameString PlacementRequiresMultipleSupportPillars = GameString.Create("PlacementRequiresMultipleSupportPillars", "Placement requires a support frame below each pillar. <color=yellow>{0}</color> out of <color=green>{1}</color> supports are present.");

	public static readonly GameString MiningHeadSpeedMultiplierTooltip = GameString.Create("MiningHeadSpeedMultiplierTooltip", "Speed Multiplier: <color=yellow>{LOCAL:Multiplier}</color>", "Multiplier");

	public static readonly GameString MiningHeadReagentYieldMultiplierTooltip = GameString.Create("MiningHeadReagentYieldMultiplierTooltip", "Reagent Yield Multiplier: <color=yellow>{LOCAL:Multiplier}</color>", "Multiplier");

	public static readonly GameString MiningHeadIceYieldMultiplierTooltip = GameString.Create("MiningHeadIceYieldMultiplierTooltip", "Ice Yield Multiplier: <color=yellow>{LOCAL:Multiplier}</color>", "Multiplier");

	public static readonly GameString MiningHeadHealthMultiplierTooltip = GameString.Create("MiningHeadHealthMultiplierTooltip", "Health Multiplier: <color=yellow>{LOCAL:Multiplier}</color>", "Multiplier");

	public static readonly GameString MiningHeadPowerConsuptionSpeedTooltip = GameString.Create("MiningHeadPowerConsuptionSpeedTooltip", "Power Consumption Multiplier: <color=yellow>{LOCAL:Multiplier}</color>", "Multiplier");

	public static readonly GameString ChartFailure = GameString.Create("ChartFailure", "No Further Nav-Points to chart");

	public static readonly GameString MiningFailure = GameString.Create("MiningFailure", "No available mine-able deposit");

	public static readonly GameString NoRocketMiner = GameString.Create("NoRocketMiner", "This Rocket has no mining device");

	public static readonly GameString NoRocketScanner = GameString.Create("NoRocketScanner", "This Rocket has no scanning device");

	public static readonly GameString UnableToDeploy = GameString.Create("UnableToDeploy", "Unable to deploy");

	public static readonly GameString SurveyCompleted = GameString.Create("SurveyCompleted", "Survey completed at {Local:DisplayName}", "DisplayName");

	public static readonly GameString SiteCapacityReached = GameString.Create("SiteCapacityReached", "Site capacity reached at {Local:DisplayName}", "DisplayName");

	public static readonly GameString SelectRocketCurrentLocationTooltip = GameString.Create("SelectRocketCurrentLocationTooltip", "Select rocket's current location");

	public static readonly GameString DepositIsDepleted = GameString.Create("DepositIsDepleted", "Deposit is depleted. No further resources can be mined at this location.");

	public static readonly GameString EstimatedTotalResources = GameString.Create("EstimatedTotalResources", "Estimated Total Resources ");

	public static readonly GameString NoRocketCargoBay = GameString.Create("NoRocketCargoBay", "This Rocket has no cargo bay to deploy from");

	public static readonly GameString NoPartnerUmbilical = GameString.Create("NoPartnerUmbilical", "No partner umbilical connected");

	public static readonly GameString NoUmbilicalForTransfer = GameString.Create("NoUmbilicalForTransfer", "No umbilical to transfer from");

	public static readonly GameString TotalMass = GameString.Create("TotalMass", "Total Mass ");

	public static readonly GameString FuelMass = GameString.Create("FuelMass", "Fuel Mass ");

	public static readonly GameString FuselageMass = GameString.Create("FuselageMass", "Fuselage Mass ");

	public static readonly GameString EngineMass = GameString.Create("EngineMass", "Engine Mass ");

	public static readonly GameString PayloadMass = GameString.Create("PayloadMass", "Payload Mass ");

	public static readonly GameString RocketDryMassTotal = GameString.Create("RocketDryMassTotal", "Total Dry Mass ");

	public static readonly GameString DepositDensityToolTip = GameString.Create("DepositDensityToolTip", "Density effects the total resources available and mining speed");

	public static readonly GameString MineOperationSpeedToolTip = GameString.Create("MineOperationSpeedToolTip", "Mine Speed ");

	public static readonly GameString RichnessOperationAmountToolTip = GameString.Create("RichnessOperationAmountToolTip", "Richness is increasing Yield by {LOCAL:RichnessMultiplier} from {LOCAL:BaseLine} to {LOCAL:RichnessQuantity}", "RichnessMultiplier", "BaseLine", "RichnessQuantity");

	public static readonly GameString CollectionQuantityToolTip = GameString.Create("CollectionQuantityToolTip", "Base Yield ");

	public static readonly GameString DensityReductionToolTip = GameString.Create("DensityReductionToolTip", "Density Degradation ");

	public static readonly GameString RichnessReductionToolTip = GameString.Create("RichnessReductionToolTip", "Richness Degradation ");

	public static readonly GameString DepositSizeToolTip = GameString.Create("DepositSizeToolTip", "Size increases the Base Yield and decreases the degradation rate of Density and Richness");

	public static readonly GameString DepositRichnessToolTip = GameString.Create("DepositRichnessToolTip", "Richness provides a short-term boost to the Yield");

	public static readonly GameString NoDownlinkFound = GameString.Create("NoDownlinkFound", "No down-link found for up-link {LOCAL:DisplayName}", "DisplayName");

	public static readonly GameString NoAvionicsFound = GameString.Create("NoAvionicsFound", "No avionics found for down-link {LOCAL:DisplayName}", "DisplayName");

	public static readonly GameString NoRocketsConnected = GameString.Create("NoRocketsConnected", "No rockets connected");

	public static readonly GameString RocketLandAborted = GameString.Create("RocketLandAborted", "Rocket landing aborted. Auto-Land Controller could not calculate a safe landing trajectory.");

	public static readonly GameString RocketEnginePlacementRule = GameString.Create("RocketEnginePlacementRule", "{LOCAL:Device} must be placed inside an <color=yellow>Engine Fuselage</color>", "Device");

	public static readonly GameString RocketCurrentLocation = GameString.Create("RocketCurrentLocation", "Current Location");

	public static readonly GameString RocketTargetLocation = GameString.Create("RocketTargetLocation", "Target Location");

	public static readonly GameString RocketFuelTime = GameString.Create("RocketFuelTime", "Fuel Time");

	public static readonly GameString RocketAcceleration = GameString.Create("RocketAcceleration", "Acceleration");

	public static readonly GameString RocketGravity = GameString.Create("RocketGravity", "Gravity");

	public static readonly GameString RocketEngineAcceleration = GameString.Create("RocketEngineAcceleration", "Engines");

	public static readonly GameString RocketTargetVelocity = GameString.Create("RocketTargetVelocity", "Target Velocity");

	public static readonly GameString RocketAltitude = GameString.Create("RocketAltitude", "Altitude");

	public static readonly GameString RocketNextLocationETA = GameString.Create("RocketNextLocationETA", "Next Location ETA");

	public static readonly GameString RocketNextLocation = GameString.Create("RocketNextLocation", "Next Location");

	public static readonly GameString RocketApex = GameString.Create("RocketApex", "Apex");

	public static readonly GameString RocketImpactVelocity = GameString.Create("RocketImpactVelocity", "Impact Velocity");

	public static readonly GameString RocketMass = GameString.Create("RocketMass", "Mass");

	public static readonly GameString RocketDryMass = GameString.Create("RocketDryMass", "Dry Mass");

	public static readonly GameString RocketThrust = GameString.Create("RocketThrust", "Thrust");

	public static readonly GameString RocketWeight = GameString.Create("RocketWeight", "Weight");

	public static readonly GameString RocketThrustToWeight = GameString.Create("RocketThrustToWeight", "Thrust to Weight");

	public static readonly GameString RocketOxidizer = GameString.Create("RocketOxidizer", "Oxidizer");

	public static readonly GameString RocketVolatiles = GameString.Create("RocketVolatiles", "Volatiles");

	public static readonly GameString RocketBatteryPercentage = GameString.Create("RocketBatteryPercentage", "Battery");

	public static readonly GameString CentrifugeSpinningError = GameString.Create("CentrifugeSpinningError", "Error! {LOCAL:Thing} must be stopped before it can export", "Thing");

	public static readonly GameString CentrifugeFull = GameString.Create("CentrifugeFull", "Error! {LOCAL:Thing} is full. Empty to continue processing", "Thing");

	public static readonly GameString RocketMinerHasNoDrillHead = GameString.Create("RocketMinerHasNoDrillHead", "{LOCAL:Thing} has no mining drill head", "Thing");

	public static readonly GameString RocketMinerDrillHeadWornOut = GameString.Create("RocketMinerDrillHeadWornOut", "{LOCAL:Thing} mining drill head is worn out", "Thing");

	public static readonly GameString RocketScannerHasNoScanningHead = GameString.Create("RocketScannerHasNoScanningHead", "{LOCAL:Thing} has no scanning head", "Thing");

	public static readonly GameString RocketScannerHeadWornOut = GameString.Create("RocketScannerHeadWornOut", "{LOCAL:Thing} scanning head is worn out", "Thing");

	public static readonly GameString RocketScannerWrongHeadType = GameString.Create("RocketScannerWrongHeadType", "{LOCAL:Thing} scanning head is the wrong type for action you are trying to do", "Thing");

	public static readonly GameString DeviceOffOrUnpowered = GameString.Create("DeviceOffOrUnpowered", "{LOCAL:Thing} is off or un-powered", "Thing");

	public static readonly GameString DeviceNotConstructed = GameString.Create("DeviceNotConstructed", "{LOCAL:Thing} has not been fully constructed", "Thing");

	public static readonly GameString DeviceOutputNetworkInvalid = GameString.Create("DeviceOutputNetworkInvalid", "<color=yellow>Output</color> network is invalid");

	public static readonly GameString ExportSlotBlocked = GameString.Create("ExportSlotBlocked", "{LOCAL:Thing} Export slot is blocked", "Thing");

	public static readonly GameString DeviceNoPayload = GameString.Create("DeviceNoPayload", "{LOCAL:Thing} has no payload", "Thing");

	public static readonly GameString Charted = GameString.Create("Charted", "Charted");

	public static readonly GameString UnchartedLocation = GameString.Create("UnchartedLocation", "???");

	public static readonly GameString UnknownDepositValue = GameString.Create("UnknownDepositValue", "???");

	public static readonly GameString AutoShutOffTooltip = GameString.Create("AutoShutOffTooltip", "Auto-Shut Off");

	public static readonly GameString AutoLandDisabledTooltip = GameString.Create("AutoLandDisabledTooltip", "Auto-Land is Disabled");

	public static readonly GameString AutoLandEnabledTooltip = GameString.Create("AutoLandEnabledTooltip", "Auto-Land is Enabled");

	public static readonly GameString SurveyDifficultyTooltip = GameString.Create("SurveyDifficultyTooltip", "Total points required to fully survey this location");

	public static readonly GameString DiscoverDifficultyTooltip = GameString.Create("DiscoverDifficultyTooltip", "Total points required to discover a new location");

	public static readonly GameString ChartDifficultyTooltip = GameString.Create("ChartDifficultyTooltip", "Total points required to fully chart this location");

	public static readonly GameString SitesDiscoveredTooltip = GameString.Create("SitesDiscoveredTooltip", "Sites discovered at this location");

	public static readonly GameString NavPointsChartedTooltip = GameString.Create("NavPointsChartedTooltip", "Nav points charted at this location");

	public static readonly GameString SurveyProgressTooltip = GameString.Create("SurveyProgressTooltip", "Total survey points generated at this location");

	public static readonly GameString DiscoverProgressTooltip = GameString.Create("DiscoverProgressTooltip", "Total discover points generated at this location");

	public static readonly GameString ChartProgressTooltip = GameString.Create("ChartProgressTooltip", "Total chart points generated at this location");

	public static readonly GameString RocketLogArrived = GameString.Create("RocketLogArrived", "{LOCAL:DisplayName} has arrived at {LOCAL:Destination}", "DisplayName", "Destination");

	public static readonly GameString RocketLogOutOfFuel = GameString.Create("RocketLogOutOfFuel", "{LOCAL:DisplayName} has run out of fuel", "DisplayName");

	public static readonly GameString RocketCrashed = GameString.Create("RocketCrashed", "{LOCAL:DisplayName} crashed into {LOCAL:CrashedInto}", "DisplayName", "CrashedInto");

	public static readonly GameString RocketLogOutOfBattery = GameString.Create("RocketLogOutOfBattery", "{LOCAL:DisplayName} has depleted its batteries", "DisplayName");

	public static readonly GameString RocketLogCharted = GameString.Create("RocketLogCharted", "A new nav-point has been charted: {LOCAL:ChartedName}", "ChartedName");

	public static readonly GameString RocketLogDeviceFull = GameString.Create("RocketLogDeviceFull", "{LOCAL:Thing} is full", "Thing");

	public static readonly GameString RocketLogDeploy = GameString.Create("RocketLogDeploy", "{LOCAL:DisplayName} has successfully deployed {LOCAL:Thing} to x: {LOCAL:X} z:{LOCAL:Z}", "DisplayName", "Thing", "X", "Z");

	public static readonly GameString LiquidFuelInput = GameString.Create("LiquidFuelInput", "Liquid Fuel Input");

	public static readonly GameString HeatExchangerLiquidInput = GameString.Create("HeatExchangerLiquidInput", "Heat Exchanger Liquid Input");

	public static readonly GameString Throttle = GameString.Create("Throttle", "Throttle");

	public static readonly GameString ImportCountTooltip = GameString.Create("ImportCountTooltip", "Import Count");

	public static readonly GameString ExportCountTooltip = GameString.Create("ExportCountTooltip", "Export Count");

	public static readonly GameString TooltipInternalQuantity = GameString.Create("TooltipInternalQuantity", "Internal Quantity {LOCAL:Value}", "Value");

	public static readonly GameString TooltipOutputSetting = GameString.Create("TooltipOutputSetting", "Output Setting {LOCAL:Value}", "Value");

	public static readonly GameString ToolRequiredToOpen = GameString.Create("ToolRequiredToOpen", "<color=green>{Local:Tool}</color> required to open", "Tool");

	public static readonly GameString ToolRequiredToClose = GameString.Create("ToolRequiredToClose", "<color=green>{Local:Tool}</color> required to close", "Tool");

	public static readonly GameString RocketTowerPlacementRule = GameString.Create("RocketTowerPlacementRule", "{LOCAL:Thing} must be built inside a Rocket Tower", "Thing");

	public static readonly GameString FailedToLoadGameData = GameString.Create("FailedToLoadGameData", "Failed to load game data at {LOCAL:Path}", "Path");

	public static readonly GameString RocketFuselageHasInternals = GameString.Create("RocketFuselageHasInternals", "Cannot deconstruct fuselage as it contains internal components");

	public static readonly GameString On = GameString.Create("On", "On");

	public static readonly GameString Off = GameString.Create("Off", "Off");

	public static readonly GameString RocketScanActionInfo = GameString.Create("RocketScanActionInfo", "Scanner: {LOCAL:OnOff}, Scanner Strength: {LOCAL:Strength}, Scan Cycle Time: {LOCAL:CycleTime}", "OnOff", "Strength", "CycleTime");

	public static readonly GameString RocketSurfaceScanActionInfo = GameString.Create("RocketSurfaceScanActionInfo", "Sending scan data to connected map motherboards. Scanner: {LOCAL:OnOff}", "OnOff");

	public static readonly GameString RocketMineActionInfo = GameString.Create("RocketMineActionInfo", "Miner: {LOCAL:OnOff}, Mining Head: {LOCAL:HeadQuality}, Next Yield: {LOCAL:Yield} Ore", "OnOff", "HeadQuality", "Yield");

	public static readonly GameString RocketGasCollectActionInfo = GameString.Create("RocketGasCollectActionInfo", "Collector: {LOCAL:OnOff}, Next Yield: {LOCAL:Yield}", "OnOff", "Yield");

	public static readonly GameString RocketDeployActionInfo = GameString.Create("RocketDeployActionInfo", "Payload Bay: {LOCAL:OnOff}, State: {LOCAL:Open}, Progress: {LOCAL:Progress}", "OnOff", "Open", "Progress");

	public static readonly GameString RocketDeployFailNotInLowOrbit = GameString.Create("RocketDeployFailNotInLowOrbit", "{LOCAL:Payload} Can only be deployed in Low Orbit", "Payload");

	public static readonly GameString RocketDeployFailNoPayload = GameString.Create("RocketDeployFailNoPayload", "No payload to deploy");

	public static readonly GameString RocketDeployFailNoFreePosition = GameString.Create("RocketDeployFailNoFreePosition", "{LOCAL:Payload} All deploy positions in Low Orbit are occupied", "Payload");

	public static readonly GameString RocketLowOrbit = GameString.Create("RocketLowOrbit", "Low Orbit");

	public static readonly GameString RocketScanNoHeadInfo = GameString.Create("RocketScanNoHeadInfo", "Scanner is inoperable due to having no scanning head attached.");

	public static readonly GameString RocketMineNoHeadInfo = GameString.Create("RocketMineNoHeadInfo", "Miner is inoperable due to having no drill head attached.");

	public static readonly GameString RocketNoDataConnection = GameString.Create("RocketNoDataConnection", "No motherboard connection to avionics through datalink");

	public static readonly GameString RocketNoOperableAvionics = GameString.Create("RocketNoOperableAvionics", "No operable avionics detected");

	public static readonly GameString RocketActionMining = GameString.Create("RocketActionMining", "Mining");

	public static readonly GameString RocketActionSurveying = GameString.Create("RocketActionSurveying", "Surveying");

	public static readonly GameString RocketActionDiscovering = GameString.Create("RocketActionDiscovering", "Discovering");

	public static readonly GameString RocketActionCharting = GameString.Create("RocketActionCharting", "Charting");

	public static readonly GameString RocketActionDeploying = GameString.Create("RocketActionDeploying", "Deploying");

	public static readonly GameString RocketActionSurfaceScan = GameString.Create("RocketActionSurfaceScan", "Scanning Surface");

	public static readonly GameString RocketActionTransfer = GameString.Create("RocketActionTransfer", "Transfering Resources");

	public static readonly GameString RocketActionSurveyProgress = GameString.Create("RocketActionSurveyProgress", "Survey Progress");

	public static readonly GameString RocketActionDiscoverProgress = GameString.Create("RocketActionDiscoverProgress", "Discover Progress");

	public static readonly GameString RocketActionChartProgress = GameString.Create("RocketActionChartProgress", "Chart Progress");

	public static readonly GameString RocketActionMiningTotalOre = GameString.Create("RocketActionMiningTotalOre", "Estimated Ore at Location: {LOCAL:Total}", "Total");

	public static readonly GameString RocketActionMiningTotalOreMined = GameString.Create("RocketActionMiningTotalOreMined", "Total Ore Mined at Location: {LOCAL:Total}", "Total");

	public static readonly GameString VeryHighConfidence = GameString.Create("VeryHighConfidence", "Very High");

	public static readonly GameString HighConfidence = GameString.Create("HighConfidence", "High");

	public static readonly GameString ModerateConfidence = GameString.Create("ModerateConfidence", "Moderate");

	public static readonly GameString LowConfidence = GameString.Create("LowConfidence", "Low");

	public static readonly GameString NoConfidence = GameString.Create("NoConfidence", "Failure");

	public static readonly GameString AutoLandNoConfidence = GameString.Create("AutoLandNoConfidence", "<color=red>Landing Aborted!</color> Auto-Land has no confidence in achieving landing success");

	public static readonly GameString LandingBeginAltitudeToolTip = GameString.Create("LandingBeginAltitudeToolTip", "Landing Altitude {LOCAL:Altitude}", "Altitude");

	public static readonly GameString AutoLandThrustRequired = GameString.Create("AutoLandThrustRequired", "Confidence {LOCAL:Confidence} (at least {LOCAL:Thrust} of thrust required. Max available thrust estimated at {LOCAL:ExpectedThrust})", "Confidence", "Thrust", "ExpectedThrust");

	public static readonly GameString NoDataLinkActive = GameString.Create("NoDataLinkActive", "Not connected to an Uplink");

	public static readonly GameString ConnectedRocketAvionics = GameString.Create("ConnectedRocketAvionics", "Avionics: {LOCAL:Avionics}", "Avionics");

	public static readonly GameString ConnectedRocketUplinks = GameString.Create("ConnectedRocketUplinks", "Uplinks: {LOCAL:Uplinks}", "Uplinks");

	public static readonly GameString ConnectedRocketDownlink = GameString.Create("ConnectedRocketDownlink", "Downlink: {LOCAL:Downlink}", "Downlink");

	public static readonly GameString DownlinkConnectedToNothing = GameString.Create("DownlinkConnectedToNothing", "No connection");

	public static readonly GameString DownlinkConnectedTo = GameString.Create("DownlinkConnectedTo", "Connected to ");

	public static readonly GameString AvionicsDeviceParentRocket = GameString.Create("AvionicsDeviceParentRocket", "Parent {LOCAL:RocketName}", "RocketName");

	public static readonly GameString AvionicsDeviceRocketMass = GameString.Create("AvionicsDeviceRocketMass", "Mass {LOCAL:Mass}", "Mass");

	public static readonly GameString AvionicsDeviceRocketWeight = GameString.Create("AvionicsDeviceRocketWeight", "Weight {LOCAL:Weight}", "Weight");

	public static readonly GameString AvionicsDeviceRocketThrustToWeight = GameString.Create("AvionicsDeviceRocketThrustToWeight", "Thrust to Weight {LOCAL:ThrustToWeight}", "ThrustToWeight");

	public static readonly GameString StartGameFailurePromptTitle = GameString.Create("StartGameFailurePromptTitle", "Failure");

	public static readonly GameString StartGameFailurePromptEmptyName = GameString.Create("StartGameFailurePromptEmptyName", "Can not create a new save with empty name");

	public static readonly GameString StartGameFailurePromptNameExists = GameString.Create("StartGameFailurePromptNameExists", "A save already exists with this name");

	public static readonly GameString SaveInvalidTerrainDataModified = GameString.Create("SaveInvalidTerrainDataModified", "The Terrain data for this map has been modified. This save is no longer valid.");

	public static readonly GameString TutorialDisabledTitle = GameString.Create("TutorialDisabledTitle", "Tutorials Disabled");

	public static readonly GameString TutorialDisabled = GameString.Create("TutorialDisabled", "Tutorials have been temporarily disabled due to recent overhauls of core game systems. If you wish to play the tutorials please switch to the beta branch: previous - pre phase change");

	public static readonly GameString OverwriteConfirmation = GameString.Create("OverwriteConfirmation", "Overwrite");

	public static readonly GameString OkayConfirmation = GameString.Create("OkayConfirmation", "Okay");

	public static readonly GameString TutorialYouDied = GameString.Create("TutorialYouDied", "You Died");

	public static readonly GameString TutorialExitToMainMenu = GameString.Create("TutorialExitToMainMenu", "Exit to Main Menu");

	public static readonly GameString ResetTerrainButton = GameString.Create("ResetTerrainButton", "Reset Terrain");

	public static readonly GameString CancelLoadButton = GameString.Create("CancelLoadButton", "Cancel");

	public static readonly GameString PlayerStatsMoodDeltaState = GameString.Create("PlayerStatsMoodDeltaState", "Mood is {LOCAL:Delta}", "Delta");

	public static readonly GameString PlayerStatsHygieneDeltaState = GameString.Create("PlayerStatsHygieneDeltaState", "Hygiene is {LOCAL:Delta}", "Delta");

	public static readonly GameString PlayerStatsTooltipTitle = GameString.Create("PlayerStatsTooltipTitle", "Player Stats");

	public static readonly GameString PlayerStatsIncreasing = GameString.Create("PlayerStatsIncreasing", "increasing");

	public static readonly GameString PlayerStatsDecreasing = GameString.Create("PlayerStatsDecreasing", "decreasing");

	public static readonly GameString PlayerStatsStable = GameString.Create("PlayerStatsStable", "not increasing");

	public static readonly GameString PlayerStatsLowHygiene = GameString.Create("PlayerStatsLowHygiene", "Low Hygiene");

	public static readonly GameString PlayerStatsHygieneOk = GameString.Create("PlayerStatsHygieneOk", "Good Hygiene");

	public static readonly GameString PlayerStatsRoomStateOk = GameString.Create("PlayerStatsRoomStateOk", "In a Room");

	public static readonly GameString PlayerStateMoodDecreasing = GameString.Create("PlayerStateMoodDecreasing", "Mood is Decreasing");

	public static readonly GameString PlayerStateMoodIncreasing = GameString.Create("PlayerStateMoodIncreasing", "Mood is Increasing");

	public static readonly GameString PlayerStateHygieneDecreasing = GameString.Create("PlayerStateHygieneDecreasing", "Hygiene is Decreasing");

	public static readonly GameString PlayerStateHygieneIncreasing = GameString.Create("PlayerStateHygieneIncreasing", "Hygiene is Increasing");

	public static readonly GameString RoomState = GameString.Create("RoomState", "in a {LOCAL:RoomType}", "RoomType");

	public static readonly GameString PlayerStatsSuitOrHelmetOn = GameString.Create("PlayerStatsSuitOrHelmetOn", "Wearing Suit or Helmet");

	public static readonly GameString PlayerStatsNoSuitOrHelmet = GameString.Create("PlayerStatsNoSuitOrHelmet", "No Suit or Helmet");

	public static readonly GameString PlayerStatsFoodQuality = GameString.Create("PlayerStatsFoodQuality", "Food quality {LOCAL:Quality}", "Quality");

	public static readonly GameString CryoAtmosphereIsUnsafe = GameString.Create("CryoAtmosphereIsUnsafe", "Breathing Atmosphere is unsafe");

	public static readonly GameString CryoNoAtmosphericInput = GameString.Create("CryoNoAtmosphericInput", "No Breathing Gas Network");

	public static readonly GameString CryoNoLiquidInput = GameString.Create("CryoNoLiquidInput", "No Cryogenic Liquid Network");

	public static readonly GameString EggIsTooCold = GameString.Create("EggIsTooCold", "Egg is too cold");

	public static readonly GameString EggIsTooHot = GameString.Create("EggIsTooHot", "Egg is too hot");

	public static readonly GameString FertilizedEggProcess = GameString.Create("FertilizedEggProcess", "The {LOCAL:Thing} is {LOCAL:Percent} to hatching", "Thing", "Percent");

	public static readonly GameString DrinkFromFountain = GameString.Create("DrinkFromFountain", "Drink");

	public static readonly GameString DrinkNotThirsty = GameString.Create("DrinkNotThirsty", "Not thirsty");

	public static readonly GameString DeviceNotEnoughWater = GameString.Create("DeviceNotEnoughWater", "Not enough water");

	public static readonly GameString DeviceWaterTooHot = GameString.Create("DeviceWaterTooHot", "Water is too hot");

	public static readonly GameString DeviceWaterTooCold = GameString.Create("DeviceWaterTooCold", "Water is too cold");

	public static readonly GameString DeviceWaterPolluted = GameString.Create("DeviceWaterPolluted", "Water is polluted");

	public static readonly GameString DeviceOutputFull = GameString.Create("DeviceOutputFull", "<color=yellow>Output</color> network is full");

	public static readonly GameString DeviceMinimumPressure = GameString.Create("DeviceMinimumPressure", "World atmosphere must be at least {LOCAL:PRESSURE}", "PRESSURE");

	public static readonly GameString DeviceNetworkInvalidSpecified = GameString.Create("DeviceNetworkInvalidSpecified", "<color=yellow>{LOCAL:NETWORK}</color> network is invalid", "NETWORK");

	public static readonly GameString SanitizerActionMessage = GameString.Create("SanitizerActionMessage", "Clean yourself");

	public static readonly GameString StationpediaNoFoodQuality = GameString.Create("StationpediaNoFoodQuality", "None");

	public static readonly GameString StationpediaLowFoodQuality = GameString.Create("StationpediaLowFoodQuality", "<color=red>Low</color> (-25% hydration capacity)");

	public static readonly GameString StationpediaOkFoodQuality = GameString.Create("StationpediaOkFoodQuality", "<color=orange>Ok</color>");

	public static readonly GameString StationpediaGoodFoodQuality = GameString.Create("StationpediaGoodFoodQuality", "<color=yellow>Good</color> (+25% hydration capacity)");

	public static readonly GameString StationpediaSuperiorFoodQuality = GameString.Create("StationpediaSuperiorFoodQuality", "<color=green>Best</color> (+75% hydration capacity)");

	public static readonly GameString TooltipLowFoodQuality = GameString.Create("TooltipLowFoodQuality", "Low <color=red>-25% hydration</color>");

	public static readonly GameString TooltipOkFoodQuality = GameString.Create("TooltipOkFoodQuality", "Ok");

	public static readonly GameString TooltipGoodFoodQuality = GameString.Create("TooltipGoodFoodQuality", "Good <color=green>+25% hydration </color>");

	public static readonly GameString TooltipSuperiorFoodQuality = GameString.Create("TooltipSuperiorFoodQuality", "Best <color=green>+75% hydration</color>");

	public static readonly GameString FoodQualityLow = GameString.Create("FoodQualityLow", "Low");

	public static readonly GameString FoodQualityOk = GameString.Create("FoodQualityOk", "Ok");

	public static readonly GameString FoodQualityGood = GameString.Create("FoodQualityGood", "Good");

	public static readonly GameString FoodQualityBest = GameString.Create("FoodQualityBest", "Best");

	public static readonly GameString TooltipFoodQuality = GameString.Create("TooltipFoodQuality", "Food Quality {LOCAL:Quality}", "Quality");

	public static readonly GameString WaterPurifierCharcoalErrorMsg = GameString.Create("WaterPurifierCharcoalErrorMsg", "Requires <color=green>{LOCAL:Prefab}</color> in Import Slot to Operate", "Prefab");

	public static readonly GameString UpgradeConstructionAction = GameString.Create("UpgradeConstructionAction", "Upgrade to <color=green>{LOCAL:Prefab}</color>", "Prefab");

	public static readonly GameString EntityCantEnterNonEntitySlot = GameString.Create("EntityCantEnterNonEntitySlot", "Entity can't enter non-entity slot");

	public static readonly GameString CantEnterOwnHierarchy = GameString.Create("CantEnterOwnHierarchy", "Can't enter own hierarchy");

	public static readonly GameString CantPickUp = GameString.Create("CantPickUp", "This this is unable to be picked up");

	public static readonly GameString CantEnterSelf = GameString.Create("CantEnterSelf", "Can't enter self");

	public static readonly GameString IsAttachedToBench = GameString.Create("IsAttachedToBench", "Can't move - detach from bench first");

	public static readonly GameString CantNestBoxes = GameString.Create("CantNestBoxes", "Can't nest cardboard boxes");

	public static readonly GameString MagazineTypeDoesNotMatch = GameString.Create("MagazineTypeDoesNotMatch", "Magazine type does not match");

	public static readonly GameString SlotDoesNotAllowDragging = GameString.Create("SlotDoesNotAllowDragging", "Slot does not allow dragging");

	public static readonly GameString SuitDoesNotSupportThisItem = GameString.Create("SuitDoesNotSupportThisItem", "Suit does not support this item");

	public static readonly GameString DetonateExplosives = GameString.Create("DetonateExplosives", "Detonate");

	public static readonly GameString DropExplosive = GameString.Create("DropExplosive", "Drop");

	public static readonly GameString LinkAndDropExplosive = GameString.Create("LinkAndDropExplosive", "Link and Drop");

	public static readonly GameString UnableToLinkExplosive = GameString.Create("UnableToLinkExplosive", "This device is already linked to another Remote Detonator");

	public static readonly GameString BrutalStartConditionDescription = GameString.Create("BrutalStartConditionDescription", "The bare minimum to survive");

	public static readonly GameString StartScreenHeaderInWorldSpawns = GameString.Create("StartScreenHeaderInWorldSpawns", "In-World Spawns");

	public static readonly GameString StartScreenHeaderPlayerLandingCapsule = GameString.Create("StartScreenHeaderPlayerLandingCapsule", "Player Landing Capsule");

	public static readonly GameString ObjectiveIsCompletedCondition = GameString.Create("ObjectiveIsCompletedCondition", "{LOCAL:Objective} must first be completed", "Objective");

	public static readonly GameString InteractableMustBeOn = GameString.Create("InteractableMustBeOn", "Switch on");

	public static readonly GameString InteractableMustBeOff = GameString.Create("InteractableMustBeOff", "Switch off");

	public static readonly GameString InteractableMustBeOpen = GameString.Create("InteractableMustBeOpen", "Must be open");

	public static readonly GameString InteractableMustBeClosed = GameString.Create("InteractableMustBeClosed", "Must be closed");

	public static readonly GameString InteractableMustBePowered = GameString.Create("InteractableMustBePowered", "Must be powered");

	public static readonly GameString InteractableMustBeUnpowered = GameString.Create("InteractableMustBeUnpowered", "Must be unpowered");

	public static readonly GameString CustomNameCondition = GameString.Create("CustomNameCondition", "Name must be <color=green>{LOCAL:Name}</color>", "Name");

	public static readonly GameString RoomMinSizeCondition = GameString.Create("RoomMinSizeCondition", "Size greater than <color=green>{LOCAL:Size}</color> grids", "Size");

	public static readonly GameString RoomMaxSizeCondition = GameString.Create("RoomMaxSizeCondition", "Size less than <color=green>{LOCAL:Size}</color> grids", "Size");

	public static readonly GameString Infinity = GameString.Create("Infinity", "Infinity");

	public static readonly GameString SurvivalPropertyCondition = GameString.Create("SurvivalPropertyCondition", "{LOCAL:Property} {LOCAL:ComparisonOperator} {LOCAL:PercentValue}", "Property", "ComparisonOperator", "PercentValue");

	public static readonly GameString AtmosphereModeCondition = GameString.Create("AtmosphereModeCondition", "Target atmosphere is {LOCAL:Mode}", "Mode");

	public static readonly GameString NoneSelected = GameString.Create("NoneSelected", "None");

	public static readonly GameString CarbonDioxideRatio = GameString.Create("CarbonDioxideRatio", "CarbonDioxide Ratio <color=green>{LOCAL:Ratio}</color>", "Ratio");

	public static readonly GameString StressedToFailure = GameString.Create("StressedToFailure", "<color=red>Stressed To Failure </color>");

	public static readonly GameString MachineTemperature = GameString.Create("MachineTemperature", "Machine Temperature");

	public static readonly GameString DispersalTowerNotEnoughPressure = GameString.Create("DispersalTowerNotEnoughPressure", "The InternalPressure must be above {LOCAL:Target}", "Target");

	public static readonly GameString DifficultyStatisticCreativeMode = GameString.Create("DifficultyStatisticCreativeMode", "Creative mode");

	public static readonly GameString DifficultyStatisticAchievements = GameString.Create("DifficultyStatisticAchievements", "Achievements");

	public static readonly GameString DifficultyStatisticWeatherDamage = GameString.Create("DifficultyStatisticWeatherDamage", "Weather damage");

	public static readonly GameString DifficultyStatisticCalmWeather = GameString.Create("DifficultyStatisticCalmWeather", "Calm weather");

	public static readonly GameString DifficultyStatisticMetabolism = GameString.Create("DifficultyStatisticMetabolism", "Metabolism");

	public static readonly GameString DifficultyStatisticHungerRate = GameString.Create("DifficultyStatisticHungerRate", "Nutrition");

	public static readonly GameString DifficultyStatisticSanitation = GameString.Create("DifficultyStatisticSanitation", "Sanitation");

	public static readonly GameString DifficultyStatisticHydrationRate = GameString.Create("DifficultyStatisticHydrationRate", "Hydration");

	public static readonly GameString DifficultyStatisticBreathing = GameString.Create("DifficultyStatisticBreathing", "Breathing");

	public static readonly GameString DifficultyStatisticRobotBattery = GameString.Create("DifficultyStatisticRobotBattery", "Robot battery");

	public static readonly GameString DifficultyStatisticMoodReduction = GameString.Create("DifficultyStatisticMoodReduction", "Mood reduction");

	public static readonly GameString DifficultyStatisticHygieneReduction = GameString.Create("DifficultyStatisticHygieneReduction", "Hygiene reduction");

	public static readonly GameString DifficultyStatisticFoodDecay = GameString.Create("DifficultyStatisticFoodDecay", "Food decay");

	public static readonly GameString DifficultyStatisticJetpackConsumption = GameString.Create("DifficultyStatisticJetpackConsumption", "Jetpack consumption");

	public static readonly GameString DifficultyStatisticMining = GameString.Create("DifficultyStatisticMining", "Mining");

	public static readonly GameString DifficultyStatisticLungDamage = GameString.Create("DifficultyStatisticLungDamage", "Lung damage");

	public static readonly GameString DifficultyStatisticOfflineMetabolism = GameString.Create("DifficultyStatisticOfflineMetabolism", "Offline metabolism");

	public static readonly GameString MolesPerHour = GameString.Create("MolesPerHour", "{LOCAL:Value} Moles per hour", "Value");

	public static readonly GameString InhaledGassesStationpedia = GameString.Create("InhaledGassesStationpedia", "Inhaled Gasses");

	public static readonly GameString ExhaledGassesStationpedia = GameString.Create("ExhaledGassesStationpedia", "Exhaled Gasses");

	public static readonly GameString ToxicGassesStatiopedia = GameString.Create("ToxicGassesStatiopedia", "Toxic Gasses");

	public static readonly GameString JetpackStabilizerState = GameString.Create("JetpackStabilizerState", "Stabilizer {LOCAL:State}", "State");

	public static readonly GameString MiningDrillModeDefault = GameString.Create("MiningDrillModeDefault", "Default");

	public static readonly GameString MiningDrillModeFlatten = GameString.Create("MiningDrillModeFlatten", "Flatten");

	public static readonly GameString FlashLightModeLowPower = GameString.Create("FlashLightModeLowPower", "Low Power");

	public static readonly GameString FlashLightModeHighPower = GameString.Create("FlashLightModeHighPower", "High Power");

	public static readonly GameString InputPanelSelectRecipe = GameString.Create("InputPanelSelectRecipe", "Select Recipe");

	public static readonly GameString RadiatorHeatingEfficiency = GameString.Create("RadiatorHeatingEfficiency", "Heating Efficiency {LOCAL:Value}", "Value");

	public static readonly GameString ThingHealth = GameString.Create("ThingHealth", "Health {LOCAL:Value}", "Value");

	public static readonly GameString And = GameString.Create("And", " and ");

	public static readonly GameString OrSubstitute = GameString.Create("OrSubstitute", " or {LOCAL:Value}", "Value");

	public static readonly GameString AdvancedComposterCurrentlyProcessing = GameString.Create("AdvancedComposterCurrentlyProcessing", "Currently processing...");

	public static readonly GameString AdvancedComposterItemsLeftToGrind = GameString.Create("AdvancedComposterItemsLeftToGrind", "Items left to grind: {LOCAL:Value}", "Value");

	public static readonly GameString AdvancedComposterItemsLeftToProcess = GameString.Create("AdvancedComposterItemsLeftToProcess", "Items left to process: {LOCAL:Value}", "Value");

	public static readonly GameString FermenterItemsLeftToFerment = GameString.Create("FermenterItemsLeftToFerment", "Fermenting <color=yellow>{LOCAL:Amount}</color> × <color=green>{LOCAL:Value} {LOCAL:Percentage}%</color>", "Value", "Percentage", "Amount");

	public static readonly GameString FermenterWillProduce = GameString.Create("FermenterWillProduce", "Will produce <color=purple>{LOCAL:Value}</color>.", "Value");

	public static readonly GameString FermenterInputIsEmpty = GameString.Create("FermenterInputIsEmpty", "No Fermentable item in Input.");

	public static readonly GameString CableAnalyserActual = GameString.Create("CableAnalyserActual", "Actual {LOCAL:Value}", "Value");

	public static readonly GameString CableAnalyserRequired = GameString.Create("CableAnalyserRequired", "Required {LOCAL:Value}", "Value");

	public static readonly GameString CableAnalyserPotential = GameString.Create("CableAnalyserPotential", "Potential {LOCAL:Value}", "Value");

	public static readonly GameString CableAnalyserNoCableNetworkFound = GameString.Create("CableAnalyserNoCableNetworkFound", "No cable network found");

	public static readonly GameString EntityStarving = GameString.Create("EntityStarving", "Starving");

	public static readonly GameString EntityDehydrated = GameString.Create("EntityDehydrated", "Dehydrated");

	public static readonly GameString EntityWheezing = GameString.Create("EntityWheezing", "Wheezing");

	public static readonly GameString EntityHungry = GameString.Create("EntityHungry", "Hungry");

	public static readonly GameString EntityHurt = GameString.Create("EntityHurt", "Hurt");

	public static readonly GameString EntityRespawnStress = GameString.Create("EntityRespawnStress", "Fatigued from respawn");

	public static readonly GameString ConsumptionSpeed = GameString.Create("ConsumptionSpeed", "Consumption Speed");

	public static readonly GameString ToolUseSpeed = GameString.Create("ToolUseSpeed", "Tool Speed");

	public static readonly GameString TradePenalty = GameString.Create("TradePenalty", "Trade Penalty");

	public static readonly GameString AirControlStatusOffline = GameString.Create("AirControlStatusOffline", "OFFLINE");

	public static readonly GameString AirControlStatusPressure = GameString.Create("AirControlStatusPressure", "PRESSURE");

	public static readonly GameString AirControlStatusDraught = GameString.Create("AirControlStatusDraught", "DRAUGHT");

	public static readonly GameString AirControlStatusError = GameString.Create("AirControlStatusError", "ERROR");

	public static readonly GameString AirControlModeButton = GameString.Create("AirControlModeButton", "Mode\n<b>{LOCAL:Value}</b>", "Value");

	public static readonly GameString AirControlVent = GameString.Create("AirControlVent", "VENT");

	public static readonly GameString AirControlSensor = GameString.Create("AirControlSensor", "SENSOR");

	public static readonly GameString AirControlModeOffline = GameString.Create("AirControlModeOffline", "Offline");

	public static readonly GameString AirControlModeDraught = GameString.Create("AirControlModeDraught", "Draught");

	public static readonly GameString AirControlModePressure = GameString.Create("AirControlModePressure", "Pressure");

	public static readonly GameString CircuitboardMasterLabel = GameString.Create("CircuitboardMasterLabel", "MASTER");

	public static readonly GameString CircuitboardSlaveLabel = GameString.Create("CircuitboardSlaveLabel", "SLAVE");

	public static readonly GameString CircuitboardMakeSlaveLabel = GameString.Create("CircuitboardMakeSlaveLabel", "MAKE SLAVE");

	public static readonly GameString InputPanelFilterDevicesTitle = GameString.Create("InputPanelFilterDevicesTitle", "Filter Devices");

	public static readonly GameString SolarControlPanelsCount = GameString.Create("SolarControlPanelsCount", "{LOCAL:Value} PANELS", "Value");

	public static readonly GameString GasDisplayModeButton = GameString.Create("GasDisplayModeButton", "Mode: <b>{LOCAL:Value}</b>", "Value");

	public static readonly GameString GasDisplayModeTitlePressure = GameString.Create("GasDisplayModeTitlePressure", "PRESSURE");

	public static readonly GameString GasDisplayModeTitleTemperature = GameString.Create("GasDisplayModeTitleTemperature", "TEMPERATURE");

	public static readonly GameString GasDisplayModePressure = GameString.Create("GasDisplayModePressure", "Pressure");

	public static readonly GameString GasDisplayModeTemperature = GameString.Create("GasDisplayModeTemperature", "Temperature");

	public static readonly GameString NaN = GameString.Create("NaN", "NaN");

	public static readonly GameString AirlockErrorState = GameString.Create("AirlockErrorState", "ERROR");

	public static readonly GameString AirlockInConfigString = GameString.Create("AirlockInConfigString", "IN CONFIG");

	public static readonly GameString AirlockCancelButton = GameString.Create("AirlockCancelButton", "CANCEL");

	public static readonly GameString AirlockPressurize = GameString.Create("AirlockPressurize", "PRESSURIZE");

	public static readonly GameString AirlockDepressurize = GameString.Create("AirlockDepressurize", "DEPRESSURIZE");

	public static readonly GameString AirlockCycle = GameString.Create("AirlockCycle", "CYCLE");

	public static readonly GameString AirlockToExterior = GameString.Create("AirlockToExterior", "TO EXTERIOR");

	public static readonly GameString AirlockToInterior = GameString.Create("AirlockToInterior", "TO INTERIOR");

	public static readonly GameString ModeControlActive = GameString.Create("ModeControlActive", "ACTIVE");

	public static readonly GameString ModeControlInactive = GameString.Create("ModeControlInactive", "INACTIVE");

	public static readonly GameString ModeControlMode = GameString.Create("ModeControlMode", "MODE");

	public static readonly GameString ModeControlToggle = GameString.Create("ModeControlToggle", "TOGGLE");

	public static readonly GameString PowerControlPower = GameString.Create("PowerControlPower", "POWER");

	public static readonly GameString PowerControlPowerOff = GameString.Create("PowerControlPowerOff", "OFF");

	public static readonly GameString PowerControlPowerOn = GameString.Create("PowerControlPowerOn", "ON");

	public static readonly GameString SolarPanelEfficiency = GameString.Create("SolarPanelEfficiency", "Efficiency");

	public static readonly GameString SPDASlotType = GameString.Create("SPDASlotType", "Type: ");

	public static readonly GameString SPDASlotIndex = GameString.Create("SPDASlotIndex", "Index: ");

	public static readonly GameString SPDAManufacturerRequirements = GameString.Create("SPDAManufacturerRequirements", "Requirements:");

	public static readonly GameString RecipeEnergy = GameString.Create("RecipeEnergy", "Energy");

	public static readonly GameString RecipeReagentFrom = GameString.Create("RecipeReagentFrom", " from ");

	public static readonly GameString RecipeTemperatureRangeSeporator = GameString.Create("RecipeTemperatureRangeSeporator", " to ");

	public static readonly GameString ConstructableInRocketsTrue = GameString.Create("ConstructableInRocketsTrue", "True");

	public static readonly GameString YesPaintable = GameString.Create("YesPaintable", "Yes");

	public static readonly GameString NoPaintable = GameString.Create("NoPaintable", "No");

	public static readonly GameString SiloItemCount = GameString.Create("SiloItemCount", "Silo Item Count: {LOCAL:TotalItemsCurrentlyStored}/{LOCAL:MaxItems}", "TotalItemsCurrentlyStored", "MaxItems");

	public static readonly GameString DaylightSensorGridSunlight = GameString.Create("DaylightSensorGridSunlight", "Grid Sunlight {LOCAL:Value}", "Value");

	public static readonly GameString DaylightSensorSolarAngle = GameString.Create("DaylightSensorSolarAngle", "Solar Angle {LOCAL:Value}", "Value");

	public static readonly GameString DaylightSensorSolarIrradiance = GameString.Create("DaylightSensorSolarIrradiance", "Solar Irradiance {LOCAL:Value}", "Value");

	public static readonly GameString DaylightSensorHorizontal = GameString.Create("DaylightSensorHorizontal", "Horizontal {LOCAL:Value}", "Value");

	public static readonly GameString DaylightSensorVertical = GameString.Create("DaylightSensorVertical", "Vertical {LOCAL:Value}", "Value");

	public static readonly GameString DaylightSensorMode = GameString.Create("DaylightSensorMode", "Mode {LOCAL:Value}", "Value");

	public static readonly GameString DaylightSensorModeError = GameString.Create("DaylightSensorModeError", "Mode {LOCAL:Value} ERROR", "Value");

	public static readonly GameString CommsMotherboardCurrentlyTrading = GameString.Create("CommsMotherboardCurrentlyTrading", "Currently trading");

	public static readonly GameString CommsMotherboardTimeTillResolveUnknown = GameString.Create("CommsMotherboardTimeTillResolveUnknown", "Time till resolve unknown");

	public static readonly GameString CommsMotherboardSecondsTillResolve = GameString.Create("CommsMotherboardSecondsTillResolve", "{LOCAL:Value} s till resolve", "Value");

	public static readonly GameString CommsMotherboardDegreesFromContact = GameString.Create("CommsMotherboardDegreesFromContact", "{LOCAL:Value}° from contact", "Value");

	public static readonly GameString CommsMotherboardPercentInterogated = GameString.Create("CommsMotherboardPercentInterogated", "{LOCAL:Value} % interrogated", "Value");

	public static readonly GameString CommsMotherboardContacted = GameString.Create("CommsMotherboardContacted", "Contacted");

	public static readonly GameString CommsTerminalInterrogationInProgressAnother = GameString.Create("CommsTerminalInterrogationInProgressAnother", "Interrogation in progress by another dish");

	public static readonly GameString CommsTerminalInterrogationInProgress = GameString.Create("CommsTerminalInterrogationInProgress", "Interrogation in progress");

	public static readonly GameString CommTerminalWattageOnContact = GameString.Create("CommTerminalWattageOnContact", "{LOCAL:Value}W/{LOCAL:Setting}W are reaching the contact", "Value", "Setting");

	public static readonly GameString CommTerminalEstimatedSecondsUntilInterrogated = GameString.Create("CommTerminalEstimatedSecondsUntilInterrogated", "estimated {LOCAL:Value}s until interrogated", "Value");

	public static readonly GameString CommsTerminalDishInUse = GameString.Create("CommsTerminalDishInUse", "Dish in use: {LOCAL:DishName}", "DishName");

	public static readonly GameString CommsTerminalLandTraderButton = GameString.Create("CommsTerminalLandTraderButton", "Land");

	public static readonly GameString CommsTerminalContactingComplete = GameString.Create("CommsTerminalContactingComplete", "Contacting complete, continue to call down trader");

	public static readonly GameString CommsTerminalContactRequires = GameString.Create("CommsTerminalContactRequires", "Contact requires {LOCAL:Value}W for {LOCAL:Time}s", "Value", "Time");

	public static readonly GameString CommsTerminalDishBusy = GameString.Create("CommsTerminalDishBusy", "Highest wattage dish busy. Please wait, or use a separate Comms Terminal");

	public static readonly GameString CommsTerminalInterrogateButton = GameString.Create("CommsTerminalInterrogateButton", "Interrogate");

	public static readonly GameString CommsTerminalNotEnoughEnergy = GameString.Create("CommsTerminalNotEnoughEnergy", "Not enough energy pointed at contact to interrogate");

	public static readonly GameString TradeActionCharge = GameString.Create("TradeActionCharge", "Charge {LOCAL:Value}", "Value");

	public static readonly GameString TradeActionData = GameString.Create("TradeActionData", "Includes {LOCAL:Value} of data", "Value");

	public static readonly GameString LogicWrite = GameString.Create("LogicWrite", "Write");

	public static readonly GameString LogicRead = GameString.Create("LogicRead", "Read");

	public static readonly GameString LogicReadWrite = GameString.Create("LogicReadWrite", "Read Write");

	public static readonly GameString MemoryAccessNone = GameString.Create("MemoryAccessNone", "None");

	public static readonly GameString SpaceMapUknownDeltaV = GameString.Create("SpaceMapUknownDeltaV", "???");

	public static readonly GameString RocketLocationCurrentAction = GameString.Create("RocketLocationCurrentAction", "Current Action: {LOCAL:Mode}", "Mode");

	public static readonly GameString TimeLengthDays = GameString.Create("TimeLengthDays", " days, ");

	public static readonly GameString TimeLengthHours = GameString.Create("TimeLengthHours", " hours, ");

	public static readonly GameString TimeLengthMinutes = GameString.Create("TimeLengthMinutes", " minutes, ");

	public static readonly GameString TimeLengthSeconds = GameString.Create("TimeLengthSeconds", " seconds");

	public static readonly GameString TimeLengthInfiniteSeconds = GameString.Create("TimeLengthInfiniteSeconds", "infinite seconds");

	public static readonly GameString TimeLengthZeroSeconds = GameString.Create("TimeLengthZeroSeconds", "0 seconds");

	public static readonly GameString TimeLengthLessThan1Millisecond = GameString.Create("TimeLengthLessThan1Millisecond", "< 1 millisecond");

	public static readonly GameString TimeLengthMillisecond = GameString.Create("TimeLengthMillisecond", "millisecond");

	public static readonly GameString TimeLengthSecond = GameString.Create("TimeLengthSecond", "second");

	public static readonly GameString TimeLengthMinute = GameString.Create("TimeLengthMinute", "minute");

	public static readonly GameString TimeLengthHour = GameString.Create("TimeLengthHour", "hour");

	public static readonly GameString TimeLengthDay = GameString.Create("TimeLengthDay", "day");

	public static readonly GameString TimeLengthWeek = GameString.Create("TimeLengthWeek", "week");

	public static readonly GameString TimeLengthMonth = GameString.Create("TimeLengthMonth", "month");

	public static readonly GameString TimeLengthYear = GameString.Create("TimeLengthYear", "year");

	public static readonly GameString TimeLengthCentury = GameString.Create("TimeLengthCentury", "century");

	public static readonly GameString TimeLengthCenturies = GameString.Create("TimeLengthCenturies", "centuries");

	public static readonly GameString TimeLengthMillennium = GameString.Create("TimeLengthMillennium", "millennium");

	public static readonly GameString TimeLengthMillennia = GameString.Create("TimeLengthMillennia", "millennia");

	public static readonly GameString TimeLengthEon = GameString.Create("TimeLengthEon", "eon");

	public static readonly GameString TimeLengthAeons = GameString.Create("TimeLengthAeons", "aeons");

	public static readonly GameString FabricatorFindRecipe = GameString.Create("FabricatorFindRecipe", "Find Recipe");

	public static readonly GameString FabricatorUknownRecipe = GameString.Create("FabricatorUknownRecipe", "Unknown Recipe");

	public static readonly GameString EntityDeceased = GameString.Create("EntityDeceased", "Deceased");

	public static readonly GameString EntityUnconsious = GameString.Create("EntityUnconsious", "Unconscious");

	public static readonly GameString PressToExit = GameString.Create("PressToExit", "Press {KEY:ExitKey} to exit", "ExitKey");

	public static readonly GameString LeavingPlayableArea = GameString.Create("LeavingPlayableArea", "Leaving playable area");

	public static readonly GameString RegionName = GameString.Create("RegionName", "Region: {LOCAL:Name}", "Name");

	public static readonly GameString OldSaveDetected = GameString.Create("OldSaveDetected", "1 Old Save Detected");

	public static readonly GameString OldSavesDetected = GameString.Create("OldSavesDetected", "{LOCAL:Count} Old Saves Detected", "Count");

	public static readonly GameString DeleteOldSave = GameString.Create("DeleteOldSave", "Are you sure you want to delete 1 old save?");

	public static readonly GameString DeleteOldSaves = GameString.Create("DeleteOldSaves", "Are you sure you want to delete {LOCAL:Count} old saves?", "Count");

	public static readonly GameString SuitOperatingRangeText = GameString.Create("SuitOperatingRangeText", "{LOCAL:Minimum} to {LOCAL:Maximum}", "Minimum", "Maximum");

	public static readonly GameString RespawnHere = GameString.Create("RespawnHere", "Spawn Point Set");

	public static readonly GameString DeviceIsNotOperable = GameString.Create("DeviceIsNotOperable", "Device is not operable");

	public static readonly GameString PumpIntoPad = GameString.Create("PumpIntoPad", "Pump into landingpad");

	public static readonly GameString PumpIntoTank = GameString.Create("PumpIntoTank", "Pump into tank");

	public static readonly GameString Inward = GameString.Create("Inward", "Inward");

	public static readonly GameString Outward = GameString.Create("Outward", "Outward");

	public static readonly GameString Warning = GameString.Create("Warning", "Warning");

	public static readonly GameString Critical = GameString.Create("Critical", "Critical");

	public static readonly GameString PartialPressureO2 = GameString.Create("PartialPressureO2", "Oxygen Partial Pressure");

	public static readonly GameString LungsEfficiency = GameString.Create("LungsEfficiency", "Breathing Efficiency");

	public static readonly GameString ToxinDamage = GameString.Create("ToxinDamage", "Toxin Damage");

	public static readonly GameString BurnDamage = GameString.Create("BurnDamage", "Burn Damage");

	public static readonly GameString VacuumDamage = GameString.Create("VacuumDamage", "Vacuum Damage");

	public static readonly GameString DamageFromLackOfFood = GameString.Create("DamageFromLackOfFood", "Loosing Health due to lack of Food");

	public static readonly GameString DamageFromLackOfWater = GameString.Create("DamageFromLackOfWater", "Loosing Health due to lack of Water");

	public static readonly GameString NoNeedToEat = GameString.Create("NoNeedToEat", "<color=green>Hunger</color> is disabled");

	public static readonly GameString NoNeedToDrink = GameString.Create("NoNeedToDrink", "<color=green>Thirst</color> is disabled");

	public static readonly GameString RobotHasNoBattery = GameString.Create("RobotHasNoBattery", "Battery missing");

	public static readonly GameString RobotBatteryNoCharge = GameString.Create("RobotBatteryNoCharge", "Battery has no charge");

	public static readonly GameString PartialPressureN2O = GameString.Create("PartialPressureN2O", "Partial Pressure Nitrous Oxide");

	public static readonly GameString Refreshed = GameString.Create("Refreshed", "Refreshed");

	public static readonly GameString MovementSpeed = GameString.Create("MovementSpeed", "Movement Speed");

	public static readonly GameString Hygiene = GameString.Create("Hygiene", "Hygiene");

	public static readonly GameString ConstructionBlockedByDynamic = GameString.Create("ConstructionBlockedByDynamic", "Construction blocked by {LOCAL:Thing}", "Thing");

	public static readonly GameString RocketNoAvailableSeat = GameString.Create("RocketNoAvailableSeat", "No seat available in crew module");

	public static readonly GameString RocketCannotExitSeatInFlight = GameString.Create("RocketCannotExitSeatInFlight", "Cannot exit the seat unless the rocket is landed on a launch mount");

	public static readonly GameString RocketCrewModuleMannedTravel = GameString.Create("RocketCrewModuleMannedTravel", "Cannot travel here while the crew module is manned");

	public static readonly GameString NoAvailableBattery = GameString.Create("NoAvailableBattery", "No battery available. Battery must be turned on and have charge");

	public static readonly GameString PowerTransferActionInfo = GameString.Create("PowerTransferActionInfo", "Transferring Power. Source {LOCAL:SourcePercent}. Target {LOCAL:TargetPercent}", "SourcePercent", "TargetPercent");

	public static readonly GameString GasTransferActionInfo = GameString.Create("GasTransferActionInfo", "Transferring Gasses and Liquids. ");

	public static readonly GameString ActionSource = GameString.Create("ActionSource", "Source ");

	public static readonly GameString ActionTarget = GameString.Create("ActionTarget", "Target ");

	public static readonly GameString PylonLink = GameString.Create("PylonLink", "Link");

	public static readonly GameString PylonStartLink = GameString.Create("PylonStartLink", "Start Link");

	public static readonly GameString PylonCompleteLink = GameString.Create("PylonCompleteLink", "Complete Link - {LOCAL:Quantity} Cables", "Quantity");

	public static readonly GameString PylonUnlink = GameString.Create("PylonUnlink", "Unlink");

	public static readonly GameString ConnectionLimitReached = GameString.Create("ConnectionLimitReached", "Connection limit reached");

	public static readonly GameString PylonStartNodeTooFar = GameString.Create("PylonStartNodeTooFar", "Start node is too far away");

	public static readonly GameString PylonNeedCablesToLink = GameString.Create("PylonNeedCablesToLink", "You need {LOCAL:Quantity} cables to complete link", "Quantity");

	public static readonly GameString PylonCantConnect = GameString.Create("PylonCantConnect", "Can't connect");

	public static readonly GameString PylonTerminusNeedsPylon = GameString.Create("PylonTerminusNeedsPylon", "Terminals must link through a pylon");

	public static readonly GameString PylonPathObstructed = GameString.Create("PylonPathObstructed", "Cable path is obstructed");

	public static readonly GameString PylonNoConnectionsToUnlink = GameString.Create("PylonNoConnectionsToUnlink", "No connections to unlink");

	public static void Initialize()
	{
	}
}
