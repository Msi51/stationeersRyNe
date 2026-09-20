using Assets.Scripts.Inventory;
using Trading;

namespace Assets.Scripts.Objects.Items;

public class AuthoringTool : Tool, IConstructionStarter, IReferencable, IEvaluable
{
	public override bool UseDefaultUiUsingSounds()
	{
		return true;
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		InventoryManager.Instance.CancelPlacement();
	}
}
