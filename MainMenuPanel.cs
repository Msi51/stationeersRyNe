using UnityEngine;

public class MainMenuPanel : MonoBehaviour
{
	[SerializeField]
	private string panelName;

	public string PanelName => panelName;

	public void SetActive(bool active)
	{
		base.gameObject.SetActive(active);
	}
}
