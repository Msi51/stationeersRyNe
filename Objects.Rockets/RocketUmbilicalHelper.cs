using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Rockets;

public static class RocketUmbilicalHelper
{
	public static readonly string[] ModeStrings = new string[3]
	{
		GameStrings.Left.DisplayString,
		GameStrings.Center.DisplayString,
		GameStrings.Right.DisplayString
	};

	public static void FindAndSetOtherUmbilicalLargeGrid(IUmbilical self)
	{
		Thing asThing = self.AsThing;
		Grid3 value = asThing.WorldGrid.Value;
		Grid3 grid = new Grid3(asThing.Forward * 2f);
		IUmbilical umbilical = null;
		int partnerDistance = 0;
		for (int i = 1; i < 10; i++)
		{
			WorldGrid worldGrid = new WorldGrid(value + grid * i);
			if (asThing.GridController.Get<Structure>(worldGrid) is IUmbilical umbilical2)
			{
				umbilical = umbilical2;
				partnerDistance = i - 1;
				break;
			}
		}
		if (umbilical != null)
		{
			self.SetPartner(umbilical);
			umbilical.SetPartner(self);
			self.PartnerDistance = partnerDistance;
			umbilical.PartnerDistance = partnerDistance;
		}
		else
		{
			self.PartnerDistance = 0;
			self.SetPartner(null);
		}
	}

	public static void FindAndSetOtherUmbilical(IUmbilical self)
	{
		Thing asThing = self.AsThing;
		Vector3 firstPartnerSearchPosition = self.FirstPartnerSearchPosition;
		firstPartnerSearchPosition = GridController.World.WorldToLocalGrid(firstPartnerSearchPosition, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset).ToVector3();
		Vector3 vector = asThing.Forward * SmallGrid.SmallGridSize;
		Vector3 vector2 = asThing.Transform.right * SmallGrid.SmallGridSize;
		IUmbilical umbilical = null;
		int num = 9999;
		int num2 = ((self.UmbilicalType == UmbilicalType.Umbilical) ? 2 : 0);
		for (int i = 0; i <= num2; i++)
		{
			for (int j = 0; j <= 13 && num >= j; j++)
			{
				Vector3 worldPosition = firstPartnerSearchPosition + vector * j + vector2 * i;
				SmallCell smallCell = GridController.World.GetSmallCell(worldPosition);
				if (smallCell == null)
				{
					continue;
				}
				if (smallCell.Device != null && !(smallCell?.Device is IUmbilical))
				{
					break;
				}
				IUmbilical umbilical2 = smallCell.Device as IUmbilical;
				if (umbilical2 != self && umbilical2 != null)
				{
					if (!self.IsCompatibleWith(umbilical2) || Vector3.Dot(-umbilical2.AsThing.Forward, self.AsThing.Forward) < 0.9f)
					{
						break;
					}
					if (j < num)
					{
						umbilical = umbilical2;
						num = j;
					}
				}
			}
		}
		if (umbilical != null)
		{
			self.SetPartner(umbilical);
			umbilical.SetPartner(self);
			int partnerDistance = (umbilical.PartnerDistance = Mathf.Max(num + 1, 0));
			self.PartnerDistance = partnerDistance;
		}
		else
		{
			self.PartnerDistance = 0;
			self.SetPartner(null);
		}
	}

	public static int FindModePosition(IUmbilical male, IUmbilical female)
	{
		int result = 1;
		if (female == null)
		{
			return result;
		}
		Vector3 firstPartnerSearchPosition = female.FirstPartnerSearchPosition;
		Grid3 grid = GridController.World.WorldToLocalGrid(firstPartnerSearchPosition, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
		for (int i = -1; i <= 1; i++)
		{
			Vector3 vector = male.AsThing.Forward * SmallGrid.SmallGridSize + male.AsThing.Transform.right * SmallGrid.SmallGridSize * i;
			Grid3 grid2 = (male.AsThing.Position + vector).ToGrid(SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
			if (grid.x == grid2.x || grid.z == grid2.z)
			{
				result = i + 1;
			}
		}
		return result;
	}
}
