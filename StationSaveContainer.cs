using System.IO;
using System.Linq;
using Assets.Scripts.Serialization;
using Steamworks;
using UI;

public class StationSaveContainer : ICloudSyncable
{
	public readonly FileInfo World;

	public readonly FileInfo WorldMeta;

	public readonly FileInfo WorldBin;

	public readonly FileInfo TerrainData;

	public readonly FileInfo Screenshot;

	public readonly FileInfo ScreenshotPreview;

	public readonly bool IsBackup;

	public readonly int Index;

	public string Name => Directory().Name;

	public DirectoryInfo RootDir { get; set; }

	public DirectoryInfo Directory()
	{
		if (!IsValid())
		{
			return null;
		}
		DirectoryInfo directoryInfo = World.Directory;
		if (directoryInfo == null)
		{
			return null;
		}
		if (directoryInfo.FullName.Contains("Backup"))
		{
			directoryInfo = directoryInfo.Parent;
		}
		return directoryInfo;
	}

	public StationSaveContainer(DirectoryInfo dir)
	{
		IsBackup = false;
		Index = -1;
		RootDir = dir;
		FileInfo[] files = dir.GetFiles();
		World = files.FirstOrDefault((FileInfo x) => x.Name == "world.xml");
		WorldMeta = files.FirstOrDefault((FileInfo x) => x.Name == "world_meta.xml");
		WorldBin = files.FirstOrDefault((FileInfo x) => x.Name == "world.bin");
		Screenshot = files.FirstOrDefault((FileInfo x) => x.Name == "screenshot.png");
		ScreenshotPreview = files.FirstOrDefault((FileInfo x) => x.Name == "preview.png");
		TerrainData = files.FirstOrDefault((FileInfo x) => x.Name == "terrain.dat");
	}

	public StationSaveContainer(DirectoryInfo dir, int index)
	{
		IsBackup = true;
		Index = index;
		FileInfo[] files = dir.GetFiles();
		World = files.FirstOrDefault((FileInfo x) => x.Name.Replace(XmlSaveLoad.AutoSave, string.Empty) == $"world({index}).xml");
		WorldMeta = files.FirstOrDefault((FileInfo x) => x.Name.Replace(XmlSaveLoad.AutoSave, string.Empty) == $"world_meta({index}).xml");
		WorldBin = files.FirstOrDefault((FileInfo x) => x.Name.Replace(XmlSaveLoad.AutoSave, string.Empty) == $"world({index}).bin");
		Screenshot = files.FirstOrDefault((FileInfo x) => x.Name.Replace(XmlSaveLoad.AutoSave, string.Empty) == $"screenshot({index}).png");
		ScreenshotPreview = files.FirstOrDefault((FileInfo x) => x.Name.Replace(XmlSaveLoad.AutoSave, string.Empty) == $"preview({index}).png");
		TerrainData = files.FirstOrDefault((FileInfo x) => x.Name.Replace(XmlSaveLoad.AutoSave, string.Empty) == $"terrain({index}).dat");
	}

	public bool IsValid()
	{
		FileInfo world = World;
		if (world != null && world.Exists)
		{
			return WorldMeta?.Exists ?? false;
		}
		return false;
	}

	public void SendToSteamCloud()
	{
		if (IsValid() && SteamClient.IsValid && SteamRemoteStorage.IsCloudEnabled)
		{
			SteamRemoteStorage.FileWrite(this.GetCloudFileName(World), File.ReadAllBytes(World.FullName));
			SteamRemoteStorage.FileWrite(this.GetCloudFileName(WorldMeta), File.ReadAllBytes(WorldMeta.FullName));
			SteamRemoteStorage.FileWrite(this.GetCloudFileName(WorldBin), File.ReadAllBytes(WorldBin.FullName));
			SteamRemoteStorage.FileWrite(this.GetCloudFileName(Screenshot), File.ReadAllBytes(Screenshot.FullName));
			SteamRemoteStorage.FileWrite(this.GetCloudFileName(ScreenshotPreview), File.ReadAllBytes(ScreenshotPreview.FullName));
			SteamRemoteStorage.FileWrite(this.GetCloudFileName(TerrainData), File.ReadAllBytes(TerrainData.FullName));
		}
	}

	public void RemoveFromSteamCloud()
	{
		if (IsValid() && SteamClient.IsValid && SteamRemoteStorage.IsCloudEnabled)
		{
			SteamRemoteStorage.FileForget(this.GetCloudFileName(World));
			SteamRemoteStorage.FileForget(this.GetCloudFileName(WorldMeta));
			SteamRemoteStorage.FileForget(this.GetCloudFileName(WorldBin));
			SteamRemoteStorage.FileForget(this.GetCloudFileName(Screenshot));
			SteamRemoteStorage.FileForget(this.GetCloudFileName(ScreenshotPreview));
			SteamRemoteStorage.FileForget(this.GetCloudFileName(TerrainData));
		}
	}

	public void DeleteFromSteamCloud()
	{
		if (IsValid() && SteamClient.IsValid && SteamRemoteStorage.IsCloudEnabled)
		{
			SteamRemoteStorage.FileDelete(this.GetCloudFileName(World));
			SteamRemoteStorage.FileDelete(this.GetCloudFileName(WorldMeta));
			SteamRemoteStorage.FileDelete(this.GetCloudFileName(WorldBin));
			SteamRemoteStorage.FileDelete(this.GetCloudFileName(Screenshot));
			SteamRemoteStorage.FileDelete(this.GetCloudFileName(ScreenshotPreview));
			SteamRemoteStorage.FileDelete(this.GetCloudFileName(TerrainData));
		}
	}

	public void DeleteFiles()
	{
		World?.Delete();
		WorldMeta?.Delete();
		WorldBin?.Delete();
		Screenshot?.Delete();
		ScreenshotPreview?.Delete();
		TerrainData?.Delete();
	}
}
