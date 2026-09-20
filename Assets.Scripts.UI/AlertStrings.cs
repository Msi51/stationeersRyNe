using UnityEngine;

namespace Assets.Scripts.UI;

internal static class AlertStrings
{
	private static readonly int InvalidIPHash = Animator.StringToHash("AlertInvalidIP");

	private static readonly int PlayerCountExceededHash = Animator.StringToHash("AlertPlayerCountExceeded");

	private static readonly int VersionMismatchHash = Animator.StringToHash("AlertVersionMismatch");

	private static readonly int NoResponseHash = Animator.StringToHash("AlertNoResponse");

	private static readonly int KickedHash = Animator.StringToHash("AlertKicked");

	private static readonly int TradingNoMoneyHash = Animator.StringToHash("TradingNoMoneyHash");

	private static readonly int SomethingWentWrongTradingHash = Animator.StringToHash("SomethingWentWrongTrading");

	private static readonly int SomethingWentWrongTrading2Hash = Animator.StringToHash("SomethingWentWrongTrading2");

	private static readonly int TraderGoneMissingHash = Animator.StringToHash("TraderGoneMissing");

	private static readonly int WrongPasswordHash = Animator.StringToHash("AlertWrongPassword");

	private static readonly int InvalidPortHash = Animator.StringToHash("AlertInvalidPort");

	private static readonly int CannotUpdatePortHash = Animator.StringToHash("AlertCannotUpdatePort");

	private static readonly int BlankSaveNameHash = Animator.StringToHash("AlertBlankSaveName");

	private static readonly int NeedToSaveBeforeTravelHash = Animator.StringToHash("SaveBeforeTravel");

	private static readonly int TitleAttentionHash = Animator.StringToHash("AlertTitleAttention");

	private static readonly int TitlePleaseWaitHash = Animator.StringToHash("AlertTitlePleaseWait");

	private static readonly int AlertNoBackupsHash = Animator.StringToHash("AlertNoBackups");

	private static readonly int AlertaYourSaveIsCooked = Animator.StringToHash("AlertYourSaveFileIsBroken");

	private static readonly int SaveLocationBrokenHash = Animator.StringToHash("SaveLocationBroken");

	private static readonly int TravelSameWorldHash = Animator.StringToHash("TravelSameWorld");

	public static string InvalidIP => Localization.GetInterface(InvalidIPHash);

	public static string PlayerCountExceeded => Localization.GetInterface(PlayerCountExceededHash);

	public static string VersionMismatch => Localization.GetInterface(VersionMismatchHash);

	public static string NoResponse => Localization.GetInterface(NoResponseHash);

	public static string Kicked => Localization.GetInterface(KickedHash);

	public static string NotEnoughMoneyTrading => Localization.GetInterface(TradingNoMoneyHash);

	public static string SomethingWentWrongTrading => Localization.GetInterface(SomethingWentWrongTradingHash);

	public static string SomethingWentWrong2Trading => Localization.GetInterface(SomethingWentWrongTrading2Hash);

	public static string TraderGoneMissing => Localization.GetInterface(TraderGoneMissingHash);

	public static string WrongPassword => Localization.GetInterface(WrongPasswordHash);

	public static string InvalidPort => Localization.GetInterface(InvalidPortHash);

	public static string CannotUpdatePort => Localization.GetInterface(CannotUpdatePortHash);

	public static string BlankSaveName => Localization.GetInterface(BlankSaveNameHash);

	public static string NeedToSaveBeforeTravel => Localization.GetInterface(NeedToSaveBeforeTravelHash);

	public static string TitleAttention => Localization.GetInterface(TitleAttentionHash);

	public static string TitlePleaseWait => Localization.GetInterface(TitlePleaseWaitHash);

	public static string AlertNoBackups => Localization.GetInterface(AlertNoBackupsHash);

	public static string AlertSaveFileBroken => Localization.GetInterface(AlertaYourSaveIsCooked);

	public static string SaveLocationBroken => Localization.GetInterface(SaveLocationBrokenHash);

	public static string TravelSameWorld => Localization.GetInterface(TravelSameWorldHash);
}
