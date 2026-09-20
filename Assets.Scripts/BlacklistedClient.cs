namespace Assets.Scripts;

internal struct BlacklistedClient
{
	public static string PATH => StationSaveUtils.GetSavePath() + "/Blacklist.txt";

	public ulong Id { get; set; }
}
