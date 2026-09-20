using Objects.Rockets.Scanning;
using TMPro;

namespace Objects.Rockets.UI;

public class RocketActionTargetDropdownOption : TMP_Dropdown.OptionData
{
	public IRocketActionProgressableTarget Target;

	public RocketActionTargetDropdownOption(IRocketActionProgressableTarget target, string name)
		: base(name)
	{
		Target = target;
	}
}
