using UnityEngine;
using UnityEngine.UI;

public class NewWorldToggle : MonoBehaviour
{
	private Toggle _toggle;

	public Transform Target;

	private bool _status;

	private void Start()
	{
		_toggle = GetComponent<Toggle>();
	}

	private void Update()
	{
		if (!WorldManager.IsGamePaused && _status != _toggle.isOn)
		{
			_status = _toggle.isOn;
			if (_status)
			{
				Target.gameObject.SetActive(value: true);
				LeanTween.scale(Target.GetChild(0).gameObject, Vector3.one * 1.05f, 1f).setEase(LeanTweenType.linear);
				LeanTween.scale(Target.GetChild(0).gameObject, Vector3.one * 1.15f, 2f).setEase(LeanTweenType.linear).setDelay(1f)
					.setLoopPingPong(-1);
			}
			else
			{
				LeanTween.cancel(Target.GetChild(0).gameObject);
				Target.GetChild(0).localScale = Vector3.one;
				Target.gameObject.SetActive(value: false);
				_status = false;
			}
		}
	}
}
