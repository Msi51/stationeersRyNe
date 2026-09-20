using System;
using System.IO;
using Assets.Scripts.Serialization;

namespace Assets.Scripts.Networking.Transports;

public static class WorkshopUtils
{
	public static DirectoryInfo GetLocalDirInfo(this SteamTransport.WorkshopType workshopType)
	{
		return workshopType switch
		{
			SteamTransport.WorkshopType.World => new DirectoryInfo(Settings.CurrentData.SavePath + "/saves"), 
			SteamTransport.WorkshopType.Mod => new DirectoryInfo(Settings.CurrentData.SavePath + "/mods"), 
			SteamTransport.WorkshopType.ICCode => new DirectoryInfo(Settings.CurrentData.SavePath + "/scripts"), 
			_ => throw new ArgumentOutOfRangeException("workshopType", workshopType, null), 
		};
	}

	public static string GetLocalFileName(this SteamTransport.WorkshopType workshopType)
	{
		return workshopType switch
		{
			SteamTransport.WorkshopType.World => "world.xml", 
			SteamTransport.WorkshopType.Mod => "About.xml", 
			SteamTransport.WorkshopType.ICCode => "instruction.xml", 
			_ => throw new ArgumentOutOfRangeException("workshopType", workshopType, null), 
		};
	}
}
