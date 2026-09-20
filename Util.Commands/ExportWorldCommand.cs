using Assets.Scripts;

namespace Util.Commands;

public class ExportWorldCommand : CommandBase
{
	public override string HelpText => "Exports the current world to a new WorldSettings entry under the StreamingAssets folder. Optionally flags the export as a tutorial and chooses whether to save prefabs.";

	public override string[] Arguments => new string[3] { "<id>", "[tutorial | true | false]", "[savePrefabs]" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("exportworld"))
		{
			return null;
		}
		int num = args.Length;
		if (num == 0 || num > 2)
		{
			return "Invalid syntax";
		}
		string text = args[0];
		if (string.IsNullOrEmpty(text))
		{
			ConsoleWindow.PrintError("No valid id supplied.", suppressStacktrace: true);
			return null;
		}
		if (!DataCollection.IsUniqueId(text) || WorldSettingData.Find(text) != null)
		{
			ConsoleWindow.PrintError("Id already exists.", suppressStacktrace: true);
			return null;
		}
		bool isTutorial = false;
		if (args.Length == 2)
		{
			bool result;
			if (string.Equals(args[1].ToLower(), "tutorial"))
			{
				isTutorial = true;
			}
			else if (bool.TryParse(args[1], out result))
			{
				isTutorial = result;
			}
		}
		bool result2 = true;
		if (args.Length == 3)
		{
			bool.TryParse(args[2], out result2);
		}
		if (result2)
		{
			WorldSettingData.SaveNewScenarioWorld(text, isTutorial).Forget();
		}
		else
		{
			WorldSettingData.SaveWorldSetting(text, WorldSetting.Current.Data);
		}
		return null;
	}
}
