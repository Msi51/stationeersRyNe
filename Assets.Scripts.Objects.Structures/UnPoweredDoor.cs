using Assets.Scripts.Networks;

namespace Assets.Scripts.Objects.Structures;

public class UnPoweredDoor : Door
{
	protected override void AssessPower(CableNetwork cableNetwork, bool isOn)
	{
		SetPower(cableNetwork, hasPower: true);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Open)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (!doAction || !GameManager.RunSimulation)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
