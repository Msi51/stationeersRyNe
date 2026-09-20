using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts;

[XmlRoot("CinematicKeyframe")]
public class CinematicKeyframe
{
	public string Label = "Keyframe";

	public Vector3 Position;

	public Quaternion Rotation = Quaternion.identity;

	public float Fov = 70f;

	public float Speed = 5f;

	public float HoldDuration;

	public CinematicEase Ease;

	public CinematicKeyframe Clone()
	{
		return new CinematicKeyframe
		{
			Label = Label,
			Position = Position,
			Rotation = Rotation,
			Fov = Fov,
			Speed = Speed,
			HoldDuration = HoldDuration,
			Ease = Ease
		};
	}

	public static float Apply(CinematicEase ease, float t)
	{
		t = Mathf.Clamp01(t);
		switch (ease)
		{
		case CinematicEase.Linear:
			return t;
		case CinematicEase.SmoothStep:
			return t * t * (3f - 2f * t);
		case CinematicEase.EaseIn:
			return t * t;
		case CinematicEase.EaseOut:
			return 1f - (1f - t) * (1f - t);
		case CinematicEase.EaseInOut:
			if (!(t < 0.5f))
			{
				return 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
			}
			return 2f * t * t;
		default:
			return t;
		}
	}
}
