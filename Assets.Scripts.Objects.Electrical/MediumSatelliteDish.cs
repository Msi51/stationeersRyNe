using System;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Trading;

namespace Assets.Scripts.Objects.Electrical;

public class MediumSatelliteDish : SatelliteDish, IMemoryReadable, IMemory, IMemoryWritable, IInstructable, ILogicTick, ILogicStack
{
	private const int STACK_SIZE = 32;

	private readonly LogicStack _stack = new LogicStack(32);

	private const string CONDITION_OPERATION = "Condition_Operation";

	public int GetStackSize()
	{
		return 32;
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

	public IEnumCollection GetInstructions()
	{
		return EnumCollections.TraderInstructions;
	}

	public void OnLogicTick()
	{
		if (!IsOperable)
		{
			return;
		}
		for (int i = 0; i < _stack.Size; i++)
		{
			TraderInstruction opcode = (TraderInstruction)new StackAddress(i, _stack[i]).Opcode;
			if (opcode == TraderInstruction.TraderBuyThingData || opcode == TraderInstruction.TraderSellThingData)
			{
				_stack.Clear(i);
			}
		}
		int lastWrite = 0;
		for (int j = 0; j < _stack.Size; j++)
		{
			StackAddress memory = new StackAddress(j, _stack[j]);
			switch ((TraderInstruction)memory.Opcode)
			{
			case TraderInstruction.WriteTraderData:
				WriteTraderData(memory);
				lastWrite = j;
				break;
			case TraderInstruction.WriteTraderBuyData:
				WriteTraderBuyData(memory, lastWrite);
				lastWrite = j;
				break;
			case TraderInstruction.WriteTraderSellData:
				WriteTraderSellData(memory, lastWrite);
				lastWrite = j;
				break;
			}
		}
	}

	private void WriteTraderBuyData(StackAddress memory, int lastWrite)
	{
		TraderContact strongestContact = GetStrongestContact();
		if (strongestContact == null)
		{
			return;
		}
		(byte, byte, byte) tuple = LogicStack.UnpackByteX2(memory.IntegerValue);
		int begin = tuple.Item2;
		int count = 0;
		byte item = tuple.Item3;
		try
		{
			foreach (BuyDataInstance buyDataInstance in strongestContact.DataInstance.BuyDataInstances)
			{
				DynamicThing dynamicThing = buyDataInstance.BuyingItem?.Prefab;
				byte b = (byte)buyDataInstance.Required;
				if (b == 0)
				{
					continue;
				}
				if (begin >= 32)
				{
					break;
				}
				if ((object)dynamicThing != null)
				{
					if (!IsValidTrade(lastWrite, buyDataInstance.BuyingItem))
					{
						continue;
					}
					_stack.Poke(ref begin, LogicStack.PackByteInt32(7, b, dynamicThing.PrefabHash));
					byte childCount = 0;
					int lastChildHash = 0;
					foreach (ConditionData condition in buyDataInstance.BuyingItem.Conditions)
					{
						if (condition is ChildItemPrefabCondition childItemPrefabCondition && IsValidTrade(lastWrite, childItemPrefabCondition))
						{
							AddChildItem(TraderInstruction.TraderBuyThingChildData, childItemPrefabCondition.PrefabNameHash, ref begin, ref count, ref childCount, ref lastChildHash);
							if (count >= item)
							{
								return;
							}
						}
					}
					count++;
				}
				else if (buyDataInstance.IsGasTransaction())
				{
					uint num = 0u;
					foreach (ConditionData condition2 in buyDataInstance.BuyData.Conditions)
					{
						if (condition2 is IGasTrade gasTrade && IsValidTrade(lastWrite, gasTrade))
						{
							num |= (uint)gasTrade.GetGasType();
						}
					}
					_stack.Poke(ref begin, LogicStack.PackByteUInt32(9, b, num));
					count++;
				}
				if (count >= item)
				{
					break;
				}
			}
		}
		catch
		{
		}
	}

	private bool IsValidTrade(int lastWrite, IGasTrade gasTrade)
	{
		for (int i = lastWrite + 1; i < _stack.Size; i++)
		{
			StackAddress stackAddress = new StackAddress(i, _stack[i]);
			if (!IsValidForMemory(stackAddress, gasTrade))
			{
				return false;
			}
			if (IsWriteInstruction(stackAddress))
			{
				break;
			}
		}
		return true;
	}

	private bool IsValidForMemory(StackAddress writableStackAddress, IGasTrade gasTrade)
	{
		TraderInstruction opcode = (TraderInstruction)writableStackAddress.Opcode;
		if (opcode != TraderInstruction.FilterGasContains && opcode != TraderInstruction.FilterGasNotContains)
		{
			return true;
		}
		bool flag = LogicStack.ContainsFlag(writableStackAddress.IntegerValue, (uint)gasTrade.GetGasType());
		if (opcode != TraderInstruction.FilterGasContains)
		{
			return !flag;
		}
		return flag;
	}

	private void AddChildItem(TraderInstruction instruction, int prefabHash, ref int index, ref int count, ref byte childCount, ref int lastChildHash)
	{
		if (index < 32)
		{
			if (lastChildHash == prefabHash)
			{
				index--;
				childCount++;
			}
			else
			{
				lastChildHash = prefabHash;
				childCount = 1;
			}
			_stack.Poke(ref index, LogicStack.PackByteInt32((byte)instruction, childCount, prefabHash));
			count++;
		}
	}

	private void WriteTraderSellData(StackAddress memory, int lastWrite)
	{
		TraderContact strongestContact = GetStrongestContact();
		if (strongestContact == null)
		{
			return;
		}
		(byte, byte, byte) tuple = LogicStack.UnpackByteX2(memory.IntegerValue);
		int begin = tuple.Item2;
		int count = 0;
		byte item = tuple.Item3;
		try
		{
			foreach (SellDataInstance sellDataInstance in strongestContact.DataInstance.SellDataInstances)
			{
				DynamicThing dynamicThing = sellDataInstance.SellingItem?.Prefab;
				byte b = (byte)sellDataInstance.Stock;
				if (b == 0)
				{
					continue;
				}
				if (begin >= 32)
				{
					break;
				}
				if (!IsValidTrade(lastWrite, sellDataInstance.SellingItem))
				{
					continue;
				}
				if ((object)dynamicThing != null)
				{
					_stack.Poke(ref begin, LogicStack.PackByteInt32(10, b, dynamicThing.PrefabHash));
					byte childCount = 0;
					int lastChildHash = 0;
					foreach (SellItem child in sellDataInstance.SellingItem.Children)
					{
						if ((object)child.Prefab != null && IsValidTrade(lastWrite, child))
						{
							AddChildItem(TraderInstruction.TraderSellThingChildData, child.Prefab.PrefabHash, ref begin, ref count, ref childCount, ref lastChildHash);
							if (count >= item)
							{
								return;
							}
						}
					}
					count++;
				}
				else if (sellDataInstance.IsGasTransaction())
				{
					uint num = 0u;
					foreach (ActionData tradeAction in sellDataInstance.SellData.TradeActions)
					{
						if (tradeAction is IGasTrade gasTrade && IsValidTrade(lastWrite, gasTrade))
						{
							num |= (uint)gasTrade.GetGasType();
						}
					}
					_stack.Poke(ref begin, LogicStack.PackByteUInt32(11, b, num));
					count++;
				}
				if (count >= item)
				{
					break;
				}
			}
		}
		catch
		{
		}
	}

	private void WriteTraderData(StackAddress memory)
	{
		int begin = LogicStack.UnpackByte(memory.IntegerValue).Item2;
		TraderContact strongestContact = GetStrongestContact();
		int value = 0;
		byte @byte = 0;
		byte byte2 = 0;
		byte byte3 = 0;
		ushort value2 = 0;
		ushort value3 = 0;
		if (strongestContact != null)
		{
			value = strongestContact.DataInstance.TraderData.IdHash;
			@byte = (byte)strongestContact.ShuttleType;
			byte2 = (byte)(strongestContact.Contacted ? 1 : 0);
			byte3 = (byte)ContactSlot.ContactSlots.IndexOf(strongestContact.ContactSlot);
			value2 = (ushort)strongestContact.WattsToResolve;
			value3 = (ushort)strongestContact.Lifetime;
		}
		try
		{
			_stack.Poke(ref begin, LogicStack.PackInt32(2, value));
			_stack.Poke(ref begin, LogicStack.PackByteX3(3, @byte, byte3, byte2));
			_stack.Poke(ref begin, LogicStack.PackUInt16X2(4, value2, value3));
		}
		catch
		{
		}
	}

	private bool IsWriteInstruction(StackAddress address)
	{
		TraderInstruction opcode = (TraderInstruction)address.Opcode;
		return opcode == TraderInstruction.WriteTraderBuyData || opcode == TraderInstruction.WriteTraderSellData || opcode == TraderInstruction.WriteTraderData;
	}

	private bool IsValidTrade(int lastWrite, ChildItemPrefabCondition childItem)
	{
		for (int i = lastWrite + 1; i < _stack.Size; i++)
		{
			StackAddress stackAddress = new StackAddress(i, _stack[i]);
			if (!IsValidForMemory(stackAddress, childItem))
			{
				return false;
			}
			if (IsWriteInstruction(stackAddress))
			{
				break;
			}
		}
		return true;
	}

	private bool IsValidTrade(int lastWrite, TradableItem data)
	{
		for (int i = lastWrite + 1; i < _stack.Size; i++)
		{
			StackAddress stackAddress = new StackAddress(i, _stack[i]);
			if (!IsValidForMemory(stackAddress, data))
			{
				return false;
			}
			if (IsWriteInstruction(stackAddress))
			{
				break;
			}
		}
		return true;
	}

	private bool IsValidForMemory(StackAddress writableStackAddress, ChildItemPrefabCondition data)
	{
		TraderInstruction opcode = (TraderInstruction)writableStackAddress.Opcode;
		if (opcode != TraderInstruction.FilterPrefabHashEquals && opcode != TraderInstruction.FilterPrefabHashNotEquals)
		{
			return true;
		}
		long integerValue = writableStackAddress.IntegerValue;
		if (opcode == TraderInstruction.FilterPrefabHashEquals || opcode == TraderInstruction.FilterPrefabHashNotEquals)
		{
			return LogicStack.CompareInt32(integerValue, data.PrefabNameHash, (opcode != TraderInstruction.FilterPrefabHashEquals) ? ConditionOperation.NotEquals : ConditionOperation.Equals);
		}
		return false;
	}

	private bool IsValidForMemory(StackAddress writableStackAddress, TradableItem data)
	{
		TraderInstruction opcode = (TraderInstruction)writableStackAddress.Opcode;
		if (opcode != TraderInstruction.FilterPrefabHashEquals && opcode != TraderInstruction.FilterPrefabHashNotEquals && opcode != TraderInstruction.FilterSortingClassCompare && opcode != TraderInstruction.FilterQuantityCompare)
		{
			return true;
		}
		long integerValue = writableStackAddress.IntegerValue;
		switch (opcode)
		{
		case TraderInstruction.FilterPrefabHashEquals:
		case TraderInstruction.FilterPrefabHashNotEquals:
			return LogicStack.CompareInt32(integerValue, data.Prefab.PrefabHash, (opcode != TraderInstruction.FilterPrefabHashEquals) ? ConditionOperation.NotEquals : ConditionOperation.Equals);
		case TraderInstruction.FilterSortingClassCompare:
			return LogicStack.CompareUInt16(integerValue, (ushort)data.Prefab.SortingClass);
		default:
		{
			ushort right = (ushort)((!(data.Prefab is IQuantity quantity)) ? 1 : ((ushort)quantity.GetQuantity));
			return LogicStack.CompareUInt16(integerValue, right);
		}
		}
	}

	public string GetInstructionDescription(int i)
	{
		switch (EnumCollections.TraderInstructions[i])
		{
		case TraderInstruction.FilterGasContains:
		case TraderInstruction.FilterGasNotContains:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Gas_Types_Bitflag", typeof(uint)));
		case TraderInstruction.TraderBuyGasData:
		case TraderInstruction.TraderSellGasData:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Quantity", typeof(byte)), new LogicStack.InstructionFormat("Gas_Types_Bitflag", typeof(uint)));
		case TraderInstruction.TraderBuyThingData:
		case TraderInstruction.TraderBuyThingChildData:
		case TraderInstruction.TraderSellThingData:
		case TraderInstruction.TraderSellThingChildData:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Quantity", typeof(byte)), new LogicStack.InstructionFormat("Prefab_Hash", typeof(uint)));
		case TraderInstruction.StrongestContactIdHash:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Trader_Id_Hash", typeof(uint)));
		case TraderInstruction.StrongestContactMetaData:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Shuttle_Type", typeof(byte)), new LogicStack.InstructionFormat("Contact_Slot_Index", typeof(byte)), new LogicStack.InstructionFormat("Contacted", typeof(byte)));
		case TraderInstruction.StrongestContactSignalData:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Watts_To_Resolve", typeof(ushort)), new LogicStack.InstructionFormat("Lifetime", typeof(ushort)));
		case TraderInstruction.WriteTraderData:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Write_Index", typeof(byte)));
		case TraderInstruction.WriteTraderBuyData:
		case TraderInstruction.WriteTraderSellData:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Write_Index", typeof(byte)), new LogicStack.InstructionFormat("Write_Count", typeof(byte)));
		case TraderInstruction.FilterPrefabHashEquals:
		case TraderInstruction.FilterPrefabHashNotEquals:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Prefab_Hash", typeof(int)));
		case TraderInstruction.FilterSortingClassCompare:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Condition_Operation", typeof(byte)), new LogicStack.InstructionFormat("Sorting_Class", typeof(ushort)));
		case TraderInstruction.FilterQuantityCompare:
			return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Condition_Operation", typeof(byte)), new LogicStack.InstructionFormat("Quantity", typeof(ushort)));
		default:
			throw new NotImplementedException();
		}
	}
}
