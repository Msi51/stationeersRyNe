using Sound;
using Trading;

namespace Assets.Scripts.Objects.Pipes;

public interface IAudioInput : ILogicable, IReferencable, IEvaluable
{
	IAudioInput AudioOutput { get; }

	PooledAudioSource InputAudioScheduled(int clipsDataHash, double startTime, double endTime, float volumeMultiplier = 1f, float pitchMultiplier = 1f, int attack = 0, int release = 0);

	PooledAudioSource InputAudio(int clipsDataHash, float volumeMultiplier = 1f, float pitchMultiplier = 1f);
}
