using UnityEngine;

namespace Assets.Scripts.UI;

internal static class PromptSteamErrorStrings
{
	private static readonly int TitleHash = Animator.StringToHash("SteamErrorTitle");

	private static readonly int BodyHash = Animator.StringToHash("SteamErrorBody");

	private static readonly int ButtonHash = Animator.StringToHash("SteamErrorButton");

	public static string Title => Localization.GetInterface(TitleHash);

	public static string Body => Localization.GetInterface(BodyHash);

	public static string Button => Localization.GetInterface(ButtonHash);
}
