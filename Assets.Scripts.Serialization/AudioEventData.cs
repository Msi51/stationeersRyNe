using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Sound;

namespace Assets.Scripts.Serialization;

[XmlRoot("AudioEventData")]
public class AudioEventData
{
	[XmlArray]
	[XmlArrayItem("Channel")]
	public List<ChannelData> AudioChannels = new List<ChannelData>();

	[XmlArray]
	[XmlArrayItem("Event")]
	public List<GameAudioEvent> AudioEvents = new List<GameAudioEvent>();
}
