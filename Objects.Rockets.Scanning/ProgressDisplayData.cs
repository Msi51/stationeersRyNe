using System;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Rockets.Scanning;

public readonly struct ProgressDisplayData : IEquatable<ProgressDisplayData>
{
	public readonly IRocketActionProgressable IRocketActionProgressable;

	public string Title => IRocketActionProgressable?.DisplayName ?? string.Empty;

	public float Ratio => IRocketActionProgressable?.GetActionProgress ?? 0f;

	public string ActionInfoText => IRocketActionProgressable?.GetActionInfoText() ?? string.Empty;

	public string RatioString()
	{
		return StringManager.Get(Mathf.RoundToInt(Ratio * 100f)) + "%";
	}

	public ProgressDisplayData(IRocketActionProgressable iRocketActionProgressable)
	{
		IRocketActionProgressable = iRocketActionProgressable;
	}

	public bool Equals(ProgressDisplayData other)
	{
		if (IRocketActionProgressable.ReferenceId == other.IRocketActionProgressable.ReferenceId)
		{
			return Mathf.Approximately(Ratio, other.Ratio);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ProgressDisplayData other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(IRocketActionProgressable.ReferenceId, Ratio);
	}

	public bool IsValid()
	{
		return IRocketActionProgressable != null;
	}
}
