using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical.Helper;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class JetpackElectric : Jetpack, IBatteryPowered, IPowered, IDensePoolable, IReferencable, IEvaluable
{
	[SerializeField]
	private BinaryAnimComponent slideAnimComponent;

	public float StabilizePowerUsed = 10f;

	public float ThrustPowerUsed = 15f;

	public Rotator turbineFanLeft;

	public Rotator turbineFanRight;

	public Slot BatterySlot { get; private set; }

	public BatteryCell Battery => BatterySlot?.Get<BatteryCell>();

	public override bool IsGasPowered => false;

	public override bool IsBatteryPowered => true;

	public bool HasPower
	{
		get
		{
			if ((object)Battery != null)
			{
				return !Battery.IsEmpty;
			}
			return false;
		}
	}

	public override bool PowerLow
	{
		get
		{
			if ((object)Battery != null)
			{
				return Battery.IsLow;
			}
			return true;
		}
	}

	public override bool PowerCritical
	{
		get
		{
			if ((object)Battery != null)
			{
				return Battery.IsCritical;
			}
			return true;
		}
	}

	public override bool HasPropellent => HasPower;

	public override bool PropellantLow => PowerLow;

	public override bool PropellantCritical => PowerCritical;

	public void Recharge(float amount)
	{
		if ((object)Battery != null && !Battery.IsCharged)
		{
			Battery.PowerStored += amount;
		}
	}

	public override void Awake()
	{
		noAtmos = true;
		base.Awake();
		ElectricityManager.Register(this);
		BatterySlot = Slots.Find((Slot s) => s.Type == Slot.Class.Battery);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (GameManager.GameState != GameState.None)
		{
			ElectricityManager.Deregister(this);
		}
	}

	public override void OnAtmosphericTick()
	{
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (base.JetPackActivate && HasPower)
		{
			float num = (base.IsStabilizing ? StabilizePowerUsed : (base.IsThrusting ? ThrustPowerUsed : 0f));
			num *= base.OutputSetting * (float)DifficultySetting.Current.JetpackRate;
			Battery.PowerStored = Mathf.Max(0f, Battery.PowerStored - num);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Lock)
		{
			if (BatterySlot.Display != null)
			{
				BatterySlot.Display.IsDisabled = IsLocked;
			}
			BatterySlot.IsLocked = IsLocked;
			BatterySlot.IsSwappable = !IsLocked;
			BatterySlot.AllowDragging = !IsLocked;
			BatterySlot.Collider.enabled = !IsLocked;
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Lock)
		{
			return Localization.GetAction(IsLocked ? ActionStrings.UnlockHash : ActionStrings.LockHash);
		}
		return base.GetContextualName(interactable);
	}

	public override bool PreventInteraction(out DelayedActionInstance failResult, Interactable interactable, Interaction interaction)
	{
		if (IsLocked)
		{
			InteractableType? interactableType = interactable?.Action;
			if (interactableType.HasValue && interactableType == InteractableType.Slot1)
			{
				failResult = new DelayedActionInstance
				{
					Duration = 0f,
					ActionMessage = interactable.ContextualName
				}.Fail(GameStrings.ThingInteractionDisabled);
				return true;
			}
		}
		failResult = null;
		return false;
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (slideAnimComponent != null)
		{
			slideAnimComponent.RefreshState(skipAnimation: true);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (slideAnimComponent != null)
		{
			slideAnimComponent.RefreshState(skipAnimation);
		}
	}

	public override void OnAnimationStart()
	{
		if (IsLocked && Battery != null)
		{
			Battery.SetVisibility(isVisible: true);
		}
	}

	public override void OnAnimationStop()
	{
		if (!IsLocked && Battery != null)
		{
			Battery.SetVisibility(isVisible: false);
		}
	}

	public override void UpdateJetpackEmissions()
	{
		if (LastFrameEmissions != base.CurrentEmission && _hasUpdated)
		{
			if (!IsOccluded)
			{
				UpdateEmissionAudio(Emission.Forward, IsCurrentlyEmitting(Emission.Forward));
				UpdateEmissionAudio(Emission.Backward, IsCurrentlyEmitting(Emission.Backward));
				UpdateEmissionAudio(Emission.Left, IsCurrentlyEmitting(Emission.Left));
				UpdateEmissionAudio(Emission.Right, IsCurrentlyEmitting(Emission.Right));
				UpdateEmissionAudio(Emission.Up, IsCurrentlyEmitting(Emission.Up));
				UpdateEmissionAudio(Emission.Down, IsCurrentlyEmitting(Emission.Down));
				UpdateEmissionAudio(Emission.Stabilize, IsCurrentlyEmitting(Emission.Stabilize));
				UpdateEmissionAudio(Emission.DownStabilize, IsCurrentlyEmitting(Emission.DownStabilize));
			}
			LastFrameEmissions = base.CurrentEmission;
		}
	}

	public override void UpdateEachFrame()
	{
		if (!GameManager.IsBatchMode && !IsOccluded)
		{
			bool running = HasPower && (base.IsThrusting || base.IsStabilizing);
			turbineFanLeft.DoUpdate(running);
			turbineFanRight.DoUpdate(running);
		}
	}

	public override void DisableJetAll()
	{
		base.JetPackActivate = false;
		StopAllAudio();
	}
}
