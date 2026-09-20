using System;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class RendererInstance
{
	public Thing Parent;

	public Renderer[] Renderer;

	private bool _enabled;

	public bool Enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			_enabled = value;
			if (Renderer == null || Renderer.Length == 0)
			{
				return;
			}
			Renderer[] renderer = Renderer;
			foreach (Renderer renderer2 in renderer)
			{
				if (renderer2 != null)
				{
					renderer2.enabled = Parent != null && !Parent.IsOccluded && _enabled;
				}
			}
		}
	}
}
