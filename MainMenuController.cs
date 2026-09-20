using System;
using System.Linq;
using Assets.Scripts;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
	[Header("Panels")]
	[SerializeField]
	private MainMenuPanel[] _menuPanels;

	private void Awake()
	{
		GameManager.EventBus.Subscribe<PanelRequestMessage>(ProcessPanelRequest);
	}

	private void ProcessPanelRequest(PanelRequestMessage msg)
	{
		_menuPanels.SingleOrDefault((MainMenuPanel p) => string.Equals(p.PanelName, msg.PanelType, StringComparison.CurrentCultureIgnoreCase))?.SetActive(active: true);
	}
}
