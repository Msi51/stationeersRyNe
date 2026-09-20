namespace Assets.Scripts.Emotes;

public struct EmoteData
{
	public int AnimationIndex { get; }

	public int ResetDelay { get; }

	public EmoteType EmoteType { get; }

	public EmoteData(int index, int delay, EmoteType emoteType)
	{
		AnimationIndex = index;
		ResetDelay = delay;
		EmoteType = emoteType;
	}
}
