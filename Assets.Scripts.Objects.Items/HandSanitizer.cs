using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Entities;
using Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class HandSanitizer : Consumable
{
	[SerializeField]
	private float _hyginePerUse;

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletionRatio = 1f)
	{
		if (RootParent == this)
		{
			return base.OnUseSecondary(doAction, actionCompletionRatio);
		}
		if (!(RootParent is Human human))
		{
			return base.OnUseSecondary(doAction, actionCompletionRatio);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance();
		delayedActionInstance.Duration = 1f;
		delayedActionInstance.ActionMessage = GameStrings.SanitizerActionMessage.DisplayString;
		if (!doAction)
		{
			return delayedActionInstance;
		}
		float num = Mathf.Min(UseAmount * actionCompletionRatio, base.Quantity);
		float num2 = _hyginePerUse * num;
		human.Hygiene = Mathf.Min(human.Hygiene + num2, 1f);
		OnUseItem(num, human);
		return delayedActionInstance;
	}
}
