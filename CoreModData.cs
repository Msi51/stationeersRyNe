using System.IO;
using UnityEngine;

public class CoreModData : ModData
{
	public override DirectoryInfo GameDataFolder()
	{
		return new DirectoryInfo(Path.Combine(Application.streamingAssetsPath, "Data"));
	}
}
