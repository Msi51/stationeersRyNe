using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;

namespace Assets.Scripts.Objects.Items;

public class EyewearBase : Item
{
	public override bool IsBurnable
	{
		get
		{
			if (base.ParentSlot?.Parent is Human human)
			{
				GasMask gasMask = human.HelmetSlot.Get<GasMask>();
				if (gasMask != null && !gasMask.IsOpen)
				{
					return false;
				}
			}
			return base.IsBurnable;
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.PersonalEyeWear);
	}
}
