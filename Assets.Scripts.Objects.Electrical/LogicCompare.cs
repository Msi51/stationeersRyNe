using System;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class LogicCompare : LogicUnitProcessor
{
	public override string[] ModeStrings => Enum.GetNames(typeof(ConditionOperation));

	public bool CompareWithOperator(double value1, double value2)
	{
		return (ConditionOperation)(byte)Mode switch
		{
			ConditionOperation.Equals => RocketMath.Approximately(value1, value2), 
			ConditionOperation.Greater => value1 > value2, 
			ConditionOperation.Less => value1 < value2, 
			ConditionOperation.NotEquals => !RocketMath.Approximately(value1, value2), 
			_ => false, 
		};
	}

	public override void Operation()
	{
		_checkValue = (CompareWithOperator(base.Input1.Setting, base.Input2.Setting) ? 1 : 0);
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
			return ((ConditionOperation)Mode/*cast due to constrained. prefix*/).ToString();
		}
		return base.GetContextualName(interactable);
	}

	public override string _NextOperatorType(bool isForward)
	{
		return ((ConditionOperation)Mode).GetNext(isForward, isSorted: true).ToString();
	}

	public override void SwitchOperator(bool isForward)
	{
		OnServer.Interact(base.InteractMode, (int)((ConditionOperation)Mode).GetNext(isForward, isSorted: true));
	}
}
