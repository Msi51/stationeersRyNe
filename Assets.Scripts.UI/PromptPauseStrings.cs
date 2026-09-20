using UnityEngine;

namespace Assets.Scripts.UI;

internal static class PromptPauseStrings
{
	private static readonly int TitleHash = Animator.StringToHash("PromptPauseTitle");

	private static readonly int PauseBodyHash = Animator.StringToHash("PromptPauseBody");

	private static readonly int LeaveButtonHash = Animator.StringToHash("PromptButtonLeave");

	private static readonly int ResumeButtonHash = Animator.StringToHash("PromptResumeButton");

	public static string Title => Localization.GetInterface(TitleHash);

	public static string PauseBody => Localization.GetInterface(PauseBodyHash);

	public static string LeaveButton => Localization.GetInterface(LeaveButtonHash);

	public static string ResumeButton => Localization.GetInterface(ResumeButtonHash);
}
