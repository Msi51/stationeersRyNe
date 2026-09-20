using System;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.UI;

public class StationCategoryInsert
{
	public string NameOfThing;

	public int PrefabHash;

	public string PageLink;

	public Sprite InsertImage;

	public static StationCategoryInsert MakeAsConsumer(Thing prefab, IResourceConsumer output)
	{
		if (prefab is ISolidFuel solidFuel && output is SolidFuelGenerator)
		{
			return new StationCategoryInsertWithSubtext(solidFuel, output);
		}
		return new StationCategoryInsert(prefab, (Thing)output);
	}

	public static StationCategoryInsert MakeAsResource(Thing prefab, Item output)
	{
		if (prefab is SolidFuelGenerator && output is ISolidFuel solidFuel)
		{
			return new StationCategoryInsertWithSubtext(prefab, solidFuel);
		}
		return new StationCategoryInsert(prefab, output);
	}

	public StationCategoryInsert()
	{
	}

	public StationCategoryInsert(Thing prefab, Thing output)
	{
		try
		{
			NameOfThing = Localization.GetThingName(output.GetPrefabName());
			PageLink = "Thing" + output.GetPrefabName();
			PrefabHash = output.GetPrefabHash();
			InsertImage = output.GetThumbnail();
		}
		catch (FormatException ex)
		{
			Debug.LogError("There was an error with text " + output.GetPrefabName() + " " + ex.Message);
		}
	}

	public virtual void ApplyTo(SPDAListItem newInsert)
	{
		newInsert.InsertTitle.text = Stationpedia.Trim(NameOfThing);
		newInsert.SubObject.SetVisible(isVisble: false);
	}
}
