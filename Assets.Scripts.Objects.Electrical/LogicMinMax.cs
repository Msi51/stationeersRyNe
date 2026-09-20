using System;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicMinMax : LogicUnitProcessor
{
	private enum _ComparisonOperation
	{
		Greater = 1,
		Less
	}

	public override string[] ModeStrings => Enum.GetNames(typeof(_ComparisonOperation));

	public bool CompareWithOperator(double value1, double value2)
	{
		if (Mode != 2)
		{
			return value1 > value2;
		}
		return value1 < value2;
	}

	public override void Operation()
	{
		_checkValue = (CompareWithOperator(base.Input1.Setting, base.Input2.Setting) ? base.Input1.Setting : base.Input2.Setting);
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
			if (Mode != 2)
			{
				return ConditionOperation.Greater.ToString();
			}
			return ConditionOperation.Less.ToString();
		}
		return base.GetContextualName(interactable);
	}

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
		result.Title = DisplayName;
		if (base.Input1 == null || base.Input2 == null)
		{
			result.Extended = InterfaceStrings.LogicNoDevice.ToString("red");
			return result;
		}
		result.Extended = string.Format("{2}({1},{3}) = <color=yellow>{0}</color>", Setting.ToStringExact(), base.Input1.ToTooltip(), ((byte)Mode != 2) ? ConditionOperation.Greater : ConditionOperation.Less, base.Input2.ToTooltip());
		return result;
	}

	public override string _NextOperatorType(bool isForward)
	{
		return ((_ComparisonOperation)Mode).GetNext(isForward, isSorted: true).ToString();
	}

	public override void SwitchOperator(bool isForward)
	{
		OnServer.Interact(base.InteractMode, (int)((_ComparisonOperation)Mode).GetNext(isForward, isSorted: true));
	}
}
