using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Util.Commands;

public class RocketCommand : CommandBase
{
	private enum Argument : byte
	{
		None,
		Refresh,
		Print,
		Abandon,
		Debug,
		InstantMove,
		Refuel,
		Recharge,
		Relink,
		Mounts,
		Clone,
		Parks
	}

	private static EnumCollection<Argument, byte> _arguments = new EnumCollection<Argument, byte>(toProper: false);

	private const float PARK_XZ_RADIUS = 24f;

	private const float PARK_Y_BELOW = 12f;

	private const float PARK_Y_ABOVE = 80f;

	public override string HelpText => "Performs rocket debug operations: refresh networks, print rocket ids ('print') or a tree of one rocket's structures and internals ('print <referenceId>'), list launch mounts ('mounts'), clone a rocket onto a launch mount ('clone <rocketId> <launchMountReferenceId>'), abandon, toggle debug draw, instant-move, refuel, recharge, or relink a decoupled rocket ('relink <referenceId>' previews, 'relink <referenceId> apply' performs it), or audit the space parks for orphan/foreign structures ('parks'). Refuel, recharge, instant-move and clone require creative mode and a running simulation; relink and parks are server-only (any difficulty).";

	public override string[] Arguments => _arguments.Names;

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("rocket"))
		{
			return null;
		}
		int num = args.Length;
		if (num == 0 || num > 3)
		{
			ConsoleWindow.PrintError("Invalid number of arguments for rocket command.", suppressStacktrace: true);
			return null;
		}
		if (!Enum.TryParse<Argument>(args[0], ignoreCase: true, out var result))
		{
			ConsoleWindow.PrintError("Argument '" + args[0] + "' is not a valid rocket command argument.", suppressStacktrace: true);
			return null;
		}
		return result switch
		{
			Argument.Refresh => Refresh(), 
			Argument.Print => Print(args), 
			Argument.Abandon => AbandonRocket(args), 
			Argument.Debug => DrawRocketDebug(args), 
			Argument.InstantMove => InstantMove(args), 
			Argument.Refuel => Refuel(args), 
			Argument.Recharge => Recharge(args), 
			Argument.Relink => RelinkRocket(args), 
			Argument.Mounts => PrintLaunchMounts(), 
			Argument.Clone => CloneRocket(args), 
			Argument.Parks => PrintRocketParks(), 
			_ => "Invalid syntax", 
		};
	}

	private bool AllowArgument()
	{
		if (!DifficultySetting.Current.Creative && !GameManager.IsBatchMode)
		{
			ConsoleWindow.PrintError("Command only available in creative mode.", suppressStacktrace: true);
			return false;
		}
		if (!GameManager.RunSimulation)
		{
			ConsoleWindow.PrintError("Command only available on server.", suppressStacktrace: true);
			return false;
		}
		return true;
	}

	private string Refuel(string[] args)
	{
		if (!AllowArgument())
		{
			return null;
		}
		Rocket rocket = GetRocket(args);
		if (rocket == null)
		{
			return null;
		}
		PressurekPa pressurekPa = new PressurekPa(30000.0);
		foreach (Atmosphere rocketAtmosphere in rocket.RocketNetwork.RocketAtmospheres)
		{
			if (rocketAtmosphere != null && !(rocketAtmosphere.PressureGassesAndLiquids > pressurekPa) && !(rocketAtmosphere.PressureGassesAndLiquids <= PressurekPa.Zero))
			{
				double num = AtmosphereHelper.GasRatio(LogicType.RatioMethane, rocketAtmosphere);
				double num2 = AtmosphereHelper.GasRatio(LogicType.RatioOxygen, rocketAtmosphere);
				if (!(num + num2 < 1.0))
				{
					TemperatureKelvin temperatureKelvin = new TemperatureKelvin(215.0);
					MoleQuantity moleQuantity = RocketMath.NumberOfMolesGas(pressurekPa, rocketAtmosphere.Volume, temperatureKelvin) - rocketAtmosphere.TotalMoles;
					GasMixture gasMixture = GasMixtureHelper.Create();
					gasMixture.Add(new Mole(Chemistry.GasType.Methane, moleQuantity * 0.6660000085830688, MoleEnergy.Zero));
					gasMixture.Add(new Mole(Chemistry.GasType.Oxygen, moleQuantity * 0.33399999141693115, MoleEnergy.Zero));
					gasMixture.TotalEnergy = IdealGas.Energy(gasMixture.HeatCapacity, temperatureKelvin);
					rocketAtmosphere.Add(gasMixture);
				}
			}
		}
		foreach (Battery battery in rocket.RocketNetwork.Batteries)
		{
			battery.PowerStored = battery.PowerMaximum;
		}
		ConsoleWindow.PrintAction("Rocket '" + rocket.DisplayName + "' has been refuelled and batteries charged.");
		return null;
	}

	private string Recharge(string[] args)
	{
		if (!AllowArgument())
		{
			return null;
		}
		Rocket rocket = GetRocket(args);
		if (rocket == null)
		{
			return null;
		}
		foreach (Battery battery in rocket.RocketNetwork.Batteries)
		{
			battery.PowerStored = battery.PowerMaximum;
		}
		ConsoleWindow.PrintAction("Rocket '" + rocket.DisplayName + "' batteries have been charged.");
		return null;
	}

	private string InstantMove(string[] args)
	{
		if (!AllowArgument())
		{
			return null;
		}
		Rocket rocket = GetRocket(args);
		if (rocket == null)
		{
			return null;
		}
		if (rocket.RocketState != RocketState.InSpace)
		{
			ConsoleWindow.PrintError("Rocket must be in space to instant move.", suppressStacktrace: true);
			return null;
		}
		rocket.Progress = 1f;
		ConsoleWindow.PrintAction("Instantly moving rocket '" + rocket.DisplayName + "' to destination.");
		return null;
	}

	private Rocket GetRocket(string[] args)
	{
		if (args.Length != 2)
		{
			ConsoleWindow.PrintError("Invalid number of arguments for rocket command.", suppressStacktrace: true);
			return null;
		}
		if (long.TryParse(args[1], out var result))
		{
			Rocket rocket = Referencable.Find<Rocket>(result);
			if (rocket != null)
			{
				return rocket;
			}
			ConsoleWindow.PrintError("Could not find rocket with reference id " + StringManager.Get(result) + ".", suppressStacktrace: true);
			return null;
		}
		ConsoleWindow.PrintError("Argument '" + args[1] + "' is not a valid rocket reference id.", suppressStacktrace: true);
		return null;
	}

	private string AbandonRocket(string[] args)
	{
		Rocket rocket = GetRocket(args);
		if (rocket == null)
		{
			return null;
		}
		ConsoleWindow.PrintAction("Removing rocket: " + rocket.DisplayName + ".");
		rocket.AbandonRocket();
		return null;
	}

	private string Print(string[] args)
	{
		if (args.Length != 2)
		{
			return PrintRocketIds();
		}
		return PrintRocketTree(args);
	}

	private string PrintRocketIds()
	{
		ConsoleWindow.Print("Printing rocket ids...");
		foreach (Rocket allRocket in Rocket.AllRockets)
		{
			ConsoleWindow.Print(allRocket.DisplayName + " Id: " + StringManager.Get(allRocket.ReferenceId));
		}
		return null;
	}

	private string PrintRocketTree(string[] args)
	{
		Rocket rocket = GetRocket(args);
		if (rocket == null)
		{
			return null;
		}
		RocketNetwork rocketNetwork = rocket.RocketNetwork;
		TreeString treeString = TreeString.Node(rocket.DisplayName + " #" + StringManager.Get(rocket.ReferenceId));
		TreeString.Variable($"State: {rocket.RocketState}", treeString);
		if (rocketNetwork != null)
		{
			TreeString.Variable("Network #" + StringManager.Get(rocketNetwork.ReferenceId), treeString);
			TreeString.Variable($"Mass: dry {rocketNetwork.DryMass:0.##}kg, gas {rocketNetwork.GasMass:0.##}kg, combined {rocketNetwork.CombinedMass():0.##}kg", treeString);
			if (rocketNetwork.CrewModule != null)
			{
				TreeString.Node("Crew Module: " + Format(rocketNetwork.CrewModule), treeString);
			}
			TreeString myParent = TreeString.Node($"Structures ({rocketNetwork.StructureList.Count})", treeString);
			foreach (INetworkedStructure structure in rocketNetwork.StructureList)
			{
				if (structure != null && structure != rocketNetwork.CrewModule)
				{
					TreeString.Node(Format(structure.GetAsThing), myParent);
				}
			}
			TreeString treeString2 = TreeString.Node($"Internals ({rocketNetwork.Internals.Count})", treeString);
			HashSet<object> hashSet = new HashSet<object>();
			AddCategory(treeString2, "Engines", rocketNetwork.Engines, hashSet);
			AddCategory(treeString2, "Batteries", rocketNetwork.Batteries, hashSet);
			AddCategory(treeString2, "Miners", rocketNetwork.RocketMiners, hashSet);
			AddCategory(treeString2, "Scanners", rocketNetwork.RocketScanners, hashSet);
			AddCategory(treeString2, "Payload Bays", rocketNetwork.RocketPayloadBays, hashSet);
			AddCategory(treeString2, "Umbilicals", rocketNetwork.RocketUmbilicals, hashSet);
			List<Thing> list = new List<Thing>();
			foreach (IRocketInternals @internal in rocketNetwork.Internals)
			{
				if (@internal != null && !hashSet.Contains(@internal) && @internal is Thing item)
				{
					list.Add(item);
				}
			}
			if (list.Count > 0)
			{
				TreeString myParent2 = TreeString.Node($"Other ({list.Count})", treeString2);
				foreach (Thing item2 in list)
				{
					TreeString.Node(Format(item2), myParent2);
				}
			}
		}
		treeString.ToConsole();
		return null;
	}

	private static void AddCategory<T>(TreeString parent, string label, List<T> items, HashSet<object> categorized) where T : class
	{
		if (items == null || items.Count == 0)
		{
			return;
		}
		TreeString myParent = TreeString.Node($"{label} ({items.Count})", parent);
		foreach (T item in items)
		{
			if (item != null)
			{
				categorized.Add(item);
				TreeString.Node(Format(item as Thing), myParent);
			}
		}
	}

	private static string Format(Thing thing)
	{
		if (!(thing == null))
		{
			return thing.DisplayName + " #" + StringManager.Get(thing.ReferenceId) + " [" + thing.GetType().Name + "]";
		}
		return "<null>";
	}

	private string DrawRocketDebug(string[] args)
	{
		RocketDebugWindow.Toggle();
		return $"Rocket debug window: {RocketDebugWindow.Show}";
	}

	private string Refresh()
	{
		foreach (RocketNetwork allRocketNetwork in RocketNetwork.AllRocketNetworks)
		{
			allRocketNetwork.RefreshRocket();
		}
		return "Launch pad networks and rocket networks have been refreshed.";
	}

	private string RelinkRocket(string[] args)
	{
		if (!GameManager.RunSimulation)
		{
			ConsoleWindow.PrintError("Command only available on server.", suppressStacktrace: true);
			return null;
		}
		if (args.Length < 2 || !long.TryParse(args[1], out var result))
		{
			ConsoleWindow.PrintError("Usage: rocket relink <referenceId> [apply]", suppressStacktrace: true);
			return null;
		}
		bool flag = args.Length == 3 && args[2].Equals("apply", StringComparison.OrdinalIgnoreCase);
		if (args.Length == 3 && !flag)
		{
			ConsoleWindow.PrintError("Unknown relink option '" + args[2] + "'. Use 'apply' to perform the relink.", suppressStacktrace: true);
			return null;
		}
		Rocket rocket = Referencable.Find<Rocket>(result);
		if (rocket?.RocketNetwork == null)
		{
			ConsoleWindow.PrintError("Could not find rocket with reference id " + StringManager.Get(result) + ".", suppressStacktrace: true);
			return null;
		}
		RocketRelinkPlan plan = RocketRelinker.Compute(rocket.RocketNetwork);
		switch (plan.Status)
		{
		case RocketRelinkStatus.NoHull:
			ConsoleWindow.PrintError("Rocket '" + rocket.DisplayName + "' has no fuselage cells to relink into (no hull?).", suppressStacktrace: true);
			return null;
		case RocketRelinkStatus.NoOrphans:
			ConsoleWindow.PrintAction("No orphaned rocket internals found. '" + rocket.DisplayName + "' appears to be coupled already.");
			return null;
		case RocketRelinkStatus.NoAlignment:
			ConsoleWindow.PrintError($"Found {plan.AnchorCount} orphaned internal(s) but none align with '{rocket.DisplayName}'s interior. Not its parts, or geometry differs.", suppressStacktrace: true);
			return null;
		case RocketRelinkStatus.AlreadyInPlace:
			ConsoleWindow.PrintAction("'" + rocket.DisplayName + "' internals are already in place (translation is zero). Try 'rocket refresh'.");
			return null;
		default:
			PrintRelinkPlan(rocket, plan);
			switch (plan.Status)
			{
			case RocketRelinkStatus.NothingToMove:
				ConsoleWindow.PrintError("Nothing found to move at the source cells - cannot relink.", suppressStacktrace: true);
				return null;
			case RocketRelinkStatus.TargetsOccupied:
				ConsoleWindow.PrintError($"{plan.OccupiedTargets} target interior cell(s) are already occupied. Aborting to avoid overlapping structures.", suppressStacktrace: true);
				return null;
			default:
				if (!flag)
				{
					ConsoleWindow.PrintAction("Preview only. Re-run with 'rocket relink " + StringManager.Get(result) + " apply' to perform the move.");
					return null;
				}
				RocketRelinker.Apply(rocket.RocketNetwork, plan);
				ConsoleWindow.PrintAction($"Relinked '{rocket.DisplayName}': moved {plan.MoveSet.Count} structure(s) into the hull and refreshed the network. Run 'rocket print {StringManager.Get(result)}' to verify.");
				return null;
			}
		}
	}

	private static void PrintRelinkPlan(Rocket rocket, RocketRelinkPlan plan)
	{
		TreeString treeString = TreeString.Node("Relink plan for '" + rocket.DisplayName + "' #" + StringManager.Get(rocket.ReferenceId));
		TreeString.Variable($"Translation: ({plan.Translation.x:0.##}, {plan.Translation.y:0.##}, {plan.Translation.z:0.##})", treeString);
		TreeString.Variable($"Anchors matched: {plan.AnchorsMatched}/{plan.AnchorCount}", treeString);
		TreeString.Variable($"Cluster coverage (type-compatible cells): {plan.Coverage}", treeString);
		TreeString.Variable($"Target cells already occupied: {plan.OccupiedTargets}", treeString);
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (Structure item in plan.MoveSet)
		{
			string name = item.GetType().Name;
			dictionary[name] = ((!dictionary.TryGetValue(name, out var value)) ? 1 : (value + 1));
		}
		TreeString myParent = TreeString.Node($"Structures to move ({plan.MoveSet.Count})", treeString);
		foreach (KeyValuePair<string, int> item2 in dictionary)
		{
			TreeString.Node($"{item2.Key}: {item2.Value}", myParent);
		}
		treeString.ToConsole();
	}

	private string PrintLaunchMounts()
	{
		ConsoleWindow.Print("Printing launch mounts...");
		int num = 0;
		foreach (SpaceMapNode allSpaceMapNode in SpaceMapNode.AllSpaceMapNodes)
		{
			if (allSpaceMapNode?.Owner is LaunchMount launchMount)
			{
				num++;
				bool flag = allSpaceMapNode.RocketsHere != null && allSpaceMapNode.RocketsHere.Count > 0;
				string text = (launchMount.IsOrbital ? "Orbital" : "Ground");
				ConsoleWindow.Print(launchMount.DisplayName + " Id: " + StringManager.Get(launchMount.ReferenceId) + " [" + text + "]" + (flag ? " (occupied)" : string.Empty));
			}
		}
		if (num == 0)
		{
			ConsoleWindow.Print("No launch mounts found.");
		}
		return null;
	}

	private string PrintRocketParks()
	{
		if (!GameManager.RunSimulation)
		{
			ConsoleWindow.PrintError("Command only available on server.", suppressStacktrace: true);
			return null;
		}
		List<RocketParkSlot> slots = RocketParkSlot.Slots;
		if (slots == null || slots.Count == 0)
		{
			ConsoleWindow.PrintError("Rocket park is not initialised (no slots).", suppressStacktrace: true);
			return null;
		}
		int[] ownCount = new int[slots.Count];
		List<IRocketInternals>[] orphanHits = new List<IRocketInternals>[slots.Count];
		List<IRocketInternals>[] foreignHits = new List<IRocketInternals>[slots.Count];
		for (int i = 0; i < slots.Count; i++)
		{
			orphanHits[i] = new List<IRocketInternals>();
			foreignHits[i] = new List<IRocketInternals>();
		}
		List<IRocketInternals> list = new List<IRocketInternals>();
		DensePool<Thing>.ActiveEnumerable.Enumerator enumerator = OcclusionManager.AllThings.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			Thing current = enumerator.Current;
			if (!(current is IRocketInternals rocketInternals))
			{
				continue;
			}
			bool flag = rocketInternals.StrictlyInternal && rocketInternals.RocketNetwork == null;
			int num = FindParkSlotIndex(slots, current.Transform.position);
			if (num < 0)
			{
				if (flag)
				{
					list.Add(rocketInternals);
				}
				continue;
			}
			RocketNetwork rocketNetwork = slots[num].AssignedRocket?.RocketNetwork;
			if (flag)
			{
				orphanHits[num].Add(rocketInternals);
			}
			else if (rocketInternals.RocketNetwork != null && rocketInternals.RocketNetwork != rocketNetwork)
			{
				foreignHits[num].Add(rocketInternals);
			}
			else if (rocketNetwork != null && rocketInternals.RocketNetwork == rocketNetwork)
			{
				ownCount[num]++;
			}
		}
		int num2 = 0;
		int num3 = 0;
		List<int> list2 = new List<int>();
		int maxName = 0;
		for (int j = 0; j < slots.Count; j++)
		{
			Rocket assignedRocket = slots[j].AssignedRocket;
			if (assignedRocket != null)
			{
				num2++;
				maxName = Mathf.Max(maxName, (assignedRocket.DisplayName + " #" + StringManager.Get(assignedRocket.ReferenceId)).Length);
			}
			else if (orphanHits[j].Count + foreignHits[j].Count > 0)
			{
				list2.Add(j);
			}
			else
			{
				num3++;
			}
		}
		TreeString treeString = TreeString.Node($"Rocket parks: {slots.Count} ({num2} occupied, {slots.Count - num2} free)");
		TreeString parent = TreeString.Node($"Occupied ({num2})", treeString);
		for (int k = 0; k < slots.Count; k++)
		{
			if (slots[k].AssignedRocket != null)
			{
				AddParkNode(parent, k);
			}
		}
		if (list2.Count > 0)
		{
			TreeString parent2 = TreeString.Node($"Junk in free parks ({list2.Count})", treeString);
			foreach (int item in list2)
			{
				AddParkNode(parent2, item);
			}
		}
		TreeString.Variable($"Free & empty: {num3}", treeString);
		if (list.Count > 0)
		{
			TreeString myParent = TreeString.Node($"Loose orphans ({list.Count})", treeString);
			foreach (IRocketInternals item2 in list)
			{
				TreeString.Node(FormatInternal(item2), myParent);
			}
		}
		treeString.ToConsole();
		return null;
		void AddParkNode(TreeString myParent3, int num4)
		{
			RocketParkSlot rocketParkSlot = slots[num4];
			Vector3 worldPosition = rocketParkSlot.GetWorldPosition();
			TreeString myParent2 = TreeString.Node($"Park ({rocketParkSlot.Location.x},{rocketParkSlot.Location.y}) @ ({worldPosition.x:0},{worldPosition.y:0},{worldPosition.z:0})", myParent3);
			Rocket assignedRocket2 = rocketParkSlot.AssignedRocket;
			TreeString myParent4;
			if (assignedRocket2 != null)
			{
				string text = (assignedRocket2.DisplayName + " #" + StringManager.Get(assignedRocket2.ReferenceId)).PadRight(maxName);
				string text2 = $"[{assignedRocket2.RocketState}]".PadRight(15);
				string text3 = $"own:{ownCount[num4],3}  foreign:{foreignHits[num4].Count,2}  orphan:{orphanHits[num4].Count,2}";
				myParent4 = TreeString.Node(text + "  " + text2 + "  " + text3, myParent2);
			}
			else
			{
				myParent4 = TreeString.Node("FREE", myParent2);
			}
			foreach (IRocketInternals item3 in orphanHits[num4])
			{
				TreeString.Node("ORPHAN " + FormatInternal(item3), myParent4);
			}
			foreach (IRocketInternals item4 in foreignHits[num4])
			{
				TreeString.Node("FOREIGN " + FormatInternal(item4) + " -> network #" + StringManager.Get(item4.RocketNetwork.ReferenceId), myParent4);
			}
		}
	}

	private static int FindParkSlotIndex(List<RocketParkSlot> slots, Vector3 pos)
	{
		for (int i = 0; i < slots.Count; i++)
		{
			Vector3 worldPosition = slots[i].GetWorldPosition();
			if (!(pos.y < worldPosition.y - 12f) && !(pos.y > worldPosition.y + 80f) && !(Mathf.Abs(pos.x - worldPosition.x) > 24f) && !(Mathf.Abs(pos.z - worldPosition.z) > 24f))
			{
				return i;
			}
		}
		return -1;
	}

	private static string FormatInternal(IRocketInternals internals)
	{
		if (!(internals is Thing thing))
		{
			return "<" + internals.GetType().Name + ">";
		}
		Vector3 position = thing.Transform.position;
		return $"{thing.DisplayName} #{StringManager.Get(thing.ReferenceId)} [{internals.GetType().Name}] @ ({position.x:0},{position.y:0},{position.z:0})";
	}

	private string CloneRocket(string[] args)
	{
		if (!AllowArgument())
		{
			return null;
		}
		if (args.Length != 3 || !long.TryParse(args[1], out var result) || !long.TryParse(args[2], out var result2))
		{
			ConsoleWindow.PrintError("Usage: rocket clone <rocketId> <launchMountReferenceId>", suppressStacktrace: true);
			return null;
		}
		Rocket rocket = Referencable.Find<Rocket>(result);
		if (rocket?.RocketNetwork == null)
		{
			ConsoleWindow.PrintError("Could not find rocket with reference id " + StringManager.Get(result) + ".", suppressStacktrace: true);
			return null;
		}
		LaunchMount launchMount = Referencable.Find<LaunchMount>(result2);
		if (launchMount == null)
		{
			ConsoleWindow.PrintError("Could not find launch mount with reference id " + StringManager.Get(result2) + ". Use 'rocket mounts' to list them.", suppressStacktrace: true);
			return null;
		}
		RocketCloneResult rocketCloneResult = RocketCloner.Clone(rocket, launchMount);
		switch (rocketCloneResult.Status)
		{
		case RocketCloneStatus.NoEngineFuselage:
			ConsoleWindow.PrintError("Rocket '" + rocket.DisplayName + "' has no engine fuselage to use as a base. Cannot clone.", suppressStacktrace: true);
			return null;
		case RocketCloneStatus.NothingToClone:
			ConsoleWindow.PrintError("Rocket '" + rocket.DisplayName + "' has no structures to clone.", suppressStacktrace: true);
			return null;
		case RocketCloneStatus.TargetOccupied:
			ConsoleWindow.PrintError($"Cannot clone onto '{launchMount.DisplayName}': {rocketCloneResult.OccupiedTargets} target cell(s) are already occupied. Clear the launch mount first.", suppressStacktrace: true);
			return null;
		case RocketCloneStatus.Success:
			ConsoleWindow.PrintAction($"Cloned '{rocket.DisplayName}' onto '{launchMount.DisplayName}' as '{rocketCloneResult.Clone?.DisplayName}' ({rocketCloneResult.Placed} structure(s) placed). No items, atmospheres or power were copied.");
			return null;
		default:
			ConsoleWindow.PrintError("Could not clone rocket '" + rocket.DisplayName + "'.", suppressStacktrace: true);
			return null;
		}
	}
}
