using UnityEngine;

namespace Assets.Scripts.Atmospherics;

public class LiquidParticle : GameBase
{
	[SerializeField]
	private Rigidbody _rigidbody;

	private float _createdTime;

	private float _maxLifeTime = 3f;

	private float _minLifeTime = 1f;

	private Atmosphere _origin;

	private GasMixture _gasMix;

	public bool IsActive { get; set; }

	public float VelocitySqrMagnitude => _rigidbody.velocity.sqrMagnitude;

	public bool HasExpired(float currentTime)
	{
		return currentTime > _createdTime + _maxLifeTime;
	}

	public bool HasRipened(float currentTime)
	{
		return currentTime > _createdTime + _minLifeTime;
	}

	public void Activate()
	{
		IsActive = true;
		_rigidbody.isKinematic = false;
		_rigidbody.detectCollisions = true;
		GameObject.SetActive(value: true);
	}

	public void Deactivate()
	{
		IsActive = false;
		_origin = null;
		_gasMix = default(GasMixture);
		_rigidbody.isKinematic = true;
		_rigidbody.detectCollisions = false;
		GameObject.SetActive(value: false);
	}

	public void Initialize(Vector3 position, Vector3 velocity, GasMixture gasMix, float creationTime, Atmosphere origin)
	{
		Transform.position = position;
		if (!_rigidbody.isKinematic)
		{
			_rigidbody.velocity = velocity;
			_rigidbody.angularVelocity = Vector3.zero;
		}
		_gasMix = gasMix;
		_origin = origin;
		_createdTime = creationTime;
		Activate();
	}

	public GasMixture GetGasMix()
	{
		return _gasMix;
	}

	public void ReturnToOrigin()
	{
		if (_origin != null)
		{
			AtmosphericEventInstance.CreateAdd(_origin, GetGasMix());
		}
	}
}
