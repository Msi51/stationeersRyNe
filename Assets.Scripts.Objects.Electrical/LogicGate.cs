using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicGate : LogicUnitProcessor
{
	public static EnumCollection<GateOperators, byte> EnumOperators = new EnumCollection<GateOperators, byte>(toProper: false);

	public override string[] ModeStrings => EnumOperators.Names;

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		foreach (Connection openEnd in OpenEnds)
		{
			if (hitCollider == openEnd.Collider && hitCollider != null)
			{
				return result.Populate(openEnd);
			}
		}
		if (base.Input1 == null || base.Input2 == null)
		{
			result.Title = DisplayName;
			result.Extended = InterfaceStrings.LogicNoDevice.ToString("red");
			return result;
		}
		result.Title = DisplayName;
		result.Extended = string.Format("{1} {2} {3} = <color=yellow>{0}</color>", Setting.ToStringExact(), base.Input1.ToTooltip(), EnumOperators.GetNameFromValue(Mode), base.Input2.ToTooltip());
		return result;
	}

	public override void Operation()
	{
		switch ((GateOperators)(byte)Mode)
		{
		case GateOperators.AND:
			_checkValue = ((base.Input1.IsSet && base.Input2.IsSet) ? 1 : 0);
			break;
		case GateOperators.OR:
			_checkValue = ((base.Input1.IsSet || base.Input2.IsSet) ? 1 : 0);
			break;
		case GateOperators.XOR:
			_checkValue = ((base.Input1.IsSet ^ base.Input2.IsSet) ? 1 : 0);
			break;
		case GateOperators.NAND:
			_checkValue = ((!base.Input1.IsSet || !base.Input2.IsSet) ? 1 : 0);
			break;
		case GateOperators.NOR:
			_checkValue = ((!base.Input1.IsSet && !base.Input2.IsSet) ? 1 : 0);
			break;
		case GateOperators.XNOR:
			_checkValue = ((!(base.Input1.IsSet ^ base.Input2.IsSet)) ? 1 : 0);
			break;
		}
		if (!RocketMath.Approximately(_checkValue, Setting))
		{
			Setting = _checkValue;
		}
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
			return EnumOperators.GetNameFromValue(Mode);
		}
		return base.GetContextualName(interactable);
	}

	public override string _NextOperatorType(bool isForward)
	{
		return EnumOperators.GetName(((GateOperators)Mode).GetNext(isForward, isSorted: true));
	}

	public override void SwitchOperator(bool isForward)
	{
		OnServer.Interact(base.InteractMode, (int)((GateOperators)Mode).GetNext(isForward, isSorted: true));
	}
}
