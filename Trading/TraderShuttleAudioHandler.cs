using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Sound;
using UnityEngine;

namespace Trading;

public class TraderShuttleAudioHandler : MonoBehaviour
{
	[SerializeField]
	private Transform mainEngineTransform;

	[SerializeField]
	private Transform engineLTransform;

	[SerializeField]
	private Transform engineRTransform;

	[Space(15f)]
	[SerializeField]
	private Transform windShield;

	[SerializeField]
	private Transform rearAssembly;

	[SerializeField]
	private Transform lift;

	[SerializeField]
	private Transform ramp;

	[SerializeField]
	private Transform door;

	[SerializeField]
	private Transform console;

	[SerializeField]
	private Transform ladder;

	private List<PooledAudioSource> _loopingSounds = new List<PooledAudioSource>(4);

	public TraderShuttle TraderShuttle { get; set; }

	public void PlaySound(AnimationEvent animEvent)
	{
		if (!GameManager.IsBatchMode)
		{
			PlaySound(Animator.StringToHash(animEvent.stringParameter), TraderShuttle.ShuttleCenter, animEvent.floatParameter);
		}
	}

	public void PlaySoundAtPosition(AnimationEvent animEvent)
	{
		if (!GameManager.IsBatchMode)
		{
			PooledAudioSource pooledAudioSource = Singleton<AudioManager>.Instance.PlayAudioClipsData(Animator.StringToHash(animEvent.stringParameter), TraderShuttle.ShuttleCenter.position);
			if (pooledAudioSource.GameAudioSource.CurrentClips.IsLooping)
			{
				_loopingSounds.Add(pooledAudioSource);
			}
		}
	}

	public void PlayWindshieldSound(AnimationEvent animEvent)
	{
		if (!GameManager.IsBatchMode)
		{
			PlaySound(Animator.StringToHash(animEvent.stringParameter), windShield);
		}
	}

	public void PlayRearAssemblySound(AnimationEvent animEvent)
	{
		PlaySound(Animator.StringToHash(animEvent.stringParameter), rearAssembly);
	}

	public void PlayLadderSound(AnimationEvent animEvent)
	{
		PlaySound(Animator.StringToHash(animEvent.stringParameter), ladder);
	}

	public void PlayDoorSound(AnimationEvent animEvent)
	{
		PlaySound(Animator.StringToHash(animEvent.stringParameter), door);
	}

	public void PlayConsoleSound(AnimationEvent animEvent)
	{
		PlaySound(Animator.StringToHash(animEvent.stringParameter), console);
	}

	public void PlayLiftSound(AnimationEvent animEvent)
	{
		PlaySound(Animator.StringToHash(animEvent.stringParameter), lift);
	}

	public void PlayRampSound(AnimationEvent animEvent)
	{
		PlaySound(Animator.StringToHash(animEvent.stringParameter), ramp);
	}

	private void PlaySound(int nameHash, Transform localTransform, float volumeMultiplier = 1f)
	{
		if (!GameManager.IsBatchMode && nameHash != 0)
		{
			if (localTransform == null)
			{
				localTransform = base.gameObject.transform;
			}
			PooledAudioSource pooledAudioSource = Singleton<AudioManager>.Instance.PlayAudioClipsData(TraderShuttle, nameHash, localTransform, null, volumeMultiplier);
			if (pooledAudioSource.GameAudioSource.CurrentClips.IsLooping)
			{
				_loopingSounds.Add(pooledAudioSource);
			}
		}
	}

	private void StopSound(int nameHash)
	{
		for (int num = _loopingSounds.Count - 1; num >= 0; num--)
		{
			PooledAudioSource pooledAudioSource = _loopingSounds[num];
			if (pooledAudioSource == null)
			{
				_loopingSounds.RemoveAt(num);
			}
			else if ((object)pooledAudioSource != null && pooledAudioSource.GameAudioSource?.CurrentClips?.NameHash == nameHash)
			{
				pooledAudioSource.Stop();
				_loopingSounds.RemoveAt(num);
				break;
			}
		}
	}

	public void StopSound(AnimationEvent animEvent)
	{
		StopSound(Animator.StringToHash(animEvent.stringParameter));
	}

	public void StopAllSound()
	{
		foreach (PooledAudioSource loopingSound in _loopingSounds)
		{
			loopingSound.Stop();
		}
		_loopingSounds.Clear();
	}

	public void EnterAtmosphereSounds()
	{
		PlaySound(ShuttleAudioHelper.MainEngineSound(TraderShuttle.ShuttleType), mainEngineTransform);
		PlaySound(ShuttleAudioHelper.MainEngineLSound(TraderShuttle.ShuttleType), engineLTransform);
		PlaySound(ShuttleAudioHelper.MainEngineRSound(TraderShuttle.ShuttleType), engineRTransform);
		PlaySound(ShuttleAudioHelper.MainEngineDistantSound(TraderShuttle.ShuttleType), mainEngineTransform);
		PlaySound(Defines.Sounds.ShuttleSmallSonicBoom, TraderShuttle.ShuttleCenter);
	}

	public void TouchDownSounds()
	{
		StopSound(ShuttleAudioHelper.MainEngineSound(TraderShuttle.ShuttleType));
		StopSound(ShuttleAudioHelper.MainEngineLSound(TraderShuttle.ShuttleType));
		StopSound(ShuttleAudioHelper.MainEngineRSound(TraderShuttle.ShuttleType));
		StopSound(ShuttleAudioHelper.MainEngineDistantSound(TraderShuttle.ShuttleType));
		PlaySound(ShuttleAudioHelper.MainEngineEndSound(TraderShuttle.ShuttleType), mainEngineTransform);
	}

	public void LiftUpSounds()
	{
		PlaySound(ShuttleAudioHelper.MainEngineStartSound(TraderShuttle.ShuttleType), mainEngineTransform);
		PlaySound(ShuttleAudioHelper.MainEngineDepartSound(TraderShuttle.ShuttleType), mainEngineTransform);
		PlaySound(ShuttleAudioHelper.MainEngineLDepartSound(TraderShuttle.ShuttleType), engineLTransform);
		PlaySound(ShuttleAudioHelper.MainEngineRDepartSound(TraderShuttle.ShuttleType), engineRTransform);
		PlaySound(ShuttleAudioHelper.MainEngineDistantDepartSound(TraderShuttle.ShuttleType), mainEngineTransform);
	}
}
