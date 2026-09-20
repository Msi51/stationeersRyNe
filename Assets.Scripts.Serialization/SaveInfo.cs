using System.Collections.Generic;
using System.IO;

namespace Assets.Scripts.Serialization;

public struct SaveInfo
{
	public string StationName;

	public DirectoryInfo StationSaveDirectory;

	public List<SaveFileInfo> Saves;

	public int ManualSaveCount;

	public int QuickSaveCount;

	public int AutoSaveCount;
}
