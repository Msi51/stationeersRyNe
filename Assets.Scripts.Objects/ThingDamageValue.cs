using UnityEngine;

namespace Assets.Scripts.Objects;

public class ThingDamageValue
{
	private readonly float _maxDamage;

	private const float MinDamage = 0f;

	public float Value { get; private set; }

	public ThingDamageValue(float maxDamage)
	{
		_maxDamage = maxDamage;
	}

	public bool Damage(ChangeDamageType change, float value)
	{
		float value2 = default(float);
		switch (change)
		{
		case ChangeDamageType.Set:
			value2 = value;
			break;
		case ChangeDamageType.Increment:
			value2 = Value + value;
			break;
		case ChangeDamageType.Decrement:
			value2 = Value - value;
			break;
		default:
			global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(change);
			break;
		}
		float num = Mathf.Clamp(value2, 0f, _maxDamage);
		bool result = !Mathf.Approximately(num, Value) || !(num < _maxDamage);
		Value = num;
		return result;
	}
}
