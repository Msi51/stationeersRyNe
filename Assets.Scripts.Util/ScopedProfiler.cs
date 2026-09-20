using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Assets.Scripts.Util;

public class ScopedProfiler : IDisposable
{
	private static readonly Dictionary<int, CustomSampler> Samplers = new Dictionary<int, CustomSampler>();

	private CustomSampler sampler;

	public ScopedProfiler(string profilerName)
	{
		int num = Animator.StringToHash(profilerName);
		Samplers.TryGetValue(num, out sampler);
		if (sampler == null)
		{
			sampler = CustomSampler.Create(profilerName);
			while (!Samplers.TryAdd(num, sampler))
			{
				num *= 2;
			}
		}
	}

	public void Dispose()
	{
	}
}
