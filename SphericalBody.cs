using Assets.Scripts;
using UnityEngine;
using UnityEngine.Rendering;

public abstract class SphericalBody : PooledCelestial
{
	[ReadOnly]
	public float SphereRadius;

	[Header("Assignments")]
	public RendererBase SphereRenderer;

	public SphereCollider SphereCollider;

	public Projector ShadowProjector;

	internal static readonly int Color1 = Shader.PropertyToID("_Color");

	internal static readonly int MainTex = Shader.PropertyToID("_MainTex");

	private static readonly int SunDirection = Shader.PropertyToID("_SunDirection");

	[ReadOnly]
	private Material _material;

	public Material Material => _material;

	public void SetMaterial()
	{
		if ((object)_material == null)
		{
			_material = SphereRenderer.Renderer.material;
		}
	}

	public override void LateUpdate()
	{
		base.LateUpdate();
		if ((object)_material != null)
		{
			_material.SetVector(SunDirection, OrbitalSimulation.WorldSunVector);
		}
	}

	public override void Rescale()
	{
		Transform.localScale = Vector3.one * (SphereRadius * OrbitalSimulation.BodyScale);
	}

	public override void SetupAsPlayerBody()
	{
		if (!GameManager.IsBatchMode)
		{
			SphereRenderer.Renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
			SphereCollider.enabled = false;
			ShadowProjector.enabled = false;
		}
	}

	private void OnEnable()
	{
		Rescale();
	}
}
