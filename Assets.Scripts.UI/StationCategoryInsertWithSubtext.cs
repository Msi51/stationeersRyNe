using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;

namespace Assets.Scripts.UI;

public class StationCategoryInsertWithSubtext : StationCategoryInsert
{
	public string SubText;

	public string SubValue;

	public StationCategoryInsertWithSubtext(ISolidFuel solidFuel, IResourceConsumer output)
		: base((Thing)solidFuel, (Thing)output)
	{
		SubValue = "Energy".AsColor("#0080FFFF");
		SubText = solidFuel.GetEnergyPerSecond().ToStringPrefix("W").AsColor("yellow");
	}

	public StationCategoryInsertWithSubtext(Thing prefab, ISolidFuel solidFuel)
		: base(prefab, (Thing)solidFuel)
	{
		SubValue = "Energy".AsColor("#0080FFFF");
		SubText = solidFuel.GetEnergyPerSecond().ToStringPrefix("W").AsColor("yellow");
	}

	public override void ApplyTo(SPDAListItem newInsert)
	{
		newInsert.InsertTitle.text = Stationpedia.Trim(NameOfThing);
		newInsert.SubText.text = SubText;
		newInsert.SubValue.text = SubValue;
		newInsert.SubObject.SetVisible(isVisble: true);
	}
}
