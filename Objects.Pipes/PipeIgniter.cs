using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;

namespace Objects.Pipes;

public class PipeIgniter : DevicePipeMounted
{
	public override bool OnOff => true;

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (base.NetworkAtmosphere != null && Powered && Error == 0 && Activate == 1)
		{
			base.NetworkAtmosphere.GasMixture.AddEnergy(new MoleEnergy(5.0));
			base.NetworkAtmosphere.Sparked = true;
		}
		OnServer.Interact(base.InteractActivate, 0);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			return HandleActivate(interactable, interaction, doAction);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	private DelayedActionInstance HandleActivate(Interactable interactable, Interaction interaction, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (!Powered)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
		}
		if (Error == 1)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceError);
		}
		if (!doAction)
		{
			return delayedActionInstance.Succeed();
		}
		OnServer.Interact(base.InteractActivate, 1);
		return delayedActionInstance.Succeed();
	}

	public override void CheckForPipe()
	{
		if (GameManager.RunSimulation && !IsValidPipe() && Error == 0)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if (GameManager.RunSimulation && IsValidPipe() && Error == 1)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}
}
