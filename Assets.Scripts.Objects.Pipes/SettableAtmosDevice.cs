using System.Globalization;
using System.Text;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Trading;

namespace Assets.Scripts.Objects.Pipes;

public class SettableAtmosDevice : DeviceInputOutput, ISetable, ILogicable, IReferencable, IEvaluable
{
	public double Setting
	{
		get
		{
			return base.OutputSetting;
		}
		set
		{
			base.OutputSetting = (float)value;
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		return interactable.Action switch
		{
			InteractableType.Button1 => GameStrings.GlobalIncrease.DisplayString, 
			InteractableType.Button2 => GameStrings.GlobalDecrease.DisplayString, 
			_ => base.GetContextualName(interactable), 
		};
	}

	public DelayedActionInstance HandleButtonSetting(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action != InteractableType.Button1 && interactable.Action != InteractableType.Button2)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
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
		return null;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.AppendLine(GameStrings.TooltipOutputSetting.AsString(base.OutputSetting.ToString(CultureInfo.InvariantCulture).AsColor("yellow")));
		return extendedText;
	}

	public override PassiveUITooltip GetPassiveUITooltip()
	{
		return PassiveUITooltip.Make(DisplayName, GetExtendedText().ToString());
	}
}
