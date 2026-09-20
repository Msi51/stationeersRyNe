using Assets.Scripts.UI.CustomScrollPanel;
using Messages;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using WorldLogSystem;

namespace Assets.Scripts.Objects.Motherboards.Comms;

public class LogTab : MonoBehaviour, IScrollHandler, IEventSystemHandler
{
	[SerializeField]
	private ScrollPanel _scrollPanel;

	[SerializeField]
	private TextMeshProUGUI _logTextMesh;

	public void Show()
	{
		base.gameObject.SetActive(value: true);
		RefreshLogText();
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}

	private void RefreshLogText()
	{
		_logTextMesh.text = WorldLog.GetLogText();
		_scrollPanel.SetContentHeight(_logTextMesh.preferredHeight);
	}

	private void OnWorldLogUpdated(OnWorldLogUpdatedMessage message)
	{
		RefreshLogText();
	}

	private void OnEnable()
	{
		RefreshLogText();
		GameManager.EventBus.Subscribe<OnWorldLogUpdatedMessage>(OnWorldLogUpdated);
	}

	private void OnDisable()
	{
		GameManager.EventBus.Unsubscribe<OnWorldLogUpdatedMessage>(OnWorldLogUpdated);
	}

	public void OnScroll(PointerEventData eventData)
	{
		_scrollPanel.OnScroll(eventData.scrollDelta);
	}
}
