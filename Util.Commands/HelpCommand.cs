using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.UI.ImGuiUi;

namespace Util.Commands;

public class HelpCommand : CommandBase
{
	private static readonly char[] _placeholderOpens = new char[2] { '<', '[' };

	public override string HelpText => "Displays helpful stuff";

	public override string[] Arguments => new string[4] { "commands", "list (l)", "<key>", "tofile: prints the help output to file" };

	public override bool IsLaunchCmd => false;

	private static uint CmdColor => ImGuiColor.Integer.White;

	private static uint ArgColor => ImGuiColor.Integer.Grey;

	private static uint HelpColor => ImGuiColor.Integer.Grey;

	private static uint LaunchTagColor => ImGuiColor.Integer.SoftMustard;

	private static uint HostTagColor => ImGuiColor.Integer.SoftCopper;

	private static uint MpTagColor => ImGuiColor.Integer.SoftSky;

	private static uint SpTagColor => ImGuiColor.Integer.SoftRose;

	private static uint GameTagColor => ImGuiColor.Integer.SoftSage;

	private static uint CreativeTagColor => ImGuiColor.Integer.SoftLilac;

	private static uint RequiredColor => ImGuiColor.Integer.SoftPeach;

	private static uint OptionalColor => ImGuiColor.Integer.SoftMint;

	public override string Execute(string[] args)
	{
		switch (args.Length)
		{
		case 0:
			PrintAll();
			return null;
		case 1:
			switch (args[0])
			{
			case "commands":
			case "list":
			case "l":
				ConsoleWindow.Print("Use 'help <key>' to find out more about that specific command", ConsoleColor.DarkYellow);
				ConsoleWindow.Print("Commands: " + string.Join(", ", CommandLine.CommandsMap.Keys));
				return null;
			case "tofile":
				PrintToFile();
				return null;
			default:
			{
				if (CommandLine.CommandsMap.TryGetValue(args[0], out var value))
				{
					PrintCommand(args[0], value, args[0].Length + 2);
					return null;
				}
				return "No such command found";
			}
			}
		default:
			return "Invalid syntax";
		}
	}

	internal static void PrintAll()
	{
		ConsoleWindow.PrintAction("Tags: [launch] usable as launch arg | [game] requires loaded game | [host] host or singleplayer only | [mp] multiplayer only | [sp] singleplayer only | [creative] creative mode only");
		int nameColumnWidth = ComputeNameColumnWidth();
		foreach (var (key, cmd) in CommandLine.CommandsMap)
		{
			PrintCommand(key, cmd, nameColumnWidth);
		}
	}

	private static int ComputeNameColumnWidth()
	{
		int num = 0;
		foreach (var (key, commandBase2) in CommandLine.CommandsMap)
		{
			if (!commandBase2.Hidden)
			{
				int num2 = NameDisplayLength(key, commandBase2);
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		return num + 2;
	}

	private static List<(string text, uint color)> NameSegments(string key, CommandBase cmd)
	{
		List<(string, uint)> list = new List<(string, uint)>(8) { (key, CmdColor) };
		if (cmd.IsLaunchCmd)
		{
			list.Add((" [launch]", LaunchTagColor));
		}
		if ((cmd.Scope & CommandScope.HostOrSinglePlayer) != CommandScope.None)
		{
			list.Add((" [host]", HostTagColor));
		}
		if ((cmd.Scope & CommandScope.MultiplayerOnly) != CommandScope.None)
		{
			list.Add((" [mp]", MpTagColor));
		}
		if ((cmd.Scope & CommandScope.SinglePlayerOnly) != CommandScope.None)
		{
			list.Add((" [sp]", SpTagColor));
		}
		if ((cmd.Scope & CommandScope.InGame) != CommandScope.None)
		{
			list.Add((" [game]", GameTagColor));
		}
		if ((cmd.Scope & CommandScope.CreativeOnly) != CommandScope.None)
		{
			list.Add((" [creative]", CreativeTagColor));
		}
		return list;
	}

	private static int NameDisplayLength(string key, CommandBase cmd)
	{
		int num = key.Length;
		if (cmd.IsLaunchCmd)
		{
			num += " [launch]".Length;
		}
		if ((cmd.Scope & CommandScope.HostOrSinglePlayer) != CommandScope.None)
		{
			num += " [host]".Length;
		}
		if ((cmd.Scope & CommandScope.MultiplayerOnly) != CommandScope.None)
		{
			num += " [mp]".Length;
		}
		if ((cmd.Scope & CommandScope.SinglePlayerOnly) != CommandScope.None)
		{
			num += " [sp]".Length;
		}
		if ((cmd.Scope & CommandScope.InGame) != CommandScope.None)
		{
			num += " [game]".Length;
		}
		if ((cmd.Scope & CommandScope.CreativeOnly) != CommandScope.None)
		{
			num += " [creative]".Length;
		}
		return num;
	}

	private static void AppendArgSegments(string arg, uint baseColor, List<(string text, uint color)> outSegs)
	{
		if (string.IsNullOrEmpty(arg))
		{
			return;
		}
		int num = 0;
		while (num < arg.Length)
		{
			int num2 = arg.IndexOfAny(_placeholderOpens, num);
			if (num2 < 0)
			{
				outSegs.Add((arg.Substring(num), baseColor));
				break;
			}
			if (num2 > num)
			{
				outSegs.Add((arg.Substring(num, num2 - num), baseColor));
			}
			bool flag = arg[num2] == '<';
			char value = (flag ? '>' : ']');
			int num3 = arg.IndexOf(value, num2 + 1);
			if (num3 < 0)
			{
				outSegs.Add((arg.Substring(num2), baseColor));
				break;
			}
			uint item = (flag ? RequiredColor : OptionalColor);
			outSegs.Add((arg.Substring(num2, num3 - num2 + 1), item));
			num = num3 + 1;
		}
	}

	private static void PrintToFile()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("# Stationeers Commands - " + GameManager.GetGameVersion());
		stringBuilder.AppendLine("| Command | Launch Command? | Arguments | Help |");
		stringBuilder.AppendLine("| :------ | :-------------: | :-------- | :--- |");
		foreach (KeyValuePair<string, CommandBase> item in CommandLine.CommandsMap)
		{
			item.Deconstruct(out var key, out var value);
			string text = key;
			CommandBase commandBase = value;
			string text2 = string.Join(", ", commandBase.Arguments ?? Array.Empty<string>()).Replace("<", "&lt;").Replace(">", "&gt;");
			stringBuilder.AppendLine($"| `{text}` | {commandBase.IsLaunchCmd} | {text2} | {commandBase.HelpText}");
		}
		FileInfo fileInfo = new FileInfo("help.md");
		File.WriteAllText(fileInfo.FullName, stringBuilder.ToString());
		ConsoleWindow.PrintAction("Created file: " + fileInfo.FullName);
	}

	internal static void PrintCommand(string key, CommandBase cmd, int nameColumnWidth)
	{
		if (cmd.Hidden)
		{
			return;
		}
		string text = new string(' ', nameColumnWidth);
		string[] array = (string.IsNullOrEmpty(cmd.HelpText) ? ("(no help text for '" + key + "')") : cmd.HelpText).Split('\n');
		List<(string, uint)[]> list = new List<(string, uint)[]>();
		if (cmd.Arguments == null || cmd.Arguments.Length == 0)
		{
			List<(string, uint)> list2 = BuildPrefixSegments(key, cmd, nameColumnWidth);
			list2.Add((array[0].TrimEnd('\r'), HelpColor));
			list.Add(list2.ToArray());
			for (int i = 1; i < array.Length; i++)
			{
				list.Add(IndentedHelpRow(text, array[i]));
			}
		}
		else
		{
			BuildArgRows(key, cmd, nameColumnWidth, text, list);
			string[] array2 = array;
			foreach (string raw in array2)
			{
				list.Add(IndentedHelpRow(text, raw));
			}
		}
		ConsoleWindow.PrintSegmentedBlockRaw(list.ToArray());
	}

	private static List<(string text, uint color)> BuildPrefixSegments(string key, CommandBase cmd, int nameColumnWidth)
	{
		List<(string, uint)> list = NameSegments(key, cmd);
		int num = NameDisplayLength(key, cmd);
		if (num < nameColumnWidth)
		{
			list.Add((new string(' ', nameColumnWidth - num), CmdColor));
		}
		return list;
	}

	private static (string text, uint color)[] IndentedHelpRow(string indent, string raw)
	{
		return new(string, uint)[2]
		{
			(indent, HelpColor),
			(raw.TrimEnd('\r'), HelpColor)
		};
	}

	private static void BuildArgRows(string key, CommandBase cmd, int nameColumnWidth, string contPrefix, List<(string text, uint color)[]> outRows)
	{
		string text = cmd.HelpTextSeparator ?? ", ";
		if (!text.Contains('\n'))
		{
			List<(string, uint)> list = BuildPrefixSegments(key, cmd, nameColumnWidth);
			AppendArgSegments(string.Join(text, cmd.Arguments), ArgColor, list);
			outRows.Add(list.ToArray());
			return;
		}
		bool flag = false;
		string[] arguments = cmd.Arguments;
		foreach (string text2 in arguments)
		{
			if (text2 != null && text2.Contains(" : "))
			{
				flag = true;
				break;
			}
		}
		int num = 0;
		if (flag)
		{
			arguments = cmd.Arguments;
			foreach (string text3 in arguments)
			{
				if (text3 != null)
				{
					int num2 = text3.IndexOf(" : ", StringComparison.Ordinal);
					int num3 = ((num2 >= 0) ? num2 : text3.Length);
					if (num3 > num)
					{
						num = num3;
					}
				}
			}
			num += 2;
		}
		for (int j = 0; j < cmd.Arguments.Length; j++)
		{
			List<(string, uint)> list2 = (List<(string, uint)>)((j == 0) ? ((IList)BuildPrefixSegments(key, cmd, nameColumnWidth)) : ((IList)new List<(string, uint)> { (contPrefix, ArgColor) }));
			string text4 = cmd.Arguments[j] ?? string.Empty;
			if (flag)
			{
				int num4 = text4.IndexOf(" : ", StringComparison.Ordinal);
				if (num4 >= 0)
				{
					string text5 = text4.Substring(0, num4);
					int num5 = num - text5.Length;
					AppendArgSegments(text5, ArgColor, list2);
					if (num5 > 0)
					{
						list2.Add((new string(' ', num5), ArgColor));
					}
					list2.Add((text4.Substring(num4 + 3), HelpColor));
				}
				else
				{
					AppendArgSegments(text4, ArgColor, list2);
				}
			}
			else
			{
				AppendArgSegments(text4, ArgColor, list2);
			}
			outRows.Add(list2.ToArray());
		}
	}
}
