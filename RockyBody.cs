using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Assets.Scripts;
using Audio;
using UnityEngine;
using UnityEngine.Rendering;

public class RockyBody : SphericalBody, IListable, IGamePoolable<RockyBody>, IPoolable<RockyBody>
{
	private static GameObjectPool<RockyBody> _prefabPool;

	public MeshFilter MeshFilter;

	[ReadOnly]
	public Bounds MeshBounds;

	private static readonly int NormalMap = Shader.PropertyToID("_NormalMap");

	private static readonly int EmissionScale = Shader.PropertyToID("_EmissionScale");

	private const float DEFAULT_EMISSION = 0.5f;

	protected static GameObjectPool<RockyBody> PrefabPool
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

	public ObjectPool<RockyBody> Pool { get; set; }

	public GameObjectPool<RockyBody> GamePool
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

	public static void Initialize(RockyBody celestialSprite)
	{
		_prefabPool = new GameObjectPool<RockyBody>("RockyBody");
		_prefabPool.Initialize(20);
		_prefabPool.PopulateAll(celestialSprite);
	}

	internal static void ReturnToPrefabPool(RockyBody rockyBody)
	{
		OrbitalSimulation.Deregister(rockyBody);
		_prefabPool.Return(rockyBody);
	}

	public static void ReturnAllPooled()
	{
		foreach (RockyBody item in new List<RockyBody>(_prefabPool._active))
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

	public static void AssignFromPool(CelestialBody body, CelestialBodyTemplate template, RockyBodyReference reference)
	{
		RockyBody rockyBody = _prefabPool.Get();
		try
		{
			rockyBody.CelestialId = body.Id;
			rockyBody.Hash = Animator.StringToHash(body.Id);
			rockyBody.Body = body;
			body.Prefab = rockyBody;
			rockyBody.Setup(template, reference);
		}
		catch (Exception exception)
		{
			ConsoleWindow.PrintError(exception).Forget();
		}
	}

	public override void Rescale()
	{
		float num = SphereRadius * MeshBounds.extents.magnitude * OrbitalSimulation.BodyScale;
		Transform.localScale = Vector3.one * num;
		ShadowProjector.orthographicSize = num;
		ShadowProjector.nearClipPlane = 0f;
		ShadowProjector.farClipPlane = num;
	}

	private void Setup(CelestialBodyTemplate template, RockyBodyReference reference)
	{
		SetMaterial();
		if (!GameManager.IsBatchMode)
		{
			Transform.SetParent(CameraController.Instance.MainCameraTransform);
		}
		string text = Regex.Replace(template.Id, "\\s", "");
		base.name = "~RockyBody" + text;
		SphereCollider.enabled = reference.CanOccult;
		SphereRadius = reference.GetRadius();
		SphereRenderer.Renderer.shadowCastingMode = ShadowCastingMode.On;
		ShadowProjector.enabled = true;
		if (reference.Rotation != null)
		{
			Vector3 euler = reference.Rotation.ToVector3();
			SphereRenderer.Transform.localRotation = Quaternion.Euler(euler);
		}
		else
		{
			SphereRenderer.Transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
		}
		Transform.localScale = Vector3.one * SphereRadius;
		base.Material.EnableKeyword("_NORMALMAP");
		base.Material.SetFloat(EmissionScale, reference.Emissive?.Value ?? 0.5f);
		base.Material.SetColor(SphericalBody.Color1, reference.Color?.ToColor() ?? Color.white);
		ApplyTo(reference.TextureRef, base.Material, SphericalBody.MainTex);
		ApplyTo(reference.NormalRef, base.Material, NormalMap);
		MeshFilter.mesh = ((reference.MeshRef != null) ? MeshLibrary.Find(reference.MeshRef.Id) : MeshLibrary.Find(MeshLibrary.DEFAULT_SPHERE));
		MeshBounds = MeshFilter.mesh.bounds;
		Rescale();
	}
}
