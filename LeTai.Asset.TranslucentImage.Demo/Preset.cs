using System;
using UnityEngine;

namespace LeTai.Asset.TranslucentImage.Demo;

[Serializable]
public struct Preset
{
	public RuntimePlatform platform;

	public float size;

	public int iteration;

	public int downsample;

	public float maxUpdateRate;
}
