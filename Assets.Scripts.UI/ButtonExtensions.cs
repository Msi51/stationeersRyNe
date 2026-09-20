using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public static class ButtonExtensions
{
	public static void OnDoubleClick(this Button button, UnityAction callback, float clickSpeed = 0.5f)
	{
		DoubleClick orAddComponent = button.GetOrAddComponent<DoubleClick>();
		orAddComponent.ClickSpeed = clickSpeed;
		orAddComponent.OnDoubleClick.AddListener(callback);
	}
}
