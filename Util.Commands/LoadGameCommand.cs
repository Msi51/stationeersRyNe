using System.IO;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;

namespace Util.Commands;

public class LoadGameCommand : CommandBase
{
	public override string HelpText => "Loads a saved world file. Can also start a new game via the launch command (e.g. -load \"my game save\" moon). Pass 'list' / 'l' to list saves.";

	public override string[] Arguments => new string[3] { "<list | l>", "<filename>", "<filename> [worldname]" };

	public override bool IsLaunchCmd => true;

	public override bool RequiresGameManagerIsInitialized => true;

	public override string Execute(string[] args)
	{
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (!CommandBase.Get(args, 0, "filename", out string result))
		{
			return null;
		}
		string text = args[0];
		if (text == "list" || text == "l")
		{
			return GetListOfSavedFiles();
		}
		string backupWorldName = ((args.Length >= 2) ? args[1] : null);
		LoadGame(result, backupWorldName).Forget();
		return null;
	}

	protected static async UniTaskVoid LoadGame(string filenameOrFullPath, string backupWorldName = null)
	{
		if (string.IsNullOrWhiteSpace(filenameOrFullPath))
		{
			ConsoleWindow.PrintError("Save name cannot be empty.", suppressStacktrace: true);
			return;
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(filenameOrFullPath);
		if (directoryInfo.Exists)
		{
			ConsoleWindow.Print(FileCommand.LoadAtFullPath(directoryInfo.FullName));
			return;
		}
		if (!directoryInfo.Exists)
		{
			directoryInfo = FileCommand.GetStationDirectory(filenameOrFullPath);
		}
		if (directoryInfo.Exists)
		{
			FileInfo[] files = directoryInfo.GetFiles(SaveLoadConstants.SaveFileSearchPattern, SearchOption.TopDirectoryOnly);
			if (files.Length != 1)
			{
				ConsoleWindow.PrintError(".save file not found at '" + directoryInfo.FullName + "'.", suppressStacktrace: true);
			}
			else
			{
				LoadHelper.LoadGame(files[0].FullName, filenameOrFullPath);
			}
			return;
		}
		if (backupWorldName == null)
		{
			ConsoleWindow.PrintError("No world name provided.", suppressStacktrace: true);
			return;
		}
		WorldSetting worldSetting = WorldSetting.Find(backupWorldName);
		if (worldSetting == null)
		{
			ConsoleWindow.PrintError("No matching world found with id '" + backupWorldName + "'. Valid worlds: " + NewGameCommand.AllWorldsList(), suppressStacktrace: true);
			return;
		}
		DifficultySetting current = DifficultySetting.Find("Normal");
		StartConditionData startCondition = DataCollection.Get<StartConditionData>(StartConditionData.GetDefaultStartCondition(worldSetting).IdHash);
		StartLocationData startLocationData = worldSetting.Data.StartLocationDatas.Pick();
		WorldSetting.SetCurrent(worldSetting, startCondition, startLocationData);
		DifficultySetting.SetCurrent(current);
		ImGuiLoadingScreen.WorldName = worldSetting.Id;
		NewGameTask(worldSetting.Id, filenameOrFullPath).Forget();
	}

	private static async UniTaskVoid NewGameTask(string worldId, string stationName)
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

	private static string GetListOfSavedFiles()
	{
		return SaveCommand.ListSaves();
	}
}
