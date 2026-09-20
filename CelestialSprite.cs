using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Assets.Scripts;
using Audio;
using UnityEngine;

public class CelestialSprite : PooledCelestial, IListable, IGamePoolable<CelestialSprite>, IPoolable<CelestialSprite>
{
	private static GameObjectPool<CelestialSprite> _prefabPool;

	private static readonly int Color1 = Shader.PropertyToID("_TintColor");

	public Transform SpriteTransform;

	public Renderer SpriteRenderer;

	public LensFlare LensFlare;

	private Material _material;

	private Color _color;

	private float _flareBrightness;

	private float _flareMinimum;

	private float _magnitude;

	protected static GameObjectPool<CelestialSprite> PrefabPool
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

	public ObjectPool<CelestialSprite> Pool { get; set; }

	public GameObjectPool<CelestialSprite> GamePool
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

	public static void Initialize(CelestialSprite celestialSprite)
	{
		_prefabPool = new GameObjectPool<CelestialSprite>("CelestialSprite");
		_prefabPool.Initialize(20);
		_prefabPool.PopulateAll(celestialSprite);
	}

	internal static void ReturnToPrefabPool(CelestialSprite celestialSprite)
	{
		OrbitalSimulation.Deregister(celestialSprite);
		_prefabPool.Return(celestialSprite);
	}

	public static void ReturnAllPooled()
	{
		foreach (CelestialSprite item in new List<CelestialSprite>(_prefabPool._active))
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

	public static void AssignFromPool(CelestialBody body, CelestialBodyTemplate template, CelestialSpriteReference reference)
	{
		CelestialSprite celestialSprite = _prefabPool.Get();
		try
		{
			celestialSprite.CelestialId = body.Id;
			celestialSprite.Hash = Animator.StringToHash(body.Id);
			celestialSprite.Body = body;
			celestialSprite.Setup(template, reference);
			body.Prefab = celestialSprite;
		}
		catch (Exception ex)
		{
			ConsoleWindow.PrintError("error assigning CelestialSprite from pool");
			ConsoleWindow.Print(ex.Message);
			ConsoleWindow.Print(ex.StackTrace);
		}
	}

	private void Setup(CelestialBodyTemplate template, CelestialSpriteReference reference)
	{
		if (!GameManager.IsBatchMode)
		{
			Transform.SetParent(CameraController.Instance.MainCameraTransform);
		}
		string text = Regex.Replace(template.Id, "\\s", "");
		base.name = "~CelestialSprite" + text;
		_material = SpriteRenderer.material;
		_color = template.Color.ToColor();
		_material.SetColor(Color1, _color);
		SpriteRenderer.enabled = true;
		StarfieldSkybox.SetDefaults(_material, 1f, 1f);
		if (SkyBoxController.DefaultData != null)
		{
			StarfieldSkybox.Apply(_material, SkyBoxController.DefaultData);
		}
		_magnitude = reference.GetMagnitude();
		_material.SetFloat(StarfieldSkybox.MAGNITUDE_PROPERTY, _magnitude);
		_material.SetFloat(StarfieldSkybox.BRIGHTNESS_PROPERTY, reference.GetBrightness());
		_material.SetFloat(StarfieldSkybox.MINIMUM_PROPERTY, reference.GetMinimum());
		if (reference.GetMagnitude() < 1f)
		{
			_flareMinimum = reference.GetMinimum() * 0.5f;
			_flareBrightness = ScaleMagnitude(_magnitude) * reference.GetBrightness();
			LensFlare.enabled = _flareBrightness > 0f;
			LensFlare.brightness = _flareBrightness;
			LensFlare.color = template.Color.ToColor();
		}
		else
		{
			_flareBrightness = 0f;
			LensFlare.enabled = false;
		}
	}

	public override void LateUpdate()
	{
		if (Body != null && GameManager.IsRunning)
		{
			Body.ProjectTo(this);
			float b = 1f - (float)(Body.DistanceToPlayer / Body.RangeToPlayer.Maximum);
			b = Mathf.Max(0.1f, b);
			_material.SetFloat(StarfieldSkybox.MAGNITUDE_PROPERTY, Mathf.Lerp(_magnitude * 2f, _magnitude, b));
			SetFlareBrightness(b);
			if ((object)_material != null)
			{
				StarfieldSkybox.UpdateInGame(_material);
			}
		}
	}

	private void SetFlareBrightness(float distanceScale)
	{
		if (!(_flareBrightness <= float.Epsilon))
		{
			if (WorldManager.HasGravity && Transform.position.y <= 0f)
			{
				LensFlare.brightness = 0f;
				return;
			}
			float num = 1f - Mathf.Clamp01(AtmosphericScattering.FogIntensity);
			float b = _flareBrightness * num * distanceScale;
			b = Mathf.Max(_flareMinimum, b);
			LensFlare.brightness = b;
			LensFlare.enabled = b > 0.01f;
		}
	}

	private static float ScaleMagnitude(float value)
	{
		value = Mathf.Max(value, 0.01f);
		float num = Mathf.Log(value);
		float num2 = Mathf.Log(0.1f);
		float num3 = Mathf.Log(1f);
		return 2f * (1f - (num - num3) / (num2 - num3));
	}

	public override void Rescale()
	{
	}

	public override void SetupAsPlayerBody()
	{
		if (!GameManager.IsBatchMode)
		{
			LensFlare.enabled = false;
			SpriteRenderer.enabled = false;
		}
	}
}
