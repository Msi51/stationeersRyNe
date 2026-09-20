using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using CharacterCustomisation;
using UnityEngine;

namespace Objects.Electrical;

public class DroidSleeper : Device, IExitable, ISmartRotatable
{
	[Header("Droid Sleeper")]
	[SerializeField]
	private float _chargePerTick = 500f;

	[SerializeField]
	private float _nonRobotDamagePerTick = 0.2f;

	[SerializeField]
	private Transform _exitPoint;

	[SerializeField]
	private Transform _cameraPoint;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.FlatExhaustive;

	public int[] OpenEndsPermutation = new int[4] { 0, 1, 2, 3 };

	private float _powerUsedDuringTick;

	private Slot _sleeperSlot => Slots[0];

	public bool FreeLook => false;

	public override void OnPowerTick()
	{
		if (Powered && OnOff && _sleeperSlot.Occupant is Human human)
		{
			switch (human.SpeciesClass)
			{
			case SpeciesClass.Human:
			case SpeciesClass.Zrilian:
				DamageNonRobot(human);
				break;
			case SpeciesClass.Robot:
				ChargeRobot(human);
				break;
			case SpeciesClass.None:
				break;
			}
		}
	}

	private void DamageNonRobot(Human human)
	{
		human.DamageState.Damage(ChangeDamageType.Increment, _nonRobotDamagePerTick, DamageUpdateType.Brute);
		_powerUsedDuringTick = _chargePerTick;
	}

	private void ChargeRobot(Human human)
	{
		BatteryCell robotBattery = human.RobotBattery;
		if (robotBattery != null)
		{
			_powerUsedDuringTick += Mathf.Min(_chargePerTick, robotBattery.PowerMaximum - robotBattery.PowerStored);
			robotBattery.AddPowerSafe(_powerUsedDuringTick);
		}
		BatteryCell batteryCell = ((human.Suit != null) ? human.Suit.Battery : null);
		if (batteryCell != null)
		{
			float num = Mathf.Min(_chargePerTick, batteryCell.PowerMaximum - batteryCell.PowerStored);
			batteryCell.AddPowerSafe(num);
			_powerUsedDuringTick += num;
		}
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (base.PowerCable == null || base.PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return UsedPower + _powerUsedDuringTick;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		if (previousChild != null)
		{
			base.OnChildExitInventory(previousChild);
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractOpen, 1);
			}
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		return interactable.Action switch
		{
			InteractableType.Open => InteractWithOpen(interactable, interaction, doAction), 
			InteractableType.Activate => InteractWithActivate(interaction, doAction), 
			_ => base.InteractWith(interactable, interaction, doAction), 
		};
	}

	private DelayedActionInstance InteractWithActivate(Interaction interaction, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0.5f,
			ActionMessage = ActionStrings.GetIn
		};
		DynamicThing occupant = interaction.SourceSlot.Occupant;
		if ((bool)_sleeperSlot.Occupant || (bool)occupant)
		{
			return HandleSwitch(interaction, _sleeperSlot.SlotIndex, delayedActionInstance, doAction);
		}
		if (GameManager.RunSimulation && doAction)
		{
			OnServer.MoveToSlot(interaction.SourceThing.AsDynamicThing, _sleeperSlot);
			CheckConnections();
		}
		return delayedActionInstance.Succeed();
	}

	private DelayedActionInstance InteractWithOpen(Interactable interactable, Interaction interaction, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (!OnOff)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.DeviceNotOn);
			return delayedActionInstance.Fail();
		}
		if (!Powered)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.DeviceNoPower);
			return delayedActionInstance.Fail();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Open)
		{
			SetActivateEnabled();
		}
	}

	private void SetActivateEnabled()
	{
		foreach (Interactable interactable in Interactables)
		{
			if (interactable.Action == InteractableType.Activate)
			{
				interactable.Collider.enabled = IsOpen;
				break;
			}
		}
	}

	public Vector3 GetExitPosition(Entity human)
	{
		return _exitPoint.position;
	}

	public void Exit(Human human)
	{
		if (!(Slots[0].Occupant == null))
		{
			Human human2 = Slots[0].Occupant as Human;
			human.MoveToWorld(GetExitPosition(human2), human2.ParentSlot.Parent.Rotation, Vector3.zero, Vector3.zero);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		_ = savedData is DroidSleeperSaveData;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DroidSleeperSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		_ = saveData is DroidSleeperSaveData;
	}

	public Transform GetCameraPoint(Entity entity)
	{
		return _cameraPoint;
	}

	public Transform GetSpawnPointTransform()
	{
		return _sleeperSlot.Location;
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}
}
