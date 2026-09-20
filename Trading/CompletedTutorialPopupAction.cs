using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UI;

namespace Trading;

public class CompletedTutorialPopupAction : PopupAction
{
	public override bool Execute<T>(T t, Entity player)
	{
		WaitThenPopup((int)(Delay * 1000f)).Forget();
		return true;
	}

	private async UniTaskVoid WaitThenPopup(int delayMs)
	{
		await UniTask.Delay(delayMs);
		if (GameManager.GameState != GameState.None)
		{
			Singleton<ConfirmationPanel>.Instance.Show(PopupTitle.Key, PopupText.Key, "TutorialBackToMenu", ReturnToTutorialMenu, "GameMenuButtonCancel");
		}
	}

	private void ReturnToTutorialMenu()
	{
		ExitThenEnableTutorialMenu().Forget();
	}

	private async UniTaskVoid ExitThenEnableTutorialMenu()
	{
		GameManager.LeaveGame();
		while (GameManager.GameState != GameState.None)
		{
			await UniTask.WaitForEndOfFrame();
		}
		await UniTask.WaitForEndOfFrame();
		MainMenu.Instance.PageManager.EnableMainMenuPage("TutorialScenarios");
		await UniTask.WaitForEndOfFrame();
		await UniTask.WaitForEndOfFrame();
		TutorialScenariosMenu.Instance.selectedWorld.PlanetScene.SetVisible(isVisble: true);
	}
}
