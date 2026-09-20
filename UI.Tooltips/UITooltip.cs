using Assets.Scripts.UI;

namespace UI.Tooltips;

public class UITooltip : UserInterfaceBase
{
	public string TooltipText { get; set; }

	private new void OnEnable()
	{
		UITooltipManager.Register(this);
	}

	private new void OnDisable()
	{
		UITooltipManager.UnRegister(this);
	}
}
