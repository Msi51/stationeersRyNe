using System.Collections.Generic;

namespace Assets.Scripts.Objects;

public interface ISuitReparier : IRepairer
{
	static List<ISuitReparier> Prefabs;

	static string Tooltip;

	string ToTooltip();

	void RepairLeak(long suitNetId, float quantityToRepair);

	static ISuitReparier()
	{
		Prefabs = new List<ISuitReparier>(10);
		Tooltip = string.Empty;
	}
}
