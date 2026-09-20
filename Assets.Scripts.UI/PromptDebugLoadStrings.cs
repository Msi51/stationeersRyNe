using UnityEngine;

namespace Assets.Scripts.UI;

internal static class PromptDebugLoadStrings
{
	private static readonly int TitleHash = Animator.StringToHash("PromptDebugLoadTitle");

	private static readonly int BodyHash = Animator.StringToHash("PromptDebugLoadBody");

	private static readonly int ButtonHash = Animator.StringToHash("PromptDebugLoadConfirm");

	public static string Title => Localization.GetInterface(TitleHash);

	public static string Body => Localization.GetInterface(BodyHash);

	public static string Button => Localization.GetInterface(ButtonHash);
}
