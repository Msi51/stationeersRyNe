using System;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class LogicMemory : LogicInputBase
{
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
		if (interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2 || interactable.Action == InteractableType.Button3 || interactable.Action == InteractableType.Button4)
		{
			Labeller labeller = interaction.SourceSlot.Occupant as Labeller;
			if ((bool)labeller)
			{
				delayedActionInstance.ActionMessage = ActionStrings.Set;
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceManualInputWindow);
				if (!labeller.OnOff)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
				if (!labeller.IsOperable)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				labeller.Set(this);
				return delayedActionInstance.Succeed();
			}
			if (!(interaction.SourceSlot.Occupant is Screwdriver))
			{
				delayedActionInstance.AppendStateMessage(GameStrings.RequiresScrewdriverOrLabeler);
				return delayedActionInstance.Fail();
			}
			if (!doAction)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalValue, Setting.ToStringExact());
				delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
				delayedActionInstance.AppendStateMessage(GameStrings.UseLabelerToSet);
				return delayedActionInstance;
			}
			if (!GameManager.RunSimulation)
			{
				return delayedActionInstance.Succeed();
			}
			double num = Setting;
			switch (interactable.Action)
			{
			case InteractableType.Button1:
				num += (double)(interaction.AltKey ? 10f : 100f);
				break;
			case InteractableType.Button2:
				num -= (double)(interaction.AltKey ? 10f : 100f);
				break;
			case InteractableType.Button3:
				num += (double)(interaction.AltKey ? 0.1f : 1f);
				break;
			case InteractableType.Button4:
				num -= (double)(interaction.AltKey ? 0.1f : 1f);
				break;
			}
			if (RocketMath.Approximately(num, Setting))
			{
				return delayedActionInstance.Succeed();
			}
			Setting = Math.Round(num, 1);
			PlayNetworkSound(Defines.Sounds.ScrewdriverSound);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return Setting;
		}
		return base.GetLogicValue(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		if (logicType == LogicType.Setting)
		{
			Setting = value;
		}
	}
}
