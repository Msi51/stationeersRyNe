using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class WindowShutter : Door, ISmartRotatable
{
	public override bool CanLightPass
	{
		get
		{
			if (!IsOpen)
			{
				return base.CanLightPass;
			}
			return true;
		}
	}

	public override Vector3 CenterPosition => base.ThingTransformPosition + ThingTransform.rotation * Bounds.center;

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Open)
		{
			if (!OnOff)
			{
				return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceNoPower);
			}
			if (!Powered)
			{
				return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceNoPower);
			}
			if (!doAction)
			{
				return DelayedActionInstance.Success(interactable.ContextualName);
			}
			interactable.State = ((!IsOpen) ? 1 : 0);
			return DelayedActionInstance.Success(interactable.ContextualName);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override object GetLocalGridBounds()
	{
		return new Grid3[1] { Grid3.zero };
	}

	public override Grid3 GetLocalGrid()
	{
		return base.GridController.WorldToLocalGrid(CenterPosition, GridSize, GridOffset);
	}

	public new SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public new void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public new void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public new int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (!base.AllowInteraction || !base.IsStructureCompleted)
		{
			return base.AttackWith(attack, doAction);
		}
		Crowbar crowbar = attack.SourceItem as Crowbar;
		if ((bool)crowbar && (attack.TargetCollider == null || DoorColliders.Contains(attack.TargetCollider) || attack.TargetCollider.transform == ThingTransform))
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = crowbar.DoorForceOpenDuration,
				ActionMessage = (IsOpen ? ActionStrings.ForceClose : ActionStrings.ForceOpen)
			};
			if (attack.TargetCollider != null && attack.TargetCollider.transform != ThingTransform)
			{
				delayedActionInstance.Selection = GetSelection(attack.TargetCollider);
			}
			if (IsLocked)
			{
				delayedActionInstance.IsDisabled = true;
				delayedActionInstance.AppendStateMessage(GameStrings.DoorUnableToForceOpenLocked);
				return delayedActionInstance;
			}
			if (!OnOff || !Powered)
			{
				if (doAction && GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractOpen, (!IsOpen) ? 1 : 0);
				}
				return delayedActionInstance;
			}
			delayedActionInstance.IsDisabled = true;
			delayedActionInstance.AppendStateMessage(GameStrings.DoorUnableToForceOpenPowered);
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}
}
