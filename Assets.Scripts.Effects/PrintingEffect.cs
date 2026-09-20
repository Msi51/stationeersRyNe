using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Effects;

public class PrintingEffect : GameBase
{
	public SimpleFabricatorBase Parent;

	public Renderer EffectRenderer;

	public MeshFilter EffectMesh;

	public Material EffectMaterial;

	public BoxCollider BoxCollider;

	public Vector3 Size;

	public Light Light;

	public float LightFlickerSpeed = 2f;

	public float LightFlickerIntensity = 10f;

	private static readonly OpenSimplexNoise LightFlicker = new OpenSimplexNoise(73L);

	public void RefreshMaterialSettings()
	{
		EffectMaterial.SetFloat("_ConstructionLevel", (float)(int)Parent.Processing / 100f);
		EffectMaterial.SetFloat("_ObjectHeight", EffectRenderer.bounds.size.y);
		EffectMaterial.SetVector("_ObjectPosition", Transform.position);
		if (Light != null)
		{
			Light.intensity = 1f + LightFlicker.Evaluate(0f, GameManager.GameTime * LightFlickerSpeed) * LightFlickerIntensity;
		}
	}

	public void SetBlueprint(DynamicThing currentProduct)
	{
		if ((bool)currentProduct && (bool)currentProduct.Blueprint)
		{
			Wireframe component = currentProduct.Blueprint.GetComponent<Wireframe>();
			if ((bool)component)
			{
				EffectMesh.mesh = component.BlueprintMeshFilter.sharedMesh;
				RefreshMaterialSettings();
			}
		}
		else
		{
			EffectMesh.mesh = null;
		}
	}
}
