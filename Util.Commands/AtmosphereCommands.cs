using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.UI.ImGuiUi;

namespace Util.Commands;

public class AtmosphereCommands : CommandBase
{
	public override string HelpText => "Toggles an atmosphere debug overlay (or prints atmosphere counts). Dev tool.";

	public override string[] Arguments => new string[1] { "<pipe | world | direction | room | global | thing | cleanup | count | liquid>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("atmosphere"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (!CommandBase.Get(args, 0, "type", out string result))
		{
			return null;
		}
		switch (result.ToLower())
		{
		case "pipe":
			ImGuiAtmosphericDebug.PipeNetworkDebugOverlay = !ImGuiAtmosphericDebug.PipeNetworkDebugOverlay;
			break;
		case "landingpad":
			ImGuiAtmosphericDebug.LandingPadNetworkDebugOverlay = !ImGuiAtmosphericDebug.LandingPadNetworkDebugOverlay;
			break;
		case "world":
			ImGuiAtmosphericDebug.DebugWorldAtmosphereOverlay = !ImGuiAtmosphericDebug.DebugWorldAtmosphereOverlay;
			break;
		case "liquid":
			ImGuiAtmosphericDebug.DebugLiquidAtmosphereOverlay = !ImGuiAtmosphericDebug.DebugLiquidAtmosphereOverlay;
			break;
		case "direction":
			ImGuiAtmosphericDebug.DebugWorldAtmosphereDirectionOverlay = !ImGuiAtmosphericDebug.DebugWorldAtmosphereDirectionOverlay;
			break;
		case "room":
			ImGuiAtmosphericDebug.DebugRoomAtmosphereOverlay = !ImGuiAtmosphericDebug.DebugRoomAtmosphereOverlay;
			break;
		case "global":
			PlanetaryAtmosphereSimulation.DrawGlobalDebug = !PlanetaryAtmosphereSimulation.DrawGlobalDebug;
			break;
		case "thing":
			ImGuiAtmosphericDebug.DebugThingAtmosphereOverlay = !ImGuiAtmosphericDebug.DebugThingAtmosphereOverlay;
			break;
		case "count":
			ConsoleWindow.Print($"Total Atmosphere count: {AtmosphericsManager.AllAtmospheres.ActiveCount}");
			ConsoleWindow.Print($"Total Rooms: {RoomController.World.Rooms.Count}");
			break;
		}
		return null;
	}
}
