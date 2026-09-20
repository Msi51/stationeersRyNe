using UnityEngine;

public class ExtinguisherParticle : MonoBehaviour
{
	public ParticleSystem extinguisherParticle;

	[SerializeField]
	private Transform emitterTransform;

	public void EmitExtinguisherParticles(Vector3 position, Quaternion rotation, int amount)
	{
		emitterTransform.position = position;
		emitterTransform.rotation = rotation;
		extinguisherParticle.Emit(amount);
	}
}
