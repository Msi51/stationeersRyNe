using System;
using System.Collections.Generic;
using System.Globalization;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;

namespace Util.Commands;

public abstract class CommandBase
{
	protected const string ERROR_INVALID_SYNTAX = "Invalid syntax";

	protected const string ERROR_INVALID_ARGUMENTS = "Invalid arguments";

	protected const string ERROR_NOT_SERVER = "Can only be run on the server";

	public virtual CommandScope Scope => CommandScope.None;

	public abstract string HelpText { get; }

	public abstract string[] Arguments { get; }

	public virtual string HelpTextSeparator => ", ";

	public abstract bool IsLaunchCmd { get; }

	public virtual bool RequiresGameManagerIsInitialized { get; }

	public virtual bool Hidden { get; set; }

	protected bool EnforceScope(string key)
	{
		CommandScope scope = Scope;
		if ((scope & CommandScope.HostOrSinglePlayer) != CommandScope.None && CannotAsClient(key))
		{
			return false;
		}
		if ((scope & CommandScope.MultiplayerOnly) != CommandScope.None && CannotInSinglePlayer(key))
		{
			return false;
		}
		if ((scope & CommandScope.SinglePlayerOnly) != CommandScope.None && (NetworkManager.IsClient || NetworkManager.IsServer))
		{
			ConsoleWindow.PrintError("cannot use '" + key + "' while in multiplayer", suppressStacktrace: true);
			return false;
		}
		if ((scope & CommandScope.InGame) != CommandScope.None && !IsInGame(key))
		{
			return false;
		}
		if ((scope & CommandScope.CreativeOnly) != CommandScope.None && !IsCreative(key))
		{
			return false;
		}
		return true;
	}

	private static bool IsCreative(string key)
	{
		if (GameManager.IsBatchMode)
		{
			return true;
		}
		if (DifficultySetting.Current == null || !DifficultySetting.Current.Creative.Value)
		{
			ConsoleWindow.PrintError("'" + key + "' requires creative mode", suppressStacktrace: true);
			return false;
		}
		return true;
	}

	public abstract string Execute(string[] args);

	public virtual IEnumerable<string> GetCompletions(int argIndex, string prefix)
	{
		return null;
	}

	protected static bool Get(string[] lineSplit, int i, string variable, out string result)
	{
		result = lineSplit[i];
		if (string.IsNullOrEmpty(result))
		{
			ConsoleWindow.PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, result), suppressStacktrace: true);
			return false;
		}
		return true;
	}

	protected static bool Get(string[] lineSplit, int i, string variable, out ushort result)
	{
		if (!ushort.TryParse(lineSplit[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
		{
			ConsoleWindow.PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, lineSplit[i]), suppressStacktrace: true);
			return false;
		}
		return true;
	}

	protected static bool Get(string[] lineSplit, int i, string variable, out int result)
	{
		if (!int.TryParse(lineSplit[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
		{
			ConsoleWindow.PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, lineSplit[i]), suppressStacktrace: true);
			return false;
		}
		return true;
	}

	protected static bool Get(string[] lineSplit, int i, string variable, out long result)
	{
		if (!long.TryParse(lineSplit[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
		{
			ConsoleWindow.PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, lineSplit[i]), suppressStacktrace: true);
			return false;
		}
		return true;
	}

	protected static bool Get(string[] lineSplit, int i, string variable, out ulong result)
	{
		if (!ulong.TryParse(lineSplit[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
		{
			ConsoleWindow.PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, lineSplit[i]), suppressStacktrace: true);
			return false;
		}
		return true;
	}

	protected static bool Get(string[] lineSplit, int i, string variable, out uint result)
	{
		if (!uint.TryParse(lineSplit[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
		{
			ConsoleWindow.PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, lineSplit[i]), suppressStacktrace: true);
			return false;
		}
		return true;
	}

	protected static bool Get(string[] lineSplit, int i, string variable, out float result)
	{
		if (i >= lineSplit.Length)
		{
			result = 0f;
			return false;
		}
		if (!float.TryParse(lineSplit[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result))
		{
			ConsoleWindow.PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, lineSplit[i]), suppressStacktrace: true);
			return false;
		}
		return true;
	}

	protected static bool GetEnum<T>(string[] lineSplit, int i, string variable, out T result) where T : struct, Enum
	{
		if (i < lineSplit.Length && Enum.TryParse<T>(lineSplit[i], ignoreCase: true, out result) && Enum.IsDefined(typeof(T), result))
		{
			return true;
		}
		result = default(T);
		string text = string.Join(", ", Enum.GetNames(typeof(T))).ToLowerInvariant();
		string text2 = ((i < lineSplit.Length) ? lineSplit[i] : "");
		ConsoleWindow.PrintError("invalid " + variable + " '" + text2 + "'. valid: " + text, suppressStacktrace: true);
		return false;
	}

	protected static bool CannotAsClient(string commandKey)
	{
		if (NetworkManager.IsActiveAsClient)
		{
			ConsoleWindow.PrintError(ConsoleStrings.Error.CannotAsClient.AsString(commandKey.ToLower()), suppressStacktrace: true);
			return true;
		}
		return false;
	}

	protected static bool CannotInSinglePlayer(string commandKey)
	{
		if (!NetworkManager.IsClient && !NetworkManager.IsServer)
		{
			ConsoleWindow.PrintError(ConsoleStrings.Error.CannotInSingleplayer.AsString(commandKey.ToLower()), suppressStacktrace: true);
			return true;
		}
		return false;
	}

	public static bool IsInGame(string command)
	{
		if (GameManager.GameState != GameState.Running)
		{
			ConsoleWindow.PrintError(ConsoleStrings.Error.NotInGame.AsString(command), suppressStacktrace: true);
			return false;
		}
		return true;
	}

	protected static Human ResolveTargetPlayer(string[] args, out int valueArgIndex)
	{
		if (args.Length >= 2)
		{
			valueArgIndex = 1;
			Human human = Human.Find(args[0]);
			if (human == null)
			{
				ConsoleWindow.PrintError("Player '" + args[0] + "' not found.", suppressStacktrace: true);
			}
			return human;
		}
		valueArgIndex = 0;
		if (InventoryManager.ParentHuman == null)
		{
			ConsoleWindow.PrintError("No local player to target.", suppressStacktrace: true);
		}
		return InventoryManager.ParentHuman;
	}
}
