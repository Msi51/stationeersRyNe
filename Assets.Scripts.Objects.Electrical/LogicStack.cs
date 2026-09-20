using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Objects.Electrical;

public class LogicStack
{
	public readonly struct InstructionFormat
	{
		public readonly Type Type;

		public readonly string Description;

		private readonly int _explicitSize;

		private readonly string _explicitTypeName;

		public int Size
		{
			get
			{
				if (!(Type != null))
				{
					return _explicitSize;
				}
				return GetBitSize(Type);
			}
		}

		public string TypeName
		{
			get
			{
				if (!(Type != null))
				{
					return _explicitTypeName;
				}
				return GetTypeName(Type);
			}
		}

		public InstructionFormat(string description, Type type)
		{
			Type = type;
			Description = description;
			_explicitSize = 0;
			_explicitTypeName = null;
		}

		public InstructionFormat(string description, int bitSize, string typeName)
		{
			Type = null;
			Description = description;
			_explicitSize = bitSize;
			_explicitTypeName = typeName;
		}
	}

	private static List<ILogicTick> _runtimeStacks = new List<ILogicTick>();

	public const int STACK_ELEMENT_SIZE = 8;

	private const string COLOR_GENERAL = "<color=white>";

	private const string COLOR_ADDRESS = "<color=grey>";

	private const string COLOR_BITS = "<color=red>";

	private const string COLOR_TYPE = "<color=orange>";

	private const string COLOR_TEXT = "<color=yellow>";

	private const string COLOR_UNUSED = "<color=grey>";

	private const string COLOR_END = "</color>";

	private const int BIT_COMMENT_LENGTH = 28;

	private const int BIT_TYPE_LENGTH = 10;

	private const int BIT_TEXT_LENGTH = 10;

	private const int LINE_LENGTH = 56;

	public const int PAYLOAD_BITS = 53;

	private const ulong PAYLOAD_MASK = 9007199254740991uL;

	public static InstructionFormat OpCode = new InstructionFormat("Op_Code", typeof(byte));

	private readonly double[] _stack;

	public static readonly string LimitNextExecutionByCount = FormatInstruction(OpCode, new InstructionFormat("Count", typeof(uint)));

	public int Size => _stack.Length;

	public double this[int index]
	{
		get
		{
			if (index < 0)
			{
				throw new StackUnderflowException();
			}
			if (index >= _stack.Length)
			{
				throw new StackOverflowException();
			}
			return _stack[index];
		}
		set
		{
			if (index < 0)
			{
				throw new StackUnderflowException();
			}
			if (index >= _stack.Length)
			{
				throw new StackOverflowException();
			}
			_stack[index] = value;
		}
	}

	public static void Register(ILogicTick stack)
	{
		_runtimeStacks.Add(stack);
	}

	public static void Deregister(ILogicTick stack)
	{
		_runtimeStacks.Remove(stack);
	}

	public static void ClearAll()
	{
		_runtimeStacks.Clear();
	}

	private static int CountCharsOutsideBrackets(string input)
	{
		bool flag = false;
		int num = 0;
		for (int i = 0; i < input.Length; i++)
		{
			switch (input[i])
			{
			case '<':
				flag = true;
				continue;
			case '>':
				flag = false;
				continue;
			}
			if (!flag)
			{
				num++;
			}
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ClampPos(long pos)
	{
		if (pos <= 0)
		{
			return 0;
		}
		if (pos >= 53)
		{
			return 53;
		}
		return (int)pos;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ClampLen(long pos, long len)
	{
		if (len <= 0)
		{
			return 0;
		}
		int num = ClampPos(pos);
		long num2 = 53 - num;
		return (int)((len > num2) ? num2 : len);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong MakeMask(int pos, int len)
	{
		if (len <= 0 || pos >= 53)
		{
			return 0uL;
		}
		if (pos < 0)
		{
			pos = 0;
		}
		if (len > 53 - pos)
		{
			len = 53 - pos;
		}
		return (ulong)(((len >= 64) ? (-1) : ((1L << len) - 1)) << pos);
	}

	private static void AppendAddressRange(StringBuilder sb, int begin, int end, string color = "")
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("| ");
		stringBuilder.Append(string.IsNullOrEmpty(color) ? "<color=grey>" : color);
		stringBuilder.Append("VALID ONLY AT ADDRESSES ");
		stringBuilder.Append(begin);
		stringBuilder.Append(" TO ");
		stringBuilder.Append(end);
		stringBuilder.Append("</color>");
		int num = CountCharsOutsideBrackets(stringBuilder.ToString());
		sb.Append(stringBuilder);
		if (num < 54)
		{
			sb.Append(' ', 54 - num);
		}
		sb.Append(" |");
	}

	private static void AppendUnsignedHashNote(StringBuilder sb, string color = "")
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("| ");
		stringBuilder.Append(string.IsNullOrEmpty(color) ? "<color=grey>" : color);
		stringBuilder.Append("HASH REPORTED UNSIGNED - MUST BE MANUALLY SIGNED");
		stringBuilder.Append("</color>");
		int num = CountCharsOutsideBrackets(stringBuilder.ToString());
		sb.Append(stringBuilder);
		if (num < 54)
		{
			sb.Append(' ', 54 - num);
		}
		sb.Append(" |");
	}

	private static void AppendAddress(StringBuilder sb, int index, string color = "")
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("| ");
		stringBuilder.Append(string.IsNullOrEmpty(color) ? "<color=grey>" : color);
		stringBuilder.Append("VALID ONLY AT ADDRESS ");
		stringBuilder.Append(index);
		stringBuilder.Append("</color>");
		int num = CountCharsOutsideBrackets(stringBuilder.ToString());
		sb.Append(stringBuilder);
		if (num < 54)
		{
			sb.Append(' ', 54 - num);
		}
		sb.Append(" |");
	}

	private static void AppendBits(StringBuilder sb, int currentBit, int bitSize, string color = "")
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("| ");
		stringBuilder.Append(string.IsNullOrEmpty(color) ? "<color=red>" : color);
		stringBuilder.Append(currentBit);
		stringBuilder.Append("-");
		stringBuilder.Append(currentBit + bitSize - 1);
		stringBuilder.Append(" ");
		stringBuilder.Append("</color>");
		int num = CountCharsOutsideBrackets(stringBuilder.ToString());
		sb.Append(stringBuilder);
		if (num < 10)
		{
			sb.Append(' ', 10 - num);
		}
		sb.Append(" | ");
	}

	private static void AppendComment(StringBuilder sb, string description, string color = "")
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(string.IsNullOrEmpty(color) ? "<color=yellow>" : color);
		stringBuilder.Append(description.ToUpper());
		stringBuilder.Append("</color>");
		int num = CountCharsOutsideBrackets(stringBuilder.ToString());
		sb.Append(stringBuilder);
		if (num < 28)
		{
			sb.Append(' ', 28 - num);
		}
		sb.Append(" | ");
	}

	private static void AppendType(StringBuilder sb, int bitSize, string color = "")
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(string.IsNullOrEmpty(color) ? "<color=red>" : color);
		stringBuilder.Append(bitSize);
		stringBuilder.Append("</color>");
		int num = CountCharsOutsideBrackets(stringBuilder.ToString());
		sb.Append(stringBuilder);
		if (num < 10)
		{
			sb.Append(' ', 10 - num);
		}
		sb.Append(" |");
	}

	private static void AppendType(StringBuilder sb, InstructionFormat bitSize)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<color=orange>");
		stringBuilder.Append(bitSize.TypeName);
		stringBuilder.Append('_');
		stringBuilder.Append(bitSize.Size);
		stringBuilder.Append("</color>");
		int num = CountCharsOutsideBrackets(stringBuilder.ToString());
		sb.Append(stringBuilder);
		if (num < 10)
		{
			sb.Append(' ', 10 - num);
		}
		sb.Append(" |");
	}

	public static (byte, int) UnpackInt32(long packed)
	{
		byte[] bytes = BitConverter.GetBytes(packed);
		if (!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		byte item = bytes[0];
		int item2 = BitConverter.ToInt32(bytes, 1);
		return (item, item2);
	}

	public static (byte, ushort) UnpackUInt16(long packed)
	{
		byte[] bytes = BitConverter.GetBytes(packed);
		if (!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		byte item = bytes[0];
		ushort item2 = BitConverter.ToUInt16(bytes, 1);
		return (item, item2);
	}

	public static (byte, bool) UnpackBool(long packed)
	{
		byte[] bytes = BitConverter.GetBytes(packed);
		if (!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		byte item = bytes[0];
		byte b = bytes[1];
		return (item, b != 0);
	}

	public static (byte, byte) UnpackByte(long packed)
	{
		byte[] bytes = BitConverter.GetBytes(packed);
		if (!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return (bytes[0], bytes[1]);
	}

	public static (byte, byte, byte) UnpackByteX2(long packed)
	{
		byte[] bytes = BitConverter.GetBytes(packed);
		if (!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		return (bytes[0], bytes[1], bytes[2]);
	}

	public static (byte, uint) UnpackUInt32(long packed)
	{
		byte[] bytes = BitConverter.GetBytes(packed);
		if (!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		byte item = bytes[0];
		uint item2 = BitConverter.ToUInt32(bytes, 1);
		return (item, item2);
	}

	public static (byte, byte, int) UnpackByteInt32(long packed)
	{
		byte[] bytes = BitConverter.GetBytes(packed);
		if (!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		byte item = bytes[0];
		byte item2 = bytes[1];
		int item3 = BitConverter.ToInt32(bytes, 2);
		return (item, item2, item3);
	}

	public static (byte, byte, ushort) UnpackByteUInt16(long packed)
	{
		byte[] bytes = BitConverter.GetBytes(packed);
		if (!BitConverter.IsLittleEndian)
		{
			Array.Reverse(bytes);
		}
		byte item = bytes[0];
		byte item2 = bytes[1];
		ushort item3 = BitConverter.ToUInt16(bytes, 2);
		return (item, item2, item3);
	}

	public static string FormatInstruction(int begin, params InstructionFormat[] parts)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendAddress(stringBuilder, begin);
		stringBuilder.AppendLine();
		stringBuilder.Append(FormatInstruction(parts));
		return stringBuilder.ToString();
	}

	public static string FormatInstruction(int begin, int end, params InstructionFormat[] parts)
	{
		StringBuilder stringBuilder = new StringBuilder();
		AppendAddressRange(stringBuilder, begin, end);
		stringBuilder.AppendLine();
		stringBuilder.Append(FormatInstruction(parts));
		return stringBuilder.ToString();
	}

	public static string FormatInstruction(params InstructionFormat[] parts)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<color=white>");
		int num = 0;
		bool flag = false;
		for (int i = 0; i < parts.Length; i++)
		{
			InstructionFormat bitSize = parts[i];
			if (i != 0)
			{
				stringBuilder.AppendLine();
			}
			AppendBits(stringBuilder, num, bitSize.Size);
			AppendComment(stringBuilder, bitSize.Description.Replace(' ', '_'));
			AppendType(stringBuilder, bitSize);
			if (bitSize.Type == typeof(uint) && bitSize.Description.Contains("Hash"))
			{
				flag = true;
			}
			num += bitSize.Size;
		}
		if (num < 64)
		{
			stringBuilder.AppendLine();
			int bitSize2 = 64 - num;
			AppendBits(stringBuilder, num, bitSize2, "<color=grey>");
			AppendComment(stringBuilder, "Unused", "<color=grey>");
			AppendType(stringBuilder, bitSize2, "<color=grey>");
		}
		if (flag)
		{
			stringBuilder.AppendLine();
			AppendUnsignedHashNote(stringBuilder);
		}
		stringBuilder.Append("</color>");
		return stringBuilder.ToString();
	}

	private static string GetTypeName(Type part)
	{
		if (part == typeof(bool))
		{
			return "BOOL";
		}
		if (part == typeof(byte))
		{
			return "BYTE";
		}
		if (part == typeof(sbyte))
		{
			return "SBYTE";
		}
		if (part == typeof(ushort))
		{
			return "USHORT";
		}
		if (part == typeof(short))
		{
			return "SHORT";
		}
		if (part == typeof(int))
		{
			return "INT";
		}
		if (part == typeof(uint))
		{
			return "UINT";
		}
		throw new NotImplementedException("GetTypeName not implemented for " + part.FullName);
	}

	private static int GetBitSize(Type part)
	{
		if (part == typeof(bool))
		{
			return 8;
		}
		if (part == typeof(byte) || part == typeof(sbyte))
		{
			return 8;
		}
		if (part == typeof(ushort) || part == typeof(short))
		{
			return 16;
		}
		if (part == typeof(int) || part == typeof(uint))
		{
			return 32;
		}
		throw new NotImplementedException("GetBitSize not implemented for " + part.FullName);
	}

	public LogicStack(int size)
	{
		_stack = new double[size];
	}

	public static bool CompareInt32(long packedLong, int right, ConditionOperation compare)
	{
		int item = UnpackInt32(packedLong).Item2;
		return CompareInt32(compare, item, right);
	}

	public static bool CompareInt32(long packedLong, int right)
	{
		(byte, byte, int) tuple = UnpackByteInt32(packedLong);
		ConditionOperation item = (ConditionOperation)tuple.Item2;
		int item2 = tuple.Item3;
		return CompareInt32(item, item2, right);
	}

	public static bool CompareUInt16(long packedLong, ushort right)
	{
		(byte, byte, ushort) tuple = UnpackByteUInt16(packedLong);
		ConditionOperation item = (ConditionOperation)tuple.Item2;
		ushort item2 = tuple.Item3;
		return CompareUInt16(item, item2, right);
	}

	public static bool CompareUInt16(ConditionOperation operation, ushort left, ushort right)
	{
		return operation switch
		{
			ConditionOperation.Equals => left == right, 
			ConditionOperation.Greater => left < right, 
			ConditionOperation.Less => left > right, 
			ConditionOperation.NotEquals => left != right, 
			_ => false, 
		};
	}

	public static bool CompareInt32(ConditionOperation operation, int left, int right)
	{
		return operation switch
		{
			ConditionOperation.Equals => left == right, 
			ConditionOperation.Greater => left < right, 
			ConditionOperation.Less => left > right, 
			ConditionOperation.NotEquals => left != right, 
			_ => false, 
		};
	}

	public void Clear()
	{
		for (int i = 0; i < _stack.Length; i++)
		{
			_stack[i] = 0.0;
		}
	}

	public LogicStackData Serialize()
	{
		LogicStackData logicStackData = new LogicStackData();
		for (int i = 0; i < _stack.Length; i++)
		{
			if ((long)_stack[i] != 0L)
			{
				logicStackData.Registers.Add(new LogicStackItemData
				{
					Index = i,
					Value = _stack[i]
				});
			}
		}
		return logicStackData;
	}

	public void Deserialize(LogicStackData data)
	{
		foreach (LogicStackItemData register in data.Registers)
		{
			if (register.Index >= 0 && register.Index < _stack.Length)
			{
				_stack[register.Index] = register.Value;
			}
		}
	}

	public static long PackInt32(byte opcode, int value)
	{
		return ((long)value << 8) | opcode;
	}

	public static long PackInt32ByteBool(byte opcode, int value1, byte value2, bool value3)
	{
		return (long)(((ulong)(uint)(value3 ? 1 : 0) << 48) | ((ulong)value2 << 40) | (ulong)((long)value1 << 8) | opcode);
	}

	public static long PackUInt32(byte opcode, uint value)
	{
		return (long)(((ulong)value << 8) | opcode);
	}

	public static long PackUInt16(byte opcode, ushort value)
	{
		return (long)(((ulong)value << 8) | opcode);
	}

	public static long PackUInt16X2(byte opcode, ushort value1, ushort value2)
	{
		return (long)(((ulong)value2 << 24) | ((ulong)value1 << 8) | opcode);
	}

	public static long PackByte(byte opCode, byte byte1)
	{
		return (long)(((ulong)byte1 << 8) | opCode);
	}

	public static long PackByteUshort(byte opCode, byte byte1, ushort ushort1)
	{
		return (long)(((ulong)ushort1 << 16) | ((ulong)byte1 << 8) | opCode);
	}

	public static long PackByteBool(byte opCode, byte byte1, bool bool1)
	{
		return (long)(((ulong)(uint)(bool1 ? 1 : 0) << 16) | ((ulong)byte1 << 8) | opCode);
	}

	public static long PackByteX4(byte opCode, byte byte1, byte byte2, byte byte3, byte byte4)
	{
		return (long)(((ulong)byte4 << 32) | ((ulong)byte3 << 24) | ((ulong)byte2 << 16) | ((ulong)byte1 << 8) | opCode);
	}

	public static long PackByteX4Ushort(byte opCode, byte byte1, byte byte2, byte byte3, byte byte4, ushort ushort1)
	{
		if (ushort1 > 8191)
		{
			ushort1 = 8191;
		}
		return (long)((((ulong)ushort1 & 0x1FFFuL) << 40) | ((ulong)byte4 << 32) | ((ulong)byte3 << 24) | ((ulong)byte2 << 16) | ((ulong)byte1 << 8) | opCode);
	}

	public static long PackByteX3(byte opCode, byte byte1, byte byte2, byte byte3)
	{
		return (long)(((ulong)byte3 << 24) | ((ulong)byte2 << 16) | ((ulong)byte1 << 8) | opCode);
	}

	public static long PackByte2(byte opCode, byte byte1, byte byte2)
	{
		return (long)(((ulong)byte2 << 16) | ((ulong)byte1 << 8) | opCode);
	}

	public static long PackByteInt32(byte opCode, byte byte1, int int1)
	{
		return (long)((ulong)((long)int1 << 16) | ((ulong)byte1 << 8) | opCode);
	}

	public static long PackByteUInt32(byte opCode, byte byte1, uint int1)
	{
		return (long)(((ulong)int1 << 16) | ((ulong)byte1 << 8) | opCode);
	}

	public static long PackOpcodeUInt13Int32(byte opCode, ushort value13, int int1)
	{
		return (long)(((ulong)(uint)int1 << 21) | (ulong)((long)(value13 & 0x1FFF) << 8) | opCode);
	}

	public static (byte, ushort, int) UnpackOpcodeUInt13Int32(long packed)
	{
		byte item = (byte)(packed & 0xFF);
		ushort item2 = (ushort)((packed >> 8) & 0x1FFF);
		int item3 = (int)((packed >> 21) & 0xFFFFFFFFu);
		return (item, item2, item3);
	}

	public static double PackInt16(byte opCode, short short1, short short2)
	{
		return ProgrammableChip.LongToDouble(((long)short1 << 24) | ((long)short2 << 8) | opCode);
	}

	public static double PackByteInt16(byte opCode, byte byte1, short short1, short short2)
	{
		return ProgrammableChip.LongToDouble((long)(((ulong)byte1 << 40) | (ulong)((long)short1 << 24) | (ulong)((long)short2 << 8) | opCode));
	}

	public static void LogicStackTick()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		for (int num = _runtimeStacks.Count - 1; num >= 0; num--)
		{
			ILogicTick logicTick = _runtimeStacks[num];
			if (logicTick == null)
			{
				_runtimeStacks.RemoveAt(num);
			}
			else
			{
				try
				{
					logicTick.OnLogicTick();
				}
				catch (Exception ex)
				{
					string text = "Exception: " + ex.Message + "\n";
					string stackTrace = ex.StackTrace;
					for (int i = 0; i < stackTrace.Length; i++)
					{
						text += stackTrace[i];
					}
					if (GameManager.GameState != GameState.None)
					{
						ConsoleWindow.PrintError("error in logic stack tick: " + text);
					}
				}
			}
		}
	}

	public void Poke(ref int begin, long value)
	{
		double num = ProgrammableChip.LongToDouble(value);
		_stack[begin] = num;
		begin++;
	}

	public void Poke(ref int begin, double value)
	{
		_stack[begin] = value;
		begin++;
	}

	public void Clear(int begin, int end)
	{
		for (int i = begin; i < end && i < Size; i++)
		{
			_stack[i] = 0.0;
		}
	}

	public void Clear(int index)
	{
		if (index >= 0 && index < Size)
		{
			_stack[index] = 0.0;
		}
	}

	public static bool ContainsFlag(long packedLong, uint flag)
	{
		return (UnpackUInt32(packedLong).Item2 & flag) == flag;
	}
}
