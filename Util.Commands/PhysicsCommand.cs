using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Util.Commands;

public class PhysicsCommand : CommandBase
{
	public override string HelpText => "Prints rigidbody/physics state for the local player character, or for the dynamic thing identified by the supplied reference id. Optionally overrides the rigidbody interpolation mode for debugging, e.g. 'physics interpolation Interpolate' or 'physics <referenceId> interpolation None'.";

	public override string[] Arguments => new string[2] { "[referenceId]", "[interpolation <None|Interpolate|Extrapolate>]" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("physics"))
		{
			return null;
		}
		DynamicThing dynamicThing = InventoryManager.ParentHuman;
		int num = 0;
		if (args.Length != 0 && long.TryParse(args[0], out var result))
		{
			dynamicThing = Thing.Find<DynamicThing>(result);
			if ((object)dynamicThing == null)
			{
				ConsoleWindow.PrintError($"Dynamic thing with id '{result}' not found.", suppressStacktrace: true);
				return null;
			}
			num = 1;
		}
		if ((object)dynamicThing == null)
		{
			ConsoleWindow.PrintError("Physics command requires an argument or player character.", suppressStacktrace: true);
			return null;
		}
		if (args.Length > num)
		{
			return ApplyOverride(dynamicThing, args, num);
		}
		TreeString treeString = new TreeString($"Rigidbody {dynamicThing.DisplayName} (#{dynamicThing.ReferenceId})");
		TreeString.Variable($"Interpolation:  {dynamicThing.ActiveRigidbody.interpolation}", treeString);
		TreeString.Variable($"Kinematic:      {dynamicThing.ActiveRigidbody.isKinematic}", treeString);
		TreeString.Variable($"Sleeping:       {dynamicThing.ActiveRigidbody.IsSleeping()}", treeString);
		TreeString.Variable($"useGravity:     {dynamicThing.ActiveRigidbody.useGravity}", treeString);
		TreeString.Variable($"collisionMode:  {dynamicThing.ActiveRigidbody.collisionDetectionMode}", treeString);
		TreeString.Variable($"Mass:           {dynamicThing.ActiveRigidbody.mass}", treeString);
		TreeString.Variable($"Drag:           {dynamicThing.ActiveRigidbody.drag}", treeString);
		TreeString.Variable($"Angular Drag:   {dynamicThing.ActiveRigidbody.angularDrag}", treeString);
		treeString.ToConsole();
		return null;
	}

	private static string ApplyOverride(DynamicThing thing, string[] args, int argIndex)
	{
		string text = args[argIndex];
		if (!text.Equals("interpolation", StringComparison.OrdinalIgnoreCase))
		{
			ConsoleWindow.PrintError("'" + text + "' is not a settable physics property. Supported: interpolation.", suppressStacktrace: true);
			return null;
		}
		if (args.Length <= argIndex + 1)
		{
			ConsoleWindow.PrintError("'interpolation' requires a value: " + string.Join(", ", Enum.GetNames(typeof(RigidbodyInterpolation))) + ".", suppressStacktrace: true);
			return null;
		}
		if (!Enum.TryParse<RigidbodyInterpolation>(args[argIndex + 1], ignoreCase: true, out var result))
		{
			ConsoleWindow.PrintError("'" + args[argIndex + 1] + "' is not a valid interpolation mode. Supported: " + string.Join(", ", Enum.GetNames(typeof(RigidbodyInterpolation))) + ".", suppressStacktrace: true);
			return null;
		}
		thing.ActiveRigidbody.interpolation = result;
		ConsoleWindow.PrintAction($"Set interpolation of '{thing.DisplayName}' (#{thing.ReferenceId}) to '{result}'.");
		return null;
	}

	public override IEnumerable<string> GetCompletions(int argIndex, string prefix)
	{
		List<string> list = new List<string>();
		if (argIndex <= 1)
		{
			list.Add("interpolation");
		}
		if (argIndex >= 1)
		{
			list.AddRange(Enum.GetNames(typeof(RigidbodyInterpolation)));
		}
		return list;
	}
}
