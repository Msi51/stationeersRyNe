using System;
using System.Diagnostics;
using Assets.Scripts;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Util.Commands;

public class RestartCommand : CommandBase
{
	public override string HelpText => "Restarts the application by quitting and relaunching the executable.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		RestartApplication().Forget();
		return null;
	}

	private static async UniTaskVoid RestartApplication()
	{
		ConsoleWindow.PrintAction("Restarting the game.");
		Process process = new Process();
		process.StartInfo.FileName = Application.dataPath + "\\..\\rocketstation.exe";
		process.StartInfo.Arguments = "-noSplash";
		process.Start();
		await UniTask.Delay(10);
		Application.Quit();
	}
}
