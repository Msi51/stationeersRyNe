using Assets.Scripts.UI;

namespace Assets.Scripts.Objects.Electrical;

public abstract class LogicInputBase : LogicUnitBase
{
	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.LogicInputCategory);
	}
}
