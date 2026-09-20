using Assets.Scripts;
using UI.ImGuiUi;

namespace Util.Commands;

public class StructureNetworkCommand : CommandBase
{
	public override string HelpText => "Toggles the ImGui debug overlay for chute or rocket networks.";

	public override string[] Arguments => new string[1] { "<chute | rocket>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("structurenetwork"))
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
		string text = result.ToLower();
		if (!(text == "chute"))
		{
			if (text == "rocket")
			{
				ImGuiStructureNetworkDebug.DebugRocketNetworks = !ImGuiStructureNetworkDebug.DebugRocketNetworks;
				return $"Rocket network debug: {ImGuiStructureNetworkDebug.DebugRocketNetworks}.";
			}
			ConsoleWindow.PrintError("Unknown network type '" + result + "'.", suppressStacktrace: true);
			return null;
		}
		ImGuiStructureNetworkDebug.DebugChuteNetworks = !ImGuiStructureNetworkDebug.DebugChuteNetworks;
		return $"Chute network debug: {ImGuiStructureNetworkDebug.DebugChuteNetworks}.";
	}
}
