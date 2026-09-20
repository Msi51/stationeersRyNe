using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.UI;

public class WorldSelectButton : MonoBehaviour
{
	private static WorldSelectButton _lastClickedButton;

	private static float _lastClickTime;

	public void DoubleClickToJoinServer()
	{
		if (_lastClickedButton == this && (double)(Time.time - _lastClickTime) < 0.3)
		{
			Debug.LogError("Not implemented yet");
			XmlSaveLoad.Instance.PanelNewWorld.SetActive(value: false);
		}
		else
		{
			_lastClickedButton = this;
		}
		_lastClickTime = Time.time;
	}
}
