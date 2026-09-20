using System;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Networks;
using TraderUI;
using Trading;

namespace Util.Commands;

public class TraderCommand : CommandBase
{
	private enum DebugAction
	{
		Buys,
		Sells,
		Evaluate
	}

	private const string ARG_REGENERATE = "regenerate";

	private const string ARG_CALL = "call";

	private const string ARG_LAND = "land";

	private const string ARG_DEPART = "depart";

	private const string ARG_CONTACTS = "contacts";

	private const string ARG_BUYS = "buys";

	private const string ARG_SELLS = "sells";

	private const string ARG_EVALUATE = "evaluate";

	private const string ARG_CHECKSUM = "checksum";

	private const string ARG_SHOWALL = "showall";

	public override string HelpText => "Debugs traders. 'regenerate' resets the contact slots; 'land [padId] [slotIndex]' summons a trader to a landing pad; 'depart [padId]' sends a trader away; 'contacts' prints all station contacts; 'buys/sells/evaluate [padId]' prints trade details for a landed trader; 'checksum' prints trader data checksums; 'showall' opens a read-only trader window listing every trade item (each Select set expanded) for visual debugging. Host or dedicated server only.";

	public override string[] Arguments => new string[9] { "regenerate", "land", "depart", "contacts", "buys", "sells", "evaluate", "checksum", "showall" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("trader"))
		{
			return null;
		}
		if (args.Length == 0)
		{
			return "Invalid arguments";
		}
		return args[0] switch
		{
			"call" => CallTrader(args), 
			"regenerate" => HandleRegenerate(), 
			"land" => HandleLand(args), 
			"depart" => HandleDepart(args), 
			"contacts" => PrintTraderInfo(), 
			"buys" => PrintTrader(args, DebugAction.Buys), 
			"sells" => PrintTrader(args, DebugAction.Sells), 
			"evaluate" => PrintTrader(args, DebugAction.Evaluate), 
			"checksum" => PrintCheckSum(), 
			"showall" => ShowAllDebugWindow(), 
			_ => "Invalid arguments", 
		};
	}

	private string ShowAllDebugWindow()
	{
		if (GameManager.IsBatchMode)
		{
			ConsoleWindow.PrintError("'trader showall' is not available in batch mode.", suppressStacktrace: true);
			return null;
		}
		if (TraderCanvas.Instance == null)
		{
			ConsoleWindow.PrintError("Trader window is not available.", suppressStacktrace: true);
			return null;
		}
		TradeData tradeData = TraderDebugData.BuildAll();
		TraderCanvas.Instance.ShowDebug(tradeData);
		return $"Showing {tradeData.Buying.Count} buyable and {tradeData.Selling.Count} sellable trade items across {TraderData.AllTraderData.Count} traders. Hover an item for its conditions/meta; trading is disabled.";
	}

	private string PrintCheckSum()
	{
		foreach (TraderData allTraderDatum in TraderData.AllTraderData)
		{
			ConsoleWindow.Print("Trader " + allTraderDatum.Id + " CheckSum: " + allTraderDatum.TraderChecksum());
		}
		return null;
	}

	private string PrintTrader(string[] args, DebugAction action)
	{
		ITraderDestination landingPad = GetLandingPad(args);
		if (landingPad == null)
		{
			ConsoleWindow.PrintError("No landing pad center found.", suppressStacktrace: true);
			return null;
		}
		TraderContact currentTradingContact = landingPad.CurrentTradingContact;
		if (landingPad.CurrentTradingContact == null)
		{
			ConsoleWindow.PrintError("Landing pad '" + landingPad.DisplayName + "' has no trader landed.", suppressStacktrace: true);
			return null;
		}
		ConsoleWindow.PrintAction($"Printing {action} trade details for '{currentTradingContact.DisplayName}'.");
		switch (action)
		{
		case DebugAction.Buys:
			currentTradingContact.PrintBuyTrade();
			break;
		case DebugAction.Sells:
			currentTradingContact.PrintSellTrade();
			break;
		case DebugAction.Evaluate:
			currentTradingContact.PrintEvaluateTrade();
			break;
		default:
			throw new ArgumentOutOfRangeException("action", action, null);
		}
		return null;
	}

	private string PrintTraderInfo()
	{
		ConsoleWindow.PrintAction($"Printing details for {TraderContact.AllStationContacts.Count} station contacts.");
		foreach (TraderContact allStationContact in TraderContact.AllStationContacts)
		{
			allStationContact.PrintDebugInfo();
		}
		return null;
	}

	private string CallTrader(string[] args)
	{
		if (RunOnClient())
		{
			return null;
		}
		if (args.Length != 2)
		{
			ConsoleWindow.PrintError("Invalid arguments", suppressStacktrace: true);
			return null;
		}
		string text = args[1];
		TraderData traderData = null;
		foreach (TraderData allTraderDatum in TraderData.AllTraderData)
		{
			if (string.Equals(allTraderDatum.Id, text))
			{
				traderData = allTraderDatum;
			}
		}
		if (traderData == null)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("TraderData Id not found. Valid Ids are: ");
			foreach (TraderData allTraderDatum2 in TraderData.AllTraderData)
			{
				stringBuilder.Append(allTraderDatum2.Id).Append(", ");
			}
			stringBuilder.Append(".");
			ConsoleWindow.Print(stringBuilder.ToString());
			return null;
		}
		ContactSlot.ContactSlots[0].TryGenerateNewContact(traderData, force: true);
		ConsoleWindow.PrintAction("Generating new contact id: " + text + " name: " + ContactSlot.ContactSlots[0].CurrentContact.DisplayName + ".");
		HandleLand(new string[1] { "" });
		return null;
	}

	private string HandleRegenerate()
	{
		if (RunOnClient())
		{
			return null;
		}
		ContactSlot.CommandResetSlots();
		return "Regenerated traders.";
	}

	private bool RunOnClient()
	{
		if (!GameManager.RunSimulation)
		{
			ConsoleWindow.PrintError("Command cannot be run on clients.", suppressStacktrace: true);
			return true;
		}
		return false;
	}

	private ITraderDestination GetLandingPad(string[] args)
	{
		ITraderDestination traderDestination = null;
		if (args.Length > 1)
		{
			if (!long.TryParse(args[1], out var result))
			{
				ConsoleWindow.PrintError("Invalid arguments", suppressStacktrace: true);
				return null;
			}
			traderDestination = Thing.Find<ITraderDestination>(result);
		}
		if (traderDestination == null)
		{
			foreach (LandingPadNetwork allLandingPadNetwork in LandingPadNetwork.AllLandingPadNetworks)
			{
				if (!(allLandingPadNetwork.LandingPadCenter == null))
				{
					traderDestination = allLandingPadNetwork.LandingPadCenter;
					break;
				}
			}
		}
		return traderDestination;
	}

	private string HandleLand(string[] args)
	{
		if (RunOnClient())
		{
			return null;
		}
		ITraderDestination landingPad = GetLandingPad(args);
		if (landingPad == null)
		{
			ConsoleWindow.PrintError("No landing pad center found.", suppressStacktrace: true);
			return null;
		}
		if (landingPad.CurrentTradingContact != null)
		{
			ConsoleWindow.PrintError($"Landing pad '{landingPad.DisplayName}' already has '{landingPad.CurrentTradingContact}' as a contact.", suppressStacktrace: true);
			return null;
		}
		TraderContact traderContact = null;
		if (args.Length > 2)
		{
			if (!int.TryParse(args[2], out var result))
			{
				return "Invalid arguments";
			}
			if (result >= 0 && result < ContactSlot.ContactSlots.Count)
			{
				traderContact = ContactSlot.ContactSlots[result].CurrentContact;
			}
		}
		if (traderContact == null)
		{
			foreach (ContactSlot contactSlot in ContactSlot.ContactSlots)
			{
				if (contactSlot.CurrentContact != null)
				{
					traderContact = contactSlot.CurrentContact;
					break;
				}
			}
		}
		if (traderContact == null)
		{
			ConsoleWindow.PrintError("No trader could be found.", suppressStacktrace: true);
			return null;
		}
		landingPad.ServerCallTrader(isLanding: true, traderContact);
		ConsoleWindow.PrintAction("Called '" + traderContact.DisplayName + "' to '" + landingPad.DisplayName + "'.");
		return null;
	}

	private string HandleDepart(string[] args)
	{
		if (RunOnClient())
		{
			return null;
		}
		if (args.Length > 1)
		{
			if (!long.TryParse(args[1], out var result))
			{
				return "Invalid arguments";
			}
			ITraderDestination traderDestination = Thing.Find<ITraderDestination>(result);
			if (traderDestination == null)
			{
				ConsoleWindow.PrintError($"No landing pad center found with id #{result}.", suppressStacktrace: true);
				return null;
			}
			if (traderDestination.CurrentTradingContact == null)
			{
				ConsoleWindow.PrintError("No contact on landing pad '" + traderDestination.DisplayName + "'.", suppressStacktrace: true);
				return null;
			}
			traderDestination.ServerCallTrader(isLanding: false, traderDestination.CurrentTradingContact);
			ConsoleWindow.PrintAction("Trader '" + traderDestination.CurrentTradingContact.DisplayName + "' departing from '" + traderDestination.DisplayName + "'.");
			return null;
		}
		foreach (LandingPadNetwork allLandingPadNetwork in LandingPadNetwork.AllLandingPadNetworks)
		{
			if (allLandingPadNetwork.LandingPadCenter?.CurrentTradingContact != null)
			{
				allLandingPadNetwork.LandingPadCenter.ServerCallTrader(isLanding: false, allLandingPadNetwork.LandingPadCenter.CurrentTradingContact);
			}
		}
		return "Departing traders from all landing pads.";
	}
}
