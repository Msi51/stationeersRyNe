using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UiButton : MonoBehaviour
{
	[SerializeField]
	private LocalizedText _text;

	[SerializeField]
	private Button _button;

	public void SetText(string localizedTextKey)
	{
		_text.StringKey = localizedTextKey;
		_text.Refresh();
	}

	public void SetOnClickCallbacks(UnityAction[] actions)
	{
		_button.onClick.RemoveAllListeners();
		foreach (UnityAction unityAction in actions)
		{
			if (unityAction != null)
			{
				_button.onClick.AddListener(unityAction);
			}
		}
	}

	public void SetActive(bool value)
	{
		base.gameObject.SetActive(value);
	}
}
