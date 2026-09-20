using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Sound;

namespace Assets.Scripts.Serialization;

[Serializable]
[XmlRoot("AudioData")]
public class AudioData
{
	[XmlArray]
	[XmlArrayItem("Clip")]
	public List<GameAudioClipsData> AudioClipsData = new List<GameAudioClipsData>();

	[XmlArray]
	[XmlArrayItem("Channel")]
	public List<ChannelData> ChannelData = new List<ChannelData>();
}
