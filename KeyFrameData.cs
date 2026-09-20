using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct KeyFrameData
{
	public Vector3 Position;

	public Vector3 Rotation;

	public Vector3 Scale;

	public static KeyFrameData Lerp(KeyFrameData state0, KeyFrameData state1, float ratio)
	{
		return new KeyFrameData
		{
			Position = Vector3.Lerp(state0.Position, state1.Position, ratio),
			Rotation = Vector3.Lerp(state0.Rotation, state1.Rotation, ratio),
			Scale = Vector3.Lerp(state0.Scale, state1.Scale, ratio)
		};
	}

	public static KeyFrameData Lerp(List<KeyFrameData> states, float ratio)
	{
		if (states == null || states.Count < 2)
		{
			throw new ArgumentException("States list must contain at least two elements.");
		}
		int num = (int)Math.Floor(ratio * (float)(states.Count - 1));
		int num2 = num + 1;
		if (num2 >= states.Count)
		{
			return states[num];
		}
		float ratio2 = ratio * (float)(states.Count - 1) - (float)num;
		return Lerp(states[num], states[num2], ratio2);
	}
}
