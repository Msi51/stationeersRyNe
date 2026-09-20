using System;
using UnityEngine;

namespace Objects.Items;

public class RocketDeepScanningHead : RocketScanningHead
{
	public static (int x, int y) GetMapPos(float t, int mapSize, float squeeze, float startOffset = 0f)
	{
		float num = (float)mapSize / 2f;
		float num2 = MathF.PI / num * squeeze;
		float num3 = num;
		float num4 = UnityEngine.Random.Range(0.001f, 0.002f);
		float num5 = t * num4 + startOffset;
		float num6 = num * Mathf.Sin(t * num2 + num5) + num3;
		return (x: (int)t % mapSize, y: (int)num6);
	}
}
