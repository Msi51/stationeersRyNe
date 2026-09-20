using Assets.Scripts;
using Cysharp.Threading.Tasks;

namespace Util.Commands;

public class DifficultySettingsCommand : CommandBase
{
	public override string HelpText => "Prints the current difficulty setting, or applies a new one when a name is provided. Setting a new difficulty is host or singleplayer only.";

	public override string[] Arguments => new string[1] { "[difficulty]" };

	public override bool IsLaunchCmd => true;

	private async UniTaskVoid WaitExecute(string[] args)
	{
		await UniTask.WaitUntil(() => GameManager.IsInitialized);
		Execute(args);
	}

	public override string Execute(string[] args)
	{
		if (args.Length != 1)
		{
			if (DifficultySetting.Current == null)
			{
				ConsoleWindow.PrintError("No difficulty is loaded.", suppressStacktrace: true);
				return null;
			}
			DifficultySetting.Current.DebugPrint();
			return null;
		}
		string text = args[0];
		if (!GameManager.IsInitialized)
		{
			ConsoleWindow.PrintAction("Loading difficulty '" + text + "' on start.");
			WaitExecute(args).Forget();
			return null;
		}
		if (CommandBase.CannotAsClient("difficulty"))
		{
			return null;
		}
		DifficultySetting difficultySetting = DifficultySetting.Find(text);
		if (difficultySetting == null)
		{
			ConsoleWindow.PrintError("No difficulty found with name '" + text + "'.", suppressStacktrace: true);
			return null;
		}
		DifficultySetting.SetCurrent(difficultySetting);
		ConsoleWindow.PrintAction("Set difficulty to '" + difficultySetting.Id + "'.");
		return null;
	}
}
