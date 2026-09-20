namespace Assets.Scripts.UI;

public class HoverTooltip : ToolTipBase
{
	public string TooltipKey;

	public override string GetString()
	{
		return Localization.GetInterface(TooltipKey);
	}
}
