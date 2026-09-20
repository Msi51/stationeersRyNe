using Assets.Scripts;
using Assets.Scripts.Util;

namespace Util.Commands;

public class DensePoolCommand : CommandBase
{
	private enum DensePoolArg : byte
	{
		NullCheck,
		Summary,
		Other
	}

	private static readonly EnumCollection<DensePoolArg, byte> DensePoolArgs = new EnumCollection<DensePoolArg, byte>();

	public override string HelpText => "Lists or inspects the state of dense pools. With no argument prints all pools; otherwise checks for nulls, summarises a pool, or prints other tracked lists. Dev tool.";

	public override string[] Arguments => DensePoolArgs.Names;

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("densepool"))
		{
			return null;
		}
		if (args == null || args.Length == 0)
		{
			ConsoleWindow.PrintAction("Listing dense pool state.");
			DensePools.PrintAllPools();
			return null;
		}
		DensePoolArg densePoolArg = DensePoolArgs.Get(args[0]);
		string text = string.Empty;
		if (args.Length > 1)
		{
			text = args[1];
		}
		switch (densePoolArg)
		{
		case DensePoolArg.NullCheck:
			ConsoleWindow.PrintAction("Checking for null references in dense pools.");
			DensePools.PrintAllNullsInPools();
			return null;
		case DensePoolArg.Summary:
			ConsoleWindow.PrintAction("Summarising all dense pool contents for '" + text + "'.");
			DensePools.PrintAllSummarys(text);
			return null;
		case DensePoolArg.Other:
		{
			ConsoleWindow.PrintAction("Summarising non-dense lists.");
			int count = OcclusionManager.UpdatingThings100MS.Count;
			int count2 = OcclusionManager.UpdatingThings1000MS.Count;
			ConsoleWindow.Print($"Update100MS:\t{count}");
			ConsoleWindow.Print($"Update1000MS:\t{count2}");
			return null;
		}
		default:
			ConsoleWindow.PrintError($"Argument '{densePoolArg}' is not implemented.", suppressStacktrace: true);
			return null;
		}
	}
}
