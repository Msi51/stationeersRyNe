using System;
using UnityEngine;

namespace Assets.Scripts.UI;

[Serializable]
public class CanvasRendererCollection
{
	public GameObject ParentGameObject;

	public CanvasRenderer[] CanvasRenderers;

	public bool IsVisible { get; private set; }

	public virtual void Render(bool render)
	{
		CanvasRenderer[] canvasRenderers = CanvasRenderers;
		for (int i = 0; i < canvasRenderers.Length; i++)
		{
			canvasRenderers[i].cull = !render;
		}
		IsVisible = render;
	}
}
