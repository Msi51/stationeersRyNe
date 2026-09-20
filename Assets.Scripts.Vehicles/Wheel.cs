using System;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Vehicles;

[Serializable]
public class Wheel
{
	[ReadOnly]
	public string DisplayName;

	[ReadOnly]
	public WheelCollider WheelCollider;

	public bool IsMotorized;

	public WheelSteeringMode Mode;

	public Transform WheelTransform;

	public Transform WheelTransformParent;

	public Renderer WheelRenderer;

	public int RumbleAudioHash;

	public int SkidAudioHash;

	public WheeledBase Parent;

	public const float MAX_TURN_ANGLE = 35f;

	public Vector3 WheelStartLocalPosition;

	private float _rpmSmoothTime = 0.1f;

	private float _slipSmoothTime = 0.01f;

	public float WheelRpm;

	public const float MaxRpm = 100f;

	private float _slip;

	private const float SlipThreshold = 0.1f;

	private const float AudioSilentThreshold = 0.0005f;

	public WheelHit GroundHit;

	private static string _mainTexString = "_MainTex";

	private Material _material;

	private float _lastOffset;

	public float UvOffsetScale = 1f;

	private float _rumbleVolume;

	private float _skidVolume;

	private float _audioLerpSpeed = 10f;

	public void Initialize(WheeledBase parent)
	{
		Parent = parent;
		DisplayName = (WheelCollider ? WheelCollider.name : "Unknown");
		RumbleAudioHash = Animator.StringToHash(DisplayName + "Rumble");
		SkidAudioHash = Animator.StringToHash(DisplayName + "Skid");
		WheelStartLocalPosition = WheelCollider.gameObject.transform.position - WheelTransformParent.position;
	}

	public void Apply(float motors, float braking, float steeringAngle)
	{
		if (IsMotorized && WheelCollider.isGrounded)
		{
			WheelCollider.motorTorque = motors;
			WheelCollider.brakeTorque = braking;
		}
		switch (Mode)
		{
		case WheelSteeringMode.Normal:
			WheelCollider.steerAngle = steeringAngle;
			break;
		case WheelSteeringMode.Inverted:
			WheelCollider.steerAngle = 0f - steeringAngle;
			break;
		}
	}

	public void AnimateAuthority()
	{
		if (!Parent.HasAuthority)
		{
			throw new Exception(Parent.DisplayName + " does not have authority over this wheel");
		}
		WheelCollider.GetWorldPose(out var pos, out var quat);
		if ((bool)WheelTransform)
		{
			WheelTransform.position = pos;
			Vector3 localPosition = WheelTransform.localPosition;
			localPosition.x -= WheelCollider.center.x;
			WheelTransform.localPosition = localPosition;
			WheelTransform.rotation = quat;
		}
		if ((bool)WheelRenderer)
		{
			if (!_material)
			{
				_material = WheelRenderer.material;
			}
			if ((bool)_material)
			{
				float num = WheelCollider.rpm / 60f * UvOffsetScale;
				_lastOffset = (RocketMath.Approximately(num, 0f) ? 0f : (_lastOffset + num * Time.fixedDeltaTime));
				_material.SetTextureOffset(_mainTexString, new Vector2(0f, _lastOffset));
			}
		}
	}

	public void Animate(Vector3 wheelRotation)
	{
		if ((bool)WheelTransform)
		{
			WheelTransform.localEulerAngles = wheelRotation;
		}
		if ((bool)WheelRenderer)
		{
			if (!_material)
			{
				_material = WheelRenderer.material;
			}
			if ((bool)_material)
			{
				float num = WheelRpm / 60f * UvOffsetScale;
				_lastOffset = (RocketMath.Approximately(num, 0f) ? 0f : (_lastOffset + num * Time.fixedDeltaTime));
				_material.SetTextureOffset(_mainTexString, new Vector2(0f, _lastOffset));
			}
		}
	}

	public void WheelAudio(GameAudioEvent rumbleAudio, GameAudioEvent skidAudio)
	{
		if (!WheelCollider.GetGroundHit(out GroundHit))
		{
			rumbleAudio?.Stop();
			skidAudio?.Stop();
			return;
		}
		float currentVelocity = 0f;
		_slip = Mathf.SmoothDamp(_slip, Mathf.Abs(GroundHit.sidewaysSlip), ref currentVelocity, _slipSmoothTime);
		float num = ((Mathf.Abs(WheelCollider.rpm) < 100f) ? Mathf.Abs(WheelCollider.rpm) : 100f);
		WheelRpm = Mathf.SmoothDamp(WheelRpm, num, ref currentVelocity, _rpmSmoothTime);
		if (rumbleAudio != null)
		{
			_rumbleVolume = Mathf.Lerp(_rumbleVolume, Mathf.Clamp01(num / 100f), Time.deltaTime * _audioLerpSpeed);
			float pitchMultiplier = Mathf.Lerp(0.5f, 1f, _rumbleVolume);
			if (WheelRpm > 1f && !rumbleAudio.AudioSource.isPlaying)
			{
				rumbleAudio.Trigger(_rumbleVolume, pitchMultiplier);
			}
			else if (WheelRpm < 1f && rumbleAudio.AudioSource.isPlaying)
			{
				rumbleAudio.Stop();
			}
			rumbleAudio.SetVolumeMultiplier(_rumbleVolume);
			rumbleAudio.SetPitchMultiplier(pitchMultiplier);
		}
		if (skidAudio != null)
		{
			_skidVolume = Mathf.Lerp(_skidVolume, Mathf.Clamp01(_slip / 1f - 0.1f), Time.deltaTime * _audioLerpSpeed);
			if (_slip > 0.1f && !skidAudio.AudioSource.isPlaying)
			{
				skidAudio.Trigger(_skidVolume);
			}
			else if (_skidVolume < 0.0005f && skidAudio.AudioSource.isPlaying)
			{
				skidAudio.Stop();
			}
			skidAudio.SetVolumeMultiplier(_skidVolume);
		}
	}
}
