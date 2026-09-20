using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Objects.Electrical;
using Trading;

namespace Assets.Scripts.Objects;

public class WearableItem : ItemRenamable, IWearable, IReferencable, IEvaluable, ISpatial, IPhysical, IProfile, IDensePoolable
{
	public Human ParentHuman => base.ParentSlot?.Occupant?.RootParent as Human;

	public virtual bool TryCollect(Item item)
	{
		return false;
	}

	public virtual void SetWearableVisibility(bool clothingOn)
	{
	}

	public virtual void RefreshVisibility()
	{
	}

	public override CanEnterResult CanEnter(Slot destinationSlot)
	{
		if (destinationSlot.Parent is SuitBase suitBase && destinationSlot == suitBase.BackSlot && !suitBase.AllowedInBack(this))
		{
			return CanEnterResult.Fail(GameStrings.SuitDoesNotSupportThisItem);
		}
		return base.CanEnter(destinationSlot);
	}
}
