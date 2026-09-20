using System.IO;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;

namespace Util.Commands;

internal class SettingsCommand : ClassManipulator<Settings.SettingData>
{
	public override string HelpText => "Reads or writes values in settings.xml at runtime. Use 'list' to enumerate property names, 'print' to dump values, '<PropertyName>' to read one, or '<PropertyName> <Value>' to set one (e.g. 'settings ServerMaxPlayers 5').";

	protected override Settings.SettingData ObjectInstance => Settings.CurrentData;

	protected override void OnValueChanged()
	{
		EnsureExistence();
		NetworkManager.UpdateSessionData(ObjectInstance);
		Settings.SaveSettings();
	}

	private static void EnsureExistence()
	{
		FileInfo fileInfo = new FileInfo(Settings.SettingData.Path);
		DirectoryInfo directory = fileInfo.Directory;
		if (directory != null && !directory.Exists)
		{
			fileInfo.Directory.Create();
		}
		if (!fileInfo.Exists)
		{
			fileInfo.Create();
		}
	}
}
