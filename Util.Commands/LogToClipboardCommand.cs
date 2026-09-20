using System;
using Assets.Scripts;
using UnityEngine;

namespace Util.Commands;

public class LogToClipboardCommand : CommandBase
{
	public override string HelpText => "Copies the contents of the console buffer to the system clipboard.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		LogToClipboard(args);
		return null;
	}

	private static void LogToClipboard(string[] lineSplit)
	{
		string text = string.Empty;
		int num = 0;
		for (int num2 = ConsoleWindow.ConsoleBuffer.Length - 1; num2 >= 0; num2--)
		{
			ConsoleLine consoleLine = ConsoleWindow.ConsoleBuffer[num2];
			if (!string.IsNullOrEmpty(consoleLine.Text))
			{
				text = ((!string.IsNullOrEmpty(text)) ? $"{text}\n{consoleLine}" : consoleLine.ToString());
				num++;
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			ConsoleWindow.PrintError("No console text to copy.", suppressStacktrace: true);
			return;
		}
		GUIUtility.systemCopyBuffer = text;
		ConsoleWindow.PrintAction($"Copied '{num}' lines from console to the clipboard.");
	}
}
