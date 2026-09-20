using Assets.Scripts.UI;
using UnityEngine;

public class ControlsPanel : UserInterfaceBase
{
	public static ControlsPanel Instance;

	public ControlsDisplay ControlsPrefab;

	private void Awake()
	{
		Instance = this;
	}

	public void ShowControls(string assignment, string description)
	{
		ControlsDisplay controlsDisplay = Object.Instantiate(ControlsPrefab, base.transform);
		if (!(controlsDisplay == null))
		{
			controlsDisplay.HotKeyDisplay.text = assignment;
			controlsDisplay.ActionText.text = description;
		}
	}

	public void DisableControlsPanel()
	{
		foreach (Transform item in base.transform)
		{
			Object.Destroy(item.gameObject);
		}
	}
}
