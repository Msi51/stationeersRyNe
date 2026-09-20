using Assets.Scripts.Localization2;
using Trading;

namespace Assets.Scripts.Objects.Structures;

public class CrateMount : MountedSmallGrid, IContainerMount, IFastenedConnector, IReferencable, IEvaluable
{
	public Slot ContainerSlot => Slots[0];

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}
}
