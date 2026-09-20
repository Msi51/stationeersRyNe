namespace Assets.Scripts;

public readonly struct ConsoleSegment(string text, uint color)
{
	public readonly string Text = text;

	public readonly uint Color = color;
}
