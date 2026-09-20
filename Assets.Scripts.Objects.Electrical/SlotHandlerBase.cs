using System;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class SlotHandlerBase : DeviceImportExport
{
	private static string[] _modeStrings = Enum.GetNames(typeof(SlotHandlerMode));

	public int CurrentOutput;

	public override string[] ModeStrings => _modeStrings;

	public SlotHandlerMode SlotHandlerMode => (SlotHandlerMode)Mode;

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new SlotHandlerBaseSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is SlotHandlerBaseSaveData slotHandlerBaseSaveData)
		{
			CurrentOutput = slotHandlerBaseSaveData.CurrentOutput;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is SlotHandlerBaseSaveData slotHandlerBaseSaveData)
		{
			slotHandlerBaseSaveData.CurrentOutput = CurrentOutput;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Output)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Output)
		{
			return CurrentOutput;
		}
		return base.GetLogicValue(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Output)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Output)
		{
			CurrentOutput = (int)value.Clamp(-1.0, 1.0);
		}
		base.SetLogicValue(logicType, value);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Mode)
		{
			SlotHandlerMode state = (SlotHandlerMode)interactable.State;
			if (state != SlotHandlerMode.Automatic && state == SlotHandlerMode.Logic)
			{
				CurrentOutput = -1;
			}
		}
	}
}
