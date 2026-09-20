using Assets.Scripts.Inventory;

namespace Assets.Scripts.Objects.Items;

public class FoodContainer : ItemContainer
{
	private bool _occludeAudio;

	public float DecayScale = 0.5f;

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		if (!IsOpen)
		{
			DelayedActionInstance result = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = ActionStrings.Open
			};
			if (!doAction)
			{
				return result;
			}
			OnServer.Interact(base.InteractOpen, 1);
			return result;
		}
		foreach (Slot slot in Slots)
		{
			if (slot.Contains<Food>(out var occupant))
			{
				return occupant.OnUseSecondary(doAction, actionCompletedRatio);
			}
		}
		return base.OnUseSecondary(doAction, actionCompletedRatio);
	}
}
