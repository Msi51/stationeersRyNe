using System;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicSorter : DeviceImportExport2, IMemoryReadable, IMemory, IMemoryWritable, IInstructable, ILogicStack
{
	private const int STACK_SIZE = 32;

	private readonly LogicStack _stack = new LogicStack(32);

	private ExecutionLimitStackAddress _stackExecutionLimitStackAddress;

	private const string CONDITION_OPERATION = "Condition_Operation";

	public override string[] ModeStrings => EnumCollections.LogicOperators;

	public override bool CanIceMelt => false;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(40f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public LogicStack GetLogicStack()
	{
		return _stack;
	}

	public double ReadMemory(int address)
	{
		return _stack[address];
	}

	public void WriteMemory(int address, double value)
	{
		_stack[address] = value;
	}

	public void ClearMemory()
	{
		_stack.Clear();
	}

	public override void Awake()
	{
		base.Awake();
		if (!GameManager.IsBatchMode)
		{
			ExportSlot.OnEnter += PlaySort1Sound;
			base.ExportSlot2.OnEnter += PlaySort2Sound;
		}
	}

	private void PlaySort1Sound()
	{
		PlaySound(Sorter.SortSlot1Hash);
		GetAudioEvent(Sorter.ImportHash)?.Stop();
	}

	private void PlaySort2Sound()
	{
		PlaySound(Sorter.SortSlot2Hash);
		GetAudioEvent(Sorter.ImportHash)?.Stop();
	}

	protected override void OnServerImportTick()
	{
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
		if (CanCompleteImport && ImportingThing == null)
		{
			OnServer.Interact(base.InteractImport, 0);
		}
	}

	protected override void OnServerExportTick()
	{
		if (OnOff && Powered)
		{
			TryFilter();
			if (CanBeginExport)
			{
				OnServer.Interact(base.InteractExport, 1);
			}
		}
	}

	protected override void OnServerExport2Tick()
	{
		if (OnOff && Powered)
		{
			if (CanBeginExport2)
			{
				OnServer.Interact(base.InteractExport2, 1);
			}
			base.OnServerExport2Tick();
		}
	}

	private void TryFilter()
	{
		if (!OnOff || !Powered || !CanCompleteImport || ImportingThing == null)
		{
			return;
		}
		if (DoFilter())
		{
			if (base.IsNextExport2Ready)
			{
				OnServer.MoveToSlot(ImportingThing, base.ExportSlot2);
			}
		}
		else if (IsNextExportReady)
		{
			OnServer.MoveToSlot(ImportingThing, ExportSlot);
		}
	}

	private bool SkipInstruction(WritableStackAddress writableStackAddress)
	{
		if (writableStackAddress.Opcode == 0)
		{
			return true;
		}
		if (writableStackAddress.Opcode == 6)
		{
			_stackExecutionLimitStackAddress = new ExecutionLimitStackAddress(writableStackAddress.StackIndex, writableStackAddress.Value);
			return true;
		}
		return false;
	}

	private bool DoFilter()
	{
		switch ((LogicOperator)(byte)Mode)
		{
		case LogicOperator.Any:
		{
			for (int k = 0; k < _stack.Size; k++)
			{
				WritableStackAddress writableStackAddress3 = new WritableStackAddress(k, _stack[k]);
				if (!SkipInstruction(writableStackAddress3) && Execute(IsFilterInstruction(writableStackAddress3)))
				{
					return true;
				}
			}
			return false;
		}
		case LogicOperator.All:
		{
			bool result2 = false;
			for (int j = 0; j < _stack.Size; j++)
			{
				WritableStackAddress writableStackAddress2 = new WritableStackAddress(j, _stack[j]);
				if (!SkipInstruction(writableStackAddress2))
				{
					if (!Execute(IsFilterInstruction(writableStackAddress2)))
					{
						return false;
					}
					result2 = true;
				}
			}
			return result2;
		}
		case LogicOperator.None:
		{
			bool result = false;
			for (int i = 0; i < _stack.Size; i++)
			{
				WritableStackAddress writableStackAddress = new WritableStackAddress(i, _stack[i]);
				if (!SkipInstruction(writableStackAddress))
				{
					if (Execute(IsFilterInstruction(writableStackAddress)))
					{
						return false;
					}
					result = true;
				}
			}
			return result;
		}
		default:
			return false;
		}
	}

	private bool IsFilterInstruction(WritableStackAddress writableStackAddress)
	{
		SorterInstruction opcode = (SorterInstruction)writableStackAddress.Opcode;
		long value = writableStackAddress.Value;
		ExecutionLimitStackAddress stackExecutionLimitStackAddress = _stackExecutionLimitStackAddress;
		if (stackExecutionLimitStackAddress != null && stackExecutionLimitStackAddress.False())
		{
			return false;
		}
		switch (opcode)
		{
		case SorterInstruction.FilterPrefabHashEquals:
		case SorterInstruction.FilterPrefabHashNotEquals:
			return LogicStack.CompareInt32(value, ImportingThing.PrefabHash, (opcode != SorterInstruction.FilterPrefabHashEquals) ? ConditionOperation.NotEquals : ConditionOperation.Equals);
		case SorterInstruction.FilterSortingClassCompare:
			return LogicStack.CompareUInt16(value, (ushort)ImportingThing.SortingClass);
		case SorterInstruction.FilterSlotTypeCompare:
			return LogicStack.CompareUInt16(value, (ushort)ImportingThing.SlotType);
		case SorterInstruction.FilterQuantityCompare:
		{
			ushort right = (ushort)((!(ImportingThing is IQuantity quantity)) ? 1 : ((ushort)quantity.GetQuantity));
			return LogicStack.CompareUInt16(value, right);
		}
		default:
			return false;
		}
	}

	private bool Execute(bool result)
	{
		if (result)
		{
			ExecutionLimitStackAddress stackExecutionLimitStackAddress = _stackExecutionLimitStackAddress;
			if (stackExecutionLimitStackAddress != null && stackExecutionLimitStackAddress.Count != 0)
			{
				_stackExecutionLimitStackAddress.Decrement();
				_stackExecutionLimitStackAddress.Write(_stack);
			}
		}
		_stackExecutionLimitStackAddress = null;
		return result;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!GameManager.IsBatchMode)
		{
			ExportSlot.OnEnter -= PlaySort1Sound;
			base.ExportSlot2.OnEnter -= PlaySort2Sound;
		}
	}

	public IEnumCollection GetInstructions()
	{
		return EnumCollections.SorterInstructions;
	}

	public string GetInstructionDescription(int i)
	{
		switch (EnumCollections.SorterInstructions[i])
		{
		default:
			throw new NotImplementedException();
		case SorterInstruction.FilterPrefabHashEquals:
		case SorterInstruction.FilterPrefabHashNotEquals:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Prefab_Hash", typeof(int)));
		case SorterInstruction.FilterSortingClassCompare:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Condition_Operation", typeof(byte)), new LogicStack.InstructionFormat("Sorting_Class", typeof(ushort)));
		case SorterInstruction.FilterSlotTypeCompare:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Condition_Operation", typeof(byte)), new LogicStack.InstructionFormat("Slot_Type", typeof(ushort)));
		case SorterInstruction.FilterQuantityCompare:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Condition_Operation", typeof(byte)), new LogicStack.InstructionFormat("Quantity", typeof(ushort)));
		case SorterInstruction.LimitNextExecutionByCount:
			return LogicStack.LimitNextExecutionByCount;
		}
	}

	public int GetStackSize()
	{
		return 32;
	}
}
