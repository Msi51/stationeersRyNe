using System.Threading;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace UI;

public class ExitGamePopup : MonoBehaviour
{
	public void ShowPopup(string status)
	{
		if (CanExitWorld())
		{
			UnityAction unityAction = ((status == "EXIT") ? new UnityAction(OnExit) : new UnityAction(OnDisconnect));
			UnityAction button2OnClick = ((status == "EXIT") ? new UnityAction(OnSaveAndExit) : new UnityAction(OnSaveAndDisconnect));
			if (GameManager.RunSimulation && !GameManager.IsNewTutorial)
			{
				Singleton<ConfirmationPanel>.Instance.Show("PromptButtonLeave", "DialogConfirmNoticeUnsaved", "ButtonCancel", null, "ButtonSave", button2OnClick, "ButtonExit", unityAction);
			}
			else
			{
				Singleton<ConfirmationPanel>.Instance.Show("PromptButtonLeave", "DialogConfirmNoticeClientLeaving", "ButtonCancel", null, "ButtonExit", unityAction);
			}
		}
	}

	private bool CanExitWorld()
	{
		if (XmlSaveLoad.Instance.CanExitWorld())
		{
			return true;
		}
		PromptPanel.Instance.ShowPrompt(PromptWaitingStrings.Title, PromptWaitingStrings.Body, PromptWaitingStrings.Button, delegate
		{
			PromptPanel.Instance.DisablePromptPanel();
		}, isEscapable: true, hideCancelButton: true);
		return false;
	}

	private static void Disconnect()
	{
		GameManager.LeaveGameAfterFade().Forget();
	}

	private void OnSaveAndExit()
	{
		SaveAndExit().Forget();
	}

	private void OnSaveAndDisconnect()
	{
		SaveAndDisconnect().Forget();
	}

	private async UniTaskVoid SaveAndExit()
	{
		if (CanExitWorld())
		{
			SaveResult saveResult = await SaveHelper.Save(XmlSaveLoad.Instance.CurrentStationName, default(CancellationToken));
			if (!saveResult.Success)
			{
				ConsoleWindow.PrintError(saveResult.Message, suppressStacktrace: true);
			}
			while (GameManager.GameState != GameState.None && (XmlSaveLoad.Instance.CopyingWorld || XmlSaveLoad.Instance.SavingWorld))
			{
				await UniTask.Delay(1);
			}
			OnExit();
		}
	}

	private async UniTaskVoid SaveAndDisconnect()
	{
		if (CanExitWorld())
		{
			SaveResult saveResult = await SaveHelper.Save(XmlSaveLoad.Instance.CurrentStationName, default(CancellationToken));
			if (!saveResult.Success)
			{
				ConsoleWindow.PrintError(saveResult.Message, suppressStacktrace: true);
			}
			while (GameManager.GameState != GameState.None && (XmlSaveLoad.Instance.CopyingWorld || XmlSaveLoad.Instance.SavingWorld))
			{
				await UniTask.Delay(1, DelayType.UnscaledDeltaTime);
			}
			OnDisconnect();
		}
	}

	private void OnDisconnect()
	{
		if (!GameManager.RunSimulation || GameManager.IsNewTutorial || CanExitWorld())
		{
			Disconnect();
		}
	}

	private void OnExit()
	{
		if (!GameManager.RunSimulation || GameManager.IsNewTutorial || CanExitWorld())
		{
			GameManager.QuitGame();
		}
	}
}
