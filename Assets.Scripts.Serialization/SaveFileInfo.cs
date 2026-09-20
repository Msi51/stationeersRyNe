using System.IO;

namespace Assets.Scripts.Serialization;

public struct SaveFileInfo(FileInfo fileInfo, SaveType saveType)
{
	public SaveType SaveType = saveType;

	public FileInfo FileInfo = fileInfo;
}
