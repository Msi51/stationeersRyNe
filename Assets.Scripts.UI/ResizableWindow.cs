using UnityEngine;

namespace Assets.Scripts.UI;

public class ResizableWindow : DraggableWindow
{
	public RectTransform ResizeTransform;

	public ResizeButton ResizeButton;

	public Vector2 MinimumSize;

	public void Resize(Vector2 deltaSize)
	{
		Vector2 sizeDelta = ResizeTransform.sizeDelta;
		deltaSize.y = 0f - deltaSize.y;
		sizeDelta += deltaSize;
		sizeDelta.x = Mathf.Max(sizeDelta.x, MinimumSize.x);
		sizeDelta.y = Mathf.Max(sizeDelta.y, MinimumSize.y);
		ResizeTransform.sizeDelta = sizeDelta;
	}
}
