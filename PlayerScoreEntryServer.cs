using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScoreEntryServer : MonoBehaviour
{
	public TextMeshProUGUI PlayerName;

	public Text SteamId;

	public void OnKickButtonClicked()
	{
		BlockedPlayerManager.Instance.ShowPopupForKickPlayer(PlayerName.text, ulong.Parse(SteamId.text));
	}

	public void OnBanButtonClicked()
	{
		BlockedPlayerManager.Instance.ShowPopupForBanPlayer(PlayerName.text, ulong.Parse(SteamId.text));
	}
}
