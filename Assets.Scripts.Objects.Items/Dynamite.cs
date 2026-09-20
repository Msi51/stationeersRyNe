using System.Collections;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Dynamite : Item
{
	[Header("Dynamite")]
	public Light DynamiteLight;

	public float UpOffset;

	private float _actualLifetime;

	private float _explosionForce;

	private const float UpScale = 0.1f;

	private bool _high;

	private Transform _dynamiteLightTransform;

	private Coroutine _dynamiteLightingCoroutine;

	public static float EnergyReleasedPerTick = 100f;

	public override void Start()
	{
		base.Start();
		_dynamiteLightTransform = DynamiteLight.transform;
		_actualLifetime = 5f;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AtmosphericsManager.Instance.Deregister(this);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DynamiteSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is DynamiteSaveData dynamiteSaveData)
		{
			_actualLifetime = dynamiteSaveData.Lifetime;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		DynamiteSaveData dynamiteSaveData = savedData as DynamiteSaveData;
		if (GameManager.GameState != GameState.None && dynamiteSaveData != null)
		{
			dynamiteSaveData.Lifetime = _actualLifetime;
		}
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		if (OnOff || _actualLifetime <= 0f || actionCompletedRatio < 1f)
		{
			return null;
		}
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 0.5f,
			ActionMessage = "Dynamite"
		};
		if (!doAction)
		{
			return result;
		}
		Interactable interactable = Interactables[0];
		InteractionMessage interactionMessage = new InteractionMessage();
		interactionMessage.DestinationId = base.netId;
		interactionMessage.InteractionId = 0;
		interactionMessage.SourceId = base.ParentSlot.Parent.netId;
		interactionMessage.SourceSlotId = base.ParentSlot.SlotIndex;
		interactionMessage.State = interactable.State;
		interactionMessage.AltKey = false;
		interactionMessage.SendToClients();
		return result;
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		if (OnOff && _dynamiteLightingCoroutine == null)
		{
			_dynamiteLightingCoroutine = StartCoroutine(DynamiteOperation());
		}
		else if (_dynamiteLightingCoroutine != null)
		{
			StopCoroutine(_dynamiteLightingCoroutine);
		}
	}

	private IEnumerator WaitThenBurn()
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractOnOff, 1);
		}
		yield break;
	}

	public override void OnFireStart()
	{
		base.OnFireStart();
		if (!OnOff)
		{
			UnityMainThreadDispatcher.Instance().Enqueue(WaitThenBurn());
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (base.WorldAtmosphere == null || base.WorldAtmosphere.Mode == AtmosphereHelper.AtmosphereMode.Global)
		{
			base.WorldAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		}
		if (base.WorldAtmosphere != null && base.WorldAtmosphere.IsValid() && OnOff)
		{
			base.WorldAtmosphere.Sparked = true;
			base.WorldAtmosphere.GasMixture.AddEnergy(new MoleEnergy(EnergyReleasedPerTick));
		}
	}

	public override void Explosion(Vector3 position, float force)
	{
		if (!OnOff)
		{
			_actualLifetime = 0.3f;
			_explosionForce = force;
			OnOff = true;
		}
	}

	private IEnumerator DynamiteOperation()
	{
		if (GameManager.RunSimulation)
		{
			AtmosphericsManager.Instance.Register(this);
		}
		float lastCheck = Time.time;
		while (_actualLifetime > 0f)
		{
			_dynamiteLightTransform.position = base.ThingTransformPosition + ThingTransform.TransformDirection(Vector3.up) * UpOffset + Vector3.up * 0.1f;
			_actualLifetime -= Time.time - lastCheck;
			lastCheck = Time.time;
			yield return Yielders.WaitForSeconds(Random.Range(0.001f, 0.02f));
		}
		if (GameManager.RunSimulation)
		{
			global::Explosion.Explode(1000f, base.ThingTransformLocalPosition, 4f);
			AtmosphericsManager.Instance.Deregister(this);
			OnServer.Destroy(this);
		}
	}
}
