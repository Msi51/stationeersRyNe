using UnityEngine;

namespace Assets.Scripts.UI;

internal static class PromptWaitingStrings
{
	private static readonly int TitleHash = Animator.StringToHash("WaitingTitle");

	private static readonly int BodyHash = Animator.StringToHash("WaitingBody");

	private static readonly int ButtonHash = Animator.StringToHash("ButtonOk");

	public static string Title => Localization.GetInterface(TitleHash);

	public static string Body => Localization.GetInterface(BodyHash);

	public static string Button => Localization.GetInterface(ButtonHash);
}
