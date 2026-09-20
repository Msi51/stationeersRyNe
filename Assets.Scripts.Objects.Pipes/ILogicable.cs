using Assets.Scripts.Objects.Motherboards;
using Trading;

namespace Assets.Scripts.Objects.Pipes;

public interface ILogicable : IReferencable, IEvaluable
{
	int TotalSlots { get; }

	Thing GetAsThing { get; }

	bool HasAnySlots { get; }

	Slot GetSlot(int slotIndex);

	int GetPrefabHash();

	int GetNextSlotId(int slotIndex, bool isForward);

	string ToTooltip();

	bool IsLogicSlotReadable();

	bool IsLogicReadable();

	bool IsLogicWritable();

	bool CanLogicRead(LogicType logicType);

	bool CanLogicWrite(LogicType logicType);

	void SetLogicValue(LogicType logicType, double value);

	double GetLogicValue(LogicType logicType);

	bool CanLogicRead(LogicSlotType logicSlotType, int slotId);

	double GetLogicValue(LogicSlotType logicSlotType, int slotId);

	int GetNameHash();
}
