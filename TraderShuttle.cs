using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using Audio;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using Sound;
using TerrainSystem.Lods;
using Trading;
using UnityEngine;
using WorldLogSystem;

public class TraderShuttle : MonoBehaviour, IAudioParent, INonThingOcclusion
{
	public static Vector3 DefaultTraderSize = new Vector2(3f, 3f);

	[SerializeField]
	private ShuttleType _shuttleType;

	[SerializeField]
	private TraderShuttleAnimationHandler _animationHandler;

	[SerializeField]
	private TraderShuttleAudioHandler _audioHandler;

	[SerializeField]
	private Transform _shuttleCenter;

	public Transform pilotPosition;

	public GameObject GameObject;

	private bool _isBeingDestroyed;

	private float ExplosionForce = 2000f;

	private float ExplosionRadius = 5f;

	private int MaxNumberOfCollisions = 20;

	private MoleQuantity FuelExplosionMoles = new MoleQuantity(1000.0);

	private bool _isDestroyed;

	private bool _canCollide = true;

	private int _numberOfCollisions;

	[NonSerialized]
	public List<PooledAudioSource> PooledAudioSources = new List<PooledAudioSource>(8);

	public NonThingOcclusionHandler OcclusionHandler;

	private Vector3 _cachedTransformPosition;

	private const float RENDER_MAX_DISTANCE = 200f;

	public TraderShuttleAnimationHandler AnimationHandler => _animationHandler;

	public TraderShuttleAudioHandler AudioHandler => _audioHandler;

	public ShuttleType ShuttleType => _shuttleType;

	public Transform ShuttleCenter => _shuttleCenter;

	public LandingPadCenter LandingPadCenter { get; set; }

	public Transform Transform { get; set; }

	public Vector3 TargetPosition { get; set; }

	public Quaternion TargetRotation { get; set; }

	public bool IsBeingDestroyed => _isBeingDestroyed;

	public Transform SoundPosition => GameObject?.transform;

	public Vector3 Position => Transform.position;

	public WorldGrid WorldGrid => new WorldGrid(Position);

	public bool IsSoundLocal()
	{
		return false;
	}

	public async UniTask DepartInvalid(ShuttleAnimationState shuttleAnimationState)
	{
		switch (shuttleAnimationState)
		{
		case ShuttleAnimationState.None:
		case ShuttleAnimationState.Approach:
		case ShuttleAnimationState.Idle:
			await _animationHandler.DepartInvalidOnArrival();
			break;
		case ShuttleAnimationState.Land:
		case ShuttleAnimationState.OpenDoors:
		case ShuttleAnimationState.CloseDoors:
			await _animationHandler.DepartInvalidOnPad();
			break;
		default:
			await _animationHandler.DepartInvalidOnDepart();
			break;
		}
		DestroyShuttle();
		LandingPadCenter = null;
	}

	private void Update()
	{
		_cachedTransformPosition = Transform.position;
		if (_animationHandler.GetAnimationState() != ShuttleAnimationState.None)
		{
			float t = Time.deltaTime * 4f;
			Transform.position = Vector3.Lerp(Transform.position, TargetPosition, t);
			Transform.rotation = Quaternion.Lerp(Transform.rotation, TargetRotation, t);
		}
	}

	private void Awake()
	{
		Transform = base.transform;
		_animationHandler.TraderShuttle = this;
		_audioHandler.TraderShuttle = this;
		GameObject = base.gameObject;
		_cachedTransformPosition = Transform.position;
		switch (ShuttleType)
		{
		case ShuttleType.Small:
		case ShuttleType.SmallGas:
			ExplosionForce = 1000f;
			ExplosionRadius = 3f;
			MaxNumberOfCollisions = 10;
			FuelExplosionMoles = new MoleQuantity(500.0);
			break;
		case ShuttleType.Medium:
		case ShuttleType.MediumGas:
		case ShuttleType.MediumPlane:
			ExplosionForce = 1500f;
			ExplosionRadius = 5f;
			MaxNumberOfCollisions = 10;
			FuelExplosionMoles = new MoleQuantity(2000.0);
			break;
		case ShuttleType.Large:
		case ShuttleType.LargeGas:
		case ShuttleType.LargePlane:
			ExplosionForce = 2000f;
			ExplosionRadius = 7f;
			MaxNumberOfCollisions = 20;
			FuelExplosionMoles = new MoleQuantity(10000.0);
			break;
		case ShuttleType.None:
			break;
		}
	}

	public void SetState(Vector3 position, Vector3 rotation, ShuttleAnimationState animationState)
	{
		TargetPosition = position;
		TargetRotation = Quaternion.Euler(rotation);
		_animationHandler.SetAnimationState(animationState);
	}

	public void DestroyShuttle()
	{
		EnsureCurrentContactIsNotCurrentlyTrading();
		_numberOfCollisions = 0;
		Singleton<TraderShuttlePool>.Instance?.RemoveShuttle(this);
		if ((bool)GameObject)
		{
			UnityEngine.Object.Destroy(GameObject);
		}
		_isBeingDestroyed = true;
	}

	private void EnsureCurrentContactIsNotCurrentlyTrading()
	{
		TraderContact traderContact = LandingPadCenter?.CurrentTradingContact;
		if (traderContact != null)
		{
			traderContact.CurrentlyTrading = false;
		}
	}

	private async UniTaskVoid CollisionDebounce()
	{
		_canCollide = false;
		await UniTask.Delay(20);
		_canCollide = true;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (_isDestroyed || !other || other.isTrigger)
		{
			return;
		}
		Structure structure = Thing.Find(other) as Structure;
		if (structure is LandingPadModular)
		{
			return;
		}
		if (_canCollide && (bool)structure)
		{
			CollisionDebounce().Forget();
			_numberOfCollisions++;
			structure.DamageState.Damage(ChangeDamageType.Increment, 1000f, DamageUpdateType.Brute);
		}
		if (_numberOfCollisions >= MaxNumberOfCollisions && (object)structure != null)
		{
			LogCrashEvent(structure.DisplayName, structure.ReferenceId, structure.Position);
			ExplodeEffects();
		}
		else
		{
			if ((bool)structure)
			{
				return;
			}
			if ((bool)other.GetComponent<LodMeshRenderer>())
			{
				ExplodeEffects();
				return;
			}
			TraderShuttle componentInParent = other.GetComponentInParent<TraderShuttle>();
			if ((bool)componentInParent && componentInParent != this)
			{
				ExplodeEffects();
			}
		}
	}

	private void ExplodeEffects()
	{
		if ((bool)_shuttleCenter)
		{
			_isDestroyed = true;
			GasMixture gasMixture = GasMixtureHelper.Create();
			gasMixture.Add(new Mole(Chemistry.GasType.Methane, FuelExplosionMoles * 2.0 / 3.0, MoleEnergy.Zero));
			gasMixture.Add(new Mole(Chemistry.GasType.Oxygen, FuelExplosionMoles * 1.0 / 3.0, MoleEnergy.Zero));
			gasMixture.AddEnergy(IdealGas.Energy(gasMixture.HeatCapacity, new TemperatureKelvin(1000.0)));
			AtmosphericEventInstance.CloneGlobalAddGasMix(new WorldGrid(_shuttleCenter.position), gasMixture, spark: true);
			Explosion.Explode(ExplosionForce, _shuttleCenter.position, ExplosionRadius, float.MaxValue, mineTerrain: true);
			if ((bool)LandingPadCenter)
			{
				LandingPadCenter.ShuttleDepartImmediate();
			}
		}
	}

	private void LogCrashEvent(string collisionName, long collisionId, Vector3 position)
	{
		string contactName = LandingPadCenter.CurrentTradingContact.ContactName;
		WorldLog.Append(new TraderCrashEvent(GameStrings.TraderCrashedEvent.AsString(contactName, collisionName, position.ToString()), LandingPadCenter.ReferenceId, collisionId, position));
	}

	public void Add(PooledAudioSource pooledAudio)
	{
		PooledAudioSources.Add(pooledAudio);
	}

	public void Remove(PooledAudioSource pooledAudio)
	{
		PooledAudioSources.Remove(pooledAudio);
	}

	public void OnDestroy()
	{
		for (int num = PooledAudioSources.Count - 1; num >= 0; num--)
		{
			PooledAudioSource pooledAudioSource = PooledAudioSources[num];
			if (!(pooledAudioSource == null))
			{
				pooledAudioSource.Stop(immediate: true);
			}
		}
	}

	public void CacheRenderers()
	{
		OcclusionHandler.CacheRenderers();
	}

	public bool CanSetOcclusion()
	{
		return !IsBeingDestroyed;
	}

	public float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(200f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public Vector3 GetCachedTransformPosition()
	{
		return _cachedTransformPosition;
	}
}
