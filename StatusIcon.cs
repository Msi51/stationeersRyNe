using System;
using Assets.Scripts.UI;

public class StatusIcon : ToolTipBase
{
	public string DisplayNameKey;

	public string DescriptionKey;

	public Func<string> ToTooltip { get; set; }

	public override string GetString()
	{
		return ToTooltip?.Invoke();
	}
}
