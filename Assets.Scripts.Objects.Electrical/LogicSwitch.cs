using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicSwitch : LogicInputBase, IDoorControl
{
	[SerializeField]
	private GenericAssignableAnimComponent leverAnimComponent;

	public List<Motherboard> LinkedMotherboards = new List<Motherboard>();

	public virtual bool IsTriggered => Setting > 0.0;

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return Setting;
		}
		return base.GetLogicValue(logicType);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Open)
		{
			Setting = interactable.State;
		}
	}

	public override void OnSettingChanged()
	{
		base.OnSettingChanged();
		SetMotherboards(IsTriggered);
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		leverAnimComponent.RefreshState(skipAnimation);
	}

	public void SetMotherboards(bool isTriggered)
	{
		foreach (Motherboard linkedMotherboard in LinkedMotherboards)
		{
			Circuitboard circuitboard = linkedMotherboard as Circuitboard;
			if ((bool)circuitboard)
			{
				circuitboard.RemoteToggle(isTriggered);
			}
		}
	}

	public override void OnLinkWithBoard(Motherboard motherboard)
	{
		base.OnLinkWithBoard(motherboard);
		LinkedMotherboards.Add(motherboard);
	}

	public override void OnUnlinkWithBoard(Motherboard motherboard)
	{
		base.OnUnlinkWithBoard(motherboard);
		LinkedMotherboards.Remove(motherboard);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (IsLocked)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceLocked.AsColor("red"));
		}
		if (!IsAuthorized(interaction.SourceThing))
		{
			return delayedActionInstance.Fail(GameStrings.AccessCardUnableToInteract);
		}
		if (interactable.Action != InteractableType.Open)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		if (!doAction)
		{
			return delayedActionInstance.Succeed();
		}
		OnServer.Interact(base.InteractOpen, (base.InteractOpen.State != 1) ? 1 : 0);
		return delayedActionInstance.Succeed();
	}
}
