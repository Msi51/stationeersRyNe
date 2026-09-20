using System;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class LogicSelect : LogicUnitProcessor
{
	public override string[] ModeStrings => Enum.GetNames(typeof(ConditionOperation));

	protected override bool IsOperable
	{
		get
		{
			if (base.Input1 == null || base.Input2 == null || base.Input3 == null || base.InputNetwork1 == null || base.InputNetwork2 == null || base.InputNetwork3 == null || !base.InputNetwork1.DataDeviceList.Contains(base.Input1) || !base.InputNetwork2.DataDeviceList.Contains(base.Input2) || !base.InputNetwork3.DataDeviceList.Contains(base.Input3))
			{
				if (Error == 0)
				{
					OnServer.Interact(base.InteractError, 1);
				}
				return false;
			}
			if (Error == 1)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return true;
		}
	}

	public override void Operation()
	{
		_checkValue = ((base.Input3.Setting > 0.0) ? base.Input2.Setting : base.Input1.Setting);
		if (!RocketMath.Approximately(_checkValue, Setting))
		{
			Setting = _checkValue;
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Button3)
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
			LogicUnitBase nextReadable = Logicable.GetNextReadable(this, base.Input3, base.InputNetwork3DevicesSorted, interaction.AltKey);
			if (!nextReadable)
			{
				return delayedActionInstance.Fail(GameStrings.LogicNoReadableDevices);
			}
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, nextReadable.ToTooltip());
			if (!KeyManager.GetButton(KeyMap.QuantityModifier))
			{
				delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			ScrewSound();
			if (GameManager.RunSimulation)
			{
				base.Input3 = nextReadable;
				Setting = 0.0;
			}
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button1)
		{
			if (!base.Input1)
			{
				return "Input 1";
			}
			return base.Input1.DisplayName;
		}
		if (interactable.Action == InteractableType.Button2)
		{
			if (!base.Input2)
			{
				return "Input 2";
			}
			return base.Input2.DisplayName;
		}
		if (interactable.Action == InteractableType.Button3)
		{
			if (!base.Input3)
			{
				return "Select";
			}
			return base.Input3.DisplayName;
		}
		return base.GetContextualName(interactable);
	}
}
