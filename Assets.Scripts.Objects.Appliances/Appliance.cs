using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.UI;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Appliances;

public class Appliance : CharacterItem, IUnfastenable, IReferencable, IEvaluable
{
	public Bench ParentBench;

	private bool _wasWrenched;

	public float UsedPower;

	public virtual float GetUsedPower()
	{
		return UsedPower;
	}

	public virtual float ReceivePower(float powerReceived)
	{
		return powerReceived - UsedPower;
	}

	public virtual void BenchPowerStateChanged(bool receivingPower)
	{
	}

	public override CanEnterResult CanEnter(Slot destinationSlot)
	{
		if ((bool)ParentBench)
		{
			return CanEnterResult.Fail(GameStrings.IsAttachedToBench);
		}
		return base.CanEnter(destinationSlot);
	}

	protected virtual bool IsOperable()
	{
		if (OnOff)
		{
			return Powered;
		}
		return false;
	}

	public override bool MoveToWorld(float force = 0f)
	{
		bool result = base.MoveToWorld(force);
		if (GameManager.RunSimulation && _wasWrenched)
		{
			RigidBody.AddTorque(Random.insideUnitCircle * 1000f);
			RigidBody.AddForce(Vector3.up * 5f);
		}
		_wasWrenched = false;
		return result;
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.ApplianceCategory);
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (parent is Bench)
		{
			_wasWrenched = true;
		}
	}

	protected void BaseInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		IsOperable();
		if (interactable.Action == InteractableType.OnOff && interactable.State == 0)
		{
			Error = 0;
			PoweredValue = 0;
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (!(attack.SourceItem as Wrench))
		{
			return base.AttackWith(attack, doAction);
		}
		Bench bench = SmallCell.Get<Bench>(CenterPosition);
		if (!bench)
		{
			return base.AttackWith(attack, doAction);
		}
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = ((base.ParentSlot != null) ? ActionStrings.Disconnect : ActionStrings.Connect)
		};
		if (!doAction)
		{
			return result;
		}
		if (base.IsChild)
		{
			if (GameManager.RunSimulation)
			{
				OnServer.MoveToWorld(this);
			}
		}
		else
		{
			Slot slot = null;
			float num = float.PositiveInfinity;
			foreach (Slot slot2 in bench.Slots)
			{
				float num2 = Vector3.Distance(slot2.Location.position, base.Position);
				if (num2 < num)
				{
					num = num2;
					slot = slot2;
				}
			}
			if (slot == null)
			{
				return result;
			}
			OnServer.MoveToSlot(this, slot);
		}
		return result;
	}

	protected bool CanCloseAppliance()
	{
		return true;
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
		if (interactable.Action == InteractableType.OnOff)
		{
			if (IsLocked)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceLocked);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			RefreshInteract(interactable);
			return delayedActionInstance.Succeed();
		}
		if (interactable.Action == InteractableType.Open)
		{
			if (IsLocked)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceLocked);
			}
			if (!CanCloseAppliance())
			{
				return delayedActionInstance.Fail(GameStrings.InteractCantClose);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (!IsOpen) ? 1 : 0);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	private void RefreshInteract(Interactable interactable)
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(interactable, (!OnOff) ? 1 : 0);
			bool flag = (bool)ParentBench && ParentBench.Powered && OnOff;
			if (flag != Powered)
			{
				OnServer.Interact(base.InteractPowered, flag ? 1 : 0);
			}
		}
	}
}
