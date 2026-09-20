using System;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class OccupancySensor : Sensor, IDoorControl, IMemoryReadable, IMemory, IInstructable, ILogicTick, ILogicStack
{
	private LogicStack _stack = new LogicStack(18);

	public override bool IsTriggered => Activate > 0;

	public int GetStackSize()
	{
		return _stack.Size;
	}

	public LogicStack GetLogicStack()
	{
		return _stack;
	}

	public double ReadMemory(int address)
	{
		return _stack[address];
	}

	public IEnumCollection GetInstructions()
	{
		return EnumCollections.InventoryInstructions;
	}

	public string GetInstructionDescription(int i)
	{
		return EnumCollections.InventoryInstructions[i] switch
		{
			OccupancyInstruction.Entity => LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Prefab_Hash", typeof(uint))), 
			OccupancyInstruction.Inventory => LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Slot_Index", typeof(byte)), new LogicStack.InstructionFormat("Prefab_Hash", typeof(uint))), 
			_ => throw new NotImplementedException(), 
		};
	}

	public void OnLogicTick()
	{
		if (!IsOperable)
		{
			return;
		}
		Room room = RoomController.World.GetRoom(base.WorldGrid);
		if (room == null)
		{
			_stack.Clear();
			return;
		}
		_stack.Clear();
		int num = 0;
		for (int num2 = Human.AllHumans.Count - 1; num2 >= 0; num2--)
		{
			Human human = Human.AllHumans[num2];
			if ((bool)human && human.Room == room)
			{
				byte opcode = 1;
				_stack[num++] = LogicStack.PackInt32(opcode, human.PrefabHash);
				if (num >= _stack.Size)
				{
					break;
				}
				for (int i = 0; i < human.Slots.Count; i++)
				{
					if (human.Slots[i].Contains<DynamicThing>(out var occupant))
					{
						opcode = 2;
						_stack[num++] = LogicStack.PackByteInt32(opcode, (byte)i, occupant.PrefabHash);
						if (num >= _stack.Size)
						{
							return;
						}
					}
				}
			}
		}
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		if (!GameManager.RunSimulation)
		{
			return;
		}
		int num = 0;
		Room room = RoomController.World.GetRoom(base.WorldGrid);
		if (room != null)
		{
			foreach (Human allHuman in Human.AllHumans)
			{
				if (allHuman.Room == room && IsAuthorized(allHuman))
				{
					num++;
				}
			}
		}
		if (Activate != num)
		{
			OnServer.Interact(base.InteractActivate, num);
		}
	}

	public override void SetMotherboards(bool isTriggered)
	{
		foreach (Motherboard linkedMotherboard in LinkedMotherboards)
		{
			if (linkedMotherboard is Circuitboard circuitboard && circuitboard.ParentComputer.AsDevice().Powered)
			{
				circuitboard.RemoteToggle(isTriggered);
			}
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		if (!(MaterialChanger == null))
		{
			MaterialChanger.ChangeState((Activate > 0) ? Defines.Animator.On : Defines.Animator.Off);
		}
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Activate)
		{
			return false;
		}
		return base.CanLogicWrite(logicType);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Quantity)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Quantity)
		{
			return Activate;
		}
		return base.GetLogicValue(logicType);
	}
}
