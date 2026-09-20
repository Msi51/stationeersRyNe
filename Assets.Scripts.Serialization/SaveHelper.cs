using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using TerrainSystem;

namespace Assets.Scripts.Serialization;

public static class SaveHelper
{
	private static Stopwatch _stopwatch = new Stopwatch();

	private static bool IsSaving { get; set; }

	public static async UniTask<SaveResult> NewSave(string stationName, CancellationToken cancellationToken)
	{
		return await SaveGame(SaveMethod.NewSave, stationName, null, cancellationToken);
	}

	public static async UniTask<SaveResult> Save(string stationName, CancellationToken cancellationToken)
	{
		return await SaveGame(SaveMethod.Save, stationName, null, cancellationToken);
	}

	public static async UniTask<SaveResult> SaveAs(string stationName, string saveFileName, CancellationToken cancellationToken)
	{
		return await SaveGame(SaveMethod.SaveAs, stationName, saveFileName, cancellationToken);
	}

	public static async UniTask<SaveResult> QuickSave(string stationName, CancellationToken cancellationToken)
	{
		return await SaveGame(SaveMethod.QuickSave, stationName, null, cancellationToken);
	}

	public static async UniTask<SaveResult> AutoSave(string stationName, CancellationToken cancellationToken)
	{
		return await SaveGame(SaveMethod.AutoSave, stationName, null, cancellationToken);
	}

	public static bool RenameStation(string oldStationName, string newStationName)
	{
		DirectoryInfo savePathSavesSubDir = StationSaveUtils.GetSavePathSavesSubDir();
		string path = $"{savePathSavesSubDir}/{oldStationName}";
		string path2 = $"{savePathSavesSubDir}/{newStationName}";
		DirectoryInfo directoryInfo = new DirectoryInfo(path);
		DirectoryInfo directoryInfo2 = new DirectoryInfo(path2);
		if (directoryInfo2.Exists)
		{
			return false;
		}
		if (!GetHeadSave(directoryInfo, out var headSaveFile))
		{
			return false;
		}
		directoryInfo.MoveTo(directoryInfo2.FullName);
		FileInfo fileInfo = new FileInfo($"{directoryInfo2}/{newStationName}{SaveLoadConstants.SaveFileExtension}");
		new FileInfo($"{directoryInfo2}/{headSaveFile.Name}").MoveTo(fileInfo.FullName);
		return true;
	}

	public static bool CreateSaveDirectory(string stationName, out DirectoryInfo directoryInfo)
	{
		directoryInfo = null;
		DirectoryInfo savePathSavesSubDir = StationSaveUtils.GetSavePathSavesSubDir();
		if (Directory.Exists($"{savePathSavesSubDir}/{stationName}"))
		{
			return false;
		}
		try
		{
			directoryInfo = savePathSavesSubDir.CreateSubdirectory(stationName);
			directoryInfo.CreateSubdirectory(SaveLoadConstants.QuickSaveFolder);
			directoryInfo.CreateSubdirectory(SaveLoadConstants.AutoSaveFolder);
			directoryInfo.CreateSubdirectory(SaveLoadConstants.ManualSaveFolder);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static bool GetUniqueDirectoryName(DirectoryInfo parentDirectory, string directoryName, out string uniqueDirectoryName)
	{
		uniqueDirectoryName = directoryName;
		string path = parentDirectory.FullName + "/" + directoryName;
		for (int i = 2; i < 100; i++)
		{
			if (!Directory.Exists(path))
			{
				return true;
			}
			uniqueDirectoryName = $"{directoryName}_{i}";
			path = parentDirectory.FullName + "/" + uniqueDirectoryName;
		}
		return false;
	}

	public static string SanitizeSaveName(string saveName)
	{
		string pattern = "[?:*<>|\\\\/\"]";
		return Regex.Replace(saveName, pattern, "_");
	}

	public static void ForceIsSavingToFalse()
	{
		IsSaving = false;
	}

	private static async UniTask<SaveResult> SaveGame(SaveMethod saveMethod, string stationName, string saveFileName, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(stationName))
		{
			return SaveResult.Fail("Save Failed: Folder name is empty.");
		}
		if (IsSaving)
		{
			return SaveResult.Fail("Save Failed: Already saving.");
		}
		ConsoleWindow.Print($"Starting {saveMethod} for {stationName}");
		SaveResult result = await PrepareToSave(cancellationToken);
		if (!result.Success)
		{
			SavingFinished().Forget();
			return result;
		}
		object result2 = saveMethod switch
		{
			SaveMethod.NewSave => await DoNewSave(stationName, cancellationToken), 
			SaveMethod.Save => await DoSave(stationName, cancellationToken), 
			SaveMethod.SaveAs => await DoSaveAs(stationName, saveFileName, cancellationToken), 
			SaveMethod.AutoSave => await DoAutoSave(stationName, cancellationToken), 
			SaveMethod.QuickSave => await DoQuickSave(stationName, cancellationToken), 
			_ => SaveResult.Fail("Save Failed: Save type invalid"), 
		};
		SavingFinished().Forget();
		return (SaveResult)result2;
	}

	private static async UniTask<SaveResult> DoNewSave(string stationName, CancellationToken cancellationToken)
	{
		if (!CreateSaveDirectory(stationName, out var directoryInfo))
		{
			return SaveResult.Fail("Save Failed: Could not create save directory.");
		}
		string saveFileName = stationName + SaveLoadConstants.SaveFileExtension;
		return await Save(directoryInfo, saveFileName, newSave: true, cancellationToken);
	}

	private static async UniTask<SaveResult> DoSave(string stationName, CancellationToken cancellationToken)
	{
		if (!GetDirectory(stationName, out var directoryInfo))
		{
			return SaveResult.Fail("Save Failed: Could not find directory with name " + stationName + ".");
		}
		if (!GetHeadSave(directoryInfo, out var headSaveFile))
		{
			return SaveResult.Fail("Save Failed: Failed to get head save at " + directoryInfo.FullName + ".");
		}
		string saveFileName = headSaveFile.Name ?? "";
		return await Save(directoryInfo, saveFileName, newSave: false, cancellationToken);
	}

	private static async UniTask<SaveResult> DoSaveAs(string stationName, string saveFileName, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(saveFileName))
		{
			return SaveResult.Fail("Save Failed: Save file name is empty.");
		}
		if (!GetDirectory(stationName, out var directoryInfo))
		{
			return SaveResult.Fail("Save Failed: Could not find directory with name " + stationName + ".");
		}
		GetDirectory(stationName + "/" + SaveLoadConstants.ManualSaveFolder, out var manualSaveDirectoryInfo, create: true);
		if (!GetHeadSave(directoryInfo, out var headSaveFileInfo))
		{
			return SaveResult.Fail("Save Failed: Failed to get head save at " + directoryInfo.FullName + ".");
		}
		if (!GetUniqueFileName(manualSaveDirectoryInfo, saveFileName, SaveLoadConstants.SaveFileExtension, out var saveName))
		{
			return SaveResult.Fail("Save Failed: File name " + saveFileName + " is not unique.");
		}
		SaveResult result = await Save(manualSaveDirectoryInfo, saveName, newSave: false, cancellationToken);
		if (!result.Success)
		{
			return result;
		}
		SaveResult result2 = await CopyToHeadSave(manualSaveDirectoryInfo.FullName + "/" + saveName, headSaveFileInfo);
		if (!result2.Success)
		{
			return result2;
		}
		return SaveResult.Succeed;
	}

	private static async UniTask<SaveResult> DoQuickSave(string stationName, CancellationToken cancellationToken)
	{
		string folderPath = stationName + "/" + SaveLoadConstants.QuickSaveFolder;
		string saveName = DateTime.Now.ToString(SaveLoadConstants.DateTimeFormat) + "_quick" + SaveLoadConstants.SaveFileExtension;
		return await RollingSave(folderPath, saveName, Settings.CurrentData.MaxQuickSaves, cancellationToken);
	}

	private static async UniTask<SaveResult> DoAutoSave(string stationName, CancellationToken cancellationToken)
	{
		string folderPath = stationName + "/" + SaveLoadConstants.AutoSaveFolder;
		string saveName = DateTime.Now.ToString(SaveLoadConstants.DateTimeFormat) + "_auto" + SaveLoadConstants.SaveFileExtension;
		return await RollingSave(folderPath, saveName, Settings.CurrentData.MaxAutoSaves, cancellationToken);
	}

	private static async UniTask<SaveResult> RollingSave(string folderPath, string saveName, int maxSaves, CancellationToken cancellationToken)
	{
		GetDirectory(folderPath, out var directoryInfo, create: true);
		SaveResult result = await Save(directoryInfo, saveName, newSave: false, cancellationToken);
		RollSaveFiles(directoryInfo, maxSaves);
		return result;
	}

	private static async UniTask<SaveResult> PrepareToSave(CancellationToken cancellationToken)
	{
		if (GameManager.GameState != GameState.Running && GameManager.GameState != GameState.Paused)
		{
			return SaveResult.Fail("Save Failed: Game must be running to save");
		}
		IsSaving = true;
		_stopwatch.Restart();
		GameManager.PauseGameTick();
		LiquidSolver.Instance.ReturnLiquidsAndReset();
		SpinnerPannel.Instance.ShowSpinner(SpinnerPannelStrings.Saving);
		await UniTask.WaitUntil(() => GameManager.GameTickPaused, PlayerLoopTiming.Update, cancellationToken);
		ConsoleWindow.Print("Saving - game tick paused in " + StringManager.Get(_stopwatch.ElapsedMilliseconds) + "ms");
		_stopwatch.Restart();
		await UniTask.SwitchToThreadPool();
		AtmosphericsController.PreSaveCleanup();
		await UniTask.SwitchToMainThread();
		ConsoleWindow.Print("Saving - atmospheres cleaned up in " + StringManager.Get(_stopwatch.ElapsedMilliseconds) + "ms");
		return SaveResult.Succeed;
	}

	private static async UniTaskVoid SavingFinished()
	{
		await UniTask.SwitchToMainThread();
		GameManager.UnpauseGameTick();
		IsSaving = false;
		SpinnerPannel.Instance.HideSpinner(SpinnerPannelStrings.Done, 2f);
	}

	private static async UniTask<SaveResult> Save(DirectoryInfo saveDirectory, string saveFileName, bool newSave, CancellationToken cancellationToken)
	{
		_stopwatch.Restart();
		XmlSaveLoad.WorldData worldData;
		try
		{
			worldData = XmlSaveLoad.GetWorldData();
		}
		catch (Exception ex)
		{
			return SaveResult.Fail(ex.Message);
		}
		ConsoleWindow.Print("Saving - got world data in " + StringManager.Get(_stopwatch.ElapsedMilliseconds) + "ms");
		if (newSave)
		{
			worldData.Id = World.GenerateWorldId();
		}
		XmlSaveLoad.WorldMetaData metaData = worldData.GetMetaData(saveDirectory.Name, 0uL);
		ConsoleWindow.Print("Saving - unpausing game tick");
		GameManager.UnpauseGameTick();
		MemoryStream terrainMs = new MemoryStream();
		MemoryStream previewMs = new MemoryStream();
		MemoryStream screenShotMs = new MemoryStream();
		if (!GameManager.IsBatchMode)
		{
			_stopwatch.Restart();
			byte[] array = GameManager.CreateScreenShot(400, 400, CameraController.CurrentCamera);
			previewMs.Write(array, 0, array.Length);
			byte[] array2 = GameManager.CreateScreenShot(613, 266, CameraController.CurrentCamera);
			screenShotMs.Write(array2, 0, array2.Length);
			ConsoleWindow.Print("Saving - created preview images in " + StringManager.Get(_stopwatch.ElapsedMilliseconds) + "ms");
		}
		_stopwatch.Restart();
		string savePath = $"{saveDirectory}/{saveFileName}";
		try
		{
			await RunOnSaveThread(delegate
			{
				using ZipOutputStream zipOutputStream = new ZipOutputStream(File.Open(savePath, FileMode.Create, FileAccess.Write));
				zipOutputStream.SetLevel(SaveLoadConstants.ZipCompressionLevel);
				zipOutputStream.PutNextEntry(new ZipEntry(SaveLoadConstants.MetaFileName));
				Serializers.WorldMetaData.Serialize(zipOutputStream, metaData);
				zipOutputStream.CloseEntry();
				zipOutputStream.PutNextEntry(new ZipEntry(SaveLoadConstants.WorldFileName));
				Serializers.WorldData.Serialize(zipOutputStream, worldData);
				zipOutputStream.CloseEntry();
				VoxelTerrain.Serialize(terrainMs);
				AddZipFile(zipOutputStream, terrainMs, SaveLoadConstants.TerrainFileName);
				if (!GameManager.IsBatchMode)
				{
					AddZipFile(zipOutputStream, previewMs, SaveLoadConstants.PreviewFileName);
					AddZipFile(zipOutputStream, screenShotMs, SaveLoadConstants.ScreenshotFileName);
				}
				zipOutputStream.Finish();
			});
		}
		catch (Exception arg)
		{
			_stopwatch.Stop();
			return SaveResult.Fail($"Failed to write save file at path {savePath} : {arg}");
		}
		finally
		{
			terrainMs.Close();
			previewMs.Close();
			screenShotMs.Close();
		}
		ConsoleWindow.Print("Saving - serialized, zipped and file created in " + StringManager.Get(_stopwatch.ElapsedMilliseconds) + "ms");
		_stopwatch.Stop();
		await UniTask.SwitchToMainThread();
		return SaveResult.Succeed;
	}

	private static void AddZipFile(ZipOutputStream zipStream, Stream memoryStream, string fileName)
	{
		memoryStream.Seek(0L, SeekOrigin.Begin);
		ZipEntry entry = new ZipEntry(fileName);
		zipStream.PutNextEntry(entry);
		byte[] array = new byte[4096];
		int num;
		do
		{
			num = memoryStream.Read(array, 0, array.Length);
			zipStream.Write(array, 0, num);
		}
		while (num > 0);
	}

	private static UniTask RunOnSaveThread(Action work)
	{
		UniTaskCompletionSource completionSource = new UniTaskCompletionSource();
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				work();
				completionSource.TrySetResult();
			}
			catch (Exception exception)
			{
				completionSource.TrySetException(exception);
			}
		});
		thread.IsBackground = true;
		thread.Priority = ThreadPriority.BelowNormal;
		thread.Name = "Stationeers Save Writer";
		thread.Start();
		return completionSource.Task;
	}

	private static async UniTask<SaveResult> CopyToHeadSave(string manualSaveFullPath, FileInfo headSaveFileInfo)
	{
		_ = 2;
		try
		{
			SaveResult result;
			await using (FileStream sourceStream = File.OpenRead(manualSaveFullPath))
			{
				SaveResult succeed;
				await using (FileStream destinationStream = new FileStream(headSaveFileInfo.FullName, FileMode.Truncate, FileAccess.Write))
				{
					await sourceStream.CopyToAsync(destinationStream);
					succeed = SaveResult.Succeed;
				}
				result = succeed;
			}
			return result;
		}
		catch (Exception ex)
		{
			return SaveResult.Fail(ex.Message);
		}
	}

	private static bool GetHeadSave(DirectoryInfo stationDirectory, out FileInfo headSaveFile)
	{
		FileInfo[] files = stationDirectory.GetFiles(SaveLoadConstants.SaveFileSearchPattern);
		if (files.Length < 1)
		{
			headSaveFile = null;
			return false;
		}
		headSaveFile = files[0];
		return true;
	}

	private static void RollSaveFiles(DirectoryInfo directoryInfo, int maxCount)
	{
		if (directoryInfo.GetFiles().Length <= maxCount)
		{
			return;
		}
		List<(FileInfo, DateTime)> list = new List<(FileInfo, DateTime)>();
		FileInfo[] files = directoryInfo.GetFiles();
		foreach (FileInfo fileInfo in files)
		{
			if (!(fileInfo.Extension != SaveLoadConstants.SaveFileExtension) && fileInfo.Name.Length > SaveLoadConstants.DateTimeFormat.Length && DateTime.TryParseExact(fileInfo.Name.Substring(0, SaveLoadConstants.DateTimeFormat.Length), SaveLoadConstants.DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var result))
			{
				list.Add((fileInfo, result));
			}
		}
		list.Sort(((FileInfo fileInfo, DateTime dateTime) a, (FileInfo fileInfo, DateTime dateTime) b) => a.dateTime.CompareTo(b.dateTime));
		list[0].Item1.Delete();
	}

	private static bool GetDirectory(string name, out DirectoryInfo directoryInfo, bool create = false)
	{
		directoryInfo = null;
		DirectoryInfo savePathSavesSubDir = StationSaveUtils.GetSavePathSavesSubDir();
		string path = $"{savePathSavesSubDir}/{name}";
		if (!Directory.Exists(path))
		{
			if (!create)
			{
				return false;
			}
			directoryInfo = Directory.CreateDirectory(path);
			return true;
		}
		directoryInfo = new DirectoryInfo(path);
		return true;
	}

	private static bool GetUniqueFileName(DirectoryInfo directory, string fileName, string extension, out string uniqueFileName)
	{
		uniqueFileName = fileName + extension;
		string path = directory.FullName + "/" + uniqueFileName;
		for (int i = 2; i < 100; i++)
		{
			if (!File.Exists(path))
			{
				return true;
			}
			uniqueFileName = $"{fileName}_{i}{extension}";
			path = directory.FullName + "/" + uniqueFileName;
		}
		return false;
	}
}
