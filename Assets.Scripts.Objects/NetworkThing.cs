namespace Assets.Scripts.Objects;

public static class NetworkThing
{
	public static long Invalid;

	public static bool IsValid(long referenceId)
	{
		return referenceId > Invalid;
	}
}
