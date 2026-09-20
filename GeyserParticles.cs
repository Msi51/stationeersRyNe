using UnityEngine;

public class GeyserParticles : MonoBehaviour
{
	[SerializeField]
	private ParticleSystem _particleSystem;

	[SerializeField]
	private Transform _transform;

	public void Emit(Vector3 position, Quaternion rotation, int amount)
	{
		_transform.position = position;
		_transform.rotation = rotation;
		_particleSystem.Emit(amount);
	}
}
