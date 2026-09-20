using Assets.Scripts.Util;
using TMPro;
using UI;
using UI.UIFade;
using UnityEngine;

public class UIGamePauseDialogue : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI _messageText;

	private string _extraMessage;

	private static readonly string _gamePausedKey = "GamePaused";

	private void Start()
	{
		NetworkBase.PausedForClientConnectEvent += NetworkServerOnPausedForClientConnectEvent;
		_messageText.enabled = false;
	}

	private void OnDestroy()
	{
		NetworkBase.PausedForClientConnectEvent -= NetworkServerOnPausedForClientConnectEvent;
	}

	private static void NetworkServerOnPausedForClientConnectEvent(bool isGamePaused, string contextMessage)
	{
		if (isGamePaused)
		{
			Singleton<ConfirmationPanel>.Instance.Show(_gamePausedKey, contextMessage, null, null, null, null, null, null, closeOnEscape: false);
			return;
		}
		if (Singleton<ConfirmationPanel>.Instance.IsVisible)
		{
			Singleton<ConfirmationPanel>.Instance.CloseCurrentPanel();
		}
		if (FadePanel.State == FadeState.Black)
		{
			FadePanel.ToTransparent(4f);
		}
	}
}
