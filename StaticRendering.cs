using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Rendering;

public class StaticRendering : Singleton<StaticRendering>
{
	public enum StaticRendererMode
	{
		Disabled,
		NoShadows,
		Shadows
	}

	public List<RenderBatch> Batches = new List<RenderBatch>();

	private StaticRendererMode _mode = StaticRendererMode.Shadows;

	public override void ManagerUpdate()
	{
		base.ManagerUpdate();
		if (GameManager.GameState != GameState.Running)
		{
			return;
		}
		if (KeyManager.GetButtonDown(KeyCode.LeftControl) && KeyManager.GetButtonDown(KeyCode.E))
		{
			if (_mode >= StaticRendererMode.Shadows)
			{
				_mode = StaticRendererMode.Disabled;
			}
			else
			{
				_mode++;
			}
			Debug.LogError($"StaticRendererMode: {_mode}");
		}
		if (_mode == StaticRendererMode.Disabled)
		{
			return;
		}
		foreach (RenderBatch batch in Batches)
		{
			if (batch != null)
			{
				if (batch.IsDirty)
				{
					batch.UpdatePropertyBlocks();
				}
				batch.Render((_mode == StaticRendererMode.Shadows && batch.ShadowMode == ShadowCastingMode.On) ? ShadowCastingMode.On : ShadowCastingMode.Off);
			}
		}
	}
}
