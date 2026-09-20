using System.Text;
using Trading;

namespace Assets.Scripts.Objects.Items;

public interface ISolidFuel : IQuantity, ITradable, IEvaluable, IReferencable
{
	Slot ParentSlot { get; }

	float GetEnergyPerSecond();

	StringBuilder GetSlotTooltip();
}
