using System;
using Assets.Scripts.UI.ImGuiUi;

namespace Util.Commands;

public class ImGuiCommands : CommandBase
{
	public override string HelpText => "Toggles the in-world ImGui test cube on or off.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override bool Hidden => true;

	public override string Execute(string[] args)
	{
		ImGuiInWorldManager.ImguiInWorldTestCube = !ImGuiInWorldManager.ImguiInWorldTestCube;
		return $"ImGuiInWorldManager.ImguiInWorldTestCube: {ImGuiInWorldManager.ImguiInWorldTestCube}";
	}
}
