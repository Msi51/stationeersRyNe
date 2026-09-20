namespace Assets.Scripts.Objects.Electrical;

public interface ISoundAlert
{
	byte SoundAlert { get; }

	byte SoundVolume { get; }

	Thing GetAsThing { get; }
}
