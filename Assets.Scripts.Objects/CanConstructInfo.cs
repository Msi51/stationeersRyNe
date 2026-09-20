namespace Assets.Scripts.Objects;

public readonly struct CanConstructInfo
{
	public static readonly CanConstructInfo ValidPlacement = new CanConstructInfo(canConstruct: true, string.Empty);

	public bool CanConstruct { get; }

	public string ErrorMessage { get; }

	public CanConstructInfo(bool canConstruct, string errorMessage)
	{
		CanConstruct = canConstruct;
		ErrorMessage = errorMessage;
	}

	public static CanConstructInfo InvalidPlacement(string error)
	{
		return new CanConstructInfo(canConstruct: false, error);
	}
}
