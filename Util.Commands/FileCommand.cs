using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;

namespace Util.Commands;

public class FileCommand : CommandBase
{
	private const string StartArgs = "start <stationname> [worldid] [difficulty] [startcondition] [startlocation] : Tries to load the latest save for a station. If not found, starts a new world with the given params and saves.";

	private const string NewArgs = "new <stationname> : Creates a new station save in the root save directory with the given name.";

	private const string SaveArgs = "save : Saves the current game into the head save of the currently loaded station.";

	private const string SaveAsArgs = "saveas <savename> : Creates a new manual save for the currently loaded station.";

	private const string QuickSaveArgs = "quicksave : Creates a new quick save for the currently loaded station.";

	private const string DeleteSaveArgs = "deletesave <fullpath> : Deletes the save file at the path.";

	private const string DeleteStationArgs = "deletestation <fullpath> : Deletes the station and all its saves at the path.";

	private const string ListArgs = "list : List all saves.";

	private const string InfoArgs = "info : Show current station and save directory information.";

	private const string LoadArgs = "load <fullpath> : Loads the save at the path. Assumes the save is inside a save folder in the save directory.";

	private const string LoadLatestArgs = "loadlatest <stationname> [savetype] : Loads the latest save for a station. Optionally specify savetype as 'auto' or 'quick'. Defaults to any save type if omitted.";

	private const string ForceAllowSave = "forceallowsave : Forces the 'is already saving' check to false. Can be used as a last resort if saving gets stuck.";

	public override string HelpText => "Save and load functions.";

	public override bool IsLaunchCmd => true;

	public override bool RequiresGameManagerIsInitialized => true;

	public override string HelpTextSeparator => Environment.NewLine;

	public override string[] Arguments => new string[12]
	{
		"start <stationname> [worldid] [difficulty] [startcondition] [startlocation] : Tries to load the latest save for a station. If not found, starts a new world with the given params and saves.", "new <stationname> : Creates a new station save in the root save directory with the given name.", "save : Saves the current game into the head save of the currently loaded station.", "saveas <savename> : Creates a new manual save for the currently loaded station.", "quicksave : Creates a new quick save for the currently loaded station.", "deletesave <fullpath> : Deletes the save file at the path.", "deletestation <fullpath> : Deletes the station and all its saves at the path.", "list : List all saves.", "info : Show current station and save directory information.", "load <fullpath> : Loads the save at the path. Assumes the save is inside a save folder in the save directory.",
		"loadlatest <stationname> [savetype] : Loads the latest save for a station. Optionally specify savetype as 'auto' or 'quick'. Defaults to any save type if omitted.", "forceallowsave : Forces the 'is already saving' check to false. Can be used as a last resort if saving gets stuck."
	};

	public override string Execute(string[] args)
	{
		if (CommandBase.CannotAsClient("save"))
		{
			return null;
		}
		return args[0] switch
		{
			"start" => HandleStart(args), 
			"new" => HandleNewSave(args), 
			"save" => HandleSave(args), 
			"saveas" => HandleSaveAs(args), 
			"quicksave" => HandleQuickSave(args), 
			"deletesave" => HandleDeleteSave(args), 
			"deletestation" => HandleDeleteStation(args), 
			"list" => HandleList(args), 
			"info" => HandleInfo(args), 
			"load" => HandleLoad(args), 
			"loadlatest" => HandleLoadLatest(args), 
			"forceallowsave" => HandleForceAllowSave(args), 
			_ => "Invalid command", 
		};
	}

	private bool CheckArg(string[] args, int index, out string arg)
	{
		arg = null;
		if (index >= args.Length)
		{
			ConsoleWindow.PrintError("index out of range for arguments array", suppressStacktrace: true);
			return false;
		}
		arg = args[index];
		return true;
	}

	public static DirectoryInfo GetStationDirectory(string folderName)
	{
		return new DirectoryInfo($"{StationSaveUtils.GetSavePathSavesSubDir()}/{folderName}");
	}

	private string HandleStart(string[] args)
	{
		if (!CheckArg(args, 1, out var arg))
		{
			return null;
		}
		DirectoryInfo stationDirectory = GetStationDirectory(arg);
		if (stationDirectory.Exists)
		{
			if (!GetSaveFilesByDate(stationDirectory, out List<(FileInfo, DateTime)> filesByDate))
			{
				return "No valid save files found";
			}
			string fullName = filesByDate[0].Item1.FullName;
			LoadHelper.LoadGame(fullName, arg);
			return "Loaded " + fullName;
		}
		ConsoleWindow.Print("No existing station found with name " + arg + ", attempting to start a new game.");
		if (args.Length < 3)
		{
			return "Tried to start a new game but no world id was provided";
		}
		string text = args[2];
		WorldSetting worldSetting = WorldSetting.Find(text);
		if (worldSetting == null)
		{
			return "[No such world name: " + text + ". Valid worlds: " + NewGameCommand.AllWorldsList();
		}
		string text2 = "Normal";
		string text3 = null;
		string text4 = null;
		if (args.Length >= 4)
		{
			text2 = args[3];
		}
		if (args.Length >= 5)
		{
			text3 = args[4];
		}
		if (args.Length >= 6)
		{
			text4 = args[5];
		}
		DifficultySetting difficultySetting = DifficultySetting.Find(text2);
		if (difficultySetting == null)
		{
			return "No matching difficulty setting found with id " + text2;
		}
		StartConditionData startConditionData = ((text3 == null) ? DataCollection.Get<StartConditionData>(StartConditionData.GetDefaultStartCondition(worldSetting).IdHash) : DataCollection.Get<StartConditionData>(text3));
		if (startConditionData == null)
		{
			return "No matching start condition found with id " + text3;
		}
		StartLocationData startLocationData = ((text4 == null) ? worldSetting.Data.SelectStartLocation() : DataCollection.Get<StartLocationData>(text4));
		if (startLocationData == null)
		{
			if (text3 != null)
			{
				return "No matching start location found with id " + text3;
			}
			return "No default start location found in world setting data";
		}
		WorldSetting.SetCurrent(worldSetting, startConditionData, startLocationData);
		DifficultySetting.SetCurrent(difficultySetting);
		ImGuiLoadingScreen.WorldName = worldSetting.Id;
		NewGameTask(worldSetting.Id, arg).Forget();
		return null;
	}

	private async UniTaskVoid NewGameTask(string worldId, string stationName)
	{
		await World.StartNewWorld(worldId);
		ConsoleWindow.PrintAction("Started new game. Saving...");
		stationName = SaveHelper.SanitizeSaveName(stationName);
		SaveResult saveResult = await SaveHelper.NewSave(stationName, default(CancellationToken));
		if (saveResult.Success)
		{
			XmlSaveLoad.Instance.CurrentStationName = stationName;
			ConsoleWindow.Print("Created new save");
		}
		else
		{
			ConsoleWindow.PrintError(saveResult.Message, suppressStacktrace: true);
		}
	}

	private string HandleNewSave(string[] args)
	{
		if (!CheckArg(args, 1, out var arg))
		{
			return null;
		}
		if (GetStationDirectory(arg).Exists)
		{
			return "Save directory already exists.";
		}
		NewSaveTask(arg).Forget();
		return null;
	}

	private async UniTaskVoid NewSaveTask(string stationName)
	{
		stationName = SaveHelper.SanitizeSaveName(stationName);
		SaveResult saveResult = await SaveHelper.NewSave(stationName, default(CancellationToken));
		if (saveResult.Success)
		{
			XmlSaveLoad.Instance.CurrentStationName = stationName;
			ConsoleWindow.Print("Created new save");
		}
		else
		{
			ConsoleWindow.PrintError(saveResult.Message, suppressStacktrace: true);
		}
	}

	private string HandleSave(string[] args)
	{
		string currentStationName = XmlSaveLoad.Instance.CurrentStationName;
		if (string.IsNullOrWhiteSpace(currentStationName))
		{
			return "Station name not set.";
		}
		if (!GetStationDirectory(currentStationName).Exists)
		{
			return "Could not find save directory.";
		}
		SaveTask(currentStationName).Forget();
		return null;
	}

	private async UniTaskVoid SaveTask(string stationName)
	{
		SaveResult saveResult = await SaveHelper.Save(stationName, default(CancellationToken));
		if (saveResult.Success)
		{
			ConsoleWindow.Print("Saved " + stationName);
		}
		else
		{
			ConsoleWindow.PrintError(saveResult.Message, suppressStacktrace: true);
		}
	}

	private string HandleSaveAs(string[] args)
	{
		if (!CheckArg(args, 1, out var arg))
		{
			return null;
		}
		string currentStationName = XmlSaveLoad.Instance.CurrentStationName;
		if (string.IsNullOrWhiteSpace(currentStationName))
		{
			return "Station name not set.";
		}
		if (!GetStationDirectory(currentStationName).Exists)
		{
			return "Could not find save directory.";
		}
		SaveAsTask(currentStationName, arg).Forget();
		return null;
	}

	private async UniTaskVoid SaveAsTask(string stationName, string saveFileName)
	{
		SaveResult saveResult = await SaveHelper.SaveAs(stationName, saveFileName, default(CancellationToken));
		if (saveResult.Success)
		{
			ConsoleWindow.Print("Saved " + stationName);
		}
		else
		{
			ConsoleWindow.PrintError(saveResult.Message, suppressStacktrace: true);
		}
	}

	private string HandleQuickSave(string[] args)
	{
		string currentStationName = XmlSaveLoad.Instance.CurrentStationName;
		if (string.IsNullOrWhiteSpace(currentStationName))
		{
			return "Station name not set.";
		}
		if (!GetStationDirectory(currentStationName).Exists)
		{
			return "Could not find save directory.";
		}
		QuickSaveTask(currentStationName).Forget();
		return null;
	}

	private async UniTaskVoid QuickSaveTask(string stationName)
	{
		SaveResult saveResult = await SaveHelper.QuickSave(stationName, default(CancellationToken));
		if (saveResult.Success)
		{
			ConsoleWindow.Print("Saved " + stationName);
		}
		else
		{
			ConsoleWindow.PrintError(saveResult.Message, suppressStacktrace: true);
		}
	}

	private string HandleDeleteSave(string[] args)
	{
		if (!CheckArg(args, 1, out var arg))
		{
			return null;
		}
		DirectoryInfo savePathSavesSubDir = StationSaveUtils.GetSavePathSavesSubDir();
		if (!arg.StartsWith(savePathSavesSubDir.FullName, StringComparison.InvariantCultureIgnoreCase))
		{
			return "Can't delete outside the root save folder.";
		}
		FileInfo fileInfo = new FileInfo(arg);
		if (!fileInfo.Exists)
		{
			return "Could not find save.";
		}
		fileInfo.Delete();
		return "Save deleted";
	}

	private string HandleDeleteStation(string[] args)
	{
		if (!CheckArg(args, 1, out var arg))
		{
			return null;
		}
		DirectoryInfo savePathSavesSubDir = StationSaveUtils.GetSavePathSavesSubDir();
		if (!arg.StartsWith(savePathSavesSubDir.FullName, StringComparison.InvariantCultureIgnoreCase))
		{
			return "Can't delete outside the root save folder.";
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(arg);
		if (!directoryInfo.Exists)
		{
			return "Could not find directory.";
		}
		directoryInfo.Delete(recursive: true);
		return "Station deleted";
	}

	private string HandleList(string[] args)
	{
		List<string> list = new List<string>();
		foreach (SaveInfo localSafe in LoadHelper.GetLocalSaves())
		{
			foreach (SaveFileInfo safe in localSafe.Saves)
			{
				list.Add(safe.FileInfo.FullName);
			}
		}
		return string.Join(Environment.NewLine, list);
	}

	private string HandleInfo(string[] args)
	{
		DirectoryInfo savePathSavesSubDir = StationSaveUtils.GetSavePathSavesSubDir();
		string currentStationName = XmlSaveLoad.Instance.CurrentStationName;
		return $"Current save directory: {savePathSavesSubDir}\nCurrent Station: {currentStationName}";
	}

	private string HandleLoad(string[] args)
	{
		if (!CheckArg(args, 1, out var arg))
		{
			return null;
		}
		return LoadAtFullPath(arg);
	}

	public static string LoadAtFullPath(string fullPath)
	{
		FileInfo fileInfo = new FileInfo(fullPath);
		if (!fileInfo.Exists)
		{
			return "Could not find save at path.";
		}
		if (fileInfo.Extension != SaveLoadConstants.SaveFileExtension)
		{
			return "Incorrect save file type. Should be a .save file.";
		}
		DirectoryInfo savePathSavesSubDir = StationSaveUtils.GetSavePathSavesSubDir();
		if (fileInfo.FullName.IndexOf(savePathSavesSubDir.FullName, StringComparison.InvariantCulture) != 0)
		{
			return "Save file is not in the root save directory.";
		}
		string[] array = Path.GetRelativePath(savePathSavesSubDir.FullName, fileInfo.FullName).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		if (array.Length < 2)
		{
			return "Save folder structure is invalid";
		}
		string stationName = array[0];
		LoadHelper.LoadGame(fileInfo.FullName, stationName);
		return "Loaded " + fileInfo.FullName;
	}

	private string HandleLoadLatest(string[] args)
	{
		if (!CheckArg(args, 1, out var arg))
		{
			return null;
		}
		DirectoryInfo stationDirectory = GetStationDirectory(arg);
		if (!stationDirectory.Exists)
		{
			return "Could not find save directory.";
		}
		DirectoryInfo directoryInfo = stationDirectory;
		if (args.Length > 2)
		{
			directoryInfo = args[2] switch
			{
				"auto" => new DirectoryInfo(stationDirectory.FullName + "/" + SaveLoadConstants.AutoSaveFolder), 
				"quick" => new DirectoryInfo(stationDirectory.FullName + "/" + SaveLoadConstants.QuickSaveFolder), 
				"manual" => new DirectoryInfo(stationDirectory.FullName + "/" + SaveLoadConstants.ManualSaveFolder), 
				_ => null, 
			};
			if (directoryInfo == null)
			{
				return "Invalid save type";
			}
			if (!directoryInfo.Exists)
			{
				return "Could not find directory " + directoryInfo.FullName;
			}
		}
		if (!GetSaveFilesByDate(directoryInfo, out List<(FileInfo, DateTime)> filesByDate))
		{
			return "No valid save files found";
		}
		string fullName = filesByDate[0].Item1.FullName;
		LoadHelper.LoadGame(fullName, arg);
		return "Loaded " + fullName;
	}

	public static bool GetSaveFilesByDate(DirectoryInfo directory, out List<(FileInfo file, DateTime dateTime)> filesByDate)
	{
		filesByDate = new List<(FileInfo, DateTime)>();
		FileInfo[] files = directory.GetFiles(SaveLoadConstants.SaveFileSearchPattern, SearchOption.AllDirectories);
		foreach (FileInfo fileInfo in files)
		{
			if (LoadHelper.UnzipMetaData(fileInfo, out var metaData))
			{
				filesByDate.Add((fileInfo, DateTime.FromFileTime(metaData.DateTime)));
			}
		}
		if (filesByDate.Count == 0)
		{
			return false;
		}
		filesByDate.Sort(((FileInfo file, DateTime dateTime) a, (FileInfo file, DateTime dateTime) b) => b.dateTime.CompareTo(a.dateTime));
		return true;
	}

	private string HandleForceAllowSave(string[] args)
	{
		SaveHelper.ForceIsSavingToFalse();
		return "IsSaving set to false.";
	}
}
