using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Reagents;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Items;

public class Ore : Stackable, ICentrifugable, IChemistryIngredient, IIngredient
{
	public static List<Ore> AllOrePrefabs = new List<Ore>();

	public static readonly int COBALT_ORE_HASH = Animator.StringToHash("ItemCobaltOre");

	[Header("Ore")]
	[FormerlySerializedAs("Temperature")]
	public float temperature = 273.15f;

	public float RefiningTime = 2f;

	public List<SpawnGas> SpawnContents = new List<SpawnGas>();

	public int QuantitySmelted;

	public TemperatureKelvin Temperature => new TemperatureKelvin(temperature);

	public virtual float ProcessTime => 1f;

	public virtual bool IsCentrifugeSmelt => true;

	public virtual ReagentMixture CentrifugeProcessUnit()
	{
		ReagentMixture reagentMixture = new ReagentMixture();
		if (IsCentrifugeSmelt)
		{
			Smelt(AtmosphericsController.World.SampleGlobalAtmosphere(base.WorldGrid), reagentMixture);
		}
		return reagentMixture;
	}

	public override void Explosion(Vector3 position, float force = 0f)
	{
	}

	public override void Merge(IMergeable stackable)
	{
		base.Merge(stackable);
		if (stackable is Ore ore)
		{
			QuantitySmelted += ore.QuantitySmelted;
		}
	}

	protected override void OnSplitStack(Stackable newStack)
	{
		base.OnSplitStack(newStack);
		int num = QuantitySmelted - base.Quantity;
		Ore ore = newStack as Ore;
		if ((bool)ore && num > 0)
		{
			ore.QuantitySmelted = num;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is OreSaveData oreSaveData)
		{
			oreSaveData.QuantitySmelted = QuantitySmelted;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new OreSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is OreSaveData oreSaveData)
		{
			QuantitySmelted = oreSaveData.QuantitySmelted;
		}
	}

	public override void Smelt(Atmosphere localAtmosphere, ReagentMixture reagentMixture)
	{
		if (SpawnContents.Count > 0)
		{
			if (QuantitySmelted >= base.Quantity)
			{
				QuantitySmelted--;
			}
			else
			{
				GasMixture gasMixture = GasMixtureHelper.Create();
				foreach (SpawnGas spawnContent in SpawnContents)
				{
					gasMixture.Add(new Mole(spawnContent.Type, spawnContent.GetQuantity(), spawnContent.GetEnergy()));
				}
				if (localAtmosphere.IsGlobalAtmosphere)
				{
					localAtmosphere = base.GridController.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
					gasMixture.TotalEnergy = IdealGas.Energy(gasMixture.HeatCapacity, RocketMath.Lerp(localAtmosphere.Temperature, Temperature, 0.6));
					AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, gasMixture);
				}
				else if (!localAtmosphere.IsAboveArmstrong())
				{
					gasMixture.TotalEnergy = IdealGas.Energy(gasMixture.HeatCapacity, Temperature);
					AtmosphericEventInstance.CreateAdd(localAtmosphere, gasMixture);
				}
				else
				{
					gasMixture.TotalEnergy = IdealGas.Energy(gasMixture.HeatCapacity, RocketMath.Lerp(localAtmosphere.Temperature, Temperature, 0.6000000238418579));
					AtmosphericEventInstance.CreateAdd(localAtmosphere, gasMixture);
				}
			}
		}
		base.Smelt(localAtmosphere, reagentMixture);
	}

	private DelayedActionInstance TryMoveOreToAvailableSlot(DynamicThing dynamicThing, bool doAction)
	{
		bool flag = false;
		foreach (Slot slot in dynamicThing.RootParent.Slots)
		{
			if (slot.Get() is MiningBelt miningBelt && miningBelt.SlotType == slot.Type)
			{
				if ((doAction && miningBelt.TryAddOre(this)) || (!doAction && miningBelt.CanAddOre(this)))
				{
					return DelayedActionInstance.Success(ActionStrings.Collect);
				}
				flag = true;
			}
		}
		if (!flag)
		{
			return DelayedActionInstance.Failure(ActionStrings.Collect, GameStrings.OreNoMiningBelt);
		}
		return DelayedActionInstance.Failure(ActionStrings.Collect, GameStrings.OreNoSlotInMiningBelt);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (attack.SourceItem is IMiningTool miningTool && !(miningTool is RobotMining) && sourceItem.ParentSlot != null)
		{
			return TryMoveOreToAvailableSlot(sourceItem, doAction);
		}
		return base.AttackWith(attack, doAction);
	}

	public static Ore CreateOreType(Ore defaultPrefabType, Vector3 worldPosition, ReagentMixture normalizedMixture, int quantity)
	{
		if (quantity <= 0)
		{
			return null;
		}
		Centrifuge.RecipeComparable.AllRecipes.TryGetValue(new Recipe(normalizedMixture, null), out var value);
		if (value == null)
		{
			value = defaultPrefabType;
		}
		Ore ore = Thing.Create<Ore>(value, worldPosition, Quaternion.identity, 0L);
		ore.ParentSlot = null;
		ore.SetQuantity(quantity);
		ore.CreatedReagentMixture = normalizedMixture;
		return ore;
	}

	public static Ore CreateOreType(Ore defaultPrefabType, Slot assignedSlot, ReagentMixture normalizedMixture, int quantity)
	{
		if (quantity <= 0)
		{
			return null;
		}
		Centrifuge.RecipeComparable.AllRecipes.TryGetValue(new Recipe(normalizedMixture, null), out var value);
		if ((object)value == null)
		{
			value = defaultPrefabType;
		}
		Ore ore = Thing.Create<Ore>(value, assignedSlot.Location);
		ore.SetQuantity(quantity);
		ore.QuantitySmelted = quantity;
		OnServer.MoveToSlot(ore, assignedSlot);
		ore.CreatedReagentMixture = normalizedMixture;
		return ore;
	}

	public static Ore CreateOreType(Ore defaultPrefabType, Connection outputConnection, ReagentMixture normalizedMixture, int quantity)
	{
		if (quantity <= 0)
		{
			return null;
		}
		Centrifuge.RecipeComparable.AllRecipes.TryGetValue(new Recipe(normalizedMixture, null), out var value);
		if ((object)value == null)
		{
			value = defaultPrefabType;
		}
		Ore ore = Thing.Create<Ore>(value, outputConnection.Transform);
		ore.SetQuantity(quantity);
		ore.QuantitySmelted = quantity;
		ore.CreatedReagentMixture = normalizedMixture;
		return ore;
	}
}
