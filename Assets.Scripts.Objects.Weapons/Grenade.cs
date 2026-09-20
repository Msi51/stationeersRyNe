using System.Collections;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Weapons;

public class Grenade : CharacterItem
{
	private const float ExplosionForce = 1600f;

	private const float ExplosionRadius = 4.3f;

	private const float EnergyReleasedPerTick = 100f;

	[Header("Grenade")]
	private float _actualLifetime = 5f;

	private Coroutine _grenadeCoroutine;

	private bool _started;

	public override void OnDestroy()
	{
		base.OnDestroy();
		AtmosphericsManager.Instance.Deregister(this);
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.FireArmCategory);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new GrenadeSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is GrenadeSaveData grenadeSaveData)
		{
			_actualLifetime = grenadeSaveData.Lifetime;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		GrenadeSaveData grenadeSaveData = savedData as GrenadeSaveData;
		if (GameManager.GameState != GameState.None && grenadeSaveData != null)
		{
			grenadeSaveData.Lifetime = _actualLifetime;
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
			ActionMessage = "Grenade"
		};
		if (!doAction)
		{
			return result;
		}
		Interactable interactable = Interactables[0];
		if (NetworkManager.IsClient)
		{
			NetworkClient.SendToServer(new InteractionMessage
			{
				DestinationId = base.netId,
				InteractionId = 0,
				SourceId = base.ParentSlot.Parent.netId,
				SourceSlotId = base.ParentSlot.SlotIndex,
				State = interactable.State,
				AltKey = false
			});
		}
		return result;
	}

	public override bool ShouldToggleOn()
	{
		if (!OnOff)
		{
			return !_started;
		}
		return false;
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		if (OnOff && _grenadeCoroutine == null)
		{
			_grenadeCoroutine = StartCoroutine(GrenadeOperation());
		}
		else if (_grenadeCoroutine != null)
		{
			StopCoroutine(_grenadeCoroutine);
		}
	}

	private IEnumerator WaitThenExplode()
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
			UnityMainThreadDispatcher.Instance().Enqueue(WaitThenExplode());
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
			base.WorldAtmosphere.GasMixture.AddEnergy(new MoleEnergy(100.0));
		}
	}

	public override void Explosion(Vector3 position, float force)
	{
		if (!OnOff)
		{
			_actualLifetime = 0.3f;
			OnOff = true;
		}
	}

	private IEnumerator GrenadeOperation()
	{
		if (GameManager.RunSimulation)
		{
			AtmosphericsManager.Instance.Register(this);
		}
		float lastCheck = Time.time;
		while (_actualLifetime > 0f)
		{
			_actualLifetime -= Time.time - lastCheck;
			lastCheck = Time.time;
			yield return Yielders.WaitForSeconds(Random.Range(0.001f, 0.02f));
		}
		if (GameManager.RunSimulation)
		{
			global::Explosion.Explode(1600f, base.transform.position, 4.3f);
			AtmosphericsManager.Instance.Deregister(this);
			OnServer.Destroy(this);
		}
	}

	public override void OnDamageDestroyed()
	{
		_grenadeCoroutine = StartCoroutine(GrenadeOperation());
	}
}
