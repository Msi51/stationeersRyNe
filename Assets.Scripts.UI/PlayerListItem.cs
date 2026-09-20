using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class PlayerListItem : MonoBehaviour
{
	[SerializeField]
	private Image _avatar;

	[SerializeField]
	private TextMeshProUGUI _usernameText;

	[SerializeField]
	private TextMeshProUGUI _timePlayedText;

	[SerializeField]
	private TextMeshProUGUI _pingText;

	[SerializeField]
	private TextMeshProUGUI _steamIdText;

	[SerializeField]
	private TextMeshProUGUI _daysLived;

	private int _prevSeconds;

	public Client ClientData { get; private set; }

	public void SetData(Client client)
	{
		if (client != null)
		{
			ClientData = client;
			if (!GameManager.IsBatchMode)
			{
				_avatar.ApplyAvatarSprite(client.ClientId).Forget();
				_usernameText.text = client.name ?? string.Empty;
				_timePlayedText.text = NetUtils.FormatSeconds(Mathf.RoundToInt(Time.unscaledTime - client.connectTime));
				_pingText.text = client.RoundTripTime.ToString();
				_steamIdText.text = client.ClientId.ToString();
				Entity clientEntity = Entity.GetClientEntity(client.ClientId);
				_daysLived.text = ((!GameManager.RunSimulation) ? client.DaysLived.ToString() : ((clientEntity != null) ? clientEntity.DaysLived.ToString() : client.DaysLived.ToString()));
			}
		}
	}

	private void Update()
	{
		int num = (int)Time.unscaledTime;
		if (num != _prevSeconds)
		{
			_prevSeconds = num;
			if (!GameManager.IsBatchMode)
			{
				_timePlayedText.text = NetUtils.FormatSeconds(Mathf.RoundToInt(Time.unscaledTime - ClientData.connectTime));
				_daysLived.text = ClientData.DaysLived.ToString();
				_pingText.text = ClientData.RoundTripTime.ToString();
			}
		}
	}
}
