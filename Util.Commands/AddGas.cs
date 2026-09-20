using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Networks;

namespace Util.Commands;

public class AddGas : CommandBase
{
	private static string[] _gasNames;

	public override string HelpText => "Adds gas to a thing's internal atmosphere or to an atmospheric pipe network. Common gases: Oxygen, Nitrogen, CarbonDioxide, Methane, Pollutant, Water, NitrousOxide, Hydrogen. Liquid forms use the Liquid prefix (e.g. LiquidOxygen). Default temperature is 293K (20°C).";

	public override string[] Arguments => new string[1] { "<gasType> <targetId> <moles> [temperatureK]" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override IEnumerable<string> GetCompletions(int argIndex, string prefix)
	{
		if (argIndex != 0)
		{
			return null;
		}
		return _gasNames ?? (_gasNames = (from n in Enum.GetNames(typeof(Chemistry.GasType))
			where !(n == "Undefined") && !(n == "Fuel") && !(n == "Air")
			select n).ToArray());
	}

	public override string Execute(string[] args)
	{
		if (!EnforceScope("addgas"))
		{
			return null;
		}
		if (args.Length < 3)
		{
			return "Invalid syntax";
		}
		if (!Enum.TryParse<Chemistry.GasType>(args[0], ignoreCase: true, out var result) || result == Chemistry.GasType.Undefined || result == Chemistry.GasType.Fuel || result == Chemistry.GasType.Air)
		{
			ConsoleWindow.PrintError("unknown gas type '" + args[0] + "'", suppressStacktrace: true);
			return null;
		}
		if (!long.TryParse(args[1], out var result2))
		{
			ConsoleWindow.PrintError("invalid reference id '" + args[1] + "'", suppressStacktrace: true);
			return null;
		}
		IReferencable referencable = Referencable.Find(result2);
		if (referencable == null)
		{
			ConsoleWindow.PrintError($"no thing or network with reference id '{result2}'", suppressStacktrace: true);
			return null;
		}
		if (!float.TryParse(args[2], out var result3))
		{
			ConsoleWindow.PrintError("invalid moles value '" + args[2] + "'", suppressStacktrace: true);
			return null;
		}
		float result4 = 293.15f;
		if (args.Length > 3 && !float.TryParse(args[3], out result4))
		{
			ConsoleWindow.PrintError("invalid temperature value '" + args[3] + "'", suppressStacktrace: true);
			return null;
		}
		if (!GameManager.RunSimulation)
		{
			AddGasCommand addGasCommand = new AddGasCommand
			{
				Target = result2,
				Temperature = result4
			};
			switch (result)
			{
			case Chemistry.GasType.Oxygen:
				addGasCommand.Oxygen = result3;
				break;
			case Chemistry.GasType.Nitrogen:
				addGasCommand.Nitrogen = result3;
				break;
			case Chemistry.GasType.CarbonDioxide:
				addGasCommand.CarbonDioxide = result3;
				break;
			case Chemistry.GasType.Methane:
				addGasCommand.Methane = result3;
				break;
			case Chemistry.GasType.Pollutant:
				addGasCommand.Pollutant = result3;
				break;
			case Chemistry.GasType.Water:
				addGasCommand.Water = result3;
				break;
			case Chemistry.GasType.PollutedWater:
				addGasCommand.PollutedWater = result3;
				break;
			case Chemistry.GasType.NitrousOxide:
				addGasCommand.NitrousOxide = result3;
				break;
			case Chemistry.GasType.LiquidNitrogen:
				addGasCommand.LiquidNitrogen = result3;
				break;
			case Chemistry.GasType.LiquidOxygen:
				addGasCommand.LiquidOxygen = result3;
				break;
			case Chemistry.GasType.LiquidMethane:
				addGasCommand.LiquidMethane = result3;
				break;
			case Chemistry.GasType.Steam:
				addGasCommand.Steam = result3;
				break;
			case Chemistry.GasType.LiquidCarbonDioxide:
				addGasCommand.LiquidCarbonDioxide = result3;
				break;
			case Chemistry.GasType.LiquidPollutant:
				addGasCommand.LiquidPollutant = result3;
				break;
			case Chemistry.GasType.LiquidNitrousOxide:
				addGasCommand.LiquidNitrousOxide = result3;
				break;
			case Chemistry.GasType.Hydrogen:
				addGasCommand.Hydrogen = result3;
				break;
			case Chemistry.GasType.LiquidHydrogen:
				addGasCommand.LiquidHydrogen = result3;
				break;
			case Chemistry.GasType.Hydrazine:
				addGasCommand.Hydrazine = result3;
				break;
			case Chemistry.GasType.LiquidHydrazine:
				addGasCommand.LiquidHydrazine = result3;
				break;
			case Chemistry.GasType.LiquidAlcohol:
				addGasCommand.LiquidAlcohol = result3;
				break;
			case Chemistry.GasType.Helium:
				addGasCommand.Helium = result3;
				break;
			case Chemistry.GasType.LiquidSodiumChloride:
				addGasCommand.SodiumChloride = result3;
				break;
			case Chemistry.GasType.Silanol:
				addGasCommand.Silanol = result3;
				break;
			case Chemistry.GasType.LiquidSilanol:
				addGasCommand.LiquidSilanol = result3;
				break;
			case Chemistry.GasType.HydrochloricAcid:
				addGasCommand.HydrochloricAcid = result3;
				break;
			case Chemistry.GasType.LiquidHydrochloricAcid:
				addGasCommand.LiquidHydrochloricAcid = result3;
				break;
			case Chemistry.GasType.Ozone:
				addGasCommand.Ozone = result3;
				break;
			case Chemistry.GasType.LiquidOzone:
				addGasCommand.LiquidOzone = result3;
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
			addGasCommand.SendToServer();
			return $"Sent add-gas request: {result3} moles of {result} to {referencable.DisplayName} at {result4}K.";
		}
		GasMixture gasMixture = new GasMixture(new Mole(result, new MoleQuantity(result3), MoleEnergy.Zero));
		gasMixture.TotalEnergy = IdealGas.Energy(gasMixture.HeatCapacity, new TemperatureKelvin(result4));
		bool flag = false;
		if (referencable is AtmosphericsNetwork atmosphericsNetwork)
		{
			AtmosphericEventInstance.CreateAdd(atmosphericsNetwork.Atmosphere, gasMixture);
			flag = true;
		}
		else if (referencable is Thing thing)
		{
			if (thing.InternalAtmosphere != null)
			{
				AtmosphericEventInstance.CreateAdd(thing.InternalAtmosphere, gasMixture);
				flag = true;
			}
			else if (thing is INetworkedAtmospherics { StructureNetwork: AtmosphericsNetwork structureNetwork })
			{
				AtmosphericEventInstance.CreateAdd(structureNetwork.Atmosphere, gasMixture);
				flag = true;
			}
		}
		else if (referencable is Atmosphere atmosphere)
		{
			AtmosphericEventInstance.CreateAdd(atmosphere, gasMixture);
			flag = true;
		}
		if (flag)
		{
			return $"Added {result3} moles of {result} to {referencable.DisplayName} at {result4}K.";
		}
		ConsoleWindow.PrintError($"target #{result2} ({referencable.DisplayName}) has no atmosphere to add gas to", suppressStacktrace: true);
		return null;
	}
}
