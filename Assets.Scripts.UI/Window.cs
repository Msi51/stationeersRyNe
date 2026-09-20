using UnityEngine;

namespace Assets.Scripts.UI;

public abstract class Window : UserInterfaceBase, IWindow
{
	public delegate void WindowVisibleEvent(Window window, bool isVisible);

	public int ZLayer => GetComponent<Canvas>().sortingOrder;

	public static event WindowVisibleEvent onVisibilityChanged;

	public override void SetVisible(bool isVisble)
	{
		base.SetVisible(isVisble);
		InvokeOnVisibilityChanged(isVisble);
	}

	protected void InvokeOnVisibilityChanged(bool isVisible)
	{
		Window.onVisibilityChanged?.Invoke(this, isVisible);
	}

	public void SetZLayer(int layer)
	{
		Canvas component = GetComponent<Canvas>();
		if ((bool)component)
		{
			component.sortingOrder = layer;
		}
	}
}
