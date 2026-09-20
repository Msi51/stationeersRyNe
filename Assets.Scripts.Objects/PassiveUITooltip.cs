namespace Assets.Scripts.Objects;

public readonly struct PassiveUITooltip(string title, string extended)
{
	public readonly string Title = title;

	public readonly string Extended = extended;

	public static PassiveUITooltip Make(string displayName)
	{
		return new PassiveUITooltip(displayName, string.Empty);
	}

	public static PassiveUITooltip Make(string displayName, string toString)
	{
		return new PassiveUITooltip(displayName, toString);
	}
}
