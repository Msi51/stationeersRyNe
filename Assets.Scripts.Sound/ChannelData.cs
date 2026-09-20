using System;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Sound;

[Serializable]
public class ChannelData
{
	public static readonly ChannelData Default = new ChannelData();

	[XmlElement]
	public string Name;

	[XmlElement]
	public int Index = -1;

	[XmlElement]
	public int Priority = 128;

	[XmlElement]
	public float Volume = 0.5f;

	[XmlElement]
	public float Pitch = 1f;

	[XmlElement]
	public SpatialReference SpatialReference = SpatialReference.ThreeD;

	[XmlElement]
	public ReverbType Reverb = ReverbType.Spatial;

	[XmlElement]
	public bool BypassReverbZones;

	[XmlElement("SpatialSettings")]
	public SpatialSoundData SpatialSoundData = new SpatialSoundData();

	[XmlElement]
	public string LocalMixerGroup = "LocalPlayer";

	[XmlElement]
	public string ExternalMixerGroup = "External";

	[XmlElement]
	public string VacuumMixerGroup = "Vacuum";

	[XmlElement]
	public string OccludedMixerGroup = "Occluded";

	[XmlElement]
	public OcclusionType OcclusionType;

	[FormerlySerializedAs("_spatialCurve")]
	[XmlIgnore]
	public AnimationCurve SpatialCurve;

	[XmlIgnore]
	public static readonly AnimationCurve SmallReverbCurve = new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(1f, 1f));

	[XmlIgnore]
	public static readonly AnimationCurve MediumReverbCurve = new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.4f, 1f), new Keyframe(1f, 1.01f));

	[XmlIgnore]
	public static readonly AnimationCurve LargeReverbCurve = new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.02f));

	[XmlIgnore]
	public static readonly AnimationCurve QuietCurve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(1f, 0.9f));

	public AnimationCurve GetSpatialCurve
	{
		get
		{
			switch (SpatialReference)
			{
			case SpatialReference.Small:
				SpatialCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 0f), new Keyframe(0.5f, 1f), new Keyframe(SpatialSoundData.MaxDistance, 1f));
				break;
			case SpatialReference.Large:
				SpatialCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1.5f, 0f), new Keyframe(3f, 1f), new Keyframe(SpatialSoundData.MaxDistance, 1f));
				break;
			case SpatialReference.Medium:
				SpatialCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.6f, 0f), new Keyframe(1f, 1f), new Keyframe(SpatialSoundData.MaxDistance, 1f));
				break;
			case SpatialReference.MediumSmall:
				SpatialCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 0f), new Keyframe(0.75f, 1f), new Keyframe(SpatialSoundData.MaxDistance, 1f));
				break;
			case SpatialReference.MediumLarge:
				SpatialCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.85f, 0f), new Keyframe(1.2f, 1f), new Keyframe(SpatialSoundData.MaxDistance, 1f));
				break;
			case SpatialReference.ThreeD:
				return null;
			case SpatialReference.TwoD:
				return null;
			default:
				return null;
			}
			return SpatialCurve;
		}
	}
}
