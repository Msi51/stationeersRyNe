using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Weapons;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class RoadFlare : StackableLight, IProjectile
{
	private bool _high;

	private UniTask _roadflareLightingTask;

	[Tooltip("These game objects are enabled when the flare is activated")]
	[SerializeField]
	private GameObject[] activateObjects;

	[SerializeField]
	private GameObject parachute;

	private const int FuseTimeMs = 1200;

	private const int ActivateLaunched = 2;

	private const int ActivateParachuteDeployed = 3;

	private const float ENERGY_RELEASED_PER_TICK = 1000f;

	public ParticleSystem LightParticles;

	private bool HasBeenLaunched => Activate == 2;

	private bool ParachuteIsDeployed => Activate == 3;

	public override void OnDestroy()
	{
		base.OnDestroy();
		AtmosphericsManager.Instance.Deregister(this);
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		ParticleSystem.MainModule main = LightParticles.main;
		main.startColor = CustomColor.Light;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (_roadflareLightingTask.Status != UniTaskStatus.Pending && OnOff)
		{
			_roadflareLightingTask = RoadFlareOperation();
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.OnOff && OnOff && _roadflareLightingTask.Status != UniTaskStatus.Pending)
		{
			SetCustomColor(emissive: true);
			_roadflareLightingTask = RoadFlareOperation();
		}
		if (interactable.Action == InteractableType.Activate)
		{
			switch (interactable.State)
			{
			case 2:
				RigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
				break;
			case 3:
				PlayPooledAudioSound(Defines.Sounds.FlareAirBurstHash, Vector3.zero);
				SetParachutePhysicsAndLighting();
				break;
			default:
				SetNormalPhysicsAndLighting();
				break;
			}
		}
	}

	public void OnProjectileLaunched(Vector3 origin, Vector3 velocity)
	{
		base.transform.rotation *= Quaternion.Euler(90f, 0f, 0f);
		OnServer.Interact(base.InteractActivate, 2);
		BurnFuse().Forget();
	}

	public async UniTaskVoid BurnFuse()
	{
		CancellationToken cancel = this.GetCancellationTokenOnDestroy();
		await UniTask.Delay(1200);
		if (!cancel.IsCancellationRequested)
		{
			if (HasBeenLaunched)
			{
				OnServer.Interact(base.InteractActivate, 3);
				RigidBody.angularVelocity = Vector3.zero;
				base.transform.rotation = quaternion.identity;
			}
			OnServer.Interact(base.InteractOnOff, 1);
		}
	}

	public override void OnCollisionEnter(Collision collision)
	{
		if (ParachuteIsDeployed)
		{
			OnServer.Interact(base.InteractActivate, 1);
		}
		base.OnCollisionEnter(collision);
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		SetNormalPhysicsAndLighting();
	}

	private void SetParachutePhysicsAndLighting()
	{
		RigidBody.drag = 10f;
		RigidBody.mass = 0.1f;
		FlareLight.range = 25f;
		parachute.SetActive(value: true);
	}

	private void SetNormalPhysicsAndLighting()
	{
		RigidBody.drag = 0f;
		RigidBody.mass = 1f;
		FlareLight.range = 10f;
		parachute.SetActive(value: false);
	}

	public override void OnFireStart()
	{
		base.OnFireStart();
		if (!OnOff)
		{
			WaitThenBurn().Forget();
		}
	}

	private Atmosphere GetBurningAtmosphere()
	{
		if (base.ParentSlot?.Parent.InternalAtmosphere != null)
		{
			return base.ParentSlot.Parent.InternalAtmosphere;
		}
		if (base.WorldAtmosphere == null)
		{
			Atmosphere atmosphere = (base.WorldAtmosphere = base.GridController.AtmosphericsController.GetAtmosphereLocal(base.WorldGrid));
		}
		if ((base.WorldAtmosphere == null || base.WorldAtmosphere.Mode == AtmosphereHelper.AtmosphereMode.Global) && AtmosphericsController.ReadonlyGlobalAtmosphere(base.Position.ToGrid()).IsAboveArmstrong())
		{
			base.WorldAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		}
		return base.WorldAtmosphere;
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		Atmosphere burningAtmosphere = GetBurningAtmosphere();
		if (burningAtmosphere != null && burningAtmosphere.IsAboveArmstrong() && OnOff)
		{
			burningAtmosphere.Sparked = true;
			burningAtmosphere.GasMixture.AddEnergy(new MoleEnergy(1000.0));
		}
	}

	private async UniTask RoadFlareOperation()
	{
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		GameObject[] array = activateObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		if (CustomColor != null)
		{
			FlareLight.color = CustomColor.Light;
			ParticleSystem.MainModule main = LightParticles.main;
			main.startColor = CustomColor.Light;
		}
		if (GameManager.RunSimulation)
		{
			AtmosphericsManager.Instance.Register(this);
		}
		await UniTask.NextFrame(cancelToken);
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		SetLightVisibility(!IsOccluded);
		float flickerTimeout = 0f;
		Transform lightTransform = FlareLight.transform;
		while (base.ActualLifetime > 0f)
		{
			if (flickerTimeout <= 0f)
			{
				float num = Mathf.Lerp(0.5f, 3f, base.ActualLifetime / 180f);
				FlareLight.intensity = UnityEngine.Random.Range(num, num + 0.8f);
				flickerTimeout = UnityEngine.Random.Range(0.1f, 0.5f);
			}
			if ((object)ThingTransform != null)
			{
				lightTransform.position = base.ThingTransformPosition + ThingTransform.TransformDirection(Vector3.up) * UpOffset + Vector3.up * 0.1f;
			}
			base.ActualLifetime -= Time.deltaTime;
			flickerTimeout -= Time.deltaTime;
			await UniTask.NextFrame(cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
		}
		if (GameManager.RunSimulation)
		{
			AtmosphericsManager.Instance.Deregister(this);
			OnServer.Destroy(this);
		}
	}
}
