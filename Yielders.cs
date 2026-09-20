using System.Collections.Generic;
using UnityEngine;

public static class Yielders
{
	private class FloatComparer : IEqualityComparer<float>
	{
		bool IEqualityComparer<float>.Equals(float x, float y)
		{
			return x == y;
		}

		int IEqualityComparer<float>.GetHashCode(float obj)
		{
			return obj.GetHashCode();
		}
	}

	private static readonly Dictionary<float, WaitForSeconds> WaitSecondsCache = new Dictionary<float, WaitForSeconds>(100, new FloatComparer());

	private static readonly Dictionary<float, WaitForSecondsRealtime> WaitRealSecondsCache = new Dictionary<float, WaitForSecondsRealtime>(100, new FloatComparer());

	private static readonly WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();

	private static readonly WaitForFixedUpdate _fixedUpdate = new WaitForFixedUpdate();

	public static WaitForEndOfFrame EndOfFrame => _endOfFrame;

	public static WaitForFixedUpdate FixedUpdate => _fixedUpdate;

	public static WaitForSeconds WaitForSeconds(float seconds)
	{
		if (!WaitSecondsCache.TryGetValue(seconds, out var value))
		{
			WaitSecondsCache.Add(seconds, value = new WaitForSeconds(seconds));
		}
		return value;
	}

	public static WaitForSecondsRealtime WaitForSecondsRealtime(float seconds)
	{
		if (!WaitRealSecondsCache.TryGetValue(seconds, out var value))
		{
			WaitRealSecondsCache.Add(seconds, value = new WaitForSecondsRealtime(seconds));
		}
		return value;
	}
}
