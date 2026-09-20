using Sound;
using UnityEngine;

namespace Assets.Scripts.Objects;

public abstract class Fire
{
	private const float MAX_DISTANCE_TO_RENDER = 50f;

	protected const float FIRE_RENDER_DISTANCE_SQUARED = 2500f;

	protected FlameParticleData _particleData;

	protected PooledAudioSource _activeAudio;

	public virtual Vector3 Position { get; }

	public virtual bool IsValid { get; }

	public virtual void PrepareEmitterState()
	{
	}
}
