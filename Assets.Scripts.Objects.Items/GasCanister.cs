using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Objects.Electrical;
using Reagents;
using Trading;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Items;

public class GasCanister : ItemRenamable, IAtmospherical, ISpatial, IPhysical, IProfile, IDensePoolable, IThermal, IVolume, IInternalAtmosphere, IReferencable, IEvaluable
{
	[Header("Gas Canister")]
	[Tooltip("What this canister can hold: Gas or Liquid")]
	public Pipe.ContentType CanisterContentType = Pipe.ContentType.Gas;

	public Mesh BrokenMesh;

	public static PressurekPa StandardPressure = Chemistry.OneAtmosphere * 16.700000762939453;

	[FormerlySerializedAs("MaxPressure")]
	[SerializeField]
	private float maxPressure = 10132.5f;

	[FormerlySerializedAs("Litres")]
	[SerializeField]
	public float litres = 64f;

	[FormerlySerializedAs("PressurePerTick")]
	[SerializeField]
	private float pressurePerTick = 101.325f;

	[SerializeField]
	private GenericAssignableAnimComponent valveAnimComponent;

	public List<SpawnGas> SpawnContents = new List<SpawnGas>();

	private static readonly int ReleaseContentsHash = Animator.StringToHash("ReleaseContents");

	private PressurekPa _deltaPressure;

	private int _damageTicker;

	private float _explosionForce = 300f;

	private float _explosionRadius = 4.4f;

	private float _maxExplosionRadius = 10f;

	private bool _hasBlown;

	public PressurekPa MaxPressure => new PressurekPa(maxPressure);

	public VolumeLitres Litres => new VolumeLitres(litres);

	private PressurekPa PressurePerTick => new PressurekPa(pressurePerTick);

	public bool IsEmpty => base.InternalAtmosphere?.PressureGassesAndLiquids < PressurekPa.One;

	public PressurekPa Pressure => base.InternalAtmosphere?.PressureGassesAndLiquids ?? PressurekPa.Zero;

	public override bool HasReadableAtmosphere => true;

	public VolumeLitres GetVolume => Litres;

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.GasCanisterCategory);
	}

	public override object GetModXmlType()
	{
		return new GasCanisterData();
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.AppendLine(GameStrings.ItemInSlotValue.AsString(ToTooltip(), GetQuantityText()));
		if (Pressure >= MaxPressure)
		{
			extendedText.AppendLine(GameStrings.ThingOverPressure.AsString(ToTooltip()));
		}
		extendedText.Append(GameStrings.GasTemperature.AsString(StringManager.Get(base.InternalAtmosphere?.Temperature.ToFloat() ?? TemperatureKelvin.Zero.ToFloat())));
		return extendedText;
	}

	public override void DeserializModData(ThingModData modData)
	{
		base.DeserializModData(modData);
		if (modData is GasCanisterData gasCanisterData)
		{
			if (!float.IsNaN(gasCanisterData.MaxPressure))
			{
				maxPressure = gasCanisterData.MaxPressure;
			}
			if (!float.IsNaN(gasCanisterData.Litres))
			{
				litres = gasCanisterData.Litres;
			}
			if (!float.IsNaN(gasCanisterData.PressurePerTick))
			{
				pressurePerTick = gasCanisterData.PressurePerTick;
			}
			if (gasCanisterData.SpawnContents != null)
			{
				SpawnContents = gasCanisterData.SpawnContents;
			}
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		valveAnimComponent?.RefreshState(skipAnimation);
	}

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Register(this);
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Litres, 0L);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (GameManager.RunSimulation && base.InternalAtmosphere != null)
		{
			base.InternalAtmosphere.Volume = GetVolume;
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if ((bool)AtmosphericsManager.Instance)
		{
			AtmosphericsManager.Instance.Deregister(this);
		}
	}

	public override float CalculateUniqueRatioIdentifier()
	{
		return SpawnContents.Capacity;
	}

	public override void OnAtmosphericsBegin()
	{
		base.OnAtmosphericsBegin();
		if (GameManager.GameState == GameState.Loading || SpawnContents.Count <= 0)
		{
			return;
		}
		foreach (SpawnGas spawnContent in SpawnContents)
		{
			base.InternalAtmosphere.GasMixture.Add(new Mole(spawnContent.Type, spawnContent.GetQuantity(), spawnContent.GetEnergy()));
		}
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (GameManager.RunSimulation && IsOpen)
		{
			OnServer.Interact(base.InteractOpen, 0);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		Atmosphere atmosphere = base.GridController.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		if (base.InternalAtmosphere.PressureGassesAndLiquids < new PressurekPa(0.0010000000474974513) && atmosphere == null)
		{
			base.InternalAtmosphere.GasMixture.Reset();
		}
		else
		{
			if (!base.GridController.CanContainAtmos(base.WorldGrid))
			{
				return;
			}
			_deltaPressure = RocketMath.Abs(atmosphere.PressureGassesAndLiquids - base.InternalAtmosphere.PressureGassesAndLiquids);
			if (_hasBlown)
			{
				atmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
				AtmosphereHelper.Mix(base.InternalAtmosphere, atmosphere, AtmosphereHelper.MatterState.All);
				return;
			}
			if (GameManager.RunSimulation)
			{
				if (_deltaPressure >= MaxPressure)
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
			}
			if (IsOpen)
			{
				GasMixture gasMixture = GasMixtureHelper.Create();
				VolumeLitres zero = VolumeLitres.Zero;
				AtmosphereHelper.MatterState matterState = CanisterContentType.AsMatterState();
				gasMixture.Add(base.InternalAtmosphere.GasMixture, matterState);
				zero += base.InternalAtmosphere.Volume;
				atmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
				gasMixture.Add(atmosphere.GasMixture, matterState);
				zero += atmosphere.Volume;
				base.InternalAtmosphere.GasMixture.Set(gasMixture, matterState);
				base.InternalAtmosphere.GasMixture.Scale((base.InternalAtmosphere.Volume / zero).ToFloat(), matterState);
				atmosphere.GasMixture.Set(gasMixture, matterState);
				atmosphere.GasMixture.Scale((atmosphere.Volume / zero).ToFloat(), matterState);
			}
		}
	}

	public override void OnDisplayInPlayerWindow()
	{
		base.OnDisplayInPlayerWindow();
		base.ParentSlot.RefreshQuantity();
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		WeldingTorch weldingTorch = attack.SourceItem as WeldingTorch;
		if ((bool)weldingTorch)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = "Burn"
			};
			if (!weldingTorch.IsOperable)
			{
				delayedActionInstance.IsDisabled = true;
				if (!weldingTorch.OnOff)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceNotOn);
				}
				if (weldingTorch.IsEmpty)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceNoFuel);
				}
				if (!weldingTorch.Inflamed)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceNotHotEnough);
				}
				return delayedActionInstance;
			}
			if (doAction)
			{
				DamageState.Damage(ChangeDamageType.Increment, 200f, DamageUpdateType.Burn);
			}
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}

	public override void Smelt(Atmosphere localAtmosphere, ReagentMixture reagentMixture)
	{
		if (localAtmosphere.IsGlobalAtmosphere)
		{
			AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, base.InternalAtmosphere.GasMixture);
		}
		else
		{
			AtmosphericEventInstance.CreateAdd(localAtmosphere, base.InternalAtmosphere.GasMixture);
		}
		base.Smelt(localAtmosphere, reagentMixture);
	}

	public override void Recycle()
	{
		Atmosphere localAtmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(base.WorldGrid);
		Smelt(localAtmosphere, ReagentMixture);
		base.Recycle();
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (!GameManager.IsBatchMode && base.AllowInteraction && IsAuthorized(interaction.SourceThing) && interactable.Action == InteractableType.Open && !IsBroken && doAction && interactable.State == 0)
		{
			PressurekPa pressureGassesAndLiquids = base.InternalAtmosphere.PressureGassesAndLiquids;
			Atmosphere atmosphere = base.GridController.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
			PressurekPa pressurekPa = PressurekPa.Zero;
			if (atmosphere != null)
			{
				pressurekPa = atmosphere.PressureGassesAndLiquids;
			}
			if (pressureGassesAndLiquids > pressurekPa)
			{
				PressurekPa pressurekPa2 = pressureGassesAndLiquids - pressurekPa;
				PlaySound(ReleaseContentsHash, Mathf.Clamp01((pressurekPa2 / Chemistry.OneAtmosphere).ToFloat()));
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnDamageDestroyed()
	{
		SetBrokenMesh();
		if (GameManager.RunSimulation && !_hasBlown)
		{
			if (base.InternalAtmosphere.PressureGassesAndLiquids > PressurekPa.Zero)
			{
				global::Explosion.Explode(_explosionForce * (base.InternalAtmosphere.PressureGassesAndLiquids / MaxPressure).ToFloat(), radius: Mathf.Clamp(_explosionRadius * (base.InternalAtmosphere.PressureGassesAndLiquids / MaxPressure).ToFloat(), 0f, _maxExplosionRadius), pos: base.transform.position, maxDamage: float.MaxValue, mineTerrain: true);
				AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, new GasMixture(base.InternalAtmosphere.GasMixture), spark: true);
				AtmosphericEventInstance.Reset(base.InternalAtmosphere);
				if (base.ParentSlot?.Parent is WeldingTorch weldingTorch)
				{
					weldingTorch.OnTankExploded();
				}
			}
			DamageState.Damage(ChangeDamageType.Set, 0f, DamageUpdateType.Burn);
			DamageState.Damage(ChangeDamageType.Set, 0f, DamageUpdateType.Brute);
			_hasBlown = true;
		}
		else if (_hasBlown)
		{
			base.OnDamageDestroyed();
		}
	}

	private void SetBrokenMesh()
	{
		if (!GameManager.IsBatchMode)
		{
			Renderers[0].MeshFilter.sharedMesh = BrokenMesh;
		}
	}

	public override string GetQuantityText()
	{
		return CanisterContentType switch
		{
			Pipe.ContentType.Liquid => StringGenerator.GetString((base.InternalAtmosphere != null) ? Mathf.RoundToInt(base.InternalAtmosphere.TotalVolumeLiquids.ToFloat() * 1000f) : 0, Unit.CanisterLiquid), 
			_ => StringGenerator.GetString((base.InternalAtmosphere != null) ? Mathf.RoundToInt(base.InternalAtmosphere.PressureGassesAndLiquids.ToFloat()) : 0, Unit.Canister), 
		};
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new GasCanisterSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is GasCanisterSaveData gasCanisterSaveData)
		{
			gasCanisterSaveData.HasBlown = _hasBlown;
		}
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is GasCanisterSaveData gasCanisterSaveData)
		{
			_hasBlown = gasCanisterSaveData.HasBlown;
			if (_hasBlown)
			{
				SetBrokenMesh();
			}
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteBoolean(_hasBlown);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_hasBlown = reader.ReadBoolean();
		if (_hasBlown)
		{
			SetBrokenMesh();
		}
	}
}
