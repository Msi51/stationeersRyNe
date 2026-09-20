using System.Collections.Generic;

namespace Assets.Scripts.Objects.Electrical;

public interface ISolarRepairer : IRepairer
{
	static List<ISolarRepairer> Prefabs;

	static string Tooltip;

	string ToTooltip();

	static ISolarRepairer()
	{
		Prefabs = new List<ISolarRepairer>(10);
		Tooltip = string.Empty;
	}
}
