using System;
using Assets.Scripts;
using Assets.Scripts.Localization2;

namespace Trading;

public static class CompareOperatorExtensions
{
	public static string DisplayString(this CompareOperator self)
	{
		return self switch
		{
			CompareOperator.Unassigned => string.Empty, 
			CompareOperator.Less => GameStrings.CompareLessThan.DisplayString, 
			CompareOperator.EqualOrLess => GameStrings.CompareEqualOrLess.DisplayString, 
			CompareOperator.Equal => GameStrings.CompareEqual.DisplayString, 
			CompareOperator.EqualOrGreater => GameStrings.CompareEqualOrGreater.DisplayString, 
			CompareOperator.Greater => GameStrings.CompareGreater.DisplayString, 
			_ => throw new ArgumentOutOfRangeException("self", self, null), 
		};
	}
}
