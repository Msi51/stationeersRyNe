using System.Threading;
using System.Timers;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Serialization;

public static class StationAutoSave
{
	private static readonly System.Timers.Timer _autoSaveTimer = new System.Timers.Timer();

	private static CancellationTokenSource _autoSavingCancellation;

	private static WaitCallback _autoSaveThreadCallback;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Init()
	{
		Application.quitting += Cancel;
		_autoSavingCancellation = new CancellationTokenSource();
	}

	private static async void TimerElapsed(object sender, ElapsedEventArgs e)
	{
		await UniTask.SwitchToMainThread();
		AutoSaveNow();
	}

	public static void ResetAutoSave()
	{
		if (GameManager.IsBatchMode)
		{
			ConsoleWindow.PrintAction("Auto save stopped");
		}
		_autoSaveTimer.Stop();
		if (Settings.CurrentData.AutoSave && !GameManager.IsTutorial && !GameManager.IsNewTutorial)
		{
			_autoSaveTimer.Elapsed -= TimerElapsed;
			_autoSaveTimer.Elapsed += TimerElapsed;
			_autoSaveTimer.Interval = Settings.CurrentData.SaveInterval * 1000;
			_autoSaveTimer.Start();
			if (GameManager.IsBatchMode)
			{
				ConsoleWindow.PrintAction($"Auto save started ({Settings.CurrentData.SaveInterval})", aged: true);
			}
		}
	}

	private static void AutoSaveNow()
	{
		if (!GameManager.IsTutorial && !GameManager.IsNewTutorial && !WorldManager.IsGamePaused && GameManager.GameState == GameState.Running && !NetworkManager.IsClient)
		{
			AutoSaveTask().Forget();
		}
	}

	private static async UniTaskVoid AutoSaveTask()
	{
		SaveResult saveResult = await SaveHelper.AutoSave(XmlSaveLoad.Instance.CurrentStationName, default(CancellationToken));
		if (!saveResult.Success)
		{
			ConsoleWindow.PrintError(saveResult.Message, suppressStacktrace: true);
		}
	}

	public static void Cancel()
	{
		_autoSavingCancellation?.Cancel();
	}
}
