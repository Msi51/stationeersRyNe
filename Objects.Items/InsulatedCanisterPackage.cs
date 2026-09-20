using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

namespace Objects.Items;

public class InsulatedCanisterPackage : DisposableCardboardBox
{
	protected override CanEnterResult CanEnterCarboardBox(Slot destinationSlot)
	{
		return CanEnterResult.Succeed;
	}
}
