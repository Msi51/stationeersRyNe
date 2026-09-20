using UnityEngine;

[ExecuteInEditMode]
public class Pulse : Projection
{
	[SerializeField]
	public Texture2D mainTex;

	[SerializeField]
	public Color color = Color.white;

	private bool[] buffers = new bool[4] { true, true, false, true };

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

	public Color Color
	{
		get
		{
			return color;
		}
		set
		{
			color = value;
			UpdateMaterial();
		}
	}

	public override Material RenderMaterial
	{
		get
		{
			if (transparencyType != TransparencyType.Blend)
			{
				return DynamicDecals.Mat_PulseCutout;
			}
			return DynamicDecals.Mat_Pulse;
		}
	}

	public override int DeferredPass => 1;

	public override bool DeferredPrePass => transparencyType == TransparencyType.Blend;

	protected override void UpdateMaterialProperties()
	{
		base.UpdateMaterialProperties();
		UpdateColor();
	}

	private void UpdateColor()
	{
		if (mainTex != null)
		{
			materialProperties.SetTexture("_MainTex", mainTex);
		}
		Color value = color;
		value.a *= base.AlphaModifier;
		materialProperties.SetColor("_Color", value);
	}

	protected override void UpdateDeferredRendering()
	{
		base.DeferredBuffers = buffers;
		base.UpdateDeferredRendering();
	}

	protected override void UpdateForwardRendering(MeshRenderer Renderer)
	{
		if (Renderer.sharedMaterials.Length != 1)
		{
			Renderer.sharedMaterials = new Material[1];
		}
		if (transparencyType == TransparencyType.Blend)
		{
			Renderer.sharedMaterial = new Material(DynamicDecals.Mat_Pulse);
		}
		else
		{
			Renderer.sharedMaterial = new Material(DynamicDecals.Mat_PulseCutout);
		}
		base.UpdateForwardRendering(Renderer);
	}

	public void CopyAllProperties(Pulse Target, bool IncludeTextures = true)
	{
		if (Target != null)
		{
			CopyBaseProperties(Target);
			CopyProperties(Target, IncludeTextures);
			CopyMaskProperties(Target);
		}
		else
		{
			Debug.LogWarning("No Decal found to copy from");
		}
	}

	public void CopyProperties(Pulse Target, bool IncludeTextures = true)
	{
		if (IncludeTextures)
		{
			MainTex = Target.MainTex;
		}
		Color = Target.Color;
	}
}
