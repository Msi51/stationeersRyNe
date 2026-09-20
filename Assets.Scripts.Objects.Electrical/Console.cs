using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Console : Computer, IAirlockDevice
{
	public override bool ShowComputerScreen
	{
		get
		{
			if (base.CurrentBuildStateIndex != 0)
			{
				return base.ShowComputerScreen;
			}
			return false;
		}
	}

	public override bool CanLogicWrite(LogicSlotType logicSlotType, int slotId)
	{
		if (logicSlotType == LogicSlotType.Mode)
		{
			return GetSlot(slotId) != null;
		}
		return base.CanLogicWrite(logicSlotType, slotId);
	}

	public override bool CanLogicRead(LogicSlotType logicSlotType, int slotId)
	{
		if (logicSlotType == LogicSlotType.Mode)
		{
			return GetSlot(slotId) != null;
		}
		return base.CanLogicRead(logicSlotType, slotId);
	}

	public override void SetLogicValue(LogicSlotType logicSlotType, int slotId, double value)
	{
		if (logicSlotType == LogicSlotType.Mode)
		{
			DynamicThing dynamicThing = GetSlot(slotId)?.Get();
			if ((object)dynamicThing != null)
			{
				int state = (int)value.Clamp(0.0, ModeStrings.Length - 1);
				OnServer.Interact(dynamicThing.InteractMode, state);
				return;
			}
		}
		base.SetLogicValue(logicSlotType, slotId, value);
	}

	public override double GetLogicValue(LogicSlotType logicSlotType, int slotId)
	{
		if (logicSlotType == LogicSlotType.Mode)
		{
			DynamicThing dynamicThing = GetSlot(slotId)?.Get();
			if ((object)dynamicThing != null)
			{
				return dynamicThing.Mode;
			}
		}
		return base.GetLogicValue(logicSlotType, slotId);
	}

	public override CanConstructInfo CanConstruct()
	{
		if (PlacementType == PlacementSnap.FaceMount)
		{
			CanMountResult canMountResult = CanMountOnWall();
			if (canMountResult.result != WallMountResult.Valid)
			{
				return CanConstructInfo.InvalidPlacement(canMountResult.ResultMessage());
			}
		}
		return base.CanConstruct();
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (PlacementType == PlacementSnap.FaceMount)
		{
			float num = Vector3.Dot(base.Screen.transform.up, Vector3.up);
			float num2 = Vector3.Dot(-base.Screen.transform.up, Vector3.up);
			float num3 = Vector3.Dot(base.Screen.transform.right, Vector3.up);
			float num4 = Vector3.Dot(-base.Screen.transform.right, Vector3.up);
			float b = Mathf.Max(num, num2, num3, num4);
			if (Mathf.Approximately(num3, b))
			{
				base.Screen.transform.Rotate(Vector3.forward, -90f, Space.Self);
			}
			else if (Mathf.Approximately(num4, b))
			{
				base.Screen.transform.Rotate(Vector3.forward, 90f, Space.Self);
			}
			else if (Mathf.Approximately(num2, b))
			{
				base.Screen.transform.Rotate(Vector3.forward, 180f, Space.Self);
			}
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			if (base.InteractActivate.State != 1)
			{
				return GameStrings.DeviceConfigMode.AsString();
			}
			return GameStrings.DeviceOperateMode.AsString();
		}
		return base.GetContextualName(interactable);
	}

	private void SetConfigState()
	{
		if (base.CurrentMotherboard != null)
		{
			base.CurrentMotherboard.SetMode(base.InteractActivate.State == 0);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		SetConfigState();
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		SetConfigState();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		SetConfigState();
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (!(interaction.SourceSlot.Occupant is Screwdriver))
			{
				return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void UpdateStateVisualizer(bool visualOnly = false)
	{
		base.UpdateStateVisualizer(visualOnly);
		if ((bool)Interactables[0].Collider)
		{
			Interactables[0].Collider.enabled = base.CurrentBuildStateIndex == 0;
		}
		base.Screen.SetActive(ShowComputerScreen);
	}

	public override void MoveAllSlotItemsToWorld()
	{
	}

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
			return base.CurrentMotherboard ? base.CurrentMotherboard.Flag : 0;
		}
		return base.GetLogicValue(logicType);
	}
}
