using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using UnityEngine;

namespace Util.Commands;

public static class CommandLine
{
	private static readonly SortedDictionary<string, CommandBase> _commandsMap;

	private static bool _processedLaunchArgs;

	private static bool _isLaunchCommands;

	private static readonly List<(string currentKey, CommandBase cmd, string[] args)> _postLaunchCommands;

	public static IReadOnlyDictionary<string, CommandBase> CommandsMap => _commandsMap;

	public static string[] CommandLineArgs { get; private set; }

	static CommandLine()
	{
		_commandsMap = new SortedDictionary<string, CommandBase>
		{
			["achievements"] = new AchievementsCommand(),
			["help"] = new HelpCommand(),
			["clear"] = new ClearCommand(),
			["quit"] = new QuitCommand(),
			["exit"] = new ExitCommand(),
			["leave"] = new ExitCommand(),
			["newgame"] = new NewGameCommand(),
			["new"] = new NewGameCommand(),
			["joingame"] = new JoinCommand(),
			["join"] = new JoinCommand(),
			["steam"] = new SteamCommand(),
			["listnetworkdevices"] = new ListNetworkDevicesCommand(),
			["testbytearray"] = new TestByteArrayCommand(),
			["rocketbinary"] = new RocketBinaryCommands(),
			["imgui"] = new ImGuiCommands(),
			["atmos"] = new AtmosphereCommands(),
			["structurenetwork"] = new StructureNetworkCommand(),
			["thing"] = new ThingCommand(),
			["keybindings"] = new KeyBindingCommands(),
			["reset"] = new RestartCommand(),
			["version"] = new VersionCommand(),
			["rocket"] = new RocketCommand(),
			["unstuck"] = new UnstuckCommand(),
			["spacemap"] = new SpaceMapCommand(),
			["spacemapnode"] = new SpaceMapNodeCommand(),
			["logtoclipboard"] = new LogToClipboardCommand(),
			["camera"] = new CameraCommand(),
			["kick"] = new KickCommand(),
			["ban"] = new BanCommand(),
			["upnp"] = new UpnpCommand(),
			["network"] = new NetworkCommand(),
			["pause"] = new PauseCommand(),
			["say"] = new SayCommand(),
			["announce"] = new AnnounceCommand(),
			["world"] = new PrintWorldSettingsCommand(),
			["log"] = new LogCommand(),
			["discord"] = new DiscordCommand(),
			["settings"] = new SettingsCommand(),
			["netconfig"] = new NetConfigCommand(),
			["settingspath"] = new SettingsPathCommand(),
			["regeneraterooms"] = new RegenerateRoomsCommand(),
			["roomevaluator"] = new RoomEvaluatorCommand(),
			["storm"] = new StormCommand(),
			["debugthreads"] = new DebugThreadsCommand(),
			["pylonlog"] = new PylonLogCommand(),
			["status"] = new StatusCommand(),
			["masterserver"] = new MasterServerCommand(),
			["deletelooseitems"] = new DeleteLooseItemsCommand(),
			["emote"] = new EmoteCommand(),
			["expression"] = new CustomFacialExpressionCommand(),
			["serverrun"] = new ServerRunCommand(),
			["windowheight"] = new ConsoleWindowHeightCommand(),
			["cleanupplayers"] = new CleanupPlayersCommand(),
			["difficulty"] = new DifficultySettingsCommand(),
			["addgas"] = new AddGas(),
			["legacycpu"] = new LegacyCpuCommand(),
			["trader"] = new TraderCommand(),
			["localization"] = new LocalizationCommand(),
			["deleteoutofbounds"] = new DeleteOutOfBoundsObjectsCommand(),
			["deletenear"] = new DeleteNearCommand(),
			["voxelfillnear"] = new VoxelFillNearCommand(),
			["printgasinfo"] = new PrintPhaseChangeInfoCommand(),
			["structure"] = new StructureCommand(),
			["plant"] = new PlantCommand(),
			["physics"] = new PhysicsCommand(),
			["power"] = new PowerCommand(),
			["orbit"] = new OrbitalCommand(),
			["celestial"] = new CelestialCommand(),
			["dlc"] = new DLCCommand(),
			["entity"] = new EntityCommand(),
			["setbatteries"] = new SetBatteriesCommand(),
			["systeminfo"] = new SystemInfoCommand(),
			["profiler"] = new ProfilerCommand(),
			["prefabs"] = new ValidateSourcePrefabsCommands(),
			["helperhints"] = new WorldObjectiveCommand(),
			["exportworld"] = new ExportWorldCommand(),
			["worldsetting"] = new WorldSettingWindowCommand(),
			["liquid"] = new LiquidCommands(),
			["vegetation"] = new VegetationCommand(),
			["minables"] = new MinableCommand(),
			["testoctree"] = new TestOctreeCommand(),
			["terraineditor"] = new TerrainEditorWindowCommand(),
			["region"] = new RegionCommand(),
			["proxy"] = new ProxyCommand(),
			["file"] = new FileCommand(),
			["map"] = new MiniMapWindowCommand(),
			["terrain"] = new TerrainCommands(),
			["geyser"] = new GeyserCommand(),
			["reloadterraintexture"] = new ReloadTerrainTextureCommand(),
			["teleport"] = new TeleportCommand(),
			["lod"] = new LodDebugWindowCommand(),
			["densepools"] = new DensePoolCommand(),
			["loworbitstation"] = new LowOrbitStationCommand(),
			["clientinfo"] = new SerializedClientInfoCommand(),
			["player"] = new PlayerCommand(),
			["organs"] = new OrgansCommand(),
			["thumbnail"] = new ThumbnailStudioCommand(),
			["loadgame"] = new LoadGameCommand(),
			["load"] = new LoadGameCommand(),
			["loadlatest"] = new LoadLatestCommand(),
			["save"] = new SaveCommand()
		};
		_postLaunchCommands = new List<(string, CommandBase, string[])>();
		AddCommand("test", new BasicCommand(delegate
		{
			ConsoleWindow.Print("Test Log");
			ConsoleWindow.PrintAction("Test Action");
			ConsoleWindow.PrintError("Test Error");
			foreach (ConsoleColor item in Enum.GetValues(typeof(ConsoleColor)).Cast<ConsoleColor>())
			{
				ConsoleWindow.Print(item.ToString(), item);
			}
			return (string)null;
		}, "Testing all the colours of the rainbow"));
	}

	public static void AddCommand(string key, CommandBase cmd)
	{
		if (_commandsMap.ContainsKey(key))
		{
			Debug.LogError("Command key has already been assigned");
		}
		else
		{
			_commandsMap[key] = cmd;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void ProcessOnLaunch()
	{
		if (!_processedLaunchArgs)
		{
			_processedLaunchArgs = true;
			_isLaunchCommands = true;
			CommandLineArgs = Environment.GetCommandLineArgs();
			Process(CommandLineArgs, onLaunch: true);
			_isLaunchCommands = false;
		}
	}

	public static bool TryGetArg(string arg, out string value)
	{
		if (CommandLineArgs == null)
		{
			value = null;
			return false;
		}
		for (int i = 0; i < CommandLineArgs.Length; i++)
		{
			if (!(CommandLineArgs[i] != arg))
			{
				value = CommandLineArgs[i + 1].Trim();
				return true;
			}
		}
		value = null;
		return false;
	}

	public static void Process(string input)
	{
		if (!string.IsNullOrEmpty(input))
		{
			string[] array = CmdLineParser.SplitCommandLine(input).ToArray();
			if (!array[0].StartsWith('-'))
			{
				array[0] = "-" + array[0];
			}
			Process(array);
		}
	}

	public static void Process(string[] args, bool onLaunch = false)
	{
		string currentKey = "";
		CommandBase cmd = null;
		List<string> list = new List<string>();
		for (int i = 0; i < args.Length; i++)
		{
			string text = args[i].Trim();
			if (text.StartsWith("-") && (i == 0 || onLaunch))
			{
				ExecuteCommand(currentKey, cmd, list.ToArray());
				currentKey = null;
				cmd = null;
				list.Clear();
				string text2 = text.TrimStart('-').ToLower();
				if (!_commandsMap.TryGetValue(text2, out var value))
				{
					if (!_isLaunchCommands)
					{
						ConsoleWindow.PrintError(ConsoleStrings.Error.CommandUnknown.AsString(text), suppressStacktrace: true);
					}
				}
				else if (_isLaunchCommands && !value.IsLaunchCmd)
				{
					ConsoleWindow.PrintError("Can not use command '" + text + "' as a launch command", suppressStacktrace: true);
					ConsoleWindow.PrintError(ConsoleStrings.Error.CommandUnknown.AsString(text), suppressStacktrace: true);
				}
				else
				{
					cmd = value;
					currentKey = text2;
				}
			}
			else
			{
				list.Add(text);
			}
		}
		ExecuteCommand(currentKey, cmd, list.ToArray());
	}

	private static void ExecuteCommand(string currentKey, CommandBase cmd, string[] args)
	{
		if (cmd == null)
		{
			return;
		}
		if (!GameManager.IsInitialized && cmd.RequiresGameManagerIsInitialized)
		{
			_postLaunchCommands.Add((currentKey, cmd, args));
			return;
		}
		string text = cmd.Execute(args);
		if (!string.IsNullOrEmpty(text))
		{
			if (text == "Invalid syntax" || text == "Invalid arguments")
			{
				ConsoleWindow.PrintError(currentKey + ": " + text.ToLowerInvariant() + ". Usage:", suppressStacktrace: true);
				HelpCommand.PrintCommand(currentKey, cmd, currentKey.Length + 2);
			}
			else
			{
				ConsoleWindow.Print(currentKey + ": " + text);
			}
		}
	}

	public static void ExecutePostLaunchCommands()
	{
		foreach (var postLaunchCommand in _postLaunchCommands)
		{
			string text = postLaunchCommand.cmd.Execute(postLaunchCommand.args);
			if (!string.IsNullOrEmpty(text))
			{
				ConsoleWindow.Print(postLaunchCommand.currentKey + ": " + text);
			}
		}
	}
}
