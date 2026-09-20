using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class ElevatorLevel : ElevatorShaft
{
	[ReadOnly]
	public Transform BottomPosition;

	[ReadOnly]
	public Grid3 LevelCarragePosition;

	public List<Collider> DoorColliders = new List<Collider>();

	private Dictionary<int, DigitGameObject> _digitLookup = new Dictionary<int, DigitGameObject>();

	public List<DigitGameObject> DigitReferences = new List<DigitGameObject>();

	private DigitGameObject _currentDigitRef;

	public override int ShaftLevel
	{
		get
		{
			return base.ShaftLevel;
		}
		set
		{
			base.ShaftLevel = value;
			RefreshDigitReference();
		}
	}

	public override void OnRegistered(Cell cell)
	{
		LevelCarragePosition = BottomPosition.position.ToGridPosition();
		base.OnRegistered(cell);
	}

	public override bool StopCarrage(ElevatorCarrage carrage)
	{
		if (carrage.LevelTarget == ShaftLevel)
		{
			return carrage.ThingTransformPosition.ToGridPosition() == LevelCarragePosition;
		}
		return false;
	}

	public override void CheckCarrageState(ElevatorCarrage carrage)
	{
		base.CheckCarrageState(carrage);
		if (GameManager.RunSimulation && HasOpenState && GameManager.GameState == GameState.Running)
		{
			OnServer.Interact(base.InteractOpen, (StopCarrage(carrage) && carrage.ElevatorMode == ElevatorMode.Stationary) ? 1 : 0);
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		Crowbar crowbar = attack.SourceItem as Crowbar;
		if (!base.AllowInteraction && !IsBroken)
		{
			return null;
		}
		if ((bool)crowbar && (attack.TargetCollider == null || DoorColliders.Contains(attack.TargetCollider)))
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = crowbar.DoorForceOpenDuration,
				ActionMessage = (IsOpen ? ActionStrings.ForceClose : ActionStrings.ForceOpen)
			};
			if (attack.TargetCollider != null)
			{
				delayedActionInstance.Selection = GetSelection(attack.TargetCollider);
			}
			if (IsLocked)
			{
				delayedActionInstance.IsDisabled = true;
				delayedActionInstance.AppendStateMessage(GameStrings.ElevatorUnableToForceOpenLocked, ToTooltip());
				return delayedActionInstance;
			}
			if (!Powered)
			{
				if (doAction && GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractOpen, (!IsOpen) ? 1 : 0);
				}
				return delayedActionInstance;
			}
			delayedActionInstance.IsDisabled = true;
			delayedActionInstance.AppendStateMessage(GameStrings.ElevatorUnableToForceOpenPowered, ToTooltip());
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}

	protected override void SetPower(CableNetwork cableNetwork, bool hasPower)
	{
		hasPower |= ShaftNetwork?.IsAnyOtherPowered(this) ?? false;
		if (Powered != hasPower && GameManager.RunSimulation)
		{
			OnServer.Interact(base.InteractPowered, hasPower ? 1 : 0);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (interactable.Action == InteractableType.Button1)
			{
				delayedActionInstance.ActionMessage = ActionStrings.CallElevator;
				if (IsLocked)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceLocked);
				}
				if (IsBroken)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceBroken);
				}
				if (!Powered)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				if (!IsAuthorized(interaction.SourceThing))
				{
					return delayedActionInstance.Fail(GameStrings.AccessCardUnableToInteract);
				}
				if (doAction && GameManager.RunSimulation)
				{
					SetElevatorTarget(ShaftLevel);
					OnServer.Interact(base.InteractActivate, (base.InteractActivate.State == 0) ? 1 : 0);
				}
				return delayedActionInstance;
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void Awake()
	{
		base.Awake();
		foreach (DigitGameObject digitReference in DigitReferences)
		{
			_digitLookup.Add(digitReference.Digit, digitReference);
		}
	}

	private void RefreshDigitReference()
	{
		int key = Mathf.Clamp(ShaftLevel, 0, 99);
		if (_currentDigitRef != null)
		{
			_currentDigitRef.IsVisible(isVisible: false);
		}
		_digitLookup.TryGetValue(key, out _currentDigitRef);
		if (_currentDigitRef != null)
		{
			_currentDigitRef.IsVisible(isVisible: true);
		}
	}
}
