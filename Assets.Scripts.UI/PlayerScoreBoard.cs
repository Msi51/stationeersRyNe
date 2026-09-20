using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class PlayerScoreBoard : ManagerBase
{
	[SerializeField]
	private PlayerListItem _playerListItemPrefab;

	[SerializeField]
	private PlayerListItem _placeholderItem;

	[SerializeField]
	private RectTransform _itemsParent;

	[SerializeField]
	private Button _btnReset;

	private readonly Dictionary<ulong, PlayerListItem> _playerItems = new Dictionary<ulong, PlayerListItem>();

	private int _playerListCountOnEnabled;

	public override void ManagerStart()
	{
		base.ManagerStart();
		UnityEngine.Object.Destroy(_placeholderItem.gameObject);
	}

	private void OnEnable()
	{
		_playerListCountOnEnabled = NetworkManager.TotalPlayersInGame;
		UpdateScoreBoard();
	}

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		PollForPlayerListChanges();
	}

	private void PollForPlayerListChanges()
	{
		if (NetworkManager.TotalPlayersInGame == _playerListCountOnEnabled)
		{
			return;
		}
		try
		{
			UpdateScoreBoard();
			_playerListCountOnEnabled = NetworkManager.TotalPlayersInGame;
		}
		catch (Exception)
		{
		}
	}

	private void UpdateScoreBoard()
	{
		foreach (Client client in NetworkBase.Clients)
		{
			if (client != null)
			{
				if (!_playerItems.TryGetValue(client.ClientId, out var value))
				{
					value = UnityEngine.Object.Instantiate(_playerListItemPrefab, _itemsParent);
					_playerItems[client.ClientId] = value;
				}
				value.SetData(client);
			}
		}
		ulong[] array = _playerItems.Keys.ToArray();
		foreach (ulong key in array)
		{
			PlayerListItem playerListItem = _playerItems[key];
			if (playerListItem.ClientData == null || Client.Find(playerListItem.ClientData.ClientId) == null)
			{
				UnityEngine.Object.Destroy(playerListItem.gameObject);
				_playerItems.Remove(key);
			}
		}
	}
}
