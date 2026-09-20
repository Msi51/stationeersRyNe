using System;
using UnityEngine;

[ExecuteInEditMode]
public class Eraser : Projection
{
	[SerializeField]
	private Texture2D mainTex;

	[SerializeField]
	private float multiplier = 1f;

	[SerializeField]
	private float projectionLimit = 80f;

	private Material renderMaterial;

	private int deferredPass;

	private bool[] buffers;

	public Texture2D MainTex
	{
		get
		{
			return mainTex;
		}
		set
		{
			mainTex = value;
			UpdateMaterial();
		}
	}

	public float AlphaMultiplier
	{
		get
		{
			return multiplier;
		}
		set
		{
			multiplier = Mathf.Clamp01(value);
			UpdateMaterial();
		}
	}

	public float ProjectionLimit
	{
		get
		{
			return projectionLimit;
		}
		set
		{
			projectionLimit = Mathf.Clamp(value, 0f, 180f);
			UpdateMaterial();
		}
	}

	public override Material RenderMaterial => renderMaterial;

	public override int DeferredPass => 1;

	public override bool DeferredPrePass
	{
		get
		{
			if (transparencyType != TransparencyType.Blend)
			{
				return false;
			}
			return true;
		}
	}

	protected override void UpdateMaterialProperties()
	{
		base.UpdateMaterialProperties();
		UpdateShape();
		UpdateProjectionClipping();
	}

	private void UpdateShape()
	{
		if (mainTex != null)
		{
			materialProperties.SetTexture("_MainTex", mainTex);
		}
		materialProperties.SetFloat("_Multiplier", multiplier * base.AlphaModifier);
	}

	private void UpdateProjectionClipping()
	{
		float value = Mathf.Cos(MathF.PI / 180f * projectionLimit);
		materialProperties.SetFloat("_NormalCutoff", value);
	}

	protected override void UpdateDeferredRendering()
	{
		if (transparencyType == TransparencyType.Blend)
		{
			renderMaterial = DynamicDecals.Mat_Eraser;
		}
		else
		{
			renderMaterial = DynamicDecals.Mat_EraserCutout;
		}
		if (buffers == null || buffers.Length != 3)
		{
			buffers = new bool[3];
			buffers[0] = true;
			buffers[1] = true;
			buffers[2] = true;
		}
		base.DeferredBuffers = buffers;
		base.UpdateDeferredRendering();
	}

	protected override void UpdateForwardRendering(MeshRenderer Renderer)
	{
		Material[] array = new Material[2];
		if (transparencyType == TransparencyType.Blend)
		{
			array[0] = new Material(DynamicDecals.Mat_Eraser);
		}
		else
		{
			array[0] = new Material(DynamicDecals.Mat_EraserCutout);
		}
		array[1] = new Material(DynamicDecals.Mat_EraserGrab);
		Renderer.sharedMaterials = array;
		base.UpdateForwardRendering(Renderer);
	}

	public void CopyAllProperties(Eraser Target)
	{
		if (Target != null)
		{
			CopyBaseProperties(Target);
			ProjectionLimit = Target.ProjectionLimit;
			mainTex = Target.mainTex;
			multiplier = Target.multiplier;
			CopyMaskProperties(Target);
		}
		else
		{
			Debug.LogWarning("No Eraser found to copy from");
		}
	}
}
