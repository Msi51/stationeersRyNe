using Assets.Scripts.Objects.Motherboards;
using TMPro;

namespace Objects.Rockets.UI;

public class RocketDropdownOption : TMP_Dropdown.OptionData
{
	public ConnectedRocketInfo RocketInfo;

	public RocketDropdownOption(ConnectedRocketInfo info, string name)
		: base(name)
	{
		RocketInfo = info;
	}
}
