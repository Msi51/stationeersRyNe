using Assets.Scripts.UI;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public interface IScriptEnum
{
	void Execute(ref bool isValueSet, ref double value, string code, InstructionInclude propertiesToUse);

	void Execute(ref bool isValueSet, ref int value, string code, InstructionInclude propertiesToUse);

	void Parse(ref string masterString);

	int Count();

	HelpReference MakePage(int i, HelpReference prefab, RectTransform parent);

	bool TryParse(string searchText);

	bool IsHashType(int hash);

	bool IsDeprecated(int i);
}
