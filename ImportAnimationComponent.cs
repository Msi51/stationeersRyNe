using Assets.Scripts.Objects;
using Assets.Scripts.Util;

public class ImportAnimationComponent : BinaryTransformAnimComponent
{
	protected override int InteractableState
	{
		get
		{
			if (!(parentThing != null))
			{
				return 0;
			}
			return parentThing.Importing;
		}
	}

	public override InteractableType AssignedAction => InteractableType.Import;

	protected override void TriggerAudio()
	{
		if (InteractableState == 1)
		{
			PlaySound(Defines.Sounds.DeviceImportHash);
		}
	}

	protected override void OnAnimationStart()
	{
		base.OnAnimationStart();
		if (InteractableState == 1)
		{
			base.InteractableCollider.enabled = false;
		}
	}

	protected override void OnAnimationCompleted()
	{
		base.OnAnimationCompleted();
		if (InteractableState == 0)
		{
			base.InteractableCollider.enabled = true;
		}
	}
}
