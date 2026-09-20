using System;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class CustomColorMapping
{
	public string name;

	public ThingRenderer ThingRenderer;

	public bool IsTextureArray;

	public int MaterialIndex;

	private MaterialPropertyBlock _propertyBlock;

	private static readonly int DiffuseIndexPropertyID = Shader.PropertyToID("_DiffuseIndex");

	private bool _resetTextureArrayMaterial;

	public CustomColorMapping()
	{
	}

	public CustomColorMapping(ThingRenderer thingRenderer, int materialIndex, int defaultColor)
	{
		name = thingRenderer.MeshFilter.sharedMesh?.name ?? "No Mesh";
		MaterialIndex = materialIndex;
		ThingRenderer = thingRenderer;
		Material[] sharedMaterials = ThingRenderer.sharedMaterials;
		sharedMaterials[MaterialIndex] = GameManager.GetTextureArrayColorMaterial();
		ThingRenderer.sharedMaterials = sharedMaterials;
		IsTextureArray = ThingRenderer.sharedMaterials[MaterialIndex]?.mainTexture is Texture2DArray;
		if (IsTextureArray)
		{
			_propertyBlock = new MaterialPropertyBlock();
			ThingRenderer.GetRenderer().GetPropertyBlock(_propertyBlock, MaterialIndex);
			_propertyBlock.SetFloat(DiffuseIndexPropertyID, defaultColor);
			ThingRenderer.GetRenderer().SetPropertyBlock(_propertyBlock, MaterialIndex);
			ThingRenderer.GetRenderer().SetPropertyBlock(_propertyBlock, MaterialIndex);
		}
	}

	public void SetEmissive(Material material)
	{
		if ((object)material != null)
		{
			Material[] sharedMaterials = ThingRenderer.sharedMaterials;
			sharedMaterials[MaterialIndex] = material;
			ThingRenderer.sharedMaterials = sharedMaterials;
			_resetTextureArrayMaterial = true;
		}
	}

	public void SetColor(Material material, int colorIndex)
	{
		if ((object)material == null)
		{
			return;
		}
		if (IsTextureArray)
		{
			if (_resetTextureArrayMaterial)
			{
				Material[] sharedMaterials = ThingRenderer.sharedMaterials;
				sharedMaterials[MaterialIndex] = GameManager.GetTextureArrayColorMaterial();
				ThingRenderer.sharedMaterials = sharedMaterials;
			}
			ThingRenderer.GetRenderer().GetPropertyBlock(_propertyBlock, MaterialIndex);
			_propertyBlock.SetFloat(DiffuseIndexPropertyID, colorIndex);
			ThingRenderer.GetRenderer().SetPropertyBlock(_propertyBlock, MaterialIndex);
		}
		else
		{
			Material[] sharedMaterials2 = ThingRenderer.sharedMaterials;
			sharedMaterials2[MaterialIndex] = material;
			ThingRenderer.sharedMaterials = sharedMaterials2;
		}
	}

	public void SetColor(Material material)
	{
		if ((object)material != null)
		{
			Material[] sharedMaterials = ThingRenderer.sharedMaterials;
			sharedMaterials[MaterialIndex] = material;
			ThingRenderer.sharedMaterials = sharedMaterials;
		}
	}
}
