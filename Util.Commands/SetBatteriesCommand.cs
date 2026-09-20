using System;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;

namespace Util.Commands;

public class SetBatteriesCommand : CommandBase
{
	public override string HelpText => "Sets every battery cell in the world to the given charge state (defaults to 'Full' if no state is given). Cannot be run as a client.";

	public override string[] Arguments => EnumCollections.BatteryCellStates.Names;

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("setbatteries"))
		{
			return null;
		}
		BatteryCellState result = BatteryCellState.Full;
		if (args.Length == 1 && !Enum.TryParse<BatteryCellState>(args[0], ignoreCase: true, out result))
		{
			ConsoleWindow.PrintError("Invalid battery cell state '" + args[0] + "'.", suppressStacktrace: true);
			return null;
		}
		int num = 0;
		DensePool<Thing>.ActiveEnumerable.Enumerator enumerator = OcclusionManager.AllThings.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			Thing current = enumerator.Current;
			if ((bool)current && !current.IsBeingDestroyed && current is IChargable chargable)
			{
				IChargable.SetPower(chargable, result);
				num++;
			}
		}
		ConsoleWindow.PrintAction($"Set {num} batteries to {EnumCollections.BatteryCellStates.GetName(result)}.");
		return null;
	}
}
