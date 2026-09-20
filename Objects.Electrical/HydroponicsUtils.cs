using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Genetics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Objects.Items;
using UnityEngine;

namespace Objects.Electrical;

public static class HydroponicsUtils
{
	private static readonly MoleQuantity UseAmount = new MoleQuantity(20.0);

	public static Thing.DelayedActionInstance HandlePlantInteraction(IGrower grower, Interactable interactable, Interaction interaction, bool doAction)
	{
		PlantToFertiliserSlotMapping plantToFertiliserSlotMapping = grower.PlantToFertiliserSlotMapping(interactable.Action);
		Slot plantSlot = plantToFertiliserSlotMapping.PlantSlot;
		Slot fertiliserSlot = plantToFertiliserSlotMapping.FertiliserSlot;
		Plant plant = plantSlot.Occupant as Plant;
		Fertiliser fertiliser = fertiliserSlot.Occupant as Fertiliser;
		Thing.DelayedActionInstance interactionResult = GetInteractionResult(grower, interactable, interaction, doAction, plantSlot, fertiliserSlot, plant, fertiliser);
		if (interactionResult != null)
		{
			if ((bool)plant)
			{
				interactionResult.Slider = ((plant.DamageState.TotalRatio > 0f) ? plant.DamageState.TotalRatioClampedUndamaged : (-1f));
				plant.SetStateMessage(interactionResult);
			}
			else if ((bool)fertiliser)
			{
				interactionResult.Slider = ((fertiliser.DamageState.TotalRatio > 0f) ? fertiliser.DamageState.TotalRatioClampedUndamaged : (-1f));
				interactionResult.AppendStateMessage(GameStrings.FertiliserClearFromSlot, Localization.QuantityModifierKey, fertiliser.DisplayName);
			}
		}
		return interactionResult;
	}

	private static Thing.DelayedActionInstance GetInteractionResult(IGrower grower, Interactable interactable, Interaction interaction, bool doAction, Slot plantSlot, Slot fertiliserSlot, Plant plant, Fertiliser fertiliser)
	{
		Thing.DelayedActionInstance result = new Thing.DelayedActionInstance();
		DynamicThing occupant = interaction.SourceSlot.Occupant;
		if (HandleClearPlant(interaction, plant, fertiliser, doAction, out result))
		{
			return result;
		}
		if (LabellerInHand(interaction, occupant, plant, doAction, out result))
		{
			return result;
		}
		if (PlantSamplerInHand(occupant, plant, doAction, out result))
		{
			return result;
		}
		if (WateringDeviceInHand(occupant, plant, grower, doAction, out result))
		{
			return result;
		}
		if (FertiliserInHand(occupant, grower, fertiliserSlot, doAction, out result))
		{
			return result;
		}
		if (PlantInHand(occupant, plant, interaction, grower, plantSlot, doAction, out result))
		{
			return result;
		}
		if (InvalidObjectInHand(occupant, plant, interaction, doAction, out result))
		{
			return result;
		}
		if (NothingInHand(occupant, plant, interaction, doAction, out result))
		{
			return result;
		}
		return null;
	}

	private static bool HandleClearPlant(Interaction interaction, Plant plant, Fertiliser fertiliser, bool doAction, out Thing.DelayedActionInstance result)
	{
		result = new Thing.DelayedActionInstance
		{
			Duration = 0.5f
		};
		if (!interaction.AltKey)
		{
			return false;
		}
		if (plant != null)
		{
			result.ActionSoundHash = Defines.Sounds.HarvestPlantActive;
			if (!doAction)
			{
				result.ActionMessage = GameStrings.ActionClearPlant.AsString(plant.ToTooltip());
				result.Succeed();
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Destroy(plant);
				AudioEvent.Create(interaction.SourceThing, Defines.Sounds.HarvestPlant);
			}
			result.Succeed();
			return true;
		}
		if ((bool)fertiliser)
		{
			result.ActionSoundHash = Defines.Sounds.HarvestPlantActive;
			if (!doAction)
			{
				result.ActionMessage = GameStrings.ClearFertiliser.DisplayString;
				result.Succeed();
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Destroy(fertiliser);
				AudioEvent.Create(interaction.SourceThing, Defines.Sounds.PlantingFinished);
			}
			result.Succeed();
			return true;
		}
		return false;
	}

	public static bool PlantSamplerInHand(DynamicThing inHand, Plant plant, bool doAction, out Thing.DelayedActionInstance result)
	{
		result = new Thing.DelayedActionInstance
		{
			Duration = 0.5f
		};
		if (inHand is PlantSampler plantSampler)
		{
			if (!plant)
			{
				result.ActionMessage = ActionStrings.Plant;
				result.Fail(GameStrings.NothingToSample);
				return true;
			}
			result.ActionMessage = GameStrings.PlantSamplerAction.AsString(plant.ToTooltip());
			if (!plantSampler.OnOff)
			{
				result.Fail(GameStrings.DeviceNotOn);
				return true;
			}
			if (!plantSampler.Powered)
			{
				result.Fail(GameStrings.DeviceNoPower);
				return true;
			}
			if (doAction)
			{
				plantSampler.AddPlantSample(plant);
			}
			result.Succeed();
			return true;
		}
		return false;
	}

	private static bool LabellerInHand(Interaction interaction, DynamicThing inHand, Plant plant, bool doAction, out Thing.DelayedActionInstance result)
	{
		result = new Thing.DelayedActionInstance();
		if (inHand is Labeller labeller)
		{
			if (!plant)
			{
				result.ActionMessage = ActionStrings.Plant;
				result.Fail(GameStrings.NoPlantToLabel);
				return true;
			}
			result.ActionMessage = ActionStrings.Rename;
			if (!labeller.OnOff)
			{
				result.Fail(GameStrings.DeviceNotOn);
				return true;
			}
			if (doAction && interaction.SourceThing == InventoryManager.ParentHuman)
			{
				labeller.Rename(plant);
			}
			result.Succeed();
			return true;
		}
		return false;
	}

	private static bool WateringDeviceInHand(DynamicThing inHand, Plant plant, IGrower grower, bool doAction, out Thing.DelayedActionInstance result)
	{
		result = new Thing.DelayedActionInstance
		{
			Duration = 0.5f
		};
		if (!(inHand is GasCanister) && !(inHand is WaterBottle))
		{
			return false;
		}
		if (grower.WaterAtmosphere == null)
		{
			return false;
		}
		if (plant == null)
		{
			result.ActionMessage = ActionStrings.Plant;
			result.Fail(GameStrings.NothingToWater);
			return true;
		}
		if (grower.WaterAtmosphere.LiquidVolumeRatio > 0.5f)
		{
			result.Fail(GameStrings.AlreadyFullOfWater);
			return true;
		}
		result.ActionMessage = GameStrings.WaterPlantWith.AsString(inHand.DisplayName);
		result.Duration = 0.5f;
		GasCanister gasCanister = inHand as GasCanister;
		WaterBottle waterBottle = inHand as WaterBottle;
		if ((bool)gasCanister && gasCanister.CanisterContentType != Pipe.ContentType.Liquid)
		{
			result.Fail(GameStrings.CanOnlyUseLiquidOnPlants);
			return true;
		}
		if (!doAction)
		{
			result.AppendStateMessage(GameStrings.WaterPlantWith, inHand.DisplayName);
			result.Succeed();
			return true;
		}
		if ((bool)gasCanister)
		{
			MoleQuantity moleQuantity = ((gasCanister.InternalAtmosphere.GasMixture.GetTotalMolesLiquids > UseAmount) ? UseAmount : gasCanister.InternalAtmosphere.GasMixture.GetTotalMolesLiquids);
			AtmosphericEventInstance.RemoveMoles(gasCanister.InternalAtmosphere, moleQuantity, AtmosphereHelper.MatterState.Liquid);
			GasMixture gasMixture = GasMixtureHelper.Create();
			gasMixture.Add(new Mole(Chemistry.GasType.Water, moleQuantity, MoleEnergy.Zero));
			gasMixture.AddEnergy(IdealGas.Energy(gasMixture.HeatCapacity, gasCanister.InternalAtmosphere.Temperature));
			AtmosphericEventInstance.CreateAdd(grower.WaterAtmosphere, gasMixture);
		}
		else if (waterBottle != null)
		{
			MoleQuantity currentMoleCount = waterBottle.GetCurrentMoleCount();
			MoleQuantity moleQuantity2 = ((currentMoleCount > UseAmount) ? UseAmount : currentMoleCount);
			currentMoleCount -= moleQuantity2;
			waterBottle.Quantity = waterBottle.GetCurrentQuanitityFromMole(currentMoleCount);
			GasMixture gasMixture2 = GasMixtureHelper.Create();
			gasMixture2.Add(new Mole(Chemistry.GasType.Water, moleQuantity2, MoleEnergy.Zero));
			gasMixture2.AddEnergy(IdealGas.Energy(gasMixture2.HeatCapacity, Chemistry.Temperature.TwentyDegrees));
			AtmosphericEventInstance.CreateAdd(grower.WaterAtmosphere, gasMixture2);
		}
		result.Succeed();
		return true;
	}

	private static bool PlantInHand(DynamicThing inHand, Plant plant, Interaction interaction, IGrower grower, Slot plantSlot, bool doAction, out Thing.DelayedActionInstance result)
	{
		result = new Thing.DelayedActionInstance
		{
			Duration = 0.5f
		};
		if (!(inHand is Stackable stackable))
		{
			return false;
		}
		if ((bool)plant)
		{
			bool flag = plant.IsSeeding && plant.SeedQuantity > 0;
			bool flag2 = plant.SeedObject?.PrefabHash == stackable.PrefabHash;
			bool flag3 = plant.IsMature && plant.HarvestQuantity > 0;
			bool flag4 = plant.FruitObject?.PrefabHash == stackable.PrefabHash;
			bool flag5 = stackable.Quantity == stackable.MaxQuantity;
			if (!doAction)
			{
				if (flag && flag2 && !flag5)
				{
					result.ActionMessage = GameStrings.ActionHarvestSeed.AsString(plant.ToTooltip());
					result.Succeed();
					return true;
				}
				if (flag3 && flag4 && !flag5)
				{
					result.ActionMessage = GameStrings.ActionHarvestPlant.AsString(plant.ToTooltip());
					result.Succeed();
					return true;
				}
				if (((flag && flag2) || (flag3 && flag4)) && flag5)
				{
					result.ActionMessage = plant.DisplayName;
					result.Fail(GameStrings.CantHoldAnyMore);
					return true;
				}
				result.ActionMessage = plant.DisplayName;
				result.Fail(GameStrings.SomethingPlantedHere);
				return true;
			}
			if (!GameManager.RunSimulation)
			{
				result.Succeed();
				return true;
			}
			if (((flag && flag2) || (flag3 && flag4)) && !flag5)
			{
				Singleton<AudioManager>.Instance.PlayAudioClipsData(interaction.SourceThing, HydroponicsStation.HarvestPlantHash, Vector3.zero);
				AudioEvent.Create(plant.ParentTray, Defines.Sounds.HarvestPlant);
				plant.Harvest(interaction.SourceThing, interaction.SourceSlot, flag && flag2);
				result.Succeed();
				return true;
			}
			result.Fail();
			return true;
		}
		if (!(inHand is Plant plant2))
		{
			return false;
		}
		result.ActionSoundHash = Defines.Sounds.PlantingPlant;
		if (!doAction)
		{
			result.ActionMessage = ActionStrings.Plant;
			result.AppendStateMessage(GameStrings.AddSeedsToSlot, plant2.DisplayName);
			result.Succeed();
			return true;
		}
		if (!GameManager.IsBatchMode)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(interaction.SourceThing, HydroponicsStation.PlantingFinishedHash, Vector3.zero);
		}
		GeneCollection genes = ((GameManager.RunSimulation && plant2 != null) ? GeneCollection.Copy(plant2.Genes) : null);
		if (GameManager.RunSimulation && doAction && plant2.OnUseItem(1f, (Thing)grower))
		{
			Plant plant3 = ((!(plant2 is Seed seed)) ? OnServer.Create<Plant>(plant2.SourcePrefab, plantSlot) : OnServer.Create<Plant>(seed.PlantType, plantSlot));
			if ((bool)plant3)
			{
				plant3.ApplySeedTraits(genes);
				AudioEvent.Create(plant3, Defines.Sounds.PlantingFinished);
			}
		}
		result.Succeed();
		return true;
	}

	private static bool FertiliserInHand(DynamicThing inHand, IGrower grower, Slot fertiliserSlot, bool doAction, out Thing.DelayedActionInstance result)
	{
		result = new Thing.DelayedActionInstance
		{
			Duration = 0.5f
		};
		if (inHand is Fertiliser fertiliser)
		{
			if (fertiliserSlot.Occupant != null)
			{
				result.ActionMessage = GameStrings.Fertiliser.DisplayString;
				result.Fail(GameStrings.SomethingInThisSlot);
				return true;
			}
			if (!doAction)
			{
				result.ActionMessage = GameStrings.Fertiliser.DisplayString;
				result.AppendStateMessage(GameStrings.AddFertiliserToSlot);
				result.Succeed();
				return true;
			}
			if (!GameManager.RunSimulation)
			{
				result.Succeed();
				return true;
			}
			if (doAction && fertiliser.OnUseItem(1f, (Thing)grower))
			{
				Fertiliser fertiliser2 = OnServer.Create<Fertiliser>(fertiliser, fertiliserSlot);
				fertiliser.CopyStats(fertiliser2);
				AudioEvent.Create(fertiliser2, Defines.Sounds.PlantingFinished);
				result.Succeed();
				return true;
			}
		}
		return false;
	}

	private static bool InvalidObjectInHand(DynamicThing inHand, Plant plant, Interaction interaction, bool doAction, out Thing.DelayedActionInstance result)
	{
		result = new Thing.DelayedActionInstance
		{
			Duration = 0.5f
		};
		if ((bool)inHand && (bool)plant)
		{
			result.Fail();
			return true;
		}
		return false;
	}

	private static bool NothingInHand(DynamicThing inHand, Plant plant, Interaction interaction, bool doAction, out Thing.DelayedActionInstance result)
	{
		result = new Thing.DelayedActionInstance
		{
			Duration = 0.5f
		};
		if (!inHand)
		{
			if (!plant)
			{
				return false;
			}
			result.ActionSoundHash = Defines.Sounds.HarvestPlantActive;
			if (!doAction)
			{
				if (plant.IsSeeding && plant.SeedQuantity > 0)
				{
					result.ActionMessage = GameStrings.ActionHarvestSeed.AsString(plant.ToTooltip());
					result.Succeed();
					return true;
				}
				if (plant.IsMature)
				{
					result.ActionMessage = GameStrings.ActionHarvestPlant.AsString(plant.ToTooltip());
					result.Succeed();
					return true;
				}
				result.ActionMessage = plant.DisplayName;
				result.Fail();
				return true;
			}
			if (!GameManager.RunSimulation)
			{
				result.Succeed();
				return true;
			}
			bool flag = plant.IsSeeding && plant.SeedQuantity > 0;
			if (plant.IsMature || flag)
			{
				AudioEvent.Create(plant.ParentTray, Defines.Sounds.HarvestPlant);
				plant.Harvest(interaction.SourceThing, interaction.SourceSlot, flag);
			}
			result.Fail();
			return true;
		}
		return false;
	}
}
