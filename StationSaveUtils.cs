using System;
using System.IO;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Serialization;
using UI;
using UnityEngine;

public static class StationSaveUtils
{
	private static DirectoryInfo _exeDirectory;

	public static DirectoryInfo ExeDirectory => _exeDirectory ?? (_exeDirectory = new DirectoryInfo(Application.dataPath).Parent);

	public static string DefaultPath
	{
		get
		{
			if (!GameManager.IsBatchMode)
			{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Stationeers");
			}
			return ExeDirectory.FullName;
		}
	}

	public static DirectoryInfo GetSavePathSavesSubDir()
	{
		return new DirectoryInfo(Path.Combine(GetSavePath(), "saves"));
	}

	public static DirectoryInfo GetSavePathScriptsSubDir()
	{
		return new DirectoryInfo(Path.Combine(GetSavePath(), "scripts"));
	}

	public static string GetSavePath()
	{
		if (string.IsNullOrEmpty(Settings.CurrentData.SavePath))
		{
			Settings.CurrentData.SavePath = DefaultPath;
		}
		string savePath = Settings.CurrentData.SavePath;
		string text = Path.Combine(savePath, "saves");
		string text2 = Path.Combine(savePath, "scripts");
		string text3 = Path.Combine(savePath, "mods");
		try
		{
			string[] array = new string[4] { savePath, text, text2, text3 };
			foreach (string path in array)
			{
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
			}
		}
		catch (UnauthorizedAccessException exception)
		{
			ConsoleWindow.PrintError("Unauthorized Access: path(" + Settings.CurrentData.SavePath + ") cannot be accessed. Falling back to default path(" + DefaultPath + ")");
			ConsoleWindow.PrintError(exception).Forget();
			Settings.CurrentData.SavePath = DefaultPath;
			savePath = Settings.CurrentData.SavePath;
			text = Path.Combine(savePath, "saves");
			text2 = Path.Combine(savePath, "scripts");
			text3 = Path.Combine(savePath, "mods");
			string[] array = new string[4] { savePath, text, text2, text3 };
			foreach (string path2 in array)
			{
				if (!Directory.Exists(path2))
				{
					Directory.CreateDirectory(path2);
				}
			}
		}
		catch (Exception exception2)
		{
			ConsoleWindow.PrintError(exception2).Forget();
			throw;
		}
		return savePath;
	}

	public static DirectoryInfo GetWorldSaveDirectory(string saveName)
	{
		return GetSavePathSavesSubDir().GetDirectories().FirstOrDefault((DirectoryInfo x) => x.Name == saveName) ?? GetSavePathSavesSubDir().CreateSubdirectory(saveName);
	}

	public static bool IsSaveExistFullName(string fullName)
	{
		return GetSavePathSavesSubDir().GetDirectories().Any((DirectoryInfo x) => x.FullName == fullName);
	}

	public static bool IsSaveExist(string saveName)
	{
		return GetSavePathSavesSubDir().GetDirectories().Any((DirectoryInfo x) => x.Name == saveName);
	}

	public static string GetCloudFileName(this ICloudSyncable syncable, FileSystemInfo fileInfo)
	{
		return syncable.RootDir.Name + "_" + fileInfo.Name;
	}
}
