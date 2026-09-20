using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Reagents;

namespace Objects.Rockets.Mining;

public class DepositCompositionData
{
	[XmlAttribute("Hidden")]
	public bool IsContentsHidden;

	[XmlElement("Ore")]
	public List<DepositMaterialOreData> Ores = new List<DepositMaterialOreData>();

	[XmlElement("ReagentMix")]
	public List<DepositMaterialReagentMixData> ReagentMixes = new List<DepositMaterialReagentMixData>();

	[XmlElement("Ice")]
	public List<DepositMaterialGasData> FrozenGasMixes = new List<DepositMaterialGasData>();

	[XmlElement("Gas")]
	public List<DepositMaterialGasData> GasMixes = new List<DepositMaterialGasData>();

	public DepositComposition ToInstance()
	{
		return new DepositComposition(this);
	}

	public bool Validate()
	{
		if (Ores.Count == 0 && ReagentMixes.Count == 0 && FrozenGasMixes.Count == 0 && GasMixes.Count == 0)
		{
			ConsoleWindow.PrintError("No Ores, ReagentMixes, or FrozenGasses in DepositCompositionData.");
			return true;
		}
		foreach (DepositMaterialGasData frozenGasMix in FrozenGasMixes)
		{
			frozenGasMix.Cache();
		}
		foreach (DepositMaterialGasData gasMix in GasMixes)
		{
			gasMix.Cache();
		}
		foreach (DepositMaterialReagentMixData reagentMix in ReagentMixes)
		{
			try
			{
				if (new ReagentMixture(reagentMix.CreateRecipe()).TotalReagents == 0.0)
				{
					ConsoleWindow.PrintError("ReagentMix in DepositCompositionData has no reagents.");
					return false;
				}
			}
			catch (Exception ex)
			{
				ConsoleWindow.PrintError("Failed to create ReagentMix in DepositCompositionData.");
				ConsoleWindow.PrintError(ex.Message);
				return false;
			}
		}
		return true;
	}
}
