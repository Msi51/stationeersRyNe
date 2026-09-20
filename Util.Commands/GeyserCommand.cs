using System.IO;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Util;
using Objects.Structures;
using UnityEngine;

namespace Util.Commands;

public class GeyserCommand : CommandBase
{
	public override string HelpText => "Provides utilities for working with geyser placements. 'export' writes all geyser locations to an XML file in the given directory.";

	public override bool IsLaunchCmd => false;

	public override string[] Arguments => new string[1] { "export <directory>" };

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("geyser"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (args[0] == "export")
		{
			return HandleExport(args);
		}
		return "Invalid syntax";
	}

	private bool CheckArg(string[] args, int index, out string arg)
	{
		arg = null;
		if (index >= args.Length)
		{
			ConsoleWindow.PrintError("Index out of range for arguments array.", suppressStacktrace: true);
			return false;
		}
		arg = args[index];
		return true;
	}

	private string HandleExport(string[] args)
	{
		if (!CheckArg(args, 1, out var arg))
		{
			return null;
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(arg);
		if (!directoryInfo.Exists)
		{
			ConsoleWindow.PrintError("Directory not found.", suppressStacktrace: true);
			return null;
		}
		string text = directoryInfo.FullName + "/geyserlocations.xml";
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Geyser allGeyser in Geyser.AllGeysers)
		{
			Vector3Int vector3Int = allGeyser.Transform.position.RoundToInt();
			Vector3Int vector3Int2 = allGeyser.Transform.eulerAngles.RoundToInt();
			stringBuilder.AppendLine((vector3Int2 == Vector3Int.zero) ? $"<Transform x=\"{vector3Int.x}\" y=\"{vector3Int.y}\" z=\"{vector3Int.z}\"/>" : $"<Transform x=\"{vector3Int.x}\" y=\"{vector3Int.y}\" z=\"{vector3Int.z}\" rx=\"{vector3Int2.x}\" ry=\"{vector3Int2.y}\" rz=\"{vector3Int2.z}\"/>");
		}
		File.WriteAllText(text, stringBuilder.ToString());
		return "Created '" + text + "'.";
	}
}
