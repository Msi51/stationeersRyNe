namespace Assets.Scripts.UI;

public interface IScreenSpaceTooltip
{
	bool TooltipIsVisible { get; }

	void DoUpdate();
}
