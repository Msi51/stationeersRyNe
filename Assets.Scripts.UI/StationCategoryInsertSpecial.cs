using UnityEngine;

namespace Assets.Scripts.UI;

public class StationCategoryInsertSpecial : StationCategoryInsert
{
	public static StationCategoryInsert MakeSpecialPage(string ingotpage)
	{
		SPDAHomePageButtonOverride homePageOverride = Stationpedia.GetHomePageOverride(ingotpage);
		Sprite image = homePageOverride.GetImage();
		return new StationCategoryInsertSpecial
		{
			NameOfThing = Stationpedia.Trim(Localization.GetInterface(homePageOverride.CategoryName)),
			PageLink = homePageOverride.LinkedPageKey,
			InsertImage = image
		};
	}
}
