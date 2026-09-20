using TMPro;

namespace Objects.Rockets.UI;

public class RocketReEntryDropdownOption : TMP_Dropdown.OptionData
{
	public ReEntryProfile ReEntryProfile;

	public RocketReEntryDropdownOption(ReEntryProfile profile, string name)
		: base(name)
	{
		ReEntryProfile = profile;
	}
}
