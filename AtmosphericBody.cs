using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Assets.Scripts;
using Audio;
using UnityEngine;
using UnityEngine.Rendering;

public class AtmosphericBody : SphericalBody, IListable, IGamePoolable<AtmosphericBody>, IPoolable<AtmosphericBody>
{
	private static GameObjectPool<AtmosphericBody> _prefabPool;

	private static readonly int CloudMap = Shader.PropertyToID("_CloudTex");

	public RendererBase RingRenderer;

	private static readonly int FresnelColor = Shader.PropertyToID("_FresnelColor");

	private static readonly int FresnelPower = Shader.PropertyToID("_FresnelPower");

	private static readonly int FresnelEmission = Shader.PropertyToID("_FresnelEmission");

	private static readonly int CloudOpacity = Shader.PropertyToID("_CloudOpacity");

	private static readonly int CloudSpeed = Shader.PropertyToID("_CloudSpeed");

	private static readonly int CloudParallax = Shader.PropertyToID("_CloudParallax");

	private static readonly int CloudShadow = Shader.PropertyToID("_CloudShadow");

	private static readonly int CloudDistortion = Shader.PropertyToID("_CloudDistortion");

	private static readonly int Metallic = Shader.PropertyToID("_Metallic");

	private static readonly int Smoothness = Shader.PropertyToID("_Glossiness");

	internal static readonly int NormalTex = Shader.PropertyToID("_NormalTex");

	internal static readonly int SpecularTex = Shader.PropertyToID("_SpecularTex");

	private static readonly int SimSpeed = Shader.PropertyToID("_SimSpeed");

	private static readonly int RimLow = Shader.PropertyToID("_RimLow");

	private static readonly int RimHigh = Shader.PropertyToID("_RimHigh");

	protected static GameObjectPool<AtmosphericBody> PrefabPool
	{
		get
		{
			return _prefabPool;
		}
		set
		{
			_prefabPool = value;
		}
	}

	public int PoolId { get; set; }

	public string DebugName { get; set; }

	public bool IsActive { get; set; }

	public ObjectPool<AtmosphericBody> Pool { get; set; }

	public GameObjectPool<AtmosphericBody> GamePool
	{
		get
		{
			return PrefabPool;
		}
		set
		{
			PrefabPool = value;
		}
	}

	public static void Initialize(AtmosphericBody celestialSprite)
	{
		_prefabPool = new GameObjectPool<AtmosphericBody>("AtmosphericBody");
		_prefabPool.Initialize(20);
		_prefabPool.PopulateAll(celestialSprite);
	}

	internal static void ReturnToPrefabPool(AtmosphericBody celestialSprite)
	{
		OrbitalSimulation.Deregister(celestialSprite);
		_prefabPool.Return(celestialSprite);
	}

	public static void ReturnAllPooled()
	{
		foreach (AtmosphericBody item in new List<AtmosphericBody>(_prefabPool._active))
		{
			ReturnToPrefabPool(item);
		}
	}

	public void DrawInList(ref int index)
	{
		throw new NotImplementedException();
	}

	public void ReturnToPool()
	{
		_prefabPool.Return(this);
	}

	public static void AssignFromPool(CelestialBody body, CelestialBodyTemplate template, AtmosphericBodyReference reference)
	{
		AtmosphericBody atmosphericBody = _prefabPool.Get();
		atmosphericBody.CelestialId = body.Id;
		atmosphericBody.Hash = Animator.StringToHash(body.Id);
		atmosphericBody.Body = body;
		atmosphericBody.Setup(template, reference);
		body.Prefab = atmosphericBody;
	}

	public override void LateUpdate()
	{
		base.LateUpdate();
		if ((object)base.Material != null)
		{
			base.Material.SetFloat(SimSpeed, (float)OrbitalSimulation.GetTimeScale());
		}
	}

	private void Setup(CelestialBodyTemplate template, AtmosphericBodyReference reference)
	{
		if (!GameManager.IsBatchMode)
		{
			SetMaterial();
			Transform.SetParent(CameraController.Instance.MainCameraTransform);
		}
		string text = Regex.Replace(template.Id, "\\s", "");
		base.name = "~AtmosphericBody" + text;
		SphereCollider.enabled = reference.CanOccult;
		SphereRadius = reference.GetRadius();
		if (!GameManager.IsBatchMode)
		{
			RingRenderer.SetVisible(reference.PlanetaryRing != null && reference.PlanetaryRing.Enabled);
			SphereRenderer.Renderer.shadowCastingMode = ShadowCastingMode.On;
			ShadowProjector.enabled = true;
		}
		Transform.localScale = Vector3.one * SphereRadius;
		if (reference.Rotation != null)
		{
			Vector3 euler = reference.Rotation.ToVector3();
			SphereRenderer.Transform.localRotation = Quaternion.Euler(euler);
		}
		else
		{
			SphereRenderer.Transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
		}
		if (!GameManager.IsBatchMode)
		{
			base.Material.EnableKeyword("_NORMALMAP");
			base.Material.SetColor(SphericalBody.Color1, reference.Color?.ToColor() ?? Color.white);
			base.Material.SetColor(FresnelColor, reference.Fresnel?.ToColor() ?? Color.white);
			base.Material.SetFloat(FresnelPower, reference.Fresnel?.Power ?? 1f);
			base.Material.SetFloat(FresnelEmission, reference.Fresnel?.Emission ?? 1f);
			base.Material.SetColor(RimLow, reference.Fresnel?.RimLow?.ToColor() ?? Color.black);
			base.Material.SetColor(RimHigh, reference.Fresnel?.RimHigh?.ToColor() ?? Color.black);
			base.Material.SetFloat(CloudOpacity, reference.CloudRef?.Opacity ?? 0f);
			base.Material.SetFloat(CloudSpeed, reference.CloudRef?.Speed ?? 0f);
			base.Material.SetFloat(CloudDistortion, (reference.CloudRef?.Distortion?.Value).GetValueOrDefault());
			CloudShadowReference cloudShadowReference = reference.CloudRef?.Shadow;
			base.Material.SetFloat(CloudParallax, cloudShadowReference?.Parallax ?? 0f);
			base.Material.SetFloat(CloudShadow, cloudShadowReference?.Opacity ?? 0f);
			base.Material.SetFloat(Metallic, reference.MaterialRef?.Metallic ?? 0f);
			base.Material.SetFloat(Smoothness, reference.MaterialRef?.Smoothness ?? 0f);
			ApplyTo(reference.NormalRef, base.Material, NormalTex);
			ApplyTo(reference.SpecularRef, base.Material, SpecularTex);
			ApplyTo(reference.TextureRef, base.Material, SphericalBody.MainTex);
			ApplyTo(reference.CloudRef, base.Material, CloudMap);
		}
		Rescale();
	}

	public override void Rescale()
	{
		float num = SphereRadius * OrbitalSimulation.BodyScale;
		Transform.localScale = Vector3.one * num;
		ShadowProjector.orthographicSize = num;
		ShadowProjector.nearClipPlane = 0f;
		ShadowProjector.farClipPlane = num * 2f;
	}
}
