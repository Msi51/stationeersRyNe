using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Networking;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.UI;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using UI;
using UI.UIFade;
using UnityEngine;

namespace Assets.Scripts.Serialization;

public static class LoadHelper
{
	public static void LoadGame(string path, string stationName)
	{
		LoadGameTask(path, stationName).Forget();
	}

	private static async UniTaskVoid LoadGameTask(string path, string stationName)
	{
		try
		{
			string text = ExtractToTemp(path);
			StationSaveContainer currentWorldSave = new StationSaveContainer(new DirectoryInfo(text));
			XmlSaveLoad.Instance.CurrentWorldSave = currentWorldSave;
			Assets.Scripts.UI.MainMenu.Instance.SetActive(active: false);
			ImGuiLoadingScreen.SetActive(active: true);
			HelperHintsTextController.RefreshDisplayState(isLoadingWorld: true);
			XmlSaveLoad.IsReadyToPlayWorldAudio = false;
			XmlSaveLoad.Instance.CurrentStationName = stationName;
			await LoadWorldTask(text);
		}
		catch (Exception exception)
		{
			ConsoleWindow.PrintError(exception).Forget();
			XmlSaveLoad.Instance.CurrentWorldSave = null;
			XmlSaveLoad.Instance.CurrentStationName = null;
			XmlSaveLoad.IsReadyToPlayWorldAudio = false;
			Singleton<ConfirmationPanel>.Instance.Show("FailedToStartGameMessageTitle", "FailedToStartGameMessageText", "ButtonExit", delegate
			{
				GameManager.LeaveGame();
				FadePanel.ToTransparentInstant();
			});
		}
	}

	public static async UniTask<List<(SaveFileInfo saveFileInfo, ulong workshopId)>> GetWorkshopSaves()
	{
		IReadOnlyList<SteamTransport.ItemWrapper> obj = await NetworkManager.GetLocalAndWorkshopItems(SteamTransport.WorkshopType.World);
		List<(SaveFileInfo, ulong)> list = new List<(SaveFileInfo, ulong)>();
		foreach (SteamTransport.ItemWrapper item in obj)
		{
			FileInfo[] files = new DirectoryInfo(item.DirectoryPath).GetFiles(SaveLoadConstants.SaveFileSearchPattern);
			if (files.Length != 0)
			{
				list.Add((new SaveFileInfo(new FileInfo(files[0].FullName), SaveType.Workshop), item.Id));
			}
		}
		return list;
	}

	public static List<SaveInfo> GetLocalSaves()
	{
		List<SaveInfo> list = new List<SaveInfo>();
		DirectoryInfo[] directories = StationSaveUtils.GetSavePathSavesSubDir().GetDirectories();
		foreach (DirectoryInfo directoryInfo in directories)
		{
			FileInfo[] files = directoryInfo.GetFiles(SaveLoadConstants.SaveFileSearchPattern);
			if (files.Length != 1)
			{
				continue;
			}
			string name = directoryInfo.Name;
			List<SaveFileInfo> list2 = new List<SaveFileInfo>();
			list2.Add(new SaveFileInfo(files[0], SaveType.Head));
			DirectoryInfo directoryInfo2 = new DirectoryInfo(directoryInfo.FullName + "/" + SaveLoadConstants.QuickSaveFolder);
			DirectoryInfo directoryInfo3 = new DirectoryInfo(directoryInfo.FullName + "/" + SaveLoadConstants.AutoSaveFolder);
			DirectoryInfo directoryInfo4 = new DirectoryInfo(directoryInfo.FullName + "/" + SaveLoadConstants.ManualSaveFolder);
			int manualSaveCount = 0;
			int quickSaveCount = 0;
			int autoSaveCount = 0;
			try
			{
				if (directoryInfo2.Exists)
				{
					FileInfo[] files2 = directoryInfo2.GetFiles(SaveLoadConstants.SaveFileSearchPattern);
					quickSaveCount = files2.Length;
					FileInfo[] array = files2;
					foreach (FileInfo fileInfo in array)
					{
						list2.Add(new SaveFileInfo(fileInfo, SaveType.Quick));
					}
				}
				if (directoryInfo3.Exists)
				{
					FileInfo[] files3 = directoryInfo3.GetFiles(SaveLoadConstants.SaveFileSearchPattern);
					autoSaveCount = files3.Length;
					FileInfo[] array = files3;
					foreach (FileInfo fileInfo2 in array)
					{
						list2.Add(new SaveFileInfo(fileInfo2, SaveType.Auto));
					}
				}
				if (directoryInfo4.Exists)
				{
					FileInfo[] files4 = directoryInfo4.GetFiles(SaveLoadConstants.SaveFileSearchPattern);
					manualSaveCount = files4.Length;
					FileInfo[] array = files4;
					foreach (FileInfo fileInfo3 in array)
					{
						list2.Add(new SaveFileInfo(fileInfo3, SaveType.Manual));
					}
				}
			}
			catch (Exception arg)
			{
				ConsoleWindow.PrintError($"Error getting saves: {arg}");
				continue;
			}
			list.Add(new SaveInfo
			{
				StationName = name,
				StationSaveDirectory = directoryInfo,
				Saves = list2,
				ManualSaveCount = manualSaveCount,
				QuickSaveCount = quickSaveCount,
				AutoSaveCount = autoSaveCount
			});
		}
		return list;
	}

	public static List<SaveFileInfo> GetManualSavesForCurrentStation()
	{
		List<SaveFileInfo> list = new List<SaveFileInfo>();
		DirectoryInfo savePathSavesSubDir = StationSaveUtils.GetSavePathSavesSubDir();
		DirectoryInfo directoryInfo = new DirectoryInfo(savePathSavesSubDir.FullName + "/" + XmlSaveLoad.Instance.CurrentStationName + "/" + SaveLoadConstants.ManualSaveFolder);
		if (!directoryInfo.Exists)
		{
			return list;
		}
		FileInfo[] files = directoryInfo.GetFiles(SaveLoadConstants.SaveFileSearchPattern);
		foreach (FileInfo fileInfo in files)
		{
			list.Add(new SaveFileInfo(fileInfo, SaveType.Manual));
		}
		return list;
	}

	public static bool UnzipMetaData(FileInfo saveFileInfo, out XmlSaveLoad.WorldMetaData metaData)
	{
		using FileStream file = File.OpenRead(saveFileInfo.FullName);
		using ZipFile zipFile = new ZipFile(file);
		ZipEntry entry = zipFile.GetEntry(SaveLoadConstants.MetaFileName);
		if (entry == null)
		{
			ConsoleWindow.PrintError(SaveLoadConstants.MetaFileName + " not found in " + saveFileInfo.Name);
			metaData = null;
			return false;
		}
		using Stream stream = zipFile.GetInputStream(entry);
		metaData = (XmlSaveLoad.WorldMetaData)Serializers.WorldMetaData.Deserialize(stream);
		return true;
	}

	public static bool UnzipPreviewImage(FileInfo saveFileInfo, out Texture2D texture)
	{
		using FileStream file = File.OpenRead(saveFileInfo.FullName);
		using ZipFile zipFile = new ZipFile(file);
		ZipEntry entry = zipFile.GetEntry(SaveLoadConstants.PreviewFileName);
		if (entry == null)
		{
			ConsoleWindow.PrintError(SaveLoadConstants.PreviewFileName + " not found in " + saveFileInfo.Name);
			texture = null;
			return false;
		}
		using Stream stream = zipFile.GetInputStream(entry);
		using MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		byte[] data = memoryStream.ToArray();
		texture = new Texture2D(1, 1);
		if (texture.LoadImage(data))
		{
			return true;
		}
		texture = null;
		return false;
	}

	private static async UniTask LoadWorldTask(string tempDir)
	{
		await XmlSaveLoad.LoadWorld();
		Directory.Delete(tempDir, recursive: true);
	}

	private static string ExtractToTemp(string path)
	{
		string text = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(text);
		using ZipInputStream zipInputStream = new ZipInputStream(File.OpenRead(path));
		while (true)
		{
			ZipEntry nextEntry = zipInputStream.GetNextEntry();
			if (nextEntry == null)
			{
				break;
			}
			using FileStream destination = File.Create(Path.Combine(text, Path.GetFileName(nextEntry.Name)));
			zipInputStream.CopyTo(destination);
		}
		return text;
	}
}
