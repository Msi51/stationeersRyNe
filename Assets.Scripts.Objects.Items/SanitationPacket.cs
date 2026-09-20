using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class SanitationPacket : Consumable, ISanitation
{
	[Header("Sanitation Packet")]
	[Tooltip("Litres of polluted water a full packet holds")]
	[SerializeField]
	private float volume = 10f;

	public VolumeLitres Volume => new VolumeLitres(volume);

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Sanitation);
	}

	public static SanitationResult HumanChecks(Human target, DelayedActionInstance result)
	{
		if ((object)target == null)
		{
			result.Duration = float.MaxValue;
			result.Fail();
			return SanitationResult.Invalid;
		}
		if (!target.CanDefecate())
		{
			result.Duration = float.MaxValue;
			result.ActionMessage = GameStrings.DefecateSuitFail;
			result.Fail(GameStrings.DifficultyCannotDefecateThroughSuit);
			return SanitationResult.WearingSuit;
		}
		if (target.SanitationRatio <= 0.25f)
		{
			result.Duration = float.MaxValue;
			result.ActionMessage = GameStrings.DefecateNeedFail;
			result.Fail(GameStrings.NoSanitationNeed, target.ToTooltip());
			return SanitationResult.NoSanitationNeed;
		}
		return SanitationResult.Allowed;
	}

	public DelayedActionInstance GetUseAction(Human target)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = GameStrings.Use
		};
		if (base.IsStackFull)
		{
			delayedActionInstance.Duration = float.MaxValue;
			delayedActionInstance.ActionMessage = GameStrings.DefecatePacketFull;
			return delayedActionInstance.Fail(GameStrings.SanitationPacketFull, ToTooltip());
		}
		HumanChecks(target, delayedActionInstance);
		return delayedActionInstance;
	}

	public override bool OnUseItem(float useRatio, Thing useOnThing)
	{
		if (!(useOnThing is Human human))
		{
			return false;
		}
		if (!GameManager.RunSimulation)
		{
			return false;
		}
		float num = DrainWaste(human, Mathf.Clamp01(useRatio));
		if (num <= 0f)
		{
			return false;
		}
		AudioEvent.Create(RootParent, Defines.Sounds.SanitationBag);
		base.Quantity += num / volume * MaxQuantity;
		return true;
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		if (RootParent == this)
		{
			return base.OnUseSecondary(doAction, actionCompletedRatio);
		}
		if (!(RootParent is Human human))
		{
			return base.OnUseSecondary(doAction, actionCompletedRatio);
		}
		DelayedActionInstance useAction = GetUseAction(human);
		if (useAction.IsDisabled)
		{
			return useAction.Fail();
		}
		if (!doAction)
		{
			return useAction;
		}
		if (GameManager.RunSimulation)
		{
			OnUseItem(actionCompletedRatio, human);
		}
		return useAction.Succeed();
	}

	private float DrainWaste(Human human, float scale)
	{
		Atmosphere atmosphere = human.OrganStomach?.InternalAtmosphere;
		if (atmosphere == null)
		{
			return 0f;
		}
		Mole pollutedWater = atmosphere.GasMixture.PollutedWater;
		if (pollutedWater.Quantity <= MoleQuantity.Zero)
		{
			return 0f;
		}
		float num = Chemistry.MolarVolumeLiquid(Chemistry.GasType.PollutedWater).ToFloat();
		float a = pollutedWater.Quantity.ToFloat() * num;
		float b = volume * (1f - base.Quantity / MaxQuantity);
		float num2 = Mathf.Min(a, b) * scale;
		if (num2 <= 0f)
		{
			return 0f;
		}
		MoleQuantity moleQuantity = new MoleQuantity(num2 / num);
		MoleEnergy energy = pollutedWater.Energy * (moleQuantity / pollutedWater.Quantity).ToFloat();
		AtmosphericEventInstance.CreateRemove(atmosphere, new GasMixture(new Mole(Chemistry.GasType.PollutedWater, moleQuantity, energy)));
		return num2;
	}
}
