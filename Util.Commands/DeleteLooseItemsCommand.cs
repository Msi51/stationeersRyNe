using System;
using Assets.Scripts;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Util.Commands;

internal class DeleteLooseItemsCommand : CommandBase
{
	private static readonly Action<DynamicThing> DeleteIfLooseItem = delegate(DynamicThing dynamicThing)
	{
		if (dynamicThing is Item item && dynamicThing.ParentSlot == null)
		{
			ConsoleWindow.PrintAction("Deleting " + item.DisplayName + "...");
			UnityEngine.Object.Destroy(item.gameObject);
		}
	};

	public override string HelpText => "Removes every dynamic item in the world that is not in a slot. Host or singleplayer only.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("deletelooseitems"))
		{
			return null;
		}
		OcclusionManager.AllDynamicThings.ForEach(DeleteIfLooseItem);
		ConsoleWindow.PrintAction("Deleted loose items.");
		return null;
	}
}
