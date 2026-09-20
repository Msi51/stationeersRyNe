using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Items;
using Trading;

namespace Assets.Scripts.Objects;

public class PlayloadDeliveryContainer : DraggableThing, IUnfastenable, IReferencable, IEvaluable
{
	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SetContentsVisibility(IsOpen);
	}

	public void SetContentsVisibility(bool isVisible)
	{
		foreach (Interactable interactable in Interactables)
		{
			if (interactable.Action != InteractableType.Open && interactable.Action != InteractableType.Button1 && (bool)interactable.Collider)
			{
				interactable.Collider.enabled = isVisible;
			}
		}
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant)
			{
				slot.Occupant.SetVisibility(isVisible);
			}
		}
	}

	public IPayloadMount GetStructuralMount()
	{
		return (GridController.GetController(CenterPosition).GetOther(CenterPosition) as IPayloadMount) ?? (GridController.GetController(CenterPosition).GetSmallCell(CenterPosition)?.Device as IPayloadMount);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if ((bool)base.Joint || !(attack.SourceItem as Wrench))
		{
			return base.AttackWith(attack, doAction);
		}
		IPayloadMount structuralMount = GetStructuralMount();
		if (structuralMount == null)
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
		if (GameManager.RunSimulation)
		{
			if (base.ParentSlot != null)
			{
				OnServer.MoveToWorld(this);
			}
			else
			{
				if (structuralMount?.PayloadSlot == null)
				{
					return result;
				}
				OnServer.MoveToSlot(this, structuralMount.PayloadSlot);
			}
		}
		return result;
	}
}
