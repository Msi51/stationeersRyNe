using System.Collections.Generic;

namespace Assets.Scripts.Objects;

public interface IRobotRepairer : IRepairer
{
	static List<IRobotRepairer> Prefabs;

	static string Tooltip;

	string ToTooltip();

	static IRobotRepairer()
	{
		Prefabs = new List<IRobotRepairer>(10);
		Tooltip = string.Empty;
	}
}
