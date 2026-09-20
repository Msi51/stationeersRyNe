using Assets.Scripts.Objects.Motherboards;
using Trading;

namespace Assets.Scripts.Objects.Pipes;

public interface ISlotWriteable : ILogicable, IReferencable, IEvaluable
{
	bool CanLogicWrite(LogicSlotType logicType, int slotId);

	void SetLogicValue(LogicSlotType logicType, int slotId, double value);
}
