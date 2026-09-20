using System.Text;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public interface ISprayer : IUsedAmount, IUsed
{
	static int SprayPaintSoundHash;

	int ConstructingSoundHash { get; }

	Material GetPaintMaterial();

	float TimeToUse();

	StringBuilder GetExtendedText();

	static Thing.DelayedActionInstance DoSpray(Thing thing, ISprayer sprayer, bool doAction)
	{
		Thing.DelayedActionInstance delayedActionInstance = new Thing.DelayedActionInstance
		{
			Duration = sprayer.TimeToUse(),
			ActionMessage = ActionStrings.Paint
		};
		if (sprayer.GetPaintMaterial() == null)
		{
			return delayedActionInstance.Fail(GameStrings.NotEnoughSprayPaint, sprayer.ToTooltip());
		}
		if ((thing.CustomColor.Normal != null && thing.CustomColor.Normal != sprayer.GetPaintMaterial()) || (thing.CustomColor.Normal == null && thing.PaintableMaterial != sprayer.GetPaintMaterial()))
		{
			ColorSwatch colorSwatch = GameManager.GetColorSwatch(sprayer.GetPaintMaterial());
			if (sprayer is Tool { OnOff: false } tool)
			{
				return delayedActionInstance.Fail(GameStrings.CannotUseWhenNotOn, tool.ToTooltip());
			}
			if (sprayer.IsEmpty)
			{
				return delayedActionInstance.Fail(GameStrings.NotEnoughSprayPaint, sprayer.ToTooltip());
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(GameStrings.ThingWillBeSprayed.AsString(thing.ToTooltip(), colorSwatch.ToTooltip()));
			if (!(sprayer is Consumable consumable))
			{
				if (sprayer is SprayGun { SprayCan: not null } sprayGun)
				{
					stringBuilder.AppendLine(GameStrings.ItemInSlotValue.AsString(sprayGun.SprayCan.ToTooltip(), sprayGun.SprayCan.GetQuantityText()));
				}
			}
			else
			{
				stringBuilder.AppendLine(GameStrings.ItemInSlotValue.AsString(consumable.ToTooltip(), consumable.GetQuantityText()));
			}
			delayedActionInstance.ExtendedMessage = stringBuilder.ToString();
			if (!doAction)
			{
				return delayedActionInstance;
			}
			if (GameManager.RunSimulation)
			{
				if (!sprayer.OnUseItem(sprayer.GetUseAmount(), thing))
				{
					return delayedActionInstance;
				}
				if (sprayer.TimeToUse() <= float.Epsilon && sprayer is Thing parent)
				{
					AudioEvent.Create(parent, SprayPaintSoundHash);
				}
				OnServer.SetCustomColor(thing, colorSwatch.Index);
			}
			return delayedActionInstance;
		}
		ColorSwatch colorSwatch2 = GameManager.GetColorSwatch(sprayer.GetPaintMaterial());
		return delayedActionInstance.Fail(GameStrings.CantPaintSameColour, thing.ToTooltip(), colorSwatch2.ToTooltip());
	}

	static ISprayer()
	{
		SprayPaintSoundHash = Animator.StringToHash("SprayPaint");
	}
}
