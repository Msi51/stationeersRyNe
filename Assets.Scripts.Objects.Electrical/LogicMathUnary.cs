using System;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicMathUnary : LogicUnitProcessor
{
	public static EnumCollection<MathOperatorsUnary, byte> EnumOperators = new EnumCollection<MathOperatorsUnary, byte>(toProper: false);

	private System.Random _rnd;

	private int _seed;

	public override string[] ModeStrings => EnumOperators.Names;

	protected override bool IsOperable
	{
		get
		{
			if (base.Input1 == null || base.InputNetwork1 == null || !base.InputNetwork1.DataDeviceList.Contains(base.Input1))
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

	public override void Awake()
	{
		base.Awake();
		_rnd = new System.Random((int)(UnityEngine.Random.value * 2.1474836E+09f));
	}

	public override void Operation()
	{
		switch ((MathOperatorsUnary)(byte)Mode)
		{
		case MathOperatorsUnary.Ceil:
			_checkValue = Math.Ceiling(base.Input1.Setting);
			break;
		case MathOperatorsUnary.Floor:
			_checkValue = Math.Floor(base.Input1.Setting);
			break;
		case MathOperatorsUnary.Abs:
			_checkValue = Math.Abs(base.Input1.Setting);
			break;
		case MathOperatorsUnary.Log:
			_checkValue = Math.Log(base.Input1.Setting);
			break;
		case MathOperatorsUnary.Exp:
			_checkValue = Math.Exp(base.Input1.Setting);
			break;
		case MathOperatorsUnary.Round:
			_checkValue = Math.Round(base.Input1.Setting);
			break;
		case MathOperatorsUnary.Rand:
			_checkValue = _rnd.NextDouble() * base.Input1.Setting;
			break;
		case MathOperatorsUnary.Sqrt:
			_checkValue = Math.Sqrt(base.Input1.Setting);
			break;
		case MathOperatorsUnary.Sin:
			_checkValue = Math.Sin(base.Input1.Setting.DegreeToRadian());
			break;
		case MathOperatorsUnary.Cos:
			_checkValue = Math.Cos(base.Input1.Setting.DegreeToRadian());
			break;
		case MathOperatorsUnary.Tan:
			_checkValue = Math.Tan(base.Input1.Setting.DegreeToRadian());
			break;
		case MathOperatorsUnary.Asin:
			_checkValue = Math.Asin(base.Input1.Setting).RadianToDegree();
			break;
		case MathOperatorsUnary.Acos:
			_checkValue = Math.Acos(base.Input1.Setting).RadianToDegree();
			break;
		case MathOperatorsUnary.Atan:
			_checkValue = Math.Atan(base.Input1.Setting).RadianToDegree();
			break;
		case MathOperatorsUnary.Not:
			_checkValue = ((!base.Input1.IsSet) ? 1 : 0);
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
				return "Input";
			}
			return base.Input1.DisplayName;
		}
		if (interactable.Action == InteractableType.Button3)
		{
			return EnumOperators.GetNameFromValue(Mode);
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
		if (base.Input1 == null)
		{
			result.Title = DisplayName;
			result.Extended = InterfaceStrings.LogicNoDevice.ToString("red");
			return result;
		}
		result.Title = DisplayName;
		result.Extended = string.Format("{2}({1}) = <color=yellow>{0}</color>", Setting.ToStringExact(), base.Input1.ToTooltip(), EnumOperators.GetNameFromValue(Mode));
		return result;
	}

	public override string _NextOperatorType(bool isForward)
	{
		return EnumOperators.GetName(((MathOperatorsUnary)Mode).GetNext(isForward, isSorted: true));
	}

	public override void SwitchOperator(bool isForward)
	{
		OnServer.Interact(base.InteractMode, (int)((MathOperatorsUnary)Mode).GetNext(isForward, isSorted: true));
	}
}
