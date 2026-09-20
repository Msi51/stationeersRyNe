using Assets.Scripts;
using ThingImport;
using UnityEngine;

public class CelestialPrefab : GameBase
{
	[ReadOnly]
	public string CelestialId;

	public Celestial Body;

	private float _scale;

	public int Hash { get; internal set; }

	public virtual void OnTimeScaleChanged(double timeScale)
	{
	}

	public virtual void Start()
	{
		OrbitalSimulation.Register(this);
		if (!GameManager.IsBatchMode)
		{
			Transform.SetParent(CameraController.Instance.MainCameraTransform);
		}
	}

	public virtual void OnDestroy()
	{
		Body = null;
		OrbitalSimulation.Deregister(this);
	}

	public void Awake()
	{
		Hash = Animator.StringToHash(CelestialId);
		_scale = Transform.localScale.magnitude;
	}

	protected void ApplyTo(TextureReference reference, Material material, int textureProperty)
	{
		material.SetTexture(textureProperty, reference?.Texture);
		if (reference?.Tiling != null)
		{
			material.SetTextureScale(textureProperty, reference.Tiling.ToVector2());
		}
		else
		{
			material.SetTextureScale(textureProperty, Vector2.one);
		}
	}

	public virtual void LateUpdate()
	{
		if (GameManager.IsRunning)
		{
			Body?.ApplyTo(this);
		}
	}

	public virtual void Rescale()
	{
		Transform.localScale = Vector3.one * _scale * OrbitalSimulation.BodyScale;
	}

	public virtual void SetupAsPlayerBody()
	{
	}

	public static void ReturnPools()
	{
		CelestialSprite.ReturnAllPooled();
		RockyBody.ReturnAllPooled();
		AtmosphericBody.ReturnAllPooled();
	}
}
