using System.Text;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Structures;

public class StructureToilet : WaterDevice, ISanitation
{
	private enum UseResult
	{
		Invalid,
		Allowed,
		NotInPosition,
		CannotWearingSuit,
		NotNeeded
	}

	public static readonly MoleQuantity MolesUsed = new MoleQuantity(50.0);

	[SerializeField]
	private Transform soundPosition;

	public const float MINIMUM_USE_RATIO = 0.25f;

	public override Transform SoundPosition => soundPosition;

	protected override bool IsOperable
	{
		get
		{
			if (base.IsStructureCompleted && base.IsInputValid && base.IsOutputValid && !base.WaterTooCold && !base.WaterTooHot && !base.WaterPolluted && base.IsMinimumWorldPressure && !base.OutputFull)
			{
				return HasEnoughWater(MolesUsed);
			}
			return false;
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Sanitation);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return false;
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return false;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = GameStrings.Use,
			ActionSoundHash = Item.WaterBottleFillHash
		};
		if (interactable.Action == InteractableType.Activate)
		{
			return HandleUse(result, interaction, doAction);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	protected virtual DelayedActionInstance HandleUse(DelayedActionInstance result, Interaction interaction, bool doAction)
	{
		MoleQuantity molesUsed = MolesUsed;
		if (!base.IsInputValid || !base.IsOutputValid)
		{
			if (!base.IsInputValid)
			{
				result.AppendStateMessage(GameStrings.DeviceNetworkInvalidSpecified.AsString("Input").AsColor("red"));
			}
			if (!base.IsOutputValid)
			{
				result.AppendStateMessage(GameStrings.DeviceNetworkInvalidSpecified.AsString("Output").AsColor("red"));
			}
		}
		else
		{
			if (!HasEnoughWater(molesUsed))
			{
				result.AppendStateMessage(GameStrings.DeviceNotEnoughWater.AsColor("red"));
			}
			if (base.WaterTooCold)
			{
				result.AppendStateMessage(GameStrings.DeviceWaterTooCold.AsColor("red"));
			}
			if (base.WaterTooHot)
			{
				result.AppendStateMessage(GameStrings.DeviceWaterTooHot.AsColor("red"));
			}
			if (base.WaterPolluted)
			{
				result.AppendStateMessage(GameStrings.DeviceWaterPolluted.AsColor("red"));
			}
			if (base.OutputFull)
			{
				result.AppendStateMessage(GameStrings.DeviceOutputFull.AsColor("red"));
			}
		}
		if (!base.IsMinimumWorldPressure)
		{
			result.AppendStateMessage(GameStrings.DeviceMinimumPressure.AsString(WaterDevice.MinimumWorldPressure.ToFloat().ToStringPrefix("Pa", "yellow")).AsColor("red"));
		}
		if (!(interaction.SourceThing is Human human))
		{
			return result.Fail();
		}
		StringBuilder stringBuilder = new StringBuilder();
		UseResult useResult = CanUse(human);
		switch (useResult)
		{
		case UseResult.NotInPosition:
			stringBuilder.AppendLine(GameStrings.DeviceNotInPosition.AsString(human.ToTooltip()).AsColor("red"));
			break;
		case UseResult.CannotWearingSuit:
			stringBuilder.AppendLine(GameStrings.DeviceCannotWhenWearing.AsString(human.Suit.ToTooltip()).AsColor("red"));
			break;
		case UseResult.NotNeeded:
			stringBuilder.AppendLine(GameStrings.NoSanitationNeed.AsString(human.ToTooltip()).AsColor("red"));
			break;
		}
		result.ExtendedMessage = stringBuilder.ToString();
		if (useResult != UseResult.Allowed)
		{
			return result.Fail();
		}
		if (!IsOperable)
		{
			return result.Fail();
		}
		if (!doAction)
		{
			return result.Succeed();
		}
		if (GameManager.RunSimulation)
		{
			DepositWaste(human);
			AudioEvent.Create(this, Defines.Sounds.ToiletFlush);
		}
		return result.Succeed();
	}

	private void DepositWaste(Human human)
	{
		GasMixture gasMixture = InputNetwork.Atmosphere.Remove(MolesUsed, AtmosphereHelper.MatterState.Liquid);
		Mole mole = gasMixture.Remove(Chemistry.GasType.Water, MoleQuantity.MaxValue);
		MoleQuantity quantity = mole.Quantity;
		MoleEnergy energy = mole.Energy;
		Atmosphere atmosphere = human.OrganStomach?.InternalAtmosphere;
		if (atmosphere != null)
		{
			Mole pollutedWater = atmosphere.GasMixture.PollutedWater;
			if (pollutedWater.Quantity > MoleQuantity.Zero)
			{
				quantity += pollutedWater.Quantity;
				energy += pollutedWater.Energy;
				AtmosphericEventInstance.CreateRemove(atmosphere, new GasMixture(new Mole(Chemistry.GasType.PollutedWater, pollutedWater.Quantity, pollutedWater.Energy)));
			}
		}
		gasMixture.Add(new Mole(Chemistry.GasType.PollutedWater, quantity, energy));
		OutputNetwork.Atmosphere.Add(gasMixture);
	}

	private UseResult CanUse(Human human)
	{
		if ((object)human == null)
		{
			return UseResult.Invalid;
		}
		if (human.SanitationRatio <= 0.25f)
		{
			return UseResult.NotNeeded;
		}
		if (human.GridPosition != LocalGrid)
		{
			return UseResult.NotInPosition;
		}
		if (!human.SuitSlot.IsEmpty())
		{
			return UseResult.CannotWearingSuit;
		}
		return UseResult.Allowed;
	}
}
