using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Objects.Rockets.Scanning;
using Trading;
using UnityEngine;

namespace Objects.Rockets;

public class CrewModule : Fuselage, IUmbilical, IRocketComponent, IReferencable, IEvaluable
{
	[SerializeField]
	private Transform _exitTransform;

	[SerializeField]
	private MultiStateDoorAnimComponent _doorAnimComponent;

	[HideInInspector]
	public RocketCrewUmbilical PartnerUmbilical;

	public const float PLASMA_FADE_START_ALTITUDE = 1600f;

	public const float PLASMA_FADE_END_ALTITUDE = 2500f;

	[Header("Launch Plasma")]
	[SerializeField]
	[Tooltip("Shell mesh hugging the hull, using the Custom/CrewModulePlasma shader.")]
	private Renderer _plasmaRenderer;

	[SerializeField]
	[Tooltip("Lights placed outside the cabin windows; driven to match the plasma.")]
	private Light[] _plasmaLights;

	[SerializeField]
	[Tooltip("Speed (m/s) at which the plasma starts to ignite.")]
	private float _plasmaStartSpeed = 12f;

	[SerializeField]
	[Tooltip("Speed (m/s) at which the plasma is at full intensity.")]
	private float _plasmaFullSpeed = 70f;

	[SerializeField]
	private float _plasmaMaxLightIntensity = 0.3f;

	[SerializeField]
	[Range(0f, 1f)]
	private float _plasmaLightFlicker = 0.35f;

	[SerializeField]
	[Range(0f, 0.2f)]
	[Tooltip("Positional jitter (m) applied to the plasma lights at full intensity.")]
	private float _plasmaLightShake = 0.025f;

	private Vector3 _exteriorPosition;

	private WorldGrid _exteriorGrid;

	private static readonly int PlasmaSpeedId = Shader.PropertyToID("_Speed");

	private static readonly int PlasmaAltitudeId = Shader.PropertyToID("_Altitude");

	private static readonly int PlasmaDensityId = Shader.PropertyToID("_AtmosphereDensity");

	private static readonly int PlasmaFlightDirId = Shader.PropertyToID("_FlightDir");

	private static readonly int PlasmaFadeStartId = Shader.PropertyToID("_FadeStartAltitude");

	private static readonly int PlasmaFadeEndId = Shader.PropertyToID("_FadeEndAltitude");

	private const float PLASMA_RESAMPLE_INTERVAL = 5f;

	private const float PLASMA_VISIBLE_THRESHOLD = 0.004f;

	private MaterialPropertyBlock _plasmaBlock;

	private float _plasmaIntensity;

	private float _plasmaResampleTimer;

	private float _plasmaDensity;

	private static readonly Color PlasmaLightColour = new Color(1f, 0.5f, 0.45f);

	private Vector3[] _plasmaLightBasePositions;

	public override float MassContribution => 1200f;

	public Thing AsThing => this;

	public Type PartnerType => typeof(RocketCrewUmbilical);

	public int PartnerDistance { get; set; }

	public Vector3 FirstPartnerSearchPosition => base.Position + Forward * SmallGrid.SmallGridSize;

	public UmbilicalType UmbilicalType => UmbilicalType.Socket;

	public float GetActionProgress => 0f;

	public IRocketActionProgressableTarget CurrentTarget => null;

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		RocketUmbilicalHelper.FindAndSetOtherUmbilicalLargeGrid(this);
		AtmosphericsManager.Instance.Register(this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		if ((object)PartnerUmbilical != null)
		{
			PartnerUmbilical.PartnerRemoved();
		}
		AtmosphericsManager.Instance.Deregister(this);
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		_doorAnimComponent?.RefreshState(skipAnimation);
	}

	public override void InitInternalAtmosphere()
	{
		base.InitInternalAtmosphere();
		if (base.InternalAtmosphere == null)
		{
			Atmosphere atmosphere = (base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(8000.0), 0L));
		}
		base.InternalAtmosphere.NeverReset = true;
	}

	public Slot GetSeatSlot()
	{
		foreach (IRocketInternals @internal in base.RocketNetwork.Internals)
		{
			if (@internal is CrewModuleChair crewModuleChair && crewModuleChair.Slots[0].IsEmpty())
			{
				return crewModuleChair.Slots[0];
			}
		}
		return null;
	}

	private Atmosphere GetExitAtmosphere()
	{
		return base.AtmosphericsController.CloneGlobalAtmosphere(_exteriorGrid, 0L, setStateActive: true, allowCrewModules: false);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!IsOpen)
		{
			return;
		}
		Atmosphere exitAtmosphere = GetExitAtmosphere();
		if (AtmosphereHelper.IsSubmerged(_exteriorPosition, exitAtmosphere))
		{
			AtmosphereHelper.Mix(base.InternalAtmosphere, exitAtmosphere, AtmosphereHelper.MatterState.All);
			return;
		}
		AtmosphereHelper.Mix(base.InternalAtmosphere, exitAtmosphere, AtmosphereHelper.MatterState.Gas);
		MoleQuantity getTotalMolesLiquids = base.InternalAtmosphere.GasMixture.GetTotalMolesLiquids;
		if (getTotalMolesLiquids > MoleQuantity.Zero)
		{
			exitAtmosphere.Add(base.InternalAtmosphere.Remove(getTotalMolesLiquids, AtmosphereHelper.MatterState.Liquid));
		}
	}

	public void OnLaunch(bool immediate)
	{
		if ((object)PartnerUmbilical != null)
		{
			if (PartnerUmbilical.IsOpen)
			{
				PartnerUmbilical.DamageState.Damage(ChangeDamageType.Set, PartnerUmbilical.DamageState.MaxDamage, DamageUpdateType.Brute);
			}
			else
			{
				PartnerUmbilical.PartnerRemoved();
			}
			PartnerUmbilical = null;
		}
	}

	public void OnLanded(bool immediate)
	{
		RocketUmbilicalHelper.FindAndSetOtherUmbilicalLargeGrid(this);
		_exteriorPosition = _exitTransform.position;
		_exteriorGrid = new WorldGrid(_exteriorPosition);
	}

	private void Update()
	{
		UpdateLaunchPlasma();
	}

	private void UpdateLaunchPlasma()
	{
		if (_plasmaRenderer == null || base.IsBeingDestroyed)
		{
			return;
		}
		float target = 0f;
		float num = 0f;
		float value = 1f;
		float value2 = 1f;
		float num2 = Mathf.Max(2500f, 1f);
		Rocket rocket = base.RocketNetwork?.Rocket;
		if (GameManager.GameState == GameState.Running && rocket != null && IsAtmosphericFlight(rocket) && IsLocalPlayerSeatedInRocket())
		{
			SamplePlasmaWorldAtmosphere();
			num = Mathf.Clamp01(Mathf.InverseLerp(_plasmaStartSpeed, _plasmaFullSpeed, Mathf.Abs(rocket.Velocity)));
			value = Mathf.Clamp01(rocket.GetAltitude() / num2);
			value2 = ((rocket.RocketState == RocketState.Launching) ? 1f : (-1f));
			float num3 = Mathf.Clamp01(Mathf.InverseLerp(1600f, 2500f, rocket.GetAltitude()));
			float num4 = 1f - num3 * num3 * (3f - 2f * num3);
			target = num * num * num4 * _plasmaDensity;
		}
		_plasmaIntensity = Mathf.MoveTowards(_plasmaIntensity, target, Time.deltaTime * 0.75f);
		bool flag = _plasmaIntensity > 0.004f;
		if (_plasmaRenderer.gameObject.activeSelf != flag)
		{
			_plasmaRenderer.gameObject.SetActive(flag);
		}
		if (_plasmaLights != null)
		{
			Light[] plasmaLights = _plasmaLights;
			foreach (Light light in plasmaLights)
			{
				if (light != null && light.gameObject.activeSelf != flag)
				{
					light.gameObject.SetActive(flag);
				}
			}
		}
		if (!flag)
		{
			return;
		}
		if (_plasmaBlock == null)
		{
			_plasmaBlock = new MaterialPropertyBlock();
		}
		_plasmaBlock.SetFloat(PlasmaSpeedId, num);
		_plasmaBlock.SetFloat(PlasmaAltitudeId, value);
		_plasmaBlock.SetFloat(PlasmaDensityId, _plasmaDensity);
		_plasmaBlock.SetFloat(PlasmaFlightDirId, value2);
		_plasmaBlock.SetFloat(PlasmaFadeStartId, Mathf.Clamp01(1600f / num2));
		_plasmaBlock.SetFloat(PlasmaFadeEndId, 1f);
		_plasmaRenderer.SetPropertyBlock(_plasmaBlock);
		if (_plasmaLights == null)
		{
			return;
		}
		if (_plasmaLightBasePositions == null || _plasmaLightBasePositions.Length != _plasmaLights.Length)
		{
			_plasmaLightBasePositions = new Vector3[_plasmaLights.Length];
			for (int j = 0; j < _plasmaLights.Length; j++)
			{
				if (_plasmaLights[j] != null)
				{
					_plasmaLightBasePositions[j] = _plasmaLights[j].transform.localPosition;
				}
			}
		}
		float num5 = (float)(GetInstanceID() & 0x3FF) * 0.37f;
		Color color = Color.Lerp(PlasmaLightColour, Color.white, 0.2f * _plasmaIntensity);
		float num6 = _plasmaLightShake * _plasmaIntensity;
		float x = Time.time * 11f;
		for (int k = 0; k < _plasmaLights.Length; k++)
		{
			Light light2 = _plasmaLights[k];
			if (!(light2 == null))
			{
				float num7 = num5 + (float)k * 7.91f;
				float b = 1f + _plasmaLightFlicker * (Mathf.PerlinNoise(x, num7) - 0.5f) * 2f;
				light2.intensity = _plasmaMaxLightIntensity * _plasmaIntensity * Mathf.Max(0f, b);
				light2.color = color;
				Vector3 vector = new Vector3(Mathf.PerlinNoise(x, num7 + 31.7f) - 0.5f, Mathf.PerlinNoise(x, num7 + 63.1f) - 0.5f, Mathf.PerlinNoise(x, num7 + 94.6f) - 0.5f);
				light2.transform.localPosition = _plasmaLightBasePositions[k] + vector * (2f * num6);
			}
		}
	}

	private bool IsLocalPlayerSeatedInRocket()
	{
		if (InventoryManager.ParentHuman?.ParentSlot?.Parent is IRocketInternals rocketInternals)
		{
			return rocketInternals.RocketNetwork == base.RocketNetwork;
		}
		return false;
	}

	private static bool IsAtmosphericFlight(Rocket rocket)
	{
		switch (rocket.RocketState)
		{
		case RocketState.Launching:
		{
			NodeType? nodeType = rocket.CurrentNode?.NodeType;
			return !nodeType.HasValue || nodeType != NodeType.LowOrbitLaunchPad;
		}
		case RocketState.Landing:
		{
			NodeType? nodeType = rocket.CurrentTransit?.Destination?.NodeType;
			return !nodeType.HasValue || nodeType != NodeType.LowOrbitLaunchPad;
		}
		default:
			return false;
		}
	}

	private void SamplePlasmaWorldAtmosphere()
	{
		_plasmaResampleTimer -= Time.deltaTime;
		if (!(_plasmaResampleTimer > 0f))
		{
			_plasmaResampleTimer = 5f;
			float num = PlanetaryAtmosphereSimulation.GlobalPressure.ToFloat();
			_plasmaDensity = ((num > 0f) ? Mathf.Clamp01(Mathf.Pow(num / 100f, 0.35f)) : 0f);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		return interactable.Action switch
		{
			InteractableType.Button1 => InteractWithButton1(interactable, interaction, doAction), 
			InteractableType.Open => InteractWithOpen(interactable, interaction, doAction), 
			_ => base.InteractWith(interactable, interaction, doAction), 
		};
	}

	private DelayedActionInstance InteractWithOpen(Interactable interactable, Interaction interaction, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (!doAction)
		{
			return delayedActionInstance.Succeed();
		}
		OnServer.Interact(base.InteractOpen, (!IsOpen) ? 1 : 0);
		return delayedActionInstance.Succeed();
	}

	private DelayedActionInstance InteractWithButton1(Interactable interactable, Interaction interaction, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		Slot seatSlot = GetSeatSlot();
		if (seatSlot == null)
		{
			return delayedActionInstance.Fail(GameStrings.RocketNoAvailableSeat);
		}
		if (!doAction)
		{
			return delayedActionInstance.Succeed();
		}
		if (GameManager.RunSimulation)
		{
			OnServer.MoveToSlot(interaction.SourceThing.AsDynamicThing, seatSlot);
		}
		return delayedActionInstance.Succeed();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new CrewModuleSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		if (thingSaveData is CrewModuleSaveData crewModuleSaveData)
		{
			PartnerDistance = crewModuleSaveData.PartnerDistance;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		if (thingSaveData is CrewModuleSaveData crewModuleSaveData)
		{
			crewModuleSaveData.PartnerDistance = PartnerDistance;
		}
	}

	public void PartnerRemoved()
	{
		PartnerUmbilical = null;
	}

	public void RetractUmbilical()
	{
	}

	public void ExtendUmbilical()
	{
	}

	public bool IsCompatibleWith(IUmbilical other)
	{
		return other is RocketCrewUmbilical;
	}

	public void SetPartner(IUmbilical partner)
	{
		PartnerUmbilical = partner as RocketCrewUmbilical;
	}

	public bool CanProgressAction(out RocketActionResult result)
	{
		result = RocketActionResult.Failure(GameStrings.None);
		return result;
	}

	public void ProgressTransferAction(float deltaTime, RocketTransfer rocketTransfer)
	{
	}

	public void ClearAction()
	{
	}

	public string GetActionInfoText()
	{
		return "";
	}

	public List<IRocketActionProgressableTarget> GetValidTargets()
	{
		return null;
	}

	public void SetTarget(IRocketActionProgressableTarget selectedTarget)
	{
	}
}
