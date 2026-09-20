using Assets.Scripts.Util;

namespace CharacterCustomisation;

public static class CharacterKitEx
{
	public static KitItem GetItem(this KitItem[] kitItems, string id, bool allowNullReturn = false)
	{
		if (!string.IsNullOrEmpty(id))
		{
			foreach (KitItem kitItem in kitItems)
			{
				if (kitItem.Id == id)
				{
					return kitItem;
				}
			}
		}
		if (!allowNullReturn && kitItems.Length != 0)
		{
			return kitItems[0];
		}
		return null;
	}

	public static string GetElementId(this KitItem[] kitItems, int index)
	{
		return kitItems.GetElement(index)?.Id ?? string.Empty;
	}

	public static int IndexOf(this KitItem[] kitItems, string id)
	{
		for (int i = 0; i < kitItems.Length; i++)
		{
			if (kitItems[i].Id == id)
			{
				return i;
			}
		}
		return -1;
	}
}
