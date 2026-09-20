using UnityEngine;

namespace Trading;

public class WorldCondition : WorldConditionBase
{
	public override bool Evaluate()
	{
		if (string.IsNullOrEmpty(WorldSetting.Current.Id))
		{
			return true;
		}
		return Animator.StringToHash(Id) == Animator.StringToHash(WorldSetting.Current.Id);
	}

	public override int GetChecksum()
	{
		return ((!string.IsNullOrEmpty(Id)) ? Animator.StringToHash(Id) : 0) * 41;
	}
}
