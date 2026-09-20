using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Objects.Rockets;
using UI.ImGuiUi;
using UI.ImGuiUi.ImGuiWindows;
using UnityEngine;

namespace Util.Commands;

public class LowOrbitStationCommand : CommandBase
{
	public override string HelpText => "Provides low-orbit station debug helpers: spawning an orbital launch mount, and teleporting the local player into the low-orbit playable area.";

	public override string[] Arguments => new string[4] { "spawnmount [positionX] [positionY] [positionZ]", "goto [positionX] [positionY] [positionZ]", "view <on|off>", "tune" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("loworbitstation"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		return args[0] switch
		{
			"spawnmount" => HandleSpawnMount(args), 
			"goto" => HandleGoto(args), 
			"view" => HandleView(args), 
			"tune" => HandleTune(), 
			_ => "Invalid syntax", 
		};
	}

	private string HandleTune()
	{
		ConsoleWindow.Hide();
		if (ImGuiOrbitalViewWindow.Window.IsShowing)
		{
			ImGuiWindowManager.Close(ImGuiOrbitalViewWindow.Window);
			return "Orbital view tuning window closed.";
		}
		ImGuiWindowManager.Open(ImGuiOrbitalViewWindow.Window);
		return "Orbital view tuning window open.";
	}

	private string HandleView(string[] args)
	{
		if (args.Length < 2)
		{
			return "Invalid syntax";
		}
		string text = args[1];
		if (!(text == "on"))
		{
			if (text == "off")
			{
				OrbitalViewController.Enabled = false;
				return "Orbital view disabled (all overrides restored).";
			}
			return "Invalid syntax";
		}
		OrbitalViewController.Enabled = true;
		return "Orbital view enabled.";
	}

	private string HandleGoto(string[] args)
	{
		if (GameManager.IsBatchMode)
		{
			ConsoleWindow.PrintError("loworbitstation goto is not available in batch mode", suppressStacktrace: true);
			return null;
		}
		Vector3 vector = Rocket.LowOrbitPlayableBounds.center;
		if (args.Length >= 4)
		{
			if (!CommandBase.Get(args, 1, "positionX", out float result))
			{
				return null;
			}
			if (!CommandBase.Get(args, 2, "positionY", out float result2))
			{
				return null;
			}
			if (!CommandBase.Get(args, 3, "positionZ", out float result3))
			{
				return null;
			}
			vector = new Vector3(result, result2, result3);
		}
		if (!Rocket.LowOrbitPlayableBounds.Contains(vector))
		{
			ConsoleWindow.PrintError("Position must be within low orbit playable bounds.", suppressStacktrace: true);
			return null;
		}
		Entity parent = InventoryManager.Parent;
		if (parent == null)
		{
			ConsoleWindow.PrintError("no local player to teleport", suppressStacktrace: true);
			return null;
		}
		parent.Transform.position = vector;
		if (parent.RigidBody != null && !parent.RigidBody.isKinematic)
		{
			parent.RigidBody.velocity = Vector3.zero;
			parent.RigidBody.angularVelocity = Vector3.zero;
		}
		return $"Teleported {parent.DisplayName} to {vector}.";
	}

	private string HandleSpawnMount(string[] args)
	{
		Vector3 position;
		if (args.Length >= 4)
		{
			if (!CommandBase.Get(args, 1, "positionX", out float result))
			{
				return null;
			}
			if (!CommandBase.Get(args, 2, "positionY", out float result2))
			{
				return null;
			}
			if (!CommandBase.Get(args, 3, "positionZ", out float result3))
			{
				return null;
			}
			Vector3 vector = new Vector3(result, result2, result3);
			if (!Rocket.LowOrbitPlayableBounds.Contains(vector))
			{
				ConsoleWindow.PrintError("Position must be within low orbit playable bounds.", suppressStacktrace: true);
				return null;
			}
			position = vector.GridCenter();
		}
		else if (!OrbitalLaunchMountPayload.TryGetFreeDeployPosition(out position))
		{
			ConsoleWindow.PrintError("No free deploy position available in the low orbit playable bounds.", suppressStacktrace: true);
			return null;
		}
		Thing.Create<LaunchMount>(Animator.StringToHash("StructureLaunchMountOrbital"), position, Quaternion.identity, 0L);
		return $"Created orbital launch mount at {position}.";
	}
}
