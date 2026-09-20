using System.IO;

namespace UI;

public interface ICloudSyncable
{
	DirectoryInfo RootDir { get; set; }

	void SendToSteamCloud();

	void RemoveFromSteamCloud();

	void DeleteFromSteamCloud();
}
