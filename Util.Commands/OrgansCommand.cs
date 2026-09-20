using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI.ImGuiUi;

namespace Util.Commands;

public class OrgansCommand : CommandBase
{
	public override string HelpText => "Lists the organs of a character and their state. Defaults to the currently possessed character.";

	public override string[] Arguments => new string[1] { "[all|<refId>]" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	private static uint NameColor => ImGuiColor.Integer.White;

	private static uint IdColor => ImGuiColor.Integer.Grey;

	private static uint DetailColor => ImGuiColor.Integer.Grey;

	private static uint HealthyColor => ImGuiColor.Integer.SoftSage;

	private static uint DamagedColor => ImGuiColor.Integer.SoftRose;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("organs"))
		{
			return null;
		}
		switch (args.Length)
		{
		case 0:
		{
			Human parentHuman = InventoryManager.ParentHuman;
			if (!parentHuman)
			{
				return "No character is currently possessed, use 'organs all' or 'organs <refId>'";
			}
			PrintOrgans(new Entity[1] { parentHuman });
			return null;
		}
		case 1:
		{
			if (args[0] == "all")
			{
				if (Human.AllHumans.Count == 0)
				{
					return "No characters found";
				}
				PrintOrgans(Human.AllHumans);
				return null;
			}
			if (!CommandBase.Get(args, 0, "refId", out long result))
			{
				return null;
			}
			Entity entity = Thing.Find<Entity>(result);
			if (!entity)
			{
				return $"No entity found with reference id {result}";
			}
			PrintOrgans(new Entity[1] { entity });
			return null;
		}
		default:
			return "Invalid syntax";
		}
	}

	public override IEnumerable<string> GetCompletions(int argIndex, string prefix)
	{
		if (argIndex != 0)
		{
			return null;
		}
		List<string> list = new List<string> { "all" };
		foreach (Human allHuman in Human.AllHumans)
		{
			list.Add(allHuman.ReferenceId.ToString());
		}
		return list;
	}

	private static void PrintOrgans(IReadOnlyList<Entity> entities)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (Entity entity in entities)
		{
			foreach (Organ organ in entity.Organs)
			{
				num = Math.Max(num, organ.DisplayName.Length);
				num2 = Math.Max(num2, organ.ReferenceId.ToString().Length + 1);
				num3 = Math.Max(num3, DamageText(organ).Length);
			}
		}
		num += 2;
		num2 += 2;
		num3 += 2;
		foreach (Entity entity2 in entities)
		{
			ConsoleWindow.PrintAction($"Organs for '{entity2.DisplayName}' #{entity2.ReferenceId} ({entity2.Organs.Count}):");
			if (entity2.Organs.Count == 0)
			{
				ConsoleWindow.Print("  (none)");
				continue;
			}
			List<(string, uint)[]> list = new List<(string, uint)[]>();
			foreach (Organ organ2 in entity2.Organs)
			{
				list.Add(new(string, uint)[5]
				{
					("  ", NameColor),
					(organ2.DisplayName.PadRight(num), NameColor),
					($"#{organ2.ReferenceId}".PadRight(num2), IdColor),
					(DamageText(organ2).PadRight(num3), (organ2.DamageState.TotalRounded > 0) ? DamagedColor : HealthyColor),
					(organ2.ToConsoleString(), DetailColor)
				});
			}
			ConsoleWindow.PrintSegmentedBlockRaw(list.ToArray());
		}
	}

	private static string DamageText(Organ organ)
	{
		return $"dmg:{organ.DamageState.TotalRounded}%";
	}
}
