using System.IO;
using System.Text;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Serialization;
using Cysharp.Threading.Tasks;

namespace Util.Commands;

public class SaveCommand : CommandBase
{
	public override string HelpText => "Saves the current game. Without arguments saves to the current station name; pass a filename to save under a different name, 'delete' / 'd' / 'rm' followed by a filename to delete a save, or 'list' / 'l' to list all saves.";

	public override string[] Arguments => new string[3] { "[filename]", "<delete | d | rm> <filename>", "<list | l>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("save"))
		{
			return null;
		}
		if (args.Length == 0)
		{
			return CreateSave(XmlSaveLoad.Instance.CurrentStationName);
		}
		switch (args[0])
		{
		case "delete":
		case "rm":
		case "d":
			return DeleteSave(args);
		case "list":
		case "l":
			return ListSaves();
		default:
			return CreateSave(args[0]);
		}
	}

	public static string CreateSave(string saveName)
	{
		GameState gameState = GameManager.GameState;
		if (gameState != GameState.Running && gameState != GameState.Paused)
		{
			ConsoleWindow.PrintError($"Cannot save game in GameState '{GameManager.GameState}'.", suppressStacktrace: true);
			return null;
		}
		if (!FileCommand.GetStationDirectory(saveName).Exists)
		{
			NewSaveTask(saveName).Forget();
			return null;
		}
		SaveTask(saveName).Forget();
		return null;
	}

	private static async UniTaskVoid SaveTask(string stationName)
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

	private static async UniTaskVoid NewSaveTask(string stationName)
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

	private static string DeleteSave(string[] args)
	{
		if (args.Length < 2)
		{
			return "Invalid syntax";
		}
		if (!CommandBase.Get(args, 1, "filename", out string result))
		{
			return null;
		}
		DirectoryInfo worldSaveDirectory = StationSaveUtils.GetWorldSaveDirectory(result);
		if (!worldSaveDirectory.Exists)
		{
			ConsoleWindow.PrintError("Directory does not exist.", suppressStacktrace: true);
			return null;
		}
		string fullName = worldSaveDirectory.FullName;
		worldSaveDirectory.Delete(recursive: true);
		return "Deleted '" + fullName + "'.";
	}

	internal static DirectoryInfo[] GetSavesList()
	{
		return StationSaveUtils.GetSavePathSavesSubDir().GetDirectories();
	}

	internal static string ListSaves()
	{
		StringBuilder stringBuilder = new StringBuilder();
		DirectoryInfo[] savesList = GetSavesList();
		foreach (DirectoryInfo directoryInfo in savesList)
		{
			stringBuilder.AppendLine(directoryInfo.FullName);
		}
		return stringBuilder.ToString();
	}
}
