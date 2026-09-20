using System.Collections;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Sound;

public class StaticAudioSource : GameBase
{
	public GameAudioSource GameAudioSource;

	public void Stop(bool ignoreFadeOut = false)
	{
		GameAudioSource?.Stop(ignoreFadeOut);
	}

	public void Stop(int clipsDataHash)
	{
		GameAudioSource?.Stop(clipsDataHash);
	}

	public void Play(int clipNameHash, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		if (ThreadedManager.IsThread)
		{
			UnityMainThreadDispatcher.Instance().Enqueue(WaitThenPlay(clipNameHash, volumeMultiplier, pitchMultiplier));
		}
		else
		{
			if (GameAudioSource == null)
			{
				return;
			}
			GameAudioClipsData clipData = Singleton<AudioManager>.Instance.GetClipData(clipNameHash);
			if (clipData != null)
			{
				ChannelData channelData = Singleton<AudioManager>.Instance.GetChannelData(Animator.StringToHash(clipData.ChannelName));
				if (channelData != null)
				{
					GameAudioSource.DeserializeRuntime(channelData, this);
					GameAudioSource.SetMixerGroupToDefault();
					GameAudioSource.Play(clipData, volumeMultiplier, pitchMultiplier);
				}
			}
		}
	}

	private IEnumerator WaitThenPlay(int clipNameHash, float volumeMultiplier, float pitchMultiplier)
	{
		Play(clipNameHash, volumeMultiplier, pitchMultiplier);
		yield return null;
	}
}
