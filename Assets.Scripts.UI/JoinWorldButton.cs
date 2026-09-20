using System;
using Assets.Scripts.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class JoinWorldButton : MonoBehaviour
{
	public Action<GameSession> OnClickSelected;

	[SerializeField]
	private Button _button;

	[SerializeField]
	private Button _serverInfoButton;

	[Header("UI")]
	[SerializeField]
	private TextMeshProUGUI _nameText;

	[SerializeField]
	private TextMeshProUGUI _addressText;

	[SerializeField]
	private TextMeshProUGUI _pingText;

	[SerializeField]
	private TextMeshProUGUI _playerCountText;

	[SerializeField]
	private TextMeshProUGUI _versionText;

	[SerializeField]
	private TextMeshProUGUI _mapNameText;

	[SerializeField]
	private GameObject _lockObj;

	[SerializeField]
	private Image _serverTypeImg;

	[SerializeField]
	private GameObject _favouriteObj;

	[SerializeField]
	private Sprite[] _serverTypeSprites;

	private GameSession _gameSession;

	public string ServerName => _nameText.text;

	private void Start()
	{
		_button.onClick.AddListener(delegate
		{
			OnClickSelected?.Invoke(_gameSession);
		});
		_serverInfoButton.onClick.AddListener(delegate
		{
		});
	}

	public void SetData(GameSession gameSession)
	{
		_gameSession = gameSession;
		_nameText.text = gameSession.Name;
		_addressText.text = gameSession.Address;
		_playerCountText.text = $"{gameSession.Players} / {gameSession.MaxPlayers}";
		_pingText.text = gameSession.Latency.ToString();
		_versionText.text = gameSession.Version;
		_lockObj.SetActive(gameSession.Password);
		_mapNameText.text = gameSession.MapName;
		ServerType type = gameSession.Type;
		_serverTypeImg.gameObject.SetActive(type > ServerType.Hosted);
		_serverTypeImg.sprite = _serverTypeSprites[(int)type];
		_favouriteObj.SetActive(NetworkManager.GameSessionFavouriteList.Contains(gameSession));
	}
}
