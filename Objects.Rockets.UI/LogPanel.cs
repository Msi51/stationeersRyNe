using System;
using Assets.Scripts;
using Assets.Scripts.UI;
using Assets.Scripts.UI.CustomScrollPanel;
using Messages;
using Objects.Rockets.Log;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Objects.Rockets.UI;

public class LogPanel : UserInterfaceBase, IScrollHandler, IEventSystemHandler
{
	[Space(15f)]
	[Header("Log panel")]
	[SerializeField]
	private ScrollPanel _scrollPanel;

	[SerializeField]
	private TextMeshProUGUI _logTextMesh;

	[SerializeField]
	private BasicButton _clearButton;

	public void Initialize()
	{
	}

	public void Refresh()
	{
		RefreshLogText();
	}

	private void RefreshLogText()
	{
		_logTextMesh.text = RocketLog.GetLogText();
		_scrollPanel.SetContentHeight(_logTextMesh.preferredHeight);
	}

	private void OnLogUpdated(OnRocketLogUpdatedMessage message)
	{
		RefreshLogText();
	}

	private void ClearLog()
	{
		RocketLog.ClearLogLocally();
		RefreshLogText();
	}

	private new void OnEnable()
	{
		RefreshLogText();
		GameManager.EventBus.Subscribe<OnRocketLogUpdatedMessage>(OnLogUpdated);
		BasicButton clearButton = _clearButton;
		clearButton.OnClick = (Action)Delegate.Combine(clearButton.OnClick, new Action(ClearLog));
	}

	private new void OnDisable()
	{
		GameManager.EventBus.Unsubscribe<OnRocketLogUpdatedMessage>(OnLogUpdated);
		BasicButton clearButton = _clearButton;
		clearButton.OnClick = (Action)Delegate.Remove(clearButton.OnClick, new Action(ClearLog));
	}

	public void OnScroll(PointerEventData eventData)
	{
		_scrollPanel.OnScroll(eventData.scrollDelta);
	}
}
