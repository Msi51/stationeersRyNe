using UnityEngine;

namespace Assets.Scripts.Serialization;

internal static class SaveButtonStrings
{
	private static readonly int SaveWorldHash = Animator.StringToHash("SaveMenuSaveWorldButton");

	private static readonly int OverwriteHash = Animator.StringToHash("SaveMenuOverwriteButton");

	public static string SaveWorld => Localization.GetInterface(SaveWorldHash);

	public static string Overwrite => Localization.GetInterface(OverwriteHash);
}
