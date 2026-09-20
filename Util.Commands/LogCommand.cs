using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;

namespace Util.Commands;

public class LogCommand : CommandBase
{
	private const string DEFAULT_LOG_NAME = "PlayerLog.txt";

	public override string HelpText => "Dumps the console buffer to a .log file in the local data folder. Pass 'clear' to delete all existing .log files instead.";

	public override string[] Arguments => new string[2] { "[logname]", "clear" };

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		if (args.Length == 1 && args[0] == "clear")
		{
			return RemoveLogs();
		}
		LogToFile(args).Forget();
		return null;
	}

	private static string RemoveLogs()
	{
		FileInfo[] files = new DirectoryInfo(Defines.Paths.LocalData).GetFiles("*.log");
		foreach (FileInfo fileInfo in files)
		{
			ConsoleWindow.PrintAction("Deleted file '" + fileInfo.FullName + "'.");
			fileInfo.Delete();
		}
		return null;
	}

	private static async UniTaskVoid LogToFile(string[] lineSplit)
	{
		string text = string.Empty;
		for (int i = 1; i < lineSplit.Length; i++)
		{
			if (CommandBase.Get(lineSplit, 1, "logname", out string result))
			{
				result = result.Replace("'", "");
				result = result.Replace("\"", "");
				text = (string.IsNullOrEmpty(text) ? result : (text + "_" + result));
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			text = string.Format("{0}_{1:yyyy-MM-dd_HH-mm-ss}", "PlayerLog.txt", DateTime.Now);
		}
		text = text.Replace(".log", "");
		text = text.Replace(" ", "_");
		text += ".log";
		ConsoleLine[] consoleBuffer = ConsoleWindow.ConsoleBuffer;
		List<string> logLines = new List<string>(consoleBuffer.Length);
		for (int num = consoleBuffer.Length - 1; num >= 0; num--)
		{
			ConsoleLine consoleLine = consoleBuffer[num];
			if (!string.IsNullOrEmpty(consoleLine.Text))
			{
				logLines.Add(consoleLine.ToString());
			}
		}
		if (logLines.Count == 0)
		{
			ConsoleWindow.PrintError("No console text to export.", suppressStacktrace: true);
			return;
		}
		string path = Defines.Paths.LocalData + text;
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		await using StreamWriter stream = new StreamWriter(path);
		foreach (string item in logLines)
		{
			await stream.WriteLineAsync(item);
		}
		ConsoleWindow.PrintAction($"Copied '{logLines.Count}' lines from console to '{path}'.");
	}
}
