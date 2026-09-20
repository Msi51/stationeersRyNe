using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.UI.ImGuiUi;

public class ImGuiThingCounterDebug
{
	public enum ThingCountRule
	{
		AllThings,
		Structures,
		DynamicThings,
		AnimatorThings,
		Pipes,
		Cables,
		Atmospheres,
		CableNetworks
	}

	private static readonly Dictionary<string, int> ThingCount = new Dictionary<string, int>();

	private static readonly string TotalNumberOf = "Total Number of ";

	private static readonly string AllAnimators = "All Animators: ";

	public static readonly string CountRules = "[AllThings / Structures / DynamicThings / AnimatorThings / Pipes / Cables / Atmospheres / CableNetworks]";

	private static string[] _countRulesArray;

	private static void SortKeysByValue(Dictionary<string, int> dictionary, out List<string> sortedList)
	{
		sortedList = new List<string>();
		foreach (string key in dictionary.Keys)
		{
			sortedList.Add(key);
		}
		sortedList.Sort((string a, string b) => SortByGreatestNumber(dictionary[a], dictionary[b]));
	}

	private static int SortByGreatestNumber(int aQuantity, int bQuantity)
	{
		if (aQuantity > bQuantity)
		{
			return 1;
		}
		if (bQuantity > aQuantity)
		{
			return -1;
		}
		return 0;
	}

	private static bool ShouldCount(ThingCountRule countRule, Thing thing)
	{
		return countRule switch
		{
			ThingCountRule.AllThings => true, 
			ThingCountRule.Structures => thing.AsStructure, 
			ThingCountRule.DynamicThings => thing.AsDynamicThing, 
			ThingCountRule.AnimatorThings => thing.BaseAnimator, 
			ThingCountRule.Pipes => thing is Pipe, 
			ThingCountRule.Cables => thing is Cable, 
			_ => false, 
		};
	}
}
