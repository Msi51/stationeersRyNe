using System;
using Assets.Scripts;
using Assets.Scripts.Objects;

namespace Util.Commands;

public class StructureCommand : CommandBase
{
	private static readonly Action<Structure> CompleteAction = delegate(Structure structure)
	{
		if (structure.CurrentBuildStateIndex < structure.BuildStates.Count - 1)
		{
			structure.UpdateBuildStateAndVisualizer(structure.BuildStates.Count - 1);
		}
	};

	public override string HelpText => "Runs structure debug actions. 'completeall' advances every structure in the world to its final build state. Host or dedicated server only.";

	public override string[] Arguments => new string[1] { "<completeall>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("structure"))
		{
			return null;
		}
		if (args.Length == 0)
		{
			return "Invalid syntax";
		}
		if (args[0] == "completeall")
		{
			return HandleCompleteAll();
		}
		return "Invalid syntax";
	}

	private string HandleCompleteAll()
	{
		GridController.AllStructuresPool.ForEach(CompleteAction);
		return "All structures set to final build state.";
	}
}
