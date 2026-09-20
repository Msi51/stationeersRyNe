using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using UnityEngine;

public class EffectManager : ManagerBase
{
	public static EffectManager Instance;

	private Transform Picking;

	private Transform Sparks;

	private Transform Explosion;

	private Transform Smoke;

	private Transform Splat;

	public ParticleSystem DestructionParticle;

	public Material[] WorldMaterial;

	public LayerMask AllChunkAndThing;

	private static int _explosionLargeHash = Animator.StringToHash("ExplosionLarge");

	private static int _eggSplatHash = Animator.StringToHash("EggSplat");

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		Instance = this;
	}

	public override void ManagerStart()
	{
		base.ManagerStart();
		Picking = base.transform.Find("Picking");
		Sparks = base.transform.Find("Sparks");
		Explosion = base.transform.Find("Explosion");
		Smoke = base.transform.Find("Smoke");
		Splat = base.transform.Find("Splat");
	}

	public static void SetEffectsMaterial(int index)
	{
		for (int i = 0; i < Instance.Picking.childCount; i++)
		{
			Instance.Picking.GetChild(i).GetComponent<ParticleSystemRenderer>().material = Instance.WorldMaterial[index];
		}
	}

	public GameObject InstantiatePool(Transform target, Vector3 pos, Quaternion rot)
	{
		for (int i = 0; i < target.childCount; i++)
		{
			GameObject gameObject = target.GetChild(i).gameObject;
			if (!gameObject.activeInHierarchy)
			{
				gameObject.transform.position = pos;
				gameObject.transform.rotation = rot;
				return gameObject;
			}
		}
		GameObject obj = Object.Instantiate(target.GetChild(0).gameObject, pos, rot);
		obj.transform.SetParent(target);
		obj.transform.localScale = target.GetChild(0).localScale;
		return obj;
	}

	private static bool isVisible(Vector3 pos)
	{
		Vector3 vector = CameraController.CurrentCamera.WorldToViewportPoint(pos);
		if (vector.z > 0f && vector.x > 0f && vector.x < 1f && vector.y > 0f && vector.y < 1f)
		{
			return true;
		}
		return false;
	}

	public static void CreatePickingEffect(Vector3 pos, float particleMultiplier)
	{
		if (GameManager.IsBatchMode || particleMultiplier <= 0f)
		{
			return;
		}
		particleMultiplier = ((particleMultiplier > 1f) ? 1f : particleMultiplier);
		if (!(Instance == null) && !(Instance.Picking == null) && isVisible(pos))
		{
			ParticleSystem component = Instance.InstantiatePool(Instance.Picking, pos, Quaternion.Euler(-90f, 0f, 0f)).GetComponent<ParticleSystem>();
			ParticleSystem.MainModule main = component.main;
			switch (Settings.GetParticleGrade())
			{
			case 0:
				main.maxParticles = 5;
				break;
			case 1:
				main.maxParticles = 10;
				break;
			default:
				main.maxParticles = 20;
				break;
			}
			main.maxParticles = (int)(Random.value * ((float)main.maxParticles * Mathf.Pow(particleMultiplier, 2f)));
			component.gameObject.SetActive(value: true);
		}
	}

	public static void CreateSparkEffect(Transform target, bool isforced = false)
	{
		if (Instance != null && Instance.Sparks != null && (isVisible(target.position) || isforced))
		{
			ParticleSystem component = Instance.InstantiatePool(Instance.Sparks, target.position, target.rotation).GetComponent<ParticleSystem>();
			component.transform.LookAt(target);
			ParticleSystem.MainModule main = component.main;
			switch (Settings.GetParticleGrade())
			{
			case 0:
				main.maxParticles = 50;
				break;
			case 1:
				main.maxParticles = 300;
				break;
			default:
				main.maxParticles = 500;
				break;
			}
			component.gameObject.SetActive(value: true);
		}
	}

	public static void CreateSmokeEffect(Human human)
	{
		if (Instance != null && Instance.Smoke != null && human != null && (WorldManager.HasGravity || human.Room != null) && human.IsGrounded(Instance.AllChunkAndThing, 0.1f))
		{
			if (human == InventoryManager.Parent)
			{
				human.ActiveRigidbody.AddForce(Vector3.up * 200f);
			}
			if ((!human.IsOccluded && isVisible(human.Position)) || human == InventoryManager.Parent)
			{
				Instance.InstantiatePool(Instance.Smoke, human.Position + Vector3.up * 0.3f, Quaternion.Euler(new Vector3(-90f, 0f, 0f))).SetActive(value: true);
			}
		}
	}

	public static void CreateExplosionEffect(Vector3 position, float radius = 5f)
	{
		if (Instance != null && Instance.Explosion != null && !GameManager.IsBatchMode)
		{
			GameObject obj = Instance.InstantiatePool(Instance.Explosion, position, Quaternion.Euler(-90f, 0f, 0f));
			ParticleSystem component = obj.GetComponent<ParticleSystem>();
			obj.gameObject.SetActive(value: true);
			component.startSize = radius;
			Singleton<AudioManager>.Instance?.PlayAudioClipsData(_explosionLargeHash, position);
			float num = Vector3.SqrMagnitude(position - InventoryManager.WorldPosition);
			if (num > 400f)
			{
				CameraController.SetCameraShake(radius / num / 1f);
			}
		}
	}

	public static void CreateEggSplatEffect(Vector3 position)
	{
		if (!(Instance == null) && !(Instance.Splat == null) && !GameManager.IsBatchMode)
		{
			Instance.InstantiatePool(Instance.Splat, position, Quaternion.identity).gameObject.SetActive(value: true);
		}
	}

	public static void CreateDeconstructionEffect(Thing obj, int numberOfParticles)
	{
		if (!GameManager.IsBatchMode && GameManager.GameState == GameState.Running && !(obj == null))
		{
			Instance.DestructionParticle.transform.position = obj.ThingTransformPosition;
			Instance.DestructionParticle.Emit(numberOfParticles);
		}
	}

	public static void CreateEffect(string parName, Thing thing)
	{
		if (Instance != null)
		{
			Transform transform = Instance.transform.Find(parName);
			if ((bool)transform && isVisible(thing.transform.position) && !thing.IsOccluded)
			{
				Instance.InstantiatePool(transform, thing.CenterPosition, thing.transform.rotation).GetComponent<ParticleSystem>().gameObject.SetActive(value: true);
			}
		}
	}
}
