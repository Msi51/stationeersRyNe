using System.Text;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;

namespace Util.Commands;

public class PlantCommand : CommandBase
{
	public override string HelpText => "Plant debug helpers. 'grow <id>' advances plants inside the IGrower with the given id by one stage; 'grow all' advances every plant in the world by one stage. 'genes <id>' prints the temperature/pressure genes of a plant or harvestable. Server only.";

	public override string[] Arguments => new string[2] { "grow <parent thing id | all>", "genes <reference id>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("plant"))
		{
			return null;
		}
		if (!GameManager.RunSimulation)
		{
			return "Can only be run on the server";
		}
		if (args.Length == 0)
		{
			return "Invalid syntax";
		}
		string text = args[0];
		if (!(text == "grow"))
		{
			if (text == "genes")
			{
				return HandleGenes(args);
			}
			return "Invalid syntax";
		}
		return HandleGrow(args);
	}

	private string HandleGrow(string[] args)
	{
		if (args.Length < 2)
		{
			return "Invalid arguments";
		}
		if (args[1] == "all")
		{
			for (int num = Plant.AllPlants.Count - 1; num >= 0; num--)
			{
				Plant plant = Plant.AllPlants[num];
				if (plant.Stage + 1 < plant.GrowthStates.Count - 1)
				{
					plant.SetNextStage();
				}
			}
			return "All plants have been set to their next growth stage.";
		}
		if (!long.TryParse(args[1], out var result))
		{
			return "Invalid arguments";
		}
		if (!(Referencable.Find(result) is Thing thing))
		{
			ConsoleWindow.PrintError($"Unknown reference id '{result}'.", suppressStacktrace: true);
			return null;
		}
		if (!(thing is IGrower))
		{
			ConsoleWindow.PrintError("Thing must be an IGrower.", suppressStacktrace: true);
			return null;
		}
		foreach (Slot slot in thing.Slots)
		{
			if (slot.Occupant is Plant plant2)
			{
				plant2.SetNextStage();
			}
		}
		return "Plants inside " + thing.DisplayName + " have been set to their next growth stage.";
	}

	private string HandleGenes(string[] args)
	{
		if (args.Length < 2)
		{
			return "Invalid arguments";
		}
		if (!long.TryParse(args[1], out var result))
		{
			return "Invalid arguments";
		}
		IReferencable referencable = Referencable.Find(result);
		if (referencable is Plant plant)
		{
			return PrintGenes(plant);
		}
		if (referencable is IHarvestable { GetPlant: var getPlant } && getPlant != null)
		{
			return PrintGenes(getPlant);
		}
		ConsoleWindow.PrintError("Invalid reference id.", suppressStacktrace: true);
		return null;
		static string PrintGenes(Plant plant2)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Temperature");
			stringBuilder.AppendLine(plant2.lifeRequirements.GrowTemperatureC.DebugPrint());
			stringBuilder.AppendLine("Pressure");
			stringBuilder.AppendLine(plant2.lifeRequirements.GrowPressure.DebugPrint());
			return stringBuilder.ToString();
		}
	}
}
