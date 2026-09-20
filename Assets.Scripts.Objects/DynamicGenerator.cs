using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class DynamicGenerator : DraggableThing, IThermal, IUnfastenable, IReferencable, IEvaluable
{
	[Header("Portable Generator")]
	[ReadOnly]
	[Tooltip("PowerConnector that I am connected too")]
	public PowerConnector PowerConnector;

	[ReadOnly]
	[Tooltip("Gas Canister that is currently inserted")]
	public GasCanister GasCanister;

	[ReadOnly]
	[Tooltip("BatteryCell that is currently inserted")]
	public BatteryCell BatteryCell;

	[ReadOnly]
	[Tooltip("Set to true when the generator is connected to the power connector")]
	public bool Connected;

	public static VolumeLitres InternalVolume = new VolumeLitres(1.5);

	public static float Efficiency = 0.06f;

	public static PressurekPa PressurePerTick = Chemistry.OneAtmosphere;

	[Tooltip("Affects how fast the needle will move towards the current pressure")]
	public float LerpSpeed = 2f;

	[Tooltip("Minimum degrees rotation on local Y")]
	public float NeedleMinimum;

	[Tooltip("Maximum degrees rotation on local Y")]
	public float NeedleMaximum;

	[Tooltip("The needle (required)")]
	public GameObject Needle;

	[Tooltip("The collider for tank display")]
	public Collider InfoPanel;

	private Transform _needleTransform;

	private float _lastAngle;

	private Quaternion _needleBaseRotation;

	private float _powerGenerated;

	private PressurekPa _pressureRating;

	private float _needleRotation;

	private Mole _pollutants;

	private MoleEnergy _previousCombustionEnergy;

	public float PowerGenerated
	{
		get
		{
			if (!OnOff || !Powered)
			{
				return 0f;
			}
			return _powerGenerated;
		}
	}

	public override bool HasReadableAtmosphere => true;

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			_needleTransform = Needle.transform;
			_needleBaseRotation = _needleTransform.localRotation;
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, InternalVolume, 0L);
		}
		base.InternalAtmosphere.Volume = InternalVolume;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		GasCanister gasCanister = newChild as GasCanister;
		if (gasCanister != null)
		{
			GasCanister = gasCanister;
			WaitThenRegister().Forget();
		}
		BatteryCell batteryCell = newChild as BatteryCell;
		if (batteryCell != null)
		{
			BatteryCell = batteryCell;
		}
	}

	private async UniTaskVoid WaitThenRegister()
	{
		await UniTask.WaitUntil(() => base.InternalAtmosphere != null);
		AtmosphericEventInstance.Reset(base.InternalAtmosphere);
		AtmosphericsManager.Instance.Register(this);
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		PowerConnector powerConnector = parent as PowerConnector;
		if (powerConnector != null)
		{
			PowerConnector = powerConnector;
			if ((bool)BaseAnimator)
			{
				BaseAnimator.SetBool(DraggableThing.AnchoredState, value: true);
			}
		}
	}

	public override void OnExitInventory(Thing parent)
	{
		base.OnExitInventory(parent);
		if (PowerConnector == parent)
		{
			PowerConnector = null;
			if ((bool)BaseAnimator)
			{
				BaseAnimator.SetBool(DraggableThing.AnchoredState, value: false);
			}
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (GasCanister == previousChild)
		{
			GasCanister = null;
			AtmosphericEventInstance.Reset(base.InternalAtmosphere);
			AtmosphericsManager.Instance.Deregister(this);
			if (GameManager.RunSimulation && Powered)
			{
				OnServer.Interact(base.InteractPowered, 0);
			}
		}
		if (BatteryCell == previousChild)
		{
			BatteryCell = null;
		}
	}

	public override bool MoveToWorld(float force = 0f)
	{
		bool result = base.MoveToWorld(force);
		if (GameManager.RunSimulation && base.Room != null)
		{
			RigidBody.AddForce(Vector3.up * 3f);
			RigidBody.AddTorque(Random.insideUnitCircle);
		}
		return result;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == InfoPanel)
		{
			PassiveTooltip result = new PassiveTooltip(true);
			result.Title = Localization.GetInterface("TankPressure");
			result.State = (GasCanister ? string.Format("{0}", GasCanister.InternalAtmosphere.PressureGassesAndLiquidsInPa.ToStringPrefix("Pa", "yellow")) : "Empty");
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
			VentAllAtmosphereToWorld();
			AtmosphericsManager.Instance.Deregister(this);
		}
	}

	private void VentAllAtmosphereToWorld()
	{
		if (base.WorldAtmosphere == null)
		{
			base.WorldAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		}
		AtmosphereHelper.MoveVolume(base.InternalAtmosphere, base.WorldAtmosphere, base.InternalAtmosphere.Volume, AtmosphereHelper.MatterState.All, MoleQuantity.Zero);
	}

	public override void OnThreadUpdate()
	{
		if (!GameManager.IsBatchMode)
		{
			_pressureRating = (GasCanister ? (GasCanister.InternalAtmosphere.PressureGassesAndLiquids / GasCanister.StandardPressure) : PressurekPa.Zero);
			if (_pressureRating.IsNaN())
			{
				_pressureRating = PressurekPa.Zero;
			}
			_needleRotation = Mathf.Lerp(NeedleMinimum, NeedleMaximum, _pressureRating.ToFloat());
		}
	}

	public override void UpdateEachFrame()
	{
		if (!GameManager.IsBatchMode && !WorldManager.IsGamePaused)
		{
			base.UpdateEachFrame();
			if (!IsOccluded)
			{
				_lastAngle = Mathf.Lerp(_lastAngle, _needleRotation, Time.deltaTime * LerpSpeed);
				_needleTransform.localRotation = _needleBaseRotation;
				_needleTransform.Rotate(0f, _lastAngle, 0f, Space.Self);
			}
		}
	}

	private void PowerGeneration()
	{
		MoleEnergy previousCombustionEnergy = _previousCombustionEnergy;
		_ = previousCombustionEnergy * (1f - Efficiency);
		MoleEnergy moleEnergy = previousCombustionEnergy * Efficiency;
		base.InternalAtmosphere.GasMixture.RemoveEnergy(moleEnergy);
		if (moleEnergy > MoleEnergy.Zero)
		{
			if (!Powered)
			{
				OnServer.Interact(base.InteractPowered, 1);
			}
			if ((bool)BatteryCell && !BatteryCell.IsCharged)
			{
				MoleEnergy moleEnergy2 = RocketMath.Min(new MoleEnergy(BatteryCell.PowerDelta), moleEnergy);
				moleEnergy -= moleEnergy2;
				BatteryCell.PowerStored += moleEnergy2.ToFloat();
			}
			if (Connected)
			{
				_powerGenerated = moleEnergy.ToFloat();
			}
		}
		else if (Powered)
		{
			OnServer.Interact(base.InteractPowered, 0);
		}
	}

	public bool IsOperable()
	{
		if (!base.InternalAtmosphere.IsAboveArmstrong() && !base.GridController.CanContainAtmos(base.WorldGrid))
		{
			if (Error == 0 && Powered && GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
		if (Error == 1 && GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractError, 0);
		}
		return true;
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!OnOff || !GasCanister)
		{
			if (Powered)
			{
				OnServer.Interact(base.InteractPowered, 0);
			}
			return;
		}
		if (!IsOperable())
		{
			_powerGenerated = 0f;
			return;
		}
		PowerGeneration();
		if (base.WorldAtmosphere == null)
		{
			base.WorldAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		}
		base.WorldAtmosphere.Add(base.InternalAtmosphere.GasMixture);
		base.InternalAtmosphere.GasMixture.Reset();
		_previousCombustionEnergy = MoleEnergy.Zero;
		Atmosphere internalAtmosphere = GasCanister.InternalAtmosphere;
		if (internalAtmosphere != null && !(internalAtmosphere.PressureGassesAndLiquids < new PressurekPa(0.001)))
		{
			MoleQuantity transferMoles = IdealGas.Quantity(RocketMath.Min(PressurePerTick, PressurePerTick - base.InternalAtmosphere.PressureGassesAndLiquids), base.InternalAtmosphere.Volume, internalAtmosphere.Temperature);
			if (internalAtmosphere.GasMixture.GetTotalMolesGassesAndLiquids < Chemistry.MINIMUM_VALID_TOTAL_MOLES)
			{
				transferMoles = internalAtmosphere.GasMixture.GetTotalMolesGassesAndLiquids;
			}
			GasMixture gasMixture = internalAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.Gas);
			if (gasMixture.IsValid)
			{
				base.InternalAtmosphere.Add(gasMixture);
				base.InternalAtmosphere.Sparked = true;
				base.InternalAtmosphere.TryCombust(0.8999999761581421, force: true);
				_previousCombustionEnergy = base.InternalAtmosphere.CombustionEnergy;
			}
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (base.Joint != null || !(attack.SourceItem as Wrench))
		{
			return base.AttackWith(attack, doAction);
		}
		PowerConnector powerConnector = SmallCell.Get<PowerConnector>(CenterPosition);
		if ((object)powerConnector == null)
		{
			return base.AttackWith(attack, doAction);
		}
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = (base.IsChild ? ActionStrings.Disconnect : ActionStrings.Connect)
		};
		if (!doAction)
		{
			return result;
		}
		if (GameManager.RunSimulation)
		{
			if (base.IsChild)
			{
				OnServer.MoveToWorld(this);
			}
			else
			{
				OnServer.MoveToSlot(this, powerConnector.ConnectedSlot);
			}
		}
		return result;
	}
}
