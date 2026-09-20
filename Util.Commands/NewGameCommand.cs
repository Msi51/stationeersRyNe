using System.Linq;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;

namespace Util.Commands;

public class NewGameCommand : CommandBase
{
	private const string ARG_WORLDNAME = "<worldname>";

	private const string ARG_DIFFICULTY = "[difficulty]";

	private const string ARG_START_CONDITION = "[startcondition]";

	public override string HelpText => "Starts a new game on the named world from launch; difficulty defaults to 'Normal' and start condition to the world's default if omitted.";

	public override string[] Arguments => new string[3] { "<worldname>", "[difficulty]", "[startcondition]" };

	public override bool IsLaunchCmd => true;

	public override bool RequiresGameManagerIsInitialized => true;

	public override string Execute(string[] args)
	{
		if (args.Length == 0)
		{
			ConsoleWindow.PrintError("Must provide a world name to create a new world. Valid worlds: " + AllWorldsList() + ".", suppressStacktrace: true);
			return null;
		}
		string text = "Moon";
		if (args.Length >= 1)
		{
			text = args[0];
		}
		string text2 = "Normal";
		if (args.Length >= 2)
		{
			text2 = args[1];
		}
		string text3 = "Default";
		if (args.Length >= 3)
		{
			text3 = args[2];
		}
		ConsoleWindow.PrintAction("Creating new world '" + text + "' with difficulty '" + text2 + "' and start condition '" + text3 + "'.");
		ExecuteAsync(text, text2, text3).Forget();
		return null;
	}

	public static string AllWorldsList()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < WorldSetting.AllWorldSettings.Count; i++)
		{
			WorldSetting worldSetting = WorldSetting.AllWorldSettings[i];
			if (!worldSetting.IsTutorial)
			{
				stringBuilder.Append(worldSetting.Id);
				if (worldSetting.IsDeprecated)
				{
					stringBuilder.Append(" (Deprecated)");
				}
				if (i < WorldSetting.AllWorldSettings.Count - 1)
				{
					stringBuilder.Append(", ");
				}
			}
		}
		return stringBuilder.ToString();
	}

	private static async UniTask ExecuteAsync(string worldName, string difficulty, string startConditionName)
	{
		WorldSetting worldSetting = WorldSetting.Find(worldName);
		if (worldSetting == null)
		{
			ConsoleWindow.PrintError("No such world name: " + worldName + ". Valid worlds: " + AllWorldsList() + ".", suppressStacktrace: true);
			return;
		}
		DifficultySetting difficultySetting = DifficultySetting.Find(difficulty);
		if (difficultySetting == null)
		{
			ConsoleWindow.PrintError("No such difficulty name: " + difficulty + ". Valid difficulties: " + string.Join(", ", DifficultySetting.AllSettings.Select((DifficultySetting x) => x.Id)) + ".", suppressStacktrace: true);
			return;
		}
		if (string.Equals("default", startConditionName.ToLower()))
		{
			foreach (StartConditionData startConditionData2 in worldSetting.Data.StartConditionDatas)
			{
				if (startConditionData2.IsDefault)
				{
					startConditionName = startConditionData2.Id;
					break;
				}
			}
		}
		StartConditionData startConditionData = DataCollection.Get<StartConditionData>(startConditionName);
		if (startConditionData == null)
		{
			ConsoleWindow.PrintError("No such start condition name: " + startConditionName + ". Valid start conditions are: " + string.Join(", ", worldSetting.Data.StartConditionDatas.Select((StartConditionData x) => x.Id)) + ".", suppressStacktrace: true);
		}
		else
		{
			WorldSetting.SetCurrent(worldSetting, startConditionData);
			DifficultySetting.SetCurrent(difficultySetting);
			ImGuiLoadingScreen.WorldName = worldSetting.Id;
			await World.StartNewWorld(worldSetting.Id);
			ConsoleWindow.PrintAction("Started new game in world " + worldName + ".");
		}
	}
}
