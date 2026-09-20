using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using Sound;
using Trading;
using UnityEngine;
using Util;

namespace Assets.Scripts.Objects;

public class DynamicGasCanister : PortableAtmospherics, IVisuallyConnectable, IReferencable, IEvaluable
{
	[Header("Dynamic Gas Canister")]
	public Transform Wheel;

	public Collider MainCollider;

	public List<SpawnGas> SpawnContents = new List<SpawnGas>();

	[SerializeField]
	private GameObject openLid;

	[SerializeField]
	private GameObject[] displays;

	[SerializeField]
	private MaterialChanger displayCritical;

	public static float MinSetting = 0f;

	public float MaxSetting = 10132.5f;

	public const float STANDARD_PRESSURE = 8106f;

	public static readonly PressurekPa StandardPressure = new PressurekPa(8106.0);

	public new static PressurekPa PressurePerTick = Chemistry.OneAtmosphere * 2.0;

	public AtmosphereHelper.MatterState MatterType = AtmosphereHelper.MatterState.Gas;

	private float _outputSetting;

	public PressurekPa PressureDelta;

	public Mesh BrokenMesh;

	public GameObject[] meshDisable;

	private float _currentWheelRotation;

	private Task _rotatingWheel;

	private static readonly int AirReleaseLoudHash = Animator.StringToHash("AirReleaseLoud");

	private static readonly int AirReleaseThinHash = Animator.StringToHash("AirReleaseThin");

	private static readonly int ValveTurnHash = Animator.StringToHash("ValveTurn");

	private static readonly int ValveTurnSmallHash = Animator.StringToHash("ValveTurnSmall");

	private static readonly int ValveCloseHash = Animator.StringToHash("ValveClose");

	private static readonly float AudioLerpSpeed = 2f;

	private float _audioPressureRatio;

	private GameAudioEvent _airReleaseThin;

	private GameAudioEvent _airReleaseLoud;

	private readonly string _deconstructInterfaceName = "DeconstructCanisterFull";

	private int _damageTicker;

	private bool _isPlayingAirReleaseSounds;

	private bool _obscuredDrag;

	private CancellationTokenWrapper _wheelRotation = new CancellationTokenWrapper();

	private GasCanisterPressureState _pressureState;

	private float _explosionForce = 1850f;

	private float _explosionRadius = 5f;

	private float _maxExplosionRadius = 20f;

	private bool _hasBlown;

	public Slot GasCanisterSlot => Slots[0];

	[ByteArraySync]
	public float OutputSetting
	{
		get
		{
			return _outputSetting;
		}
		set
		{
			int nameHash = ((Mathf.Abs(OutputSetting - value) > 1f) ? ValveTurnHash : ValveTurnSmallHash);
			if (!Mathf.Approximately(value, OutputSetting))
			{
				PlaySound(nameHash);
			}
			_outputSetting = value;
			if (GameManager.GameState == GameState.Running)
			{
				_wheelRotation.CancelAndInitialize();
				RotateWheel(_wheelRotation.Token).Forget();
			}
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public bool IsPlayingAirReleaseSounds
	{
		get
		{
			return _isPlayingAirReleaseSounds;
		}
		set
		{
			if (value != _isPlayingAirReleaseSounds)
			{
				_isPlayingAirReleaseSounds = value;
				if (_isPlayingAirReleaseSounds)
				{
					PlaySound(AirReleaseLoudHash);
					PlaySound(AirReleaseThinHash);
				}
				else
				{
					StopSound(AirReleaseLoudHash);
					StopSound(AirReleaseThinHash);
				}
			}
		}
	}

	public bool HasBlown => _hasBlown;

	public override void Awake()
	{
		base.Awake();
		_airReleaseThin = GetAudioEvent(AirReleaseThinHash);
		_airReleaseLoud = GetAudioEvent(AirReleaseLoudHash);
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		if (!WorldManager.IsGamePaused)
		{
			_audioPressureRatio = Mathf.Lerp(_audioPressureRatio, Mathf.Clamp01((PressureDelta / PressurePerTick).ToFloat()), AudioLerpSpeed * deltaTime);
			if (_audioPressureRatio <= float.Epsilon || PressureDelta <= PressurekPa.Zero || PortablesConnector != null || IsBroken || base.InternalAtmosphere.TotalMoles <= Chemistry.MINIMUM_QUANTITY_MOLES)
			{
				IsPlayingAirReleaseSounds = false;
				return;
			}
			IsPlayingAirReleaseSounds = true;
			float volumeMultiplier = AudioCurves.Instance.DynamicCanisterAirThinVol.Evaluate(_audioPressureRatio);
			float pitch = AudioCurves.Instance.DynamicCanisterAirThinPitch.Evaluate(_audioPressureRatio);
			float volumeMultiplier2 = AudioCurves.Instance.DynamicCanisterAirLoud.Evaluate(_audioPressureRatio);
			_airReleaseThin?.SetVolumeAndPitch(volumeMultiplier, pitch);
			_airReleaseLoud?.SetVolumeMultiplier(volumeMultiplier2);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DynamicGasCanisterSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is DynamicGasCanisterSaveData dynamicGasCanisterSaveData)
		{
			OutputSetting = dynamicGasCanisterSaveData.OutputSetting;
			_currentWheelRotation = OutputSetting;
			Wheel.localRotation = Quaternion.AngleAxis((0f - _currentWheelRotation) * 10f, Vector3.right);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is DynamicGasCanisterSaveData dynamicGasCanisterSaveData)
		{
			dynamicGasCanisterSaveData.OutputSetting = OutputSetting;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(OutputSetting);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			OutputSetting = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(OutputSetting);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		OutputSetting = reader.ReadSingle();
	}

	public override string GetDeconstructText()
	{
		Atmosphere internalAtmosphere = base.InternalAtmosphere;
		if (internalAtmosphere != null && internalAtmosphere.PressureGassesAndLiquids > PressurekPa.Zero)
		{
			return Localization.GetToolTip(_deconstructInterfaceName);
		}
		return base.GetDeconstructText();
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == InfoPanel)
		{
			PassiveTooltip result = new PassiveTooltip(true);
			result.Title = DisplayName;
			result.Extended = AtmosphericsManager.DisplayBasicAtmosphere(base.InternalAtmosphere);
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override bool CanDeconstruct()
	{
		return base.InternalAtmosphere?.PressureGassesAndLiquids <= PressurekPa.Zero;
	}

	public override void OnAtmosphericsBegin()
	{
		base.OnAtmosphericsBegin();
		if (GameManager.GameState == GameState.Loading)
		{
			return;
		}
		GasMixture gasMixture = GasMixtureHelper.Create();
		foreach (SpawnGas spawnContent in SpawnContents)
		{
			gasMixture.Add(new Mole(spawnContent.Type, spawnContent.GetQuantity(), spawnContent.GetEnergy()));
		}
		AtmosphericEventInstance.CreateAdd(base.InternalAtmosphere, gasMixture);
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractOpen, 1);
		}
	}

	public override void OnChildExitInventory(DynamicThing newChild)
	{
		base.OnChildExitInventory(newChild);
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractOpen, 0);
		}
	}

	public override void OnAtmosphereClient()
	{
		base.OnAtmosphereClient();
		if (PortablesConnector != null || OutputSetting <= 0f || IsBroken || base.InternalAtmosphere.TotalMoles <= Chemistry.MINIMUM_QUANTITY_MOLES)
		{
			PressureDelta = PressurekPa.Zero;
			return;
		}
		Atmosphere atmosphere = base.GridController.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		if (atmosphere == null)
		{
			PressureDelta = PressurekPa.Zero;
		}
		else
		{
			PressureDelta = RocketMath.Min(PressurePerTick, new PressurekPa(OutputSetting) - atmosphere.PressureGassesAndLiquids);
		}
	}

	public bool Smelt(DynamicThing dynamicThing)
	{
		dynamicThing.Smelt(base.InternalAtmosphere, ReagentMixture);
		return true;
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		DynamicThing occupant = GasCanisterSlot.Occupant;
		if (!(occupant is GasCanister gasCanister))
		{
			if (occupant is Ice dynamicThing)
			{
				Smelt(dynamicThing);
			}
		}
		else
		{
			AtmosphereHelper.Mix(base.InternalAtmosphere, gasCanister.InternalAtmosphere, MatterType);
		}
		if (IsBroken && base.InternalAtmosphere.TotalMoles > Chemistry.MINIMUM_QUANTITY_MOLES)
		{
			base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L).Add(base.InternalAtmosphere.GasMixture);
			base.InternalAtmosphere.GasMixture.Reset();
			PressureDelta = PressurekPa.Zero;
			return;
		}
		Atmosphere atmosphere = base.GridController.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		PressureDelta = RocketMath.Abs(atmosphere.PressureGassesAndLiquids - base.InternalAtmosphere.PressureGassesAndLiquids);
		if (PressureDelta >= new PressurekPa(MaxSetting))
		{
			if (_damageTicker > 5)
			{
				DamageState.Damage(ChangeDamageType.Increment, 5f, DamageUpdateType.Brute);
			}
			_damageTicker++;
		}
		else
		{
			_damageTicker = 0;
		}
		float num = (base.InternalAtmosphere.PressureGassesAndLiquids / MaxSetting).ToFloat();
		GasCanisterPressureState gasCanisterPressureState = GasCanisterPressureState.Empty;
		if (num >= 0.99f)
		{
			gasCanisterPressureState = GasCanisterPressureState.Critical;
		}
		else if (num >= 0.9f)
		{
			gasCanisterPressureState = GasCanisterPressureState.Full;
		}
		else if (num >= 0.75f)
		{
			gasCanisterPressureState = GasCanisterPressureState.Medium;
		}
		else if (num >= 0.5f)
		{
			gasCanisterPressureState = GasCanisterPressureState.Low;
		}
		_pressureState = gasCanisterPressureState;
		if (GameManager.RunSimulation && gasCanisterPressureState != (GasCanisterPressureState)Mode)
		{
			OnServer.Interact(base.InteractMode, (int)gasCanisterPressureState);
		}
		PressureDelta = RocketMath.Min(PressurePerTick, new PressurekPa(OutputSetting) - atmosphere.PressureGassesAndLiquids);
		if (PressureDelta > PressurekPa.Zero)
		{
			MoleQuantity transferMoles = IdealGas.Quantity(PressureDelta, base.InternalAtmosphere.Volume, base.InternalAtmosphere.Temperature);
			if (base.InternalAtmosphere.TotalMoles < Chemistry.MINIMUM_VALID_TOTAL_MOLES)
			{
				transferMoles = base.InternalAtmosphere.TotalMoles;
			}
			GasMixture gasMixture = base.InternalAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.All);
			base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L).Add(gasMixture);
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button3 && _obscuredDrag)
		{
			return base.GetContextualName(interactable) + "\n" + GameStrings.DeviceCannotDragCollision.AsString();
		}
		return base.GetContextualName(interactable);
	}

	private async UniTaskVoid RotateWheel(CancellationToken cancellationToken)
	{
		while (Math.Abs(_currentWheelRotation - OutputSetting * 10f) > 0.1f && !cancellationToken.IsCancellationRequested)
		{
			_currentWheelRotation = Mathf.Lerp(_currentWheelRotation, OutputSetting * 10f, 2f * Time.deltaTime);
			Wheel.localRotation = Quaternion.AngleAxis(0f - _currentWheelRotation, Vector3.right);
			await UniTask.NextFrame(cancellationToken);
		}
		_wheelRotation.Cancel();
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (PortablesConnector == null)
		{
			switch (interactable.Action)
			{
			case InteractableType.Button2:
				if (!doAction)
				{
					DelayedActionInstance delayedActionInstance3 = new DelayedActionInstance();
					delayedActionInstance3.ActionMessage = GameStrings.GlobalIncrease.AsString();
					delayedActionInstance3.AppendStateMessage(GameStrings.OutputKPA, StringManager.Get((int)OutputSetting));
					delayedActionInstance3.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance3.Succeed();
				}
				if (OutputSetting < MaxSetting)
				{
					OutputSetting += (interaction.AltKey ? 1f : 10f);
				}
				OutputSetting = Mathf.Min(OutputSetting, MaxSetting);
				return delayedActionInstance.Succeed();
			case InteractableType.Button1:
				if (!doAction)
				{
					DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance();
					delayedActionInstance2.ActionMessage = GameStrings.GlobalDecrease.AsString();
					delayedActionInstance2.AppendStateMessage(GameStrings.OutputKPA, StringManager.Get((int)OutputSetting));
					delayedActionInstance2.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance2.Succeed();
				}
				if (OutputSetting > MinSetting)
				{
					OutputSetting -= (interaction.AltKey ? 1f : 10f);
				}
				OutputSetting = Mathf.Max(OutputSetting, MinSetting);
				return delayedActionInstance.Succeed();
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Open && openLid != null)
		{
			openLid.SetActive(!IsOpen);
		}
		if (interactable.Action == InteractableType.Mode && displays != null && displays.Length != 0)
		{
			RefreshQuantityDisplay();
		}
	}

	private void RefreshQuantityDisplay()
	{
		GameObject[] array = displays;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		switch (Mode)
		{
		case 0:
			displays[0].SetActive(value: true);
			break;
		case 1:
			displays[1].SetActive(value: true);
			break;
		case 2:
			displays[2].SetActive(value: true);
			break;
		case 3:
			displays[3].SetActive(value: true);
			displayCritical.ChangeState(Defines.Animator.Normal);
			break;
		case 4:
			displays[3].SetActive(value: true);
			displayCritical.ChangeState(Defines.Animator.Critical);
			break;
		}
	}

	public override void OnDamageDestroyed()
	{
		IsPlayingAirReleaseSounds = false;
		if ((bool)BrokenMesh)
		{
			Renderers[0].MeshFilter.mesh = BrokenMesh;
			GameObject[] array = meshDisable;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: false);
			}
		}
		if (GameManager.RunSimulation && !_hasBlown)
		{
			if (base.InternalAtmosphere?.PressureGassesAndLiquids > PressurekPa.Zero)
			{
				global::Explosion.Explode(_explosionForce * base.InternalAtmosphere.PressureGassesAndLiquids.ToFloat() / MaxSetting, radius: Mathf.Clamp(_explosionRadius * base.InternalAtmosphere.PressureGassesAndLiquids.ToFloat() / MaxSetting, 0f, _maxExplosionRadius), pos: base.ThingTransformPosition, maxDamage: float.MaxValue, mineTerrain: true);
				AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, new GasMixture(base.InternalAtmosphere.GasMixture), spark: true);
				AtmosphericEventInstance.Reset(base.InternalAtmosphere);
			}
			DamageState?.Damage(ChangeDamageType.Set, 0f, DamageUpdateType.Burn);
			DamageState?.Damage(ChangeDamageType.Set, 0f, DamageUpdateType.Brute);
			_hasBlown = true;
		}
		else if (_hasBlown)
		{
			base.OnDamageDestroyed();
		}
	}

	public bool IsAllowed(GasCanister gasCanister)
	{
		if (MatterType == AtmosphereHelper.MatterState.All || gasCanister.CanisterContentType == Pipe.ContentType.All)
		{
			return true;
		}
		switch (gasCanister.CanisterContentType)
		{
		case Pipe.ContentType.Gas:
			if (MatterType == AtmosphereHelper.MatterState.Gas)
			{
				return true;
			}
			break;
		case Pipe.ContentType.Liquid:
			if (MatterType == AtmosphereHelper.MatterState.Liquid)
			{
				return true;
			}
			break;
		}
		return false;
	}
}
