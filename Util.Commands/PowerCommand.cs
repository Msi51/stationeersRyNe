using System;
using Assets.Scripts;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;

namespace Util.Commands;

public class PowerCommand : CommandBase
{
	private enum PowerArguments : byte
	{
		None,
		ChargeAll
	}

	private EnumCollection<PowerArguments, byte> _powerArguments = new EnumCollection<PowerArguments, byte>(toProper: false);

	private static Action<IPowered> _chargeAllAction = delegate(IPowered powered)
	{
		if (powered is IChargable chargable)
		{
			chargable.PowerStored = chargable.GetPowerMaximum();
		}
	};

	public override string HelpText => "Power debug helpers. 'chargeall' fills every IChargable in the world to its maximum stored power; only available in creative mode and on the server.";

	public override string[] Arguments => _powerArguments.Names;

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("power"))
		{
			return null;
		}
		if (args.Length == 0)
		{
			ConsoleWindow.PrintError("Power command requires an argument.", suppressStacktrace: true);
			return null;
		}
		if (!Enum.TryParse<PowerArguments>(args[0], ignoreCase: true, out var result))
		{
			ConsoleWindow.PrintError("'" + args[0] + "' is not a valid argument.", suppressStacktrace: true);
			return null;
		}
		if (result == PowerArguments.ChargeAll)
		{
			if (!DifficultySetting.Current.Creative && !GameManager.IsBatchMode)
			{
				ConsoleWindow.PrintError($"'{result}' is only available in creative mode.", suppressStacktrace: true);
				return null;
			}
			if (!GameManager.RunSimulation)
			{
				ConsoleWindow.PrintError($"'{result}' can only be used on the server.", suppressStacktrace: true);
				return null;
			}
			ElectricityManager.AllPoweredThings.ForEach(_chargeAllAction);
			ConsoleWindow.PrintAction("Charged all batteries.");
		}
		else
		{
			ConsoleWindow.PrintError("'" + args[0] + "' not implemented.", suppressStacktrace: true);
		}
		return null;
	}
}
