using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace Assets.Scripts.Sound;

[Serializable]
public class GameAudioEvent
{
	[XmlElement]
	[ReadOnly]
	public string Name;

	[XmlIgnore]
	[ReadOnly]
	public int NameHash;

	[XmlElement]
	[ReadOnly]
	public int Channel;

	[XmlIgnore]
	[ReadOnly]
	public Thing Parent;

	[XmlElement]
	[ReadOnly]
	public GameAudioClipsData ClipsData;

	[XmlElement]
	[ReadOnly]
	public bool StopIfInvalid;

	[ReadOnly]
	[XmlArray]
	[XmlArrayItem("Condition")]
	public List<SoundEffectCondition> Conditions = new List<SoundEffectCondition>();

	[ReadOnly]
	[XmlArray]
	[XmlArrayItem("BuildState")]
	public List<int> BuildStateConditions = new List<int>();

	private int _lastFrameCountPlayed = -1;

	public GameAudioSource AudioSource => Parent.GetAudioSource(Channel);

	public bool IsPlaying
	{
		get
		{
			if (AudioSource?.CurrentClips != null && AudioSource.CurrentClips.NameHash == ClipsData.NameHash)
			{
				return AudioSource.isPlaying;
			}
			return false;
		}
	}

	public bool IsValid
	{
		get
		{
			foreach (SoundEffectCondition condition in Conditions)
			{
				if (!Parent.IsState(condition.Type, condition.Value))
				{
					return false;
				}
			}
			if ((bool)Parent.AsStructure && BuildStateConditions.Count > 0)
			{
				foreach (int buildStateCondition in BuildStateConditions)
				{
					if (!Parent.AsStructure.IsBroken && Parent.AsStructure.CurrentBuildStateIndex == buildStateCondition)
					{
						return true;
					}
				}
				return false;
			}
			return true;
		}
	}

	private bool IsDuplicateTrigger()
	{
		int lastFrameCountPlayed = _lastFrameCountPlayed;
		_lastFrameCountPlayed = Time.frameCount;
		if (!AudioSource.isPlaying)
		{
			return false;
		}
		if (AudioSource.CurrentClips != null && AudioSource.CurrentClips.NameHash == ClipsData.NameHash)
		{
			if (AudioSource.CurrentClips.IsLooping)
			{
				return true;
			}
			if (lastFrameCountPlayed == Time.frameCount)
			{
				return true;
			}
		}
		return false;
	}

	public void Trigger(float volumeMultiplier = 1f, float pitchMultiplier = 1f, bool pregame = false)
	{
		if ((bool)Parent && AudioSource != null && (GameManager.GameState == GameState.Running || !ClipsData.IsLooping) && ((!Parent.SuppressSound && XmlSaveLoad.IsReadyToPlayWorldAudio) || ClipsData.IsLooping) && !IsDuplicateTrigger())
		{
			AudioSource.SetMixerGroup(Parent);
			Play(volumeMultiplier, pitchMultiplier, pregame);
		}
	}

	private void Play(float volumeMultiplier, float pitchMultiplier, bool pregame = false)
	{
		if ((bool)Parent)
		{
			AudioSource?.Play(ClipsData, volumeMultiplier, pitchMultiplier, 1f, pregame);
		}
	}

	public void Stop(bool immediate = false)
	{
		if ((bool)Parent)
		{
			AudioSource?.Stop(ClipsData.NameHash, immediate);
		}
	}

	public void UpdatePlayState(bool shouldPlay)
	{
		if (shouldPlay && !IsPlaying)
		{
			Trigger();
		}
		else if (!shouldPlay && IsPlaying)
		{
			Stop();
		}
	}

	public void SetVolumeMultiplier(float volumeMultiplier)
	{
		if ((bool)Parent && AudioSource != null)
		{
			AudioSource.SetVolumeMultiplier(ClipsData.NameHash, volumeMultiplier);
		}
	}

	public void LerpVolumeAndPitch(float targetVolumeMultiplier, float targetPitchMultiplier, float t)
	{
		if ((bool)Parent && AudioSource != null)
		{
			t = Mathf.Clamp01(t);
			AudioSource.SetVolumeMultiplier(ClipsData.NameHash, Mathf.Lerp(AudioSource.VolumeMultiplier, targetVolumeMultiplier, t));
			AudioSource.SetPitchMultiplier(ClipsData.NameHash, Mathf.Lerp(AudioSource.PitchMultiplier, targetPitchMultiplier, t));
		}
	}

	public void SetVolumeAndPitch(float volumeMultiplier, float pitch)
	{
		if ((bool)Parent && AudioSource != null)
		{
			SetVolumeMultiplier(volumeMultiplier);
			SetPitchMultiplier(pitch);
		}
	}

	public void SetSourceVolume(float sourceVolume)
	{
		if ((bool)Parent)
		{
			AudioSource?.SetSourceVolume(ClipsData.NameHash, sourceVolume);
		}
	}

	public void SetPitchMultiplier(float pitch)
	{
		if ((bool)Parent)
		{
			AudioSource?.SetPitchMultiplier(ClipsData.NameHash, pitch);
		}
	}

	public void SetConcurrencyIds()
	{
		if (ClipsData.Concurrencies.Count <= 0 || (ClipsData.ConcurrencyIds != null && ClipsData.ConcurrencyIds.Count == ClipsData.Concurrencies.Count))
		{
			return;
		}
		ClipsData.ConcurrencyIds = new List<int>(ClipsData.Concurrencies.Count);
		foreach (AudioClipsConcurrency concurrency in ClipsData.Concurrencies)
		{
			ClipsData.ConcurrencyIds.Add(concurrency.NameHash);
		}
	}
}
