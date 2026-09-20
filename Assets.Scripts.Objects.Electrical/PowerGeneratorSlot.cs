using System;
using System.Collections.Generic;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Reagents;
using Trading;

namespace Assets.Scripts.Objects.Electrical;

public class PowerGeneratorSlot : DeviceImport, IResourceConsumer, IReferencable, IEvaluable
{
	public static string[] GeneratorModeStrings = new string[2] { "Not Generating", "Generating" };

	protected int PoweredTicks;

	public List<OreResource> Resources = new List<OreResource>();

	public float PowerGenerated = 20000f;

	public override string[] ModeStrings => GeneratorModeStrings;

	public List<Item> GetResourcesUsed()
	{
		List<Item> list = new List<Item>(Resources.Count);
		foreach (OreResource resource in Resources)
		{
			Item generatorOre = resource.GeneratorOre;
			if (!(generatorOre == null))
			{
				list.Add(generatorOre);
			}
		}
		return list;
	}

	public bool CanProcess(Recipe recipe)
	{
		foreach (OreResource resource in Resources)
		{
			if (resource.GeneratorOre.CreatedReagentMixture.ContainsSome(recipe))
			{
				return true;
			}
		}
		return false;
	}

	public bool CanProcess(Reagent reagentType)
	{
		foreach (OreResource resource in Resources)
		{
			if (resource.GeneratorOre.CreatedReagentMixture.Contains(reagentType))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsValidOre()
	{
		foreach (OreResource resource in Resources)
		{
			if (resource.GeneratorOre.GetPrefabHash() == ImportingThing.PrefabHash)
			{
				return true;
			}
		}
		return false;
	}

	public int TicksPerResource()
	{
		if (!(ImportingThing is ISolidFuel solidFuel))
		{
			return 0;
		}
		return (int)Math.Round(solidFuel.GetEnergyPerSecond() / PowerGenerated) * 2;
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (cableNetwork != base.PowerCableNetwork)
		{
			return 0f;
		}
		if (PoweredTicks <= 0 || !OnOff)
		{
			return 0f;
		}
		return PowerGenerated;
	}
}
