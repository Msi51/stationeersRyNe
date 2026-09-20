using Assets.Scripts.Objects;
using Assets.Scripts.Util;

namespace Assets.Scripts.AssetCreation;

public class ThingAssetCreation
{
	public static string SanitizePrefabName(Thing prefab)
	{
		return SanitizePrefabName(prefab.PrefabName);
	}

	public static string SanitizePrefabName(string name)
	{
		string[] array = new string[2] { "Item", "Structure" };
		foreach (string text in array)
		{
			if (name.StartsWith(text))
			{
				name = name.Substring(text.Length);
			}
		}
		return name.ToProper();
	}
}
