using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts;
using Assets.Scripts.Serialization;

namespace Util.Commands;

public class LoadLatestCommand : LoadGameCommand
{
	private const string NOT_FOUND = "Latest save not found.";

	public override string HelpText => "Loads the most recent save (including auto-saves). Optionally restricts the search to a named save folder.";

	public override string[] Arguments => new string[1] { "[filename]" };

	public override string Execute(string[] args)
	{
		int num = args.Length;
		if (num < 1)
		{
			if (num == 0)
			{
				return FindAndLoadLatestGame();
			}
			return "Invalid arguments";
		}
		if (!(FindAndLoadLatestGame(args) == "Latest save not found."))
		{
			return null;
		}
		return base.Execute(args);
	}

	private static string FindAndLoadLatestGame()
	{
		DirectoryInfo[] directories = StationSaveUtils.GetSavePathSavesSubDir().GetDirectories();
		FileInfo fileInfo = null;
		DirectoryInfo[] array = directories;
		for (int i = 0; i < array.Length; i++)
		{
			FileInfo[] files = array[i].GetFiles();
			if (files.Length == 1)
			{
				FileInfo fileInfo2 = files[0];
				if (fileInfo == null || fileInfo.LastWriteTime < fileInfo2.LastWriteTime)
				{
					fileInfo = fileInfo2;
				}
			}
		}
		if (fileInfo != null)
		{
			ConsoleWindow.Print(FileCommand.LoadAtFullPath(fileInfo.FullName));
			return null;
		}
		return "Latest save not found.";
	}

	private static string FindAndLoadLatestGame(string[] arguments)
	{
		string text = "saveGame";
		if (arguments.Length >= 1)
		{
			text = arguments[0];
		}
		DirectoryInfo stationDirectory = FileCommand.GetStationDirectory(text);
		if (!stationDirectory.Exists)
		{
			return "Latest save not found.";
		}
		if (!FileCommand.GetSaveFilesByDate(stationDirectory, out List<(FileInfo, DateTime)> filesByDate))
		{
			return "Latest save not found.";
		}
		LoadHelper.LoadGame(filesByDate[0].Item1.FullName, text);
		return null;
	}
}
