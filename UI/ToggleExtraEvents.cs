using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI;

[RequireComponent(typeof(Toggle))]
public class ToggleExtraEvents : MonoBehaviour
{
	[SerializeField]
	private Toggle _toggle;

	public UnityEvent OnEvent;

	public UnityEvent OffEvent;

	private void Start()
	{
		_toggle.onValueChanged.AddListener(OnToggleChanged);
	}

	private void OnToggleChanged(bool isOn)
	{
		if (isOn)
		{
			OnEvent?.Invoke();
		}
		else
		{
			OffEvent?.Invoke();
		}
	}
}
