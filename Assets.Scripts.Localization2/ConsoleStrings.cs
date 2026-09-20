namespace Assets.Scripts.Localization2;

public static class ConsoleStrings
{
	public static class Log
	{
		public static GameString WorldSaved = GameString.Create("WorldSaved", "saved world '{0}' {1}");

		public static GameString WorldLoaded = GameString.Create("WorldLoaded", "loaded world '{0}' {1} in {2} seconds");

		public static GameString ClientConnected = GameString.Create("ClientConnected", "client '{0}' from '{1}' on '{2}' connected");

		public static GameString ClientStateChanged = GameString.Create("ClientStateChanged", "client '{0}' join state '{1}'");

		public static GameString ConnectedAsClient = GameString.Create("ConnectedAsClient", "connected to '{0}' on '{1}' as client");

		public static GameString DisconnectedAsClient = GameString.Create("DisconnectedAsClient", "disconnected from '{0}' on '{1}' as client");

		public static GameString ReceivedJoinPackage = GameString.Create("ReceivedJoinPackage", "received join package in '{0}' compressed at '{1}%%' of original");

		public static GameString LoadedJoinPackage = GameString.Create("LoadedJoinPackage", "loaded join package in {0} seconds");

		public static GameString LoadingGameData = GameString.Create("LoadingGameData", "loading game data");

		public static GameString LoadedGameData = GameString.Create("LoadedGameData", "game data loaded in {0} seconds");

		public static GameString LoadedTextures = GameString.Create("LoadedTextures", "loaded {0} textures");

		public static GameString LoadedMeshes = GameString.Create("LoadedTextures", "loaded {0} meshes");

		public static GameString StartedHosting = GameString.Create("StartedHosting", "started hosting on port '{0}'");

		public static GameString StoppedHosting = GameString.Create("StoppedHosting", "stopped hosting on port '{0}'");

		public static GameString StartedClientResult = GameString.Create("StartedClientResult", "initialize client on port '{0}' result '{1}'");

		public static GameString InitializingTime = GameString.Create("InitializingTime", "initialized '{0}' in {1}ms");

		public static GameString CancelMakingWorld = GameString.Create("CancelMakingWorld", "interrupted make new world");

		public static GameString CancelLoadingWorld = GameString.Create("CancelLoadingWorld", "interrupted loading world '{0}'");

		public static GameString LoadingWorld = GameString.Create("LoadingWorld", "loading world '{0}'");

		public static GameString SavingWorld = GameString.Create("SavingWorld", "saving world '{0}'");

		public static GameString CompanyFounded = GameString.Create("CompanyFounded", "company '{0}' founded");

		public static GameString CompanyJoinNotifyHost = GameString.Create("CompanyJoinNotifyHost", "client '{0}' joined '{1}'");

		public static GameString MakingNewWorld = GameString.Create("MakingNewWorld", "making new world '{0} x {1}'");

		public static GameString GamePaused = GameString.Create("GamePaused", "game paused");

		public static GameString GameUnpaused = GameString.Create("GameUnpaused", "game unpaused");
	}

	public static class Error
	{
		public static GameString DebugScopeNotValid = GameString.Create("DebugScopeNotValid", "type '{0}' is not a valid scope for debug");

		public static GameString WheelEventVehicleNull = GameString.Create("WheelEventVehicleNull", "error on wheel event for vehicle #'{0}' as it does not exist");

		public static GameString ClientDisconnected = GameString.Create("ClientDisconnected", "client '{0}' from '{1}' on '{2}' disconnected");

		public static GameString UnableToDisconnect = GameString.Create("UnableToDisconnect", "error disconnecting '{0}', {1}");

		public static GameString UpdateCargoNull = GameString.Create("CargoContainerNull", "error unable to deserialize cargo chunk (cannot find container #{0})");

		public static GameString UpdateIsNull = GameString.Create("UpdateIsNull", "error updating {1} #'{0}' as it does not exist");

		public static GameString LineIsNull = GameString.Create("LineIsNull", "error updating {1} as line '#{0}' does not exist");

		public static GameString TargetIsNull = GameString.Create("TargetIsNull", "error changing target for '{1}' at target #'{0}' does not exist");

		public static GameString FailedHostPortInUse = GameString.Create("FailedHostPortInUse", "failed to host on '{0}', check port not in use");

		public static GameString FailedCreateFromPrefab = GameString.Create("FailedCreateFromPrefab", "error unable to create new {1} ('{0}')");

		public static GameString PrefabNotFound = GameString.Create("PrefabNotFound", "error failed to create '{0}' prefab not found");

		public static GameString EarnFundsThingNull = GameString.Create("EarnFundsThingNull", "error displaying funds on #'{0}' as it does not exist");

		public static GameString FailedNetworkRoleChange = GameString.Create("FailedNetworkRoleChange", "cannot connect as {0} when already {1}");

		public static GameString NetworkError = GameString.Create("NetworkError", "network error {0} on {1}");

		public static GameString CommandArgumentInvalid = GameString.Create("CommandArgumentInvalid", "invalid {0} for '{1}'");

		public static GameString CommandArgumentUnknown = GameString.Create("CommandArgumentUnknown", "unknown {0} using '{1}'");

		public static GameString CommandScopeInvalid = GameString.Create("CommandScopeInvalid", "scope '{1}' is not valid for {0}");

		public static GameString CommandUnauthorized = GameString.Create("CommandUnauthorized", "invalid permissions for {0} on '{1}'");

		public static GameString CommandUnknown = GameString.Create("CommandUnknown", "unknown command '{0}', use 'help' for commands");

		public static GameString OnlyPositiveAllowed = GameString.Create("OnlyPositiveAllowed", "can only use positive values '{0}'");

		public static GameString CannotAsClient = GameString.Create("CannotAsClient", "cannot use '{0}' while client");

		public static GameString CannotInMultiplayer = GameString.Create("CannotInMultiplayer", "cannot use '{0}' while in multiplayer");

		public static GameString CannotInSingleplayer = GameString.Create("CannotInSingleplayer", "cannot use '{0}' while in singleplayer");

		public static GameString NotHostingGame = GameString.Create("NotHostingGame", "cannot stop host, as not hosting");

		public static GameString NotConnectedAsClient = GameString.Create("NotConnectedAsClient", "cannot disconnect as not connected as a client");

		public static GameString NoNetworkNewGames = GameString.Create("NoNetworkNewGames", "cannot make a new game while network game in progress");

		public static GameString NoNetworkLoadGames = GameString.Create("NoNetworkLoadGames", "cannot load game while network game in progress");

		public static GameString CannotLoadDirectoryNull = GameString.Create("CannotLoadDirectoryNull", "cannot load '{0}', directory does not exist");

		public static GameString CannotLoadGameFilesNull = GameString.Create("CannotLoadGameFilesNull", "cannot load '{0}', game files do not exist");

		public static GameString CannotLoadMetaFileNull = GameString.Create("CannotLoadMetaFileNull", "cannot load '{0}', cannot load meta data");

		public static GameString InvalidVehicleForDepot = GameString.Create("InvalidVehicleForDepot", "{0}' cannot hold '{1}' vehicles");

		public static GameString ChildrenNotAllowed = GameString.Create("ChildrenNotAllowed", "cannot use a child vehicle, '#{0}' is the root parent");

		public static GameString CannotResolveAddressToIp = GameString.Create("CannotResolveAddressToIp", "cannot resolve '{0}' to an ip address");

		public static GameString CannotModifyCargoOnThing = GameString.Create("CannotModifyCargoOnThing", "cannot modify cargo type '{0}' to '{1}'");

		public static GameString CannotChangePauseState = GameString.Create("CannotChangePauseState", "unable to change game pause state");

		public static GameString DictionaryContainsKey = GameString.Create("DictionaryContainsKey", "error {0} dictionary already contains key {1}");

		public static GameString LoadInProgress = GameString.Create("LoadInProgress", "cannot load '{0}', already loading a game");

		public static GameString WrongVersionAsClient = GameString.Create("WrongVersionAsClient", "wrong version! you need v{0} to join this server");

		public static GameString ExceedsMaxGameYear = GameString.Create("ExceedsMaxGameYear", "error date exceeds maximum game year");

		public static GameString NotInGame = GameString.Create("NotInGame", "error '{0}' can only be using during a game");

		public static GameString ClientWrongVersion = GameString.Create("ClientWrongVersion", "error '{0}' has a different game version");
	}

	public static class Editor
	{
		public static GameString UpdatedCursorObject = GameString.Create("UpdatingCursorObject", "updated '{0}' cursor library object");
	}
}
