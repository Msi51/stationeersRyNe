using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Electrical;
using UnityEngine;

namespace Assets.Scripts.Sound;

[Serializable]
public class GameAudioClipsData
{
	[XmlElement]
	[ReadOnly]
	public string Name;

	[XmlElement]
	[ReadOnly]
	public string ChannelName;

	[XmlElement]
	[ReadOnly]
	public bool Looping;

	[XmlElement]
	[ReadOnly]
	public bool LoopingOneShots;

	[XmlElement]
	[ReadOnly]
	public bool RandomStartTime;

	[XmlElement]
	[ReadOnly]
	public bool RandomPitch;

	[XmlElement]
	[ReadOnly]
	public float RandomPitchRangeHigh = 1.2f;

	[XmlElement]
	[ReadOnly]
	public float RandomPitchRangeLow = 0.8f;

	[XmlElement]
	[ReadOnly]
	public float Delay;

	[XmlElement]
	[ReadOnly]
	public bool FadeIn;

	[XmlElement]
	[ReadOnly]
	public bool FadeOut;

	[XmlElement]
	[ReadOnly]
	public bool FadeDown;

	[XmlElement]
	[ReadOnly]
	public float FadeDownTime = 5f;

	[XmlElement]
	[ReadOnly]
	public float FadeDownTarget = 0.5f;

	[XmlElement]
	[ReadOnly]
	public bool FadePitch;

	[XmlElement]
	[ReadOnly]
	public float FadeInTime = 0.2f;

	[XmlElement]
	[ReadOnly]
	public float FadeOutTime = 0.2f;

	[NonSerialized]
	[ReadOnly]
	[XmlArray]
	[XmlArrayItem("Path")]
	public List<string> ClipNames = new List<string>();

	[NonSerialized]
	[ReadOnly]
	[XmlArray]
	[XmlArrayItem("SettingName")]
	public List<string> ConcurrencySettings = new List<string>();

	[XmlIgnore]
	[ReadOnly]
	public List<AudioClip> Clips = new List<AudioClip>();

	[XmlIgnore]
	[ReadOnly]
	public List<AudioClipsConcurrency> Concurrencies = new List<AudioClipsConcurrency>();

	[XmlIgnore]
	[ReadOnly]
	public List<int> ConcurrencyIds;

	[XmlIgnore]
	[ReadOnly]
	public int NameHash;

	[XmlIgnore]
	[ReadOnly]
	public SoundAlert SoundAlert;

	public bool IsLooping
	{
		get
		{
			if (!Looping)
			{
				return LoopingOneShots;
			}
			return true;
		}
	}
}
