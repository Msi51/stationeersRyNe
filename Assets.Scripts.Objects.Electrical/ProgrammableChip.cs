using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using Objects.Rockets;
using Reagents;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class ProgrammableChip : Item, ISourceCode, IReferencable, IEvaluable, ISpatial, IPhysical, IProfile, IDensePoolable, ILogicable, IMemoryWritable, IMemory, IMemoryReadable
{
	[Flags]
	private enum _AliasTarget
	{
		None = 0,
		Register = 1,
		Device = 2,
		Network = 4,
		All = 0xFFFFFFF
	}

	private struct _AliasValue(_AliasTarget target, int index)
	{
		public readonly _AliasTarget Target = target;

		public readonly int Index = index;
	}

	private struct HelpString
	{
		private readonly string _string;

		public HelpString(string str)
		{
			_string = str;
		}

		public HelpString(string type, string color)
		{
			_string = "<color=" + color + ">" + type + "</color>";
		}

		public HelpString(string type, string output, string color)
		{
			_string = "<color=" + color + ">" + output + "</color>";
		}

		public HelpString(HelpString parent, string format)
		{
			_string = string.Format(format, parent._string);
		}

		public new readonly string ToString()
		{
			return _string;
		}

		public HelpString Var(string variable)
		{
			return new HelpString(variable + "(" + _string + ")");
		}

		public static HelpString operator +(HelpString left, HelpString right)
		{
			return new HelpString(left._string + OR.ToString() + right._string);
		}
	}

	public readonly struct Constant : IEquatable<Constant>
	{
		public readonly string Literal;

		public readonly string Description;

		public readonly double Value;

		public readonly int Hash;

		public bool Equals(Constant other)
		{
			return Hash == other.Hash;
		}

		public override bool Equals(object obj)
		{
			if (obj is Constant other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Hash;
		}

		public static bool operator ==(Constant a, string b)
		{
			if (string.IsNullOrEmpty(b))
			{
				return false;
			}
			return b.Equals(a.Literal, StringComparison.OrdinalIgnoreCase);
		}

		public static bool operator !=(Constant a, string b)
		{
			if (string.IsNullOrEmpty(b))
			{
				return true;
			}
			return !b.Equals(a.Literal, StringComparison.OrdinalIgnoreCase);
		}

		public Constant(string literalString, string description, double value, bool addValueToDescription = true)
		{
			Literal = literalString;
			if (addValueToDescription)
			{
				Description = "<color=yellow>" + value.ToString("0." + new string('#', 339), CultureInfo.CurrentCulture) + "</color><br>" + description;
			}
			else
			{
				Description = description;
			}
			Value = value;
			Hash = Animator.StringToHash(Literal);
		}

		public string GetName()
		{
			return "<color=#20B2AA>" + Literal + "</color>";
		}

		public string GetValue()
		{
			double value;
			if (RocketMath.Approximately(Value - Math.Floor(Value), 0.0, 0.01))
			{
				value = Value;
				return value.ToString("0", CultureInfo.CurrentCulture);
			}
			value = Value;
			return value.ToString("0." + new string('#', 8), CultureInfo.CurrentCulture);
		}
	}

	private class _LineOfCode
	{
		public readonly _Operation Operation;

		public readonly string LineOfCode;

		public _LineOfCode(ProgrammableChip chip, string lineOfCode, int lineNumber)
		{
			string masterString = ((lineOfCode.IndexOf('#') < 0) ? lineOfCode : lineOfCode.Substring(0, lineOfCode.IndexOf('#')));
			Localization.RegexResult matchesForStringPreprocessing = Localization.GetMatchesForStringPreprocessing(ref masterString);
			for (int i = 0; i < matchesForStringPreprocessing.Count(); i++)
			{
				masterString = masterString.Replace(matchesForStringPreprocessing.GetFull(i), PackAscii6(matchesForStringPreprocessing.GetName(i), lineNumber).ToString(CultureInfo.InvariantCulture));
			}
			try
			{
				Localization.RegexResult matchesForHashPreprocessing = Localization.GetMatchesForHashPreprocessing(ref masterString);
				for (int j = 0; j < matchesForHashPreprocessing.Count(); j++)
				{
					masterString = masterString.Replace(matchesForHashPreprocessing.GetFull(j), Animator.StringToHash(matchesForHashPreprocessing.GetName(j)).ToString());
				}
			}
			catch (Exception)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.InvalidPreprocessHash, lineNumber);
			}
			try
			{
				Localization.RegexResult matchesForBinaryPreprocessing = Localization.GetMatchesForBinaryPreprocessing(ref masterString);
				for (int k = 0; k < matchesForBinaryPreprocessing.Count(); k++)
				{
					string value = matchesForBinaryPreprocessing.GetName(k).Replace("_", "");
					masterString = masterString.Replace(matchesForBinaryPreprocessing.GetFull(k), Convert.ToInt64(value, 2).ToString());
				}
			}
			catch (Exception)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.InvalidProcessBinary, lineNumber);
			}
			try
			{
				Localization.RegexResult matchesForHexPreprocessing = Localization.GetMatchesForHexPreprocessing(ref masterString);
				for (int l = 0; l < matchesForHexPreprocessing.Count(); l++)
				{
					string value2 = matchesForHexPreprocessing.GetName(l).Replace("_", "");
					masterString = masterString.Replace(matchesForHexPreprocessing.GetFull(l), Convert.ToInt64(value2, 16).ToString());
				}
			}
			catch (Exception)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.InvalidPreprocessHex, lineNumber);
			}
			string[] array = masterString.Split();
			for (int num = array.Length - 1; num >= 0; num--)
			{
				if (string.IsNullOrEmpty(array[num]))
				{
					array = array.RemoveAt(num);
				}
			}
			if (array.Length == 0)
			{
				Operation = new _NOOP_Operation(chip, lineNumber);
			}
			else if (array.Length == 1 && array[0].Length >= 2 && array[0][array[0].Length - 1] == ':')
			{
				Operation = new _NOOP_Operation(chip, lineNumber);
				string key = array[0].Substring(0, array[0].Length - 1);
				if (chip._JumpTags.ContainsKey(key))
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.JumpTagDuplicate, lineNumber);
				}
				chip._JumpTags.Add(key, lineNumber);
			}
			else
			{
				if (!Enum.IsDefined(typeof(ScriptCommand), array[0]))
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.UnrecognisedInstruction, lineNumber);
				}
				switch ((ScriptCommand)Enum.Parse(typeof(ScriptCommand), array[0]))
				{
				case ScriptCommand.l:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _L_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.ld:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _LD_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.lb:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _LB_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.lbs:
					if (array.Length != 6)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _LBS_Operation(chip, lineNumber, array[1], array[2], array[3], array[4], array[5]);
					break;
				case ScriptCommand.lbns:
					if (array.Length != 7)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _LBNS_Operation(chip, lineNumber, array[1], array[2], array[3], array[4], array[5], array[6]);
					break;
				case ScriptCommand.lbn:
					if (array.Length != 6)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _LBN_Operation(chip, lineNumber, array[1], array[2], array[3], array[4], array[5]);
					break;
				case ScriptCommand.s:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _S_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.sd:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SD_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.ss:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SS_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.sbs:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SBS_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.sb:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SB_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.sbn:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SBN_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.ls:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _LS_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.lr:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _LR_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.alias:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _ALIAS_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.define:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _DEFINE_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.move:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _MOVE_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.add:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _ADD_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.sub:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SUB_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.sdse:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SDSE_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.sdns:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SDNS_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.slt:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SLT_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.sgt:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SGT_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.sle:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SLE_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.sge:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SGE_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.seq:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SEQ_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.sne:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SNE_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.sap:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SAP_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.sna:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SNA_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.sltz:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SLTZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.sgtz:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SGTZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.slez:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SLEZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.sgez:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SGEZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.seqz:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SEQZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.snez:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SNEZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.sapz:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SAPZ_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.snaz:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SNAZ_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.and:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _AND_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.or:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _OR_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.xor:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _XOR_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.nor:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _NOR_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.not:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _NOT_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.mul:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _MUL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.div:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _DIV_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.mod:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _MOD_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.j:
					if (array.Length != 2)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _J_Operation(chip, lineNumber, array[1]);
					break;
				case ScriptCommand.bdse:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BDSE_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.bdns:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BDNS_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.blt:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BLT_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bgt:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BGT_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.ble:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BLE_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bge:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BGE_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.beq:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BEQ_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bnan:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BNAN_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.brnan:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRNAN_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.bne:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BNE_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bap:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BAP_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.bna:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BNA_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.bltz:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BLTZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.bgez:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BGEZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.blez:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BLEZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.bgtz:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BGTZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.beqz:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BEQZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.bnez:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BNEZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.bapz:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BAPZ_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bnaz:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BNAZ_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.jal:
					if (array.Length != 2)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _JAL_Operation(chip, lineNumber, array[1]);
					break;
				case ScriptCommand.bdseal:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BDSEAL_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.bdnsal:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BDNSAL_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.bltal:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BLTAL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bgeal:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BGEAL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bleal:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BLEAL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bgtal:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BGTAL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bltzal:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BLTZAL_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.bgezal:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BGEZAL_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.blezal:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BLEZAL_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.bgtzal:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BGTZAL_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.beqal:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BEQAL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bneal:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BNEAL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bapal:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BAPAL_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.bnaal:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BNAAL_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.beqzal:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BEQZAL_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.bnezal:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BNEZAL_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.bapzal:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BAPZAL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bnazal:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BNAZAL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.jr:
					if (array.Length != 2)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _JR_Operation(chip, lineNumber, array[1]);
					break;
				case ScriptCommand.brdse:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRDSE_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.brdns:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRDNS_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.brlt:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRLT_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.brge:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRGE_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.brle:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRLE_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.brgt:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRGT_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.brltz:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRLTZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.brgez:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRGEZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.brlez:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRLEZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.brgtz:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRGTZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.breq:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BREQ_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.brne:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRNE_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.brap:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRAP_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.brna:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRNA_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.breqz:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BREQZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.brnez:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRNEZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.brapz:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRAPZ_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.brnaz:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BRNAZ_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.sqrt:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SQRT_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.round:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _ROUND_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.trunc:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _TRUNC_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.ceil:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _CEIL_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.floor:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _FLOOR_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.max:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _MAX_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.min:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _MIN_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.pow:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _POW_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.abs:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _ABS_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.sgn:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SGN_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.log:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _LOG_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.exp:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _EXP_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.lerp:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _LERP_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.clamp:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _CLAMP_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.rand:
					if (array.Length != 2)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _RAND_Operation(chip, lineNumber, array[1]);
					break;
				case ScriptCommand.hcf:
					if (array.Length != 1)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _HCF_Operation(chip, lineNumber);
					break;
				case ScriptCommand.yield:
					if (array.Length != 1)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _YIELD_Operation(chip, lineNumber);
					break;
				case ScriptCommand.label:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _LABEL_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.peek:
					if (array.Length != 2)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _PEEK_Operation(chip, lineNumber, array[1]);
					break;
				case ScriptCommand.push:
					if (array.Length != 2)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _PUSH_Operation(chip, lineNumber, array[1]);
					break;
				case ScriptCommand.poke:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _POKE_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.pop:
					if (array.Length != 2)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _POP_Operation(chip, lineNumber, array[1]);
					break;
				case ScriptCommand.select:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SELECT_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.ext:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _EXT_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.ins:
					if (array.Length != 5)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _INS_Operation(chip, lineNumber, array[1], array[2], array[3], array[4]);
					break;
				case ScriptCommand.sleep:
					if (array.Length != 2)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SLEEP_Operation(chip, lineNumber, array[1]);
					break;
				case ScriptCommand.sin:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SIN_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.asin:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _ASIN_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.cos:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _COS_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.acos:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _ACOS_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.tan:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _TAN_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.atan:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _ATAN_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.atan2:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _ATAN2_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.snan:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SNAN_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.snanz:
					if (array.Length != 3)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SNANZ_Operation(chip, lineNumber, array[1], array[2]);
					break;
				case ScriptCommand.srl:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SRL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.sra:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SRA_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.sll:
				case ScriptCommand.sla:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _SLA_SLL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.rol:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _ROL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.ror:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _ROR_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.getd:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _GETD_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.putd:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _PUTD_Operation(chip, lineNumber, array[3], array[1], array[2]);
					break;
				case ScriptCommand.clr:
					if (array.Length != 2)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _CLR_Operation(chip, lineNumber, array[1]);
					break;
				case ScriptCommand.clrd:
					if (array.Length != 2)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _CLRD_Operation(chip, lineNumber, array[1]);
					break;
				case ScriptCommand.get:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _GET_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.put:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _PUT_Operation(chip, lineNumber, array[3], array[1], array[2]);
					break;
				case ScriptCommand.rmap:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _RMAP_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bdnvs:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BDNVS_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				case ScriptCommand.bdnvl:
					if (array.Length != 4)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectArgumentCount, lineNumber);
					}
					Operation = new _BDNVL_Operation(chip, lineNumber, array[1], array[2], array[3]);
					break;
				default:
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.UnrecognisedInstruction, lineNumber);
				}
			}
			LineOfCode = lineOfCode;
		}

		public override string ToString()
		{
			return LineOfCode;
		}
	}

	private abstract class _Operation
	{
		public class Variable
		{
			protected readonly ProgrammableChip _Chip;

			protected readonly int _LineNumber;

			protected readonly InstructionInclude _PropertiesToUse;

			protected readonly int _RegisterIndex = -1;

			protected readonly int _RegisterRecurse = -1;

			protected readonly string _Alias;

			protected Variable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
			{
				_Chip = chip;
				_LineNumber = lineNumber;
				_PropertiesToUse = propertiesToUse;
				if ((propertiesToUse & InstructionInclude.RegisterIndex) != InstructionInclude.None)
				{
					_RegisterIndex = _GetRegisterIndex(code, out _RegisterRecurse, throwException);
				}
				if ((propertiesToUse & (InstructionInclude.Alias | InstructionInclude.JumpTag)) != InstructionInclude.None)
				{
					if (code.Contains(':'))
					{
						code = code.Split(':', 2)[0];
					}
					_Alias = code;
				}
			}

			private int _GetIndex(string rCode, char firstLetter, bool throwException = true)
			{
				int recurseCount;
				return _GetIndex(rCode, firstLetter, out recurseCount, throwException);
			}

			private int _GetIndex(string rCode, char firstLetter, out int recurseCount, bool throwException = true)
			{
				int i;
				for (i = 0; rCode[i] == firstLetter; i++)
				{
				}
				if (i == 0)
				{
					if (throwException)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, _LineNumber);
					}
					recurseCount = -1;
					return -1;
				}
				string text = rCode.Substring(i);
				if (text.Contains(':'))
				{
					text = text.Split(':', 2)[0];
				}
				if (!int.TryParse(text, out var result))
				{
					if (throwException)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, _LineNumber);
					}
					recurseCount = -1;
					return -1;
				}
				recurseCount = i;
				return result;
			}

			protected int _GetRegisterIndex(string rCode, out int recurseCount, bool throwException = true)
			{
				return _GetIndex(rCode, 'r', out recurseCount, throwException);
			}

			protected int _GetDeviceIndex(string dCode, out int recurseCount, bool throwException = true)
			{
				if (dCode.StartsWith("db", StringComparison.Ordinal))
				{
					recurseCount = 0;
					return int.MaxValue;
				}
				if (dCode.Length >= 2 && dCode[1] == 'r')
				{
					return _GetIndex(dCode.Substring(1), 'r', out recurseCount, throwException);
				}
				recurseCount = 0;
				return _GetIndex(dCode, 'd', throwException);
			}

			protected int _GetNetworkIndex(string dCode, bool throwException = true)
			{
				if (dCode.Contains(':'))
				{
					dCode = dCode.Split(':', 2)[1];
					if (!int.TryParse(dCode, out var result))
					{
						if (throwException)
						{
							throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, _LineNumber);
						}
						return int.MinValue;
					}
					return result;
				}
				return int.MinValue;
			}

			protected _AliasTarget GetAliasType(string alias, bool throwException = true)
			{
				if (string.IsNullOrEmpty(_Alias) || !_Chip._Aliases.TryGetValue(alias, out var value))
				{
					if (throwException)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, _LineNumber);
					}
					return _AliasTarget.None;
				}
				return value.Target;
			}

			public bool TryParseAliasAsJumpTagValue(out int value, bool throwException = true)
			{
				if (_Alias == null || !_Chip._JumpTags.ContainsKey(_Alias))
				{
					value = 0;
					return false;
				}
				value = _Chip._JumpTags[_Alias];
				return true;
			}
		}

		protected class IndexVariable : IntValuedVariable
		{
			public IndexVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
				: base(chip, lineNumber, code, propertiesToUse, throwException)
			{
			}

			protected bool TryParseAliasAsIndex(_AliasTarget type, out int index)
			{
				if (_Alias == null || !_Chip._Aliases.ContainsKey(_Alias))
				{
					index = -1;
					return false;
				}
				_AliasValue aliasValue = _Chip._Aliases[_Alias];
				if (aliasValue.Target != type)
				{
					index = -1;
					return false;
				}
				index = aliasValue.Index;
				return true;
			}

			protected bool TryParseRegisterIndexAsIndex(out int index, bool throwException = true)
			{
				if (_RegisterIndex < 0 || _RegisterRecurse < 0)
				{
					index = -1;
					return false;
				}
				int num = _RegisterIndex;
				int registerRecurse = _RegisterRecurse;
				while (registerRecurse-- > 1)
				{
					num = (int)Math.Round(_Chip._Registers[num]);
					if (num < 0 || num >= _Chip._Registers.Length)
					{
						if (throwException)
						{
							throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.OutOfRegisterBounds, _LineNumber);
						}
						index = -1;
						return false;
					}
				}
				index = num;
				return true;
			}

			public virtual int GetVariableIndex(_AliasTarget type, bool throwError = true)
			{
				int num = 0;
				int index;
				if ((_PropertiesToUse & InstructionInclude.Define) != InstructionInclude.None && _Chip._Defines.TryGetValue(_Alias, out var value))
				{
					num = (int)value;
				}
				else if ((_PropertiesToUse & InstructionInclude.Alias) != InstructionInclude.None && TryParseAliasAsIndex(type, out index))
				{
					num = index;
				}
				else if ((_PropertiesToUse & InstructionInclude.JumpTag) != InstructionInclude.None && TryParseAliasAsJumpTagValue(out index))
				{
					num = index;
				}
				else if ((_PropertiesToUse & InstructionInclude.RegisterIndex) != InstructionInclude.None && TryParseRegisterIndexAsIndex(out index, throwError))
				{
					num = index;
				}
				else if (throwError)
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, _LineNumber);
				}
				if (throwError)
				{
					if ((type & _AliasTarget.Register) != _AliasTarget.None && (num < 0 || num >= _Chip._Registers.Length))
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.OutOfRegisterBounds, _LineNumber);
					}
					if ((type & _AliasTarget.Device) != _AliasTarget.None && !_Chip.CircuitHousing.IsValidIndex(num))
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.OutOfDeviceBounds, _LineNumber);
					}
				}
				return num;
			}
		}

		protected class ValueVariable : Variable
		{
			public ValueVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
				: base(chip, lineNumber, code, propertiesToUse, throwException)
			{
			}

			protected bool TryParseAliasAsValue(_AliasTarget type, out double value, bool throwException = true)
			{
				if (_Alias == null || type != _AliasTarget.Register || !_Chip._Aliases.ContainsKey(_Alias))
				{
					value = double.NaN;
					return false;
				}
				_AliasValue aliasValue = _Chip._Aliases[_Alias];
				if (aliasValue.Target != type)
				{
					value = double.NaN;
					return false;
				}
				if (aliasValue.Index < 0 || aliasValue.Index >= _Chip._Registers.Length)
				{
					if (throwException)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.OutOfRegisterBounds, _LineNumber);
					}
					value = double.NaN;
					return false;
				}
				value = _Chip._Registers[aliasValue.Index];
				return true;
			}

			protected bool TryParseRegisterIndexAsValue(out double value, bool throwException = true)
			{
				if (_RegisterIndex < 0 || _RegisterRecurse < 0)
				{
					value = double.NaN;
					return false;
				}
				int num = _RegisterIndex;
				int registerRecurse = _RegisterRecurse;
				while (registerRecurse-- > 1)
				{
					num = (int)Math.Round(_Chip._Registers[num]);
					if (num < 0 || num >= _Chip._Registers.Length)
					{
						if (throwException)
						{
							throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.OutOfRegisterBounds, _LineNumber);
						}
						value = double.NaN;
						return false;
					}
				}
				value = _Chip._Registers[num];
				return true;
			}
		}

		protected class DoubleValueVariable : ValueVariable
		{
			private readonly double _Value = double.NaN;

			private readonly bool _qNaN;

			public double Get()
			{
				return _Value;
			}

			public DoubleValueVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
				: base(chip, lineNumber, code, propertiesToUse, throwException)
			{
				bool isValueSet = false;
				_Value = double.NaN;
				_qNaN = false;
				foreach (IScriptEnum internalEnum in InternalEnums)
				{
					internalEnum.Execute(ref isValueSet, ref _Value, code, propertiesToUse);
				}
				if (isValueSet || (propertiesToUse & InstructionInclude.Value) == 0)
				{
					return;
				}
				Constant[] allConstants = AllConstants;
				for (int i = 0; i < allConstants.Length; i++)
				{
					Constant constant = allConstants[i];
					if (constant == code)
					{
						_Value = constant.Value;
						return;
					}
				}
				if (double.TryParse(code, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out var result))
				{
					_Value = result;
				}
			}

			protected bool TryParseValueAsValue(out double value)
			{
				if (!_qNaN && double.IsNaN(_Value))
				{
					value = double.NaN;
					return false;
				}
				value = _Value;
				return true;
			}

			public long GetVariableLong(_AliasTarget type, bool signed = true, bool errorAtEnd = true)
			{
				double variableValue = GetVariableValue(type, errorAtEnd);
				if (!(variableValue < -9.223372036854776E+18))
				{
					if (variableValue > 9.223372036854776E+18)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftOverflow, _LineNumber);
					}
					return DoubleToLong(variableValue, signed);
				}
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftUnderflow, _LineNumber);
			}

			public int GetVariableInt(_AliasTarget type, bool errorAtEnd = true)
			{
				double variableValue = GetVariableValue(type, errorAtEnd);
				if (!(variableValue < -2147483648.0))
				{
					if (variableValue > 2147483647.0)
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftOverflow, _LineNumber);
					}
					return (int)variableValue;
				}
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftUnderflow, _LineNumber);
			}

			public double GetVariableValue(_AliasTarget type, bool errorAtEnd = true)
			{
				if ((type & _AliasTarget.Register) == 0)
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, _LineNumber);
				}
				if ((_PropertiesToUse & InstructionInclude.Define) != InstructionInclude.None && _Chip._Defines.TryGetValue(_Alias, out var value))
				{
					return value;
				}
				if ((_PropertiesToUse & InstructionInclude.Alias) != InstructionInclude.None && TryParseAliasAsValue(type, out var value2))
				{
					return value2;
				}
				if ((_PropertiesToUse & InstructionInclude.JumpTag) != InstructionInclude.None && TryParseAliasAsJumpTagValue(out var value3))
				{
					return value3;
				}
				if ((_PropertiesToUse & InstructionInclude.RegisterIndex) != InstructionInclude.None && TryParseRegisterIndexAsValue(out var value4, errorAtEnd))
				{
					return value4;
				}
				if ((_PropertiesToUse & (InstructionInclude.Value | InstructionInclude.Enum | InstructionInclude.LogicType | InstructionInclude.LogicSlotType | InstructionInclude.LogicReagentMode | InstructionInclude.LogicBatchMethod)) != InstructionInclude.None && TryParseValueAsValue(out var value5))
				{
					return value5;
				}
				if (errorAtEnd)
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, _LineNumber);
				}
				return double.NaN;
			}
		}

		protected class LineNumberVariable : ValueVariable
		{
			private readonly bool _IsValueSet;

			private readonly int _Value;

			public LineNumberVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
				: base(chip, lineNumber, code, propertiesToUse, throwException)
			{
				if ((propertiesToUse & InstructionInclude.Value) != InstructionInclude.None)
				{
					if (int.TryParse(code, out _Value))
					{
						_IsValueSet = true;
						return;
					}
					_IsValueSet = false;
					_Value = 0;
				}
			}

			public int GetVariableValue(_AliasTarget type, bool throwException = true)
			{
				if ((type & _AliasTarget.Register) != _AliasTarget.None && TryParseAliasAsValue(type, out var value, throwException))
				{
					return (int)value;
				}
				if ((_PropertiesToUse & InstructionInclude.Define) != InstructionInclude.None && _Chip._Defines.TryGetValue(_Alias, out var value2))
				{
					return (int)value2;
				}
				if ((_PropertiesToUse & InstructionInclude.JumpTag) != InstructionInclude.None && TryParseAliasAsJumpTagValue(out var value3))
				{
					return value3;
				}
				if ((_PropertiesToUse & InstructionInclude.RegisterIndex) != InstructionInclude.None && TryParseRegisterIndexAsValue(out var value4, throwException))
				{
					return (int)Math.Round(value4);
				}
				if ((_PropertiesToUse & (InstructionInclude.Value | InstructionInclude.Enum | InstructionInclude.LogicType | InstructionInclude.LogicSlotType | InstructionInclude.LogicReagentMode | InstructionInclude.LogicBatchMethod)) != InstructionInclude.None && _IsValueSet)
				{
					return _Value;
				}
				if ((_PropertiesToUse & (InstructionInclude.Value | InstructionInclude.Enum | InstructionInclude.LogicType | InstructionInclude.LogicSlotType | InstructionInclude.LogicReagentMode | InstructionInclude.LogicBatchMethod)) != InstructionInclude.None && _IsValueSet)
				{
					return _Value;
				}
				if (throwException)
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, _LineNumber);
				}
				return -1;
			}
		}

		protected interface IDeviceVariable
		{
			int GetNetworkIndex();

			ILogicable GetDevice(ICircuitHolder chipCircuitHousing);
		}

		protected class DirectDeviceVariable : IntValuedVariable, IDeviceVariable
		{
			protected readonly int _DeviceNetwork = int.MinValue;

			public DirectDeviceVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
				: base(chip, lineNumber, code, propertiesToUse, throwException)
			{
				if ((propertiesToUse & InstructionInclude.NetworkIndex) != InstructionInclude.None)
				{
					_DeviceNetwork = _GetNetworkIndex(code, throwException);
				}
			}

			public int GetNetworkIndex()
			{
				return _DeviceNetwork;
			}

			public ILogicable GetDevice(ICircuitHolder chipCircuitHousing)
			{
				int variableValue = GetVariableValue(_AliasTarget.Register);
				return chipCircuitHousing.GetLogicableFromId(variableValue, _DeviceNetwork);
			}
		}

		protected class DeviceAliasVariable : IndexVariable, IDeviceVariable
		{
			protected readonly int _DeviceNetwork = int.MinValue;

			public DeviceAliasVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
				: base(chip, lineNumber, code, propertiesToUse, throwException)
			{
				if ((propertiesToUse & InstructionInclude.NetworkIndex) != InstructionInclude.None)
				{
					_DeviceNetwork = _GetNetworkIndex(code, throwException);
				}
			}

			public int GetNetworkIndex()
			{
				return _DeviceNetwork;
			}

			public ILogicable GetDevice(ICircuitHolder chipCircuitHousing)
			{
				if (string.IsNullOrEmpty(_Alias))
				{
					int variableIndex = GetVariableIndex(_AliasTarget.Device, throwError: false);
					return chipCircuitHousing.GetLogicableFromIndex(variableIndex, _DeviceNetwork);
				}
				_AliasTarget aliasType = GetAliasType(_Alias);
				switch (aliasType)
				{
				case _AliasTarget.Device:
				{
					int variableIndex2 = GetVariableIndex(aliasType, throwError: false);
					return chipCircuitHousing.GetLogicableFromIndex(variableIndex2, _DeviceNetwork);
				}
				case _AliasTarget.Register:
				{
					int variableValue = GetVariableValue(aliasType, throwException: false);
					return chipCircuitHousing.GetLogicableFromId(variableValue, _DeviceNetwork);
				}
				default:
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.AliasNotFound, _LineNumber);
				}
			}
		}

		protected class DeviceIndexVariable : IndexVariable, IDeviceVariable
		{
			protected readonly int _DeviceIndex = -1;

			protected readonly int _DeviceRecurse = -1;

			protected readonly int _DeviceNetwork = int.MinValue;

			public DeviceIndexVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
				: base(chip, lineNumber, code, propertiesToUse, throwException)
			{
				if ((propertiesToUse & InstructionInclude.DeviceIndex) != InstructionInclude.None)
				{
					_DeviceIndex = _GetDeviceIndex(code, out _DeviceRecurse, throwException);
				}
				if ((propertiesToUse & InstructionInclude.NetworkIndex) != InstructionInclude.None)
				{
					_DeviceNetwork = _GetNetworkIndex(code, throwException);
				}
			}

			public int GetNetworkIndex()
			{
				return _DeviceNetwork;
			}

			public ILogicable GetDevice(ICircuitHolder chipCircuitHousing)
			{
				int variableIndex = GetVariableIndex(_AliasTarget.Device, throwError: false);
				return chipCircuitHousing.GetLogicableFromIndex(variableIndex, _DeviceNetwork);
			}

			public override int GetVariableIndex(_AliasTarget type, bool throwError = true)
			{
				if ((_PropertiesToUse & InstructionInclude.Alias) != InstructionInclude.None && TryParseAliasAsIndex(type, out var index))
				{
					return index;
				}
				if ((_PropertiesToUse & InstructionInclude.DeviceIndex) != InstructionInclude.None && TryParseDeviceIndexAsIndex(out var index2, throwError))
				{
					return index2;
				}
				if ((_PropertiesToUse & InstructionInclude.NetworkIndex) != InstructionInclude.None)
				{
					return _DeviceNetwork;
				}
				if (throwError)
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, _LineNumber);
				}
				return 0;
			}

			protected bool TryParseDeviceIndexAsIndex(out int index, bool throwException = true)
			{
				if (_DeviceIndex < 0 || _DeviceRecurse < 0)
				{
					index = -1;
					return false;
				}
				int num = _DeviceIndex;
				int deviceRecurse = _DeviceRecurse;
				while (deviceRecurse-- > 0)
				{
					num = (int)Math.Round(_Chip._Registers[num]);
					if (num < 0 || num >= _Chip._Registers.Length)
					{
						if (throwException)
						{
							throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.OutOfDeviceBounds, _LineNumber);
						}
						index = -1;
						return false;
					}
				}
				index = num;
				return true;
			}
		}

		protected class IntValuedVariable : ValueVariable
		{
			private readonly bool _IsValueSet;

			private readonly int _Value = -1;

			public IntValuedVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
				: base(chip, lineNumber, code, propertiesToUse, throwException)
			{
				_IsValueSet = false;
				_Value = -1;
				foreach (IScriptEnum internalEnum in InternalEnums)
				{
					internalEnum.Execute(ref _IsValueSet, ref _Value, code, propertiesToUse);
				}
				if (!_IsValueSet && (propertiesToUse & InstructionInclude.Value) != InstructionInclude.None)
				{
					Constant[] allConstants = AllConstants;
					for (int i = 0; i < allConstants.Length; i++)
					{
						Constant constant = allConstants[i];
						if (constant == code)
						{
							_Value = (int)constant.Value;
							return;
						}
					}
					if (int.TryParse(code, out _Value))
					{
						_IsValueSet = true;
					}
				}
				if (!_IsValueSet && throwException)
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.InvalidInteger, _LineNumber);
				}
			}

			public int GetVariableValue(_AliasTarget type, bool throwException = true)
			{
				if ((type & _AliasTarget.Register) != _AliasTarget.None && TryParseAliasAsValue(type, out var value, throwException))
				{
					return (int)value;
				}
				if ((_PropertiesToUse & InstructionInclude.Define) != InstructionInclude.None && _Chip._Defines.TryGetValue(_Alias, out var value2))
				{
					return (int)value2;
				}
				if ((_PropertiesToUse & InstructionInclude.JumpTag) != InstructionInclude.None && TryParseAliasAsJumpTagValue(out var value3, throwException))
				{
					return value3;
				}
				if ((_PropertiesToUse & InstructionInclude.RegisterIndex) != InstructionInclude.None && TryParseRegisterIndexAsValue(out var value4, throwException))
				{
					return (int)Math.Round(value4);
				}
				if ((_PropertiesToUse & (InstructionInclude.Value | InstructionInclude.Enum | InstructionInclude.LogicType | InstructionInclude.LogicSlotType | InstructionInclude.LogicReagentMode | InstructionInclude.LogicBatchMethod)) != InstructionInclude.None && _IsValueSet)
				{
					return _Value;
				}
				if (throwException)
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.InvalidInteger, _LineNumber);
				}
				return 0;
			}
		}

		protected class EnumValuedVariable<T> : ValueVariable where T : Enum, IConvertible, new()
		{
			private readonly bool _IsValueSet;

			private readonly int _Value = -1;

			public EnumValuedVariable(ProgrammableChip chip, int lineNumber, string code, InstructionInclude propertiesToUse, bool throwException = true)
				: base(chip, lineNumber, code, propertiesToUse, throwException)
			{
				_IsValueSet = false;
				_Value = -1;
				if (_IsValueSet || (propertiesToUse & InstructionInclude.Value) == 0)
				{
					return;
				}
				try
				{
					_Value = GetTypeOf(code).ToInt32(CultureInfo.InvariantCulture);
					_IsValueSet = true;
					return;
				}
				catch
				{
				}
				Constant[] allConstants = AllConstants;
				for (int i = 0; i < allConstants.Length; i++)
				{
					Constant constant = allConstants[i];
					if (constant == code)
					{
						_Value = (int)constant.Value;
						_IsValueSet = true;
						return;
					}
				}
				if (int.TryParse(code, out _Value))
				{
					_IsValueSet = true;
				}
			}

			private T GetTypeOf(string logicTypeCode)
			{
				if (!Enum.IsDefined(typeof(T), logicTypeCode))
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, _LineNumber);
				}
				return (T)Enum.Parse(typeof(T), logicTypeCode);
			}

			public T GetVariableValue(_AliasTarget type, bool throwException = true)
			{
				if ((type & _AliasTarget.Register) != _AliasTarget.None && TryParseAliasAsValue(type, out var value, throwException))
				{
					return (T)Enum.ToObject(typeof(T), (int)value);
				}
				if ((_PropertiesToUse & InstructionInclude.Define) != InstructionInclude.None && _Chip._Defines.TryGetValue(_Alias, out var value2))
				{
					return (T)Enum.ToObject(typeof(T), (int)value2);
				}
				if ((_PropertiesToUse & InstructionInclude.JumpTag) != InstructionInclude.None && TryParseAliasAsJumpTagValue(out var value3, throwException))
				{
					return (T)Enum.ToObject(typeof(T), value3);
				}
				if ((_PropertiesToUse & InstructionInclude.RegisterIndex) != InstructionInclude.None && TryParseRegisterIndexAsValue(out var value4, throwException))
				{
					return (T)Enum.ToObject(typeof(T), (int)value4);
				}
				if ((_PropertiesToUse & InstructionInclude.Value) != InstructionInclude.None && _IsValueSet)
				{
					return (T)Enum.ToObject(typeof(T), _Value);
				}
				if (throwException)
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, _LineNumber);
				}
				if (throwException)
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, _LineNumber);
				}
				return default(T);
			}
		}

		protected readonly ProgrammableChip _Chip;

		protected readonly int _LineNumber;

		protected const string _ZeroString = "0";

		public _Operation(ProgrammableChip chip, int lineNumber)
		{
			_Chip = chip;
			_LineNumber = lineNumber;
		}

		protected LogicType _GetLogicType(string logicTypeCode)
		{
			if (!Enum.IsDefined(typeof(LogicType), logicTypeCode))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, _LineNumber);
			}
			return (LogicType)Enum.Parse(typeof(LogicType), logicTypeCode);
		}

		protected LogicSlotType _GetLogicSlotType(string logicTypeCode)
		{
			if (!Enum.IsDefined(typeof(LogicSlotType), logicTypeCode))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, _LineNumber);
			}
			return (LogicSlotType)Enum.Parse(typeof(LogicSlotType), logicTypeCode);
		}

		protected LogicReagentMode _GetLogicReagentMode(string logicReagentModeCode)
		{
			if (!Enum.IsDefined(typeof(LogicReagentMode), logicReagentModeCode))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectReagentMode, _LineNumber);
			}
			return (LogicReagentMode)Enum.Parse(typeof(LogicReagentMode), logicReagentModeCode);
		}

		protected void _SetDeviceValue(Device device, LogicType logicType, double value)
		{
			if (device == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotSet, _LineNumber);
			}
			device.SetLogicValue(logicType, value);
		}

		public abstract int Execute(int index);

		protected static IDeviceVariable _MakeDeviceVariable(ProgrammableChip chip, int lineNumber, string deviceCode)
		{
			if (Regex.IsMatch(deviceCode, "^r+(?:1[0-5]|[0-9])$"))
			{
				return new DirectDeviceVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDoubleValue | InstructionInclude.DeviceIndex, throwException: false);
			}
			if (chip._Defines.ContainsKey(deviceCode))
			{
				return new DirectDeviceVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDoubleValue, throwException: false);
			}
			if (deviceCode.Length > 0 && (deviceCode[0] == '$' || deviceCode[0] == '%' || char.IsDigit(deviceCode[0])))
			{
				return new DirectDeviceVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDoubleValue | InstructionInclude.DeviceIndex | InstructionInclude.NetworkIndex, throwException: false);
			}
			if (deviceCode.Length > 1 && deviceCode[0] == 'r' && char.IsDigit(deviceCode[1]))
			{
				return new DirectDeviceVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDoubleValue | InstructionInclude.DeviceIndex | InstructionInclude.NetworkIndex, throwException: false);
			}
			string[] array = deviceCode.Split(':');
			if (array.Length != 0 && array[0].StartsWith('d'))
			{
				string text = array[0];
				if (text == "db")
				{
					return new DeviceIndexVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDeviceIndex, throwException: false);
				}
				if (Regex.IsMatch(text, "^(d[0-9]|dr*[r0-9][0-9])$"))
				{
					return new DeviceIndexVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDeviceIndex, throwException: false);
				}
			}
			return new DeviceAliasVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDoubleValue | InstructionInclude.DeviceIndex | InstructionInclude.NetworkIndex, throwException: false);
		}
	}

	private abstract class _Operation_I : _Operation_1_0
	{
		protected readonly IntValuedVariable _DeviceId;

		public _Operation_I(ProgrammableChip chip, int lineNumber, string registerStoreCode, string referenceId)
			: base(chip, lineNumber, registerStoreCode)
		{
			_DeviceId = new IntValuedVariable(chip, lineNumber, referenceId, InstructionInclude.MaskDoubleValue, throwException: false);
		}
	}

	private abstract class _Operation_1_0 : _Operation
	{
		protected IndexVariable _Store;

		public _Operation_1_0(ProgrammableChip chip, int lineNumber, string registerStoreCode)
			: base(chip, lineNumber)
		{
			_Store = new IndexVariable(chip, lineNumber, registerStoreCode, InstructionInclude.MaskStoreIndex, throwException: false);
		}
	}

	private abstract class _Operation_1_1 : _Operation_1_0
	{
		protected DoubleValueVariable _Argument1;

		public _Operation_1_1(ProgrammableChip chip, int lineNumber, string registerStoreCode, string argument1Code)
			: base(chip, lineNumber, registerStoreCode)
		{
			_Argument1 = new DoubleValueVariable(chip, lineNumber, argument1Code, InstructionInclude.MaskDoubleValue, throwException: false);
		}
	}

	private abstract class _Operation_1_2 : _Operation_1_1
	{
		protected DoubleValueVariable _Argument2;

		public _Operation_1_2(ProgrammableChip chip, int lineNumber, string registerStoreCode, string argument1Code, string argument2Code)
			: base(chip, lineNumber, registerStoreCode, argument1Code)
		{
			_Argument2 = new DoubleValueVariable(chip, lineNumber, argument2Code, InstructionInclude.MaskDoubleValue, throwException: false);
		}
	}

	private abstract class _Operation_1_3 : _Operation_1_2
	{
		protected DoubleValueVariable _Argument3;

		public _Operation_1_3(ProgrammableChip chip, int lineNumber, string registerStoreCode, string argument1Code, string argument2Code, string argument3Code)
			: base(chip, lineNumber, registerStoreCode, argument1Code, argument2Code)
		{
			_Argument3 = new DoubleValueVariable(chip, lineNumber, argument3Code, InstructionInclude.MaskDoubleValue, throwException: false);
		}
	}

	private abstract class _Operation_J_0 : _Operation
	{
		protected readonly LineNumberVariable _JumpIndex;

		public _Operation_J_0(ProgrammableChip chip, int lineNumber, string jumpAddressCode)
			: base(chip, lineNumber)
		{
			_JumpIndex = new LineNumberVariable(chip, lineNumber, jumpAddressCode, InstructionInclude.MaskDoubleValue, throwException: false);
		}
	}

	private abstract class _Operation_J_1 : _Operation_J_0
	{
		protected DoubleValueVariable _Argument1;

		public _Operation_J_1(ProgrammableChip chip, int lineNumber, string argument1Code, string jumpAddressCode)
			: base(chip, lineNumber, jumpAddressCode)
		{
			_Argument1 = new DoubleValueVariable(chip, lineNumber, argument1Code, InstructionInclude.MaskDoubleValue, throwException: false);
		}
	}

	private abstract class _Operation_J_2 : _Operation_J_1
	{
		protected DoubleValueVariable _Argument2;

		public _Operation_J_2(ProgrammableChip chip, int lineNumber, string argument1Code, string argument2Code, string jumpAddressCode)
			: base(chip, lineNumber, argument1Code, jumpAddressCode)
		{
			_Argument2 = new DoubleValueVariable(chip, lineNumber, argument2Code, InstructionInclude.MaskDoubleValue, throwException: false);
		}
	}

	private abstract class _Operation_J_3 : _Operation_J_2
	{
		protected DoubleValueVariable _Argument3;

		public _Operation_J_3(ProgrammableChip chip, int lineNumber, string argument1Code, string argument2Code, string argument3Code, string jumpAddressCode)
			: base(chip, lineNumber, argument1Code, argument2Code, jumpAddressCode)
		{
			_Argument3 = new DoubleValueVariable(chip, lineNumber, argument3Code, InstructionInclude.MaskDoubleValue, throwException: false);
		}
	}

	private class _L_Operation : _Operation_1_0
	{
		protected readonly IDeviceVariable _DeviceIndex;

		protected readonly EnumValuedVariable<LogicType> _LogicType;

		public _L_Operation(ProgrammableChip chip, int lineNumber, string registerCode, string deviceCode, string logicTypeCode)
			: base(chip, lineNumber, registerCode)
		{
			_DeviceIndex = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
			_LogicType = new EnumValuedVariable<LogicType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicType, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			ILogicable device = _DeviceIndex.GetDevice(_Chip.CircuitHousing);
			if (device == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber);
			}
			LogicType variableValue = _LogicType.GetVariableValue(_AliasTarget.Register);
			if (variableValue == LogicType.None)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.LogicTypeIsNone, _LineNumber);
			}
			if (!device.CanLogicRead(variableValue))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, _LineNumber);
			}
			_Chip._Registers[variableIndex] = device.GetLogicValue(variableValue);
			return index + 1;
		}
	}

	private class _LD_Operation : _Operation_I
	{
		protected readonly EnumValuedVariable<LogicType> _LogicType;

		public _LD_Operation(ProgrammableChip chip, int lineNumber, string registerCode, string referenceId, string logicTypeCode)
			: base(chip, lineNumber, registerCode, referenceId)
		{
			_LogicType = new EnumValuedVariable<LogicType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicType, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			int variableValue = _DeviceId.GetVariableValue(_AliasTarget.Register);
			ILogicable logicableFromId = _Chip.CircuitHousing.GetLogicableFromId(variableValue);
			LogicType variableValue2 = _LogicType.GetVariableValue(_AliasTarget.Register);
			if (variableValue2 == LogicType.None)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.LogicTypeIsNone, _LineNumber);
			}
			if (!logicableFromId.CanLogicRead(variableValue2))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, _LineNumber);
			}
			_Chip._Registers[variableIndex] = logicableFromId.GetLogicValue(variableValue2);
			return index + 1;
		}
	}

	private class _S_Operation : _Operation
	{
		protected readonly IDeviceVariable _DeviceIndex;

		protected readonly DoubleValueVariable _Argument1;

		protected readonly EnumValuedVariable<LogicType> _LogicType;

		public _S_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string logicTypeCode, string registerOrValueCode)
			: base(chip, lineNumber)
		{
			_DeviceIndex = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
			_Argument1 = new DoubleValueVariable(chip, lineNumber, registerOrValueCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_LogicType = new EnumValuedVariable<LogicType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicType, throwException: false);
		}

		public override int Execute(int index)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			LogicType variableValue2 = _LogicType.GetVariableValue(_AliasTarget.Register);
			if (variableValue2 == LogicType.None)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.LogicTypeIsNone, _LineNumber);
			}
			ILogicable device = _DeviceIndex.GetDevice(_Chip.CircuitHousing);
			if (device == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber);
			}
			double logicValue = device.GetLogicValue(variableValue2);
			if (!device.CanLogicWrite(variableValue2))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, _LineNumber);
			}
			if (logicValue != variableValue)
			{
				device.SetLogicValue(variableValue2, variableValue);
			}
			return index + 1;
		}
	}

	private class _SD_Operation : _Operation
	{
		protected readonly IntValuedVariable _DeviceId;

		protected readonly DoubleValueVariable _Argument1;

		protected readonly EnumValuedVariable<LogicType> _LogicType;

		public _SD_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string logicTypeCode, string registerOrValueCode)
			: base(chip, lineNumber)
		{
			_DeviceId = new IntValuedVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_Argument1 = new DoubleValueVariable(chip, lineNumber, registerOrValueCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_LogicType = new EnumValuedVariable<LogicType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicType, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableValue = _DeviceId.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument1.GetVariableValue(_AliasTarget.Register);
			LogicType variableValue3 = _LogicType.GetVariableValue(_AliasTarget.Register);
			ILogicable logicableFromId = _Chip.CircuitHousing.GetLogicableFromId(variableValue);
			if (logicableFromId == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber);
			}
			double logicValue = logicableFromId.GetLogicValue(variableValue3);
			if (!logicableFromId.CanLogicWrite(variableValue3))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, _LineNumber);
			}
			if (logicValue != variableValue2)
			{
				logicableFromId.SetLogicValue(variableValue3, variableValue2);
			}
			return index + 1;
		}
	}

	private class _SS_Operation : _Operation
	{
		protected readonly IDeviceVariable _DeviceRef;

		protected readonly DoubleValueVariable _Argument1;

		protected readonly EnumValuedVariable<LogicSlotType> _LogicType;

		protected readonly IntValuedVariable _SlotIndex;

		public _SS_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string slotIndex, string logicTypeCode, string registerOrValueCode)
			: base(chip, lineNumber)
		{
			_DeviceRef = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
			_Argument1 = new DoubleValueVariable(chip, lineNumber, registerOrValueCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_SlotIndex = new IntValuedVariable(chip, lineNumber, slotIndex, InstructionInclude.MaskDoubleValue, throwException: false);
			_LogicType = new EnumValuedVariable<LogicSlotType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicType, throwException: false);
		}

		public override int Execute(int index)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			LogicSlotType variableValue2 = _LogicType.GetVariableValue(_AliasTarget.Register);
			int variableValue3 = _SlotIndex.GetVariableValue(_AliasTarget.Register);
			ILogicable obj = _DeviceRef.GetDevice(_Chip.CircuitHousing) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber);
			if (!(obj is ISlotWriteable slotWriteable))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotSlotWriteable, _LineNumber);
			}
			if (!slotWriteable.CanLogicWrite(variableValue2, variableValue3))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicSlotType, _LineNumber);
			}
			if (obj.GetLogicValue(variableValue2, variableValue3) != variableValue)
			{
				slotWriteable.SetLogicValue(variableValue2, variableValue3, variableValue);
			}
			return index + 1;
		}
	}

	private class _SBS_Operation : _Operation
	{
		protected readonly IntValuedVariable _DeviceHash;

		protected readonly DoubleValueVariable _Argument1;

		protected readonly EnumValuedVariable<LogicSlotType> _LogicType;

		protected readonly IntValuedVariable _SlotIndex;

		public _SBS_Operation(ProgrammableChip chip, int lineNumber, string deviceHash, string slotIndex, string logicTypeCode, string registerOrValueCode)
			: base(chip, lineNumber)
		{
			_DeviceHash = new IntValuedVariable(chip, lineNumber, deviceHash, InstructionInclude.MaskDoubleValue, throwException: false);
			_Argument1 = new DoubleValueVariable(chip, lineNumber, registerOrValueCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_SlotIndex = new IntValuedVariable(chip, lineNumber, slotIndex, InstructionInclude.MaskDoubleValue, throwException: false);
			_LogicType = new EnumValuedVariable<LogicSlotType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicType, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableValue = _DeviceHash.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument1.GetVariableValue(_AliasTarget.Register);
			LogicSlotType variableValue3 = _LogicType.GetVariableValue(_AliasTarget.Register);
			int variableValue4 = _SlotIndex.GetVariableValue(_AliasTarget.Register);
			List<ILogicable> batchOutput = _Chip.CircuitHousing.GetBatchOutput();
			if (batchOutput == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, _LineNumber);
			}
			int count = batchOutput.Count;
			while (count-- > 0)
			{
				ILogicable logicable = batchOutput[count];
				if (logicable != null && logicable.GetPrefabHash() == variableValue)
				{
					if (!(logicable is ISlotWriteable slotWriteable))
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotSlotWriteable, _LineNumber);
					}
					if (!slotWriteable.CanLogicWrite(variableValue3, variableValue4))
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicSlotType, _LineNumber);
					}
					if (slotWriteable.GetLogicValue(variableValue3, variableValue4) != variableValue2)
					{
						slotWriteable.SetLogicValue(variableValue3, variableValue4, variableValue2);
					}
				}
			}
			return index + 1;
		}
	}

	private class _LB_Operation : _Operation_1_0
	{
		protected readonly IntValuedVariable _DeviceHash;

		protected readonly EnumValuedVariable<LogicType> _LogicType;

		protected readonly EnumValuedVariable<LogicBatchMethod> _BatchMode;

		public _LB_Operation(ProgrammableChip chip, int lineNumber, string registerCode, string deviceCode, string logicTypeCode, string logicBatchMode)
			: base(chip, lineNumber, registerCode)
		{
			_DeviceHash = new IntValuedVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_LogicType = new EnumValuedVariable<LogicType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicType, throwException: false);
			_BatchMode = new EnumValuedVariable<LogicBatchMethod>(chip, lineNumber, logicBatchMode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicBatchMethod, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			int variableValue = _DeviceHash.GetVariableValue(_AliasTarget.Register);
			LogicType variableValue2 = _LogicType.GetVariableValue(_AliasTarget.Register);
			if (variableValue2 == LogicType.None)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.LogicTypeIsNone, _LineNumber);
			}
			LogicBatchMethod variableValue3 = _BatchMode.GetVariableValue(_AliasTarget.Register);
			List<ILogicable> batchOutput = _Chip.CircuitHousing.GetBatchOutput();
			if (batchOutput == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, _LineNumber);
			}
			int count = batchOutput.Count;
			while (count-- > 0)
			{
				ILogicable logicable = batchOutput[count];
				if (logicable != null && logicable.GetPrefabHash() == variableValue && !logicable.CanLogicRead(variableValue2))
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, _LineNumber);
				}
			}
			_Chip._Registers[variableIndex] = Device.BatchRead(variableValue3, variableValue2, variableValue, batchOutput);
			return index + 1;
		}
	}

	private class _LBN_Operation : _Operation_1_0
	{
		protected readonly IntValuedVariable _DeviceHash;

		protected readonly IntValuedVariable _NameHash;

		protected readonly EnumValuedVariable<LogicType> _LogicType;

		protected readonly EnumValuedVariable<LogicBatchMethod> _BatchMode;

		public _LBN_Operation(ProgrammableChip chip, int lineNumber, string registerCode, string deviceCode, string nameCode, string logicTypeCode, string logicBatchMode)
			: base(chip, lineNumber, registerCode)
		{
			_DeviceHash = new IntValuedVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_NameHash = new IntValuedVariable(chip, lineNumber, nameCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_LogicType = new EnumValuedVariable<LogicType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicType, throwException: false);
			_BatchMode = new EnumValuedVariable<LogicBatchMethod>(chip, lineNumber, logicBatchMode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicBatchMethod, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			int variableValue = _DeviceHash.GetVariableValue(_AliasTarget.Register);
			int variableValue2 = _NameHash.GetVariableValue(_AliasTarget.Register);
			LogicType variableValue3 = _LogicType.GetVariableValue(_AliasTarget.Register);
			LogicBatchMethod variableValue4 = _BatchMode.GetVariableValue(_AliasTarget.Register);
			List<ILogicable> batchOutput = _Chip.CircuitHousing.GetBatchOutput();
			if (batchOutput == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, _LineNumber);
			}
			int count = batchOutput.Count;
			while (count-- > 0)
			{
				ILogicable logicable = batchOutput[count];
				if (logicable != null && logicable.GetPrefabHash() == variableValue && logicable.GetNameHash() == variableValue2 && !logicable.CanLogicRead(variableValue3))
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, _LineNumber);
				}
			}
			_Chip._Registers[variableIndex] = Device.BatchRead(variableValue4, variableValue3, variableValue, variableValue2, batchOutput);
			return index + 1;
		}
	}

	private class _SB_Operation : _Operation
	{
		protected readonly IntValuedVariable _DeviceHash;

		protected readonly DoubleValueVariable _Argument1;

		protected readonly EnumValuedVariable<LogicType> _LogicType;

		public _SB_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string logicTypeCode, string registerOrValueCode)
			: base(chip, lineNumber)
		{
			_DeviceHash = new IntValuedVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_Argument1 = new DoubleValueVariable(chip, lineNumber, registerOrValueCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_LogicType = new EnumValuedVariable<LogicType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicType, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableValue = _DeviceHash.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument1.GetVariableValue(_AliasTarget.Register);
			LogicType variableValue3 = _LogicType.GetVariableValue(_AliasTarget.Register);
			if (variableValue3 == LogicType.None)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.LogicTypeIsNone, _LineNumber);
			}
			List<ILogicable> batchOutput = _Chip.CircuitHousing.GetBatchOutput();
			if (batchOutput == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, _LineNumber);
			}
			int count = batchOutput.Count;
			while (count-- > 0)
			{
				ILogicable logicable = batchOutput[count];
				if (logicable != null && logicable.GetPrefabHash() == variableValue)
				{
					if (!logicable.CanLogicWrite(variableValue3))
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, _LineNumber);
					}
					if (logicable.GetLogicValue(variableValue3) != variableValue2)
					{
						logicable.SetLogicValue(variableValue3, variableValue2);
					}
				}
			}
			return index + 1;
		}
	}

	private class _SBN_Operation : _Operation
	{
		protected readonly IntValuedVariable _DeviceHash;

		protected readonly IntValuedVariable _NameHash;

		protected readonly DoubleValueVariable _Argument1;

		protected readonly EnumValuedVariable<LogicType> _LogicType;

		public _SBN_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string nameHash, string logicTypeCode, string registerOrValueCode)
			: base(chip, lineNumber)
		{
			_DeviceHash = new IntValuedVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_NameHash = new IntValuedVariable(chip, lineNumber, nameHash, InstructionInclude.MaskDoubleValue, throwException: false);
			_Argument1 = new DoubleValueVariable(chip, lineNumber, registerOrValueCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_LogicType = new EnumValuedVariable<LogicType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicType, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableValue = _DeviceHash.GetVariableValue(_AliasTarget.Register);
			int variableValue2 = _NameHash.GetVariableValue(_AliasTarget.Register);
			double variableValue3 = _Argument1.GetVariableValue(_AliasTarget.Register);
			LogicType variableValue4 = _LogicType.GetVariableValue(_AliasTarget.Register);
			if (variableValue4 == LogicType.None)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.LogicTypeIsNone, _LineNumber);
			}
			List<ILogicable> batchOutput = _Chip.CircuitHousing.GetBatchOutput();
			if (batchOutput == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, _LineNumber);
			}
			int count = batchOutput.Count;
			while (count-- > 0)
			{
				ILogicable logicable = batchOutput[count];
				if (logicable != null && logicable.GetPrefabHash() == variableValue && logicable.GetNameHash() == variableValue2)
				{
					if (!logicable.CanLogicWrite(variableValue4))
					{
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, _LineNumber);
					}
					if (logicable.GetLogicValue(variableValue4) != variableValue3)
					{
						logicable.SetLogicValue(variableValue4, variableValue3);
					}
				}
			}
			return index + 1;
		}
	}

	private class _LS_Operation : _Operation_1_0
	{
		protected readonly IDeviceVariable _DeviceRef;

		protected readonly IntValuedVariable _SlotIndex;

		protected readonly EnumValuedVariable<LogicSlotType> _LogicType;

		public _LS_Operation(ProgrammableChip chip, int lineNumber, string registerCode, string deviceCode, string slotCode, string logicTypeCode)
			: base(chip, lineNumber, registerCode)
		{
			_DeviceRef = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
			_SlotIndex = new IntValuedVariable(chip, lineNumber, slotCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_LogicType = new EnumValuedVariable<LogicSlotType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicSlotType, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			int variableValue = _SlotIndex.GetVariableValue(_AliasTarget.Register);
			ILogicable device = _DeviceRef.GetDevice(_Chip.CircuitHousing);
			if (device == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber);
			}
			LogicSlotType variableValue2 = _LogicType.GetVariableValue(_AliasTarget.Register);
			if (!device.CanLogicRead(variableValue2, variableValue))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, _LineNumber);
			}
			_Chip._Registers[variableIndex] = device.GetLogicValue(variableValue2, variableValue);
			return index + 1;
		}
	}

	private class _LBS_Operation : _Operation_1_0
	{
		protected readonly IntValuedVariable _DeviceHash;

		protected readonly IntValuedVariable _SlotIndex;

		protected readonly EnumValuedVariable<LogicSlotType> _LogicType;

		protected readonly EnumValuedVariable<LogicBatchMethod> _BatchMode;

		public _LBS_Operation(ProgrammableChip chip, int lineNumber, string registerCode, string deviceCode, string slotCode, string logicTypeCode, string logicBatchMode)
			: base(chip, lineNumber, registerCode)
		{
			_DeviceHash = new IntValuedVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_SlotIndex = new IntValuedVariable(chip, lineNumber, slotCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_LogicType = new EnumValuedVariable<LogicSlotType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicSlotType, throwException: false);
			_BatchMode = new EnumValuedVariable<LogicBatchMethod>(chip, lineNumber, logicBatchMode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicBatchMethod, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			int variableValue = _DeviceHash.GetVariableValue(_AliasTarget.Register);
			int variableValue2 = _SlotIndex.GetVariableValue(_AliasTarget.Register);
			LogicSlotType variableValue3 = _LogicType.GetVariableValue(_AliasTarget.Register);
			LogicBatchMethod variableValue4 = _BatchMode.GetVariableValue(_AliasTarget.Register);
			List<ILogicable> batchOutput = _Chip.CircuitHousing.GetBatchOutput();
			if (batchOutput == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, _LineNumber);
			}
			int count = batchOutput.Count;
			while (count-- > 0)
			{
				ILogicable logicable = batchOutput[count];
				if (logicable != null && logicable.GetPrefabHash() == variableValue && !logicable.CanLogicRead(variableValue3, variableValue2))
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicSlotType, _LineNumber);
				}
			}
			_Chip._Registers[variableIndex] = Device.BatchRead(variableValue4, variableValue3, variableValue2, variableValue, batchOutput);
			return index + 1;
		}
	}

	private class _LBNS_Operation : _Operation_1_0
	{
		protected readonly IntValuedVariable _DeviceHash;

		protected readonly IntValuedVariable _NameHash;

		protected readonly IntValuedVariable _SlotIndex;

		protected readonly EnumValuedVariable<LogicSlotType> _LogicType;

		protected readonly EnumValuedVariable<LogicBatchMethod> _BatchMode;

		public _LBNS_Operation(ProgrammableChip chip, int lineNumber, string registerCode, string deviceCode, string nameCode, string slotCode, string logicTypeCode, string logicBatchMode)
			: base(chip, lineNumber, registerCode)
		{
			_DeviceHash = new IntValuedVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_NameHash = new IntValuedVariable(chip, lineNumber, nameCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_SlotIndex = new IntValuedVariable(chip, lineNumber, slotCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_LogicType = new EnumValuedVariable<LogicSlotType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicSlotType, throwException: false);
			_BatchMode = new EnumValuedVariable<LogicBatchMethod>(chip, lineNumber, logicBatchMode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicBatchMethod, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			int variableValue = _DeviceHash.GetVariableValue(_AliasTarget.Register);
			int variableValue2 = _NameHash.GetVariableValue(_AliasTarget.Register);
			int variableValue3 = _SlotIndex.GetVariableValue(_AliasTarget.Register);
			LogicSlotType variableValue4 = _LogicType.GetVariableValue(_AliasTarget.Register);
			LogicBatchMethod variableValue5 = _BatchMode.GetVariableValue(_AliasTarget.Register);
			List<ILogicable> batchOutput = _Chip.CircuitHousing.GetBatchOutput();
			if (batchOutput == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, _LineNumber);
			}
			int count = batchOutput.Count;
			while (count-- > 0)
			{
				ILogicable logicable = batchOutput[count];
				if (logicable != null && logicable.GetPrefabHash() == variableValue && logicable.GetNameHash() == variableValue2 && !logicable.CanLogicRead(variableValue4, variableValue3))
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicSlotType, _LineNumber);
				}
			}
			_Chip._Registers[variableIndex] = Device.BatchRead(variableValue5, variableValue4, variableValue3, variableValue, variableValue2, batchOutput);
			return index + 1;
		}
	}

	private class _LR_Operation : _Operation_1_0
	{
		protected readonly IDeviceVariable _DeviceRef;

		protected readonly EnumValuedVariable<LogicReagentMode> _LogicReagentMode;

		protected readonly IntValuedVariable _ReagentInt;

		public _LR_Operation(ProgrammableChip chip, int lineNumber, string registerCode, string deviceCode, string logicReagentModeCode, string reagentCode)
			: base(chip, lineNumber, registerCode)
		{
			_DeviceRef = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
			_LogicReagentMode = new EnumValuedVariable<LogicReagentMode>(chip, lineNumber, logicReagentModeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicReagentMode, throwException: false);
			_ReagentInt = new IntValuedVariable(chip, lineNumber, reagentCode, InstructionInclude.MaskDoubleValue, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			LogicReagentMode variableValue = _LogicReagentMode.GetVariableValue(_AliasTarget.Register);
			ILogicable device = _DeviceRef.GetDevice(_Chip.CircuitHousing);
			if (device == null)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber);
			}
			switch (variableValue)
			{
			case LogicReagentMode.Contents:
				if (!(device is Device device3))
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectReagentDevice, _LineNumber);
				}
				_Chip._Registers[variableIndex] = device3.ReadableReagentMixture.Get(Reagent.Find(_ReagentInt.GetVariableValue(_AliasTarget.Register)));
				break;
			case LogicReagentMode.Required:
				if (!(device is IRequireReagent requireReagent))
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectReagentDevice, _LineNumber);
				}
				_Chip._Registers[variableIndex] = requireReagent.RequiredReagents.Get(Reagent.Find(_ReagentInt.GetVariableValue(_AliasTarget.Register)));
				break;
			case LogicReagentMode.Recipe:
				if (!(device is IRequireReagent requireReagent2))
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectReagentDevice, _LineNumber);
				}
				_Chip._Registers[variableIndex] = requireReagent2.CurrentRecipe.Get(Reagent.Find(_ReagentInt.GetVariableValue(_AliasTarget.Register)));
				break;
			case LogicReagentMode.TotalContents:
				if (!(device is Device device2))
				{
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectReagentDevice, _LineNumber);
				}
				_Chip._Registers[variableIndex] = device2.ReadableReagentMixture.TotalReagents;
				break;
			default:
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.UnhandledReagentMode, _LineNumber);
			}
			return index + 1;
		}
	}

	private class _LABEL_Operation : _ALIAS_Operation
	{
		public _LABEL_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string labelCode)
			: base(chip, lineNumber, labelCode, deviceCode)
		{
		}
	}

	private class _ALIAS_Operation : _Operation
	{
		private readonly string _AliasCode;

		private readonly _AliasTarget _TargetType;

		private readonly IndexVariable _Target;

		public _ALIAS_Operation(ProgrammableChip chip, int lineNumber, string aliasCode, string targetCode)
			: base(chip, lineNumber)
		{
			_AliasCode = aliasCode;
			if (targetCode[0] == 'r')
			{
				_TargetType = _AliasTarget.Register;
				_Target = new IndexVariable(chip, lineNumber, targetCode, InstructionInclude.MaskStoreIndex | InstructionInclude.JumpTag, throwException: false);
			}
			else if (targetCode[0] == 'd')
			{
				_TargetType = _AliasTarget.Device;
				_Target = new DeviceIndexVariable(chip, lineNumber, targetCode, InstructionInclude.Alias | InstructionInclude.JumpTag | InstructionInclude.DeviceIndex, throwException: false);
			}
		}

		public override int Execute(int index)
		{
			return Execute(index, updateLabels: true);
		}

		public int Execute(int index, bool updateLabels)
		{
			int variableIndex = _Target.GetVariableIndex(_TargetType);
			_AliasValue value = new _AliasValue(_TargetType, variableIndex);
			if (value.Index < 0 || (value.Target == _AliasTarget.Register && value.Index >= _Chip._Registers.Length) || (value.Target == _AliasTarget.Device && !_Chip.CircuitHousing.IsValidIndex(value.Index)))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IndexOutOfRange, _LineNumber);
			}
			if (_Chip._Aliases.ContainsKey(_AliasCode))
			{
				_AliasValue aliasValue = _Chip._Aliases[_AliasCode];
				if (_Chip._Aliases[_AliasCode].Target == _AliasTarget.Device)
				{
					_Chip.CircuitHousing.SetDeviceLabel(aliasValue.Index, "");
				}
				_Chip._Aliases[_AliasCode] = value;
			}
			else
			{
				_Chip._Aliases.Add(_AliasCode, value);
			}
			if (value.Target == _AliasTarget.Device)
			{
				_Chip.CircuitHousing.SetDeviceLabel(value.Index, _AliasCode);
			}
			return index + 1;
		}
	}

	private class _DEFINE_Operation : _Operation
	{
		public _DEFINE_Operation(ProgrammableChip chip, int lineNumber, string defineCode, string floatCode)
			: base(chip, lineNumber)
		{
			DoubleValueVariable doubleValueVariable = new DoubleValueVariable(chip, lineNumber, floatCode, InstructionInclude.MaskDefineValue);
			if (chip._Defines.ContainsKey(defineCode))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ExtraDefine, lineNumber);
			}
			chip._Defines.Add(defineCode, doubleValueVariable.Get());
		}

		public override int Execute(int index)
		{
			return index + 1;
		}
	}

	private class _MOVE_Operation : _Operation_1_1
	{
		public _MOVE_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = variableValue;
			return index + 1;
		}
	}

	private class _ADD_Operation : _Operation_1_2
	{
		public _ADD_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = variableValue + variableValue2;
			return index + 1;
		}
	}

	private class _SUB_Operation : _Operation_1_2
	{
		public _SUB_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = variableValue - variableValue2;
			return index + 1;
		}
	}

	private class _SDSE_Operation : _Operation_1_0
	{
		protected readonly IDeviceVariable _DeviceRef;

		public _SDSE_Operation(ProgrammableChip chip, int lineNumber, string registerCode, string deviceCode)
			: base(chip, lineNumber, registerCode)
		{
			_DeviceRef = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			ILogicable device = _DeviceRef.GetDevice(_Chip.CircuitHousing);
			_Chip._Registers[variableIndex] = ((device == null) ? 0f : 1f);
			return index + 1;
		}
	}

	private class _SDNS_Operation : _Operation_1_0
	{
		protected readonly IDeviceVariable _DeviceRef;

		public _SDNS_Operation(ProgrammableChip chip, int lineNumber, string registerCode, string deviceCode)
			: base(chip, lineNumber, registerCode)
		{
			_DeviceRef = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			ILogicable device = _DeviceRef.GetDevice(_Chip.CircuitHousing);
			_Chip._Registers[variableIndex] = ((device == null) ? 1f : 0f);
			return index + 1;
		}
	}

	private class _SLT_Operation : _Operation_1_2
	{
		public _SLT_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = ((variableValue < variableValue2) ? 1.0 : 0.0);
			return index + 1;
		}
	}

	private class _SGT_Operation : _Operation_1_2
	{
		public _SGT_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = ((variableValue > variableValue2) ? 1.0 : 0.0);
			return index + 1;
		}
	}

	private class _SLE_Operation : _Operation_1_2
	{
		public _SLE_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = ((variableValue <= variableValue2) ? 1.0 : 0.0);
			return index + 1;
		}
	}

	private class _SGE_Operation : _Operation_1_2
	{
		public _SGE_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = ((variableValue >= variableValue2) ? 1.0 : 0.0);
			return index + 1;
		}
	}

	private class _SEQ_Operation : _Operation_1_2
	{
		public _SEQ_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = ((variableValue == variableValue2) ? 1.0 : 0.0);
			return index + 1;
		}
	}

	private class _SNE_Operation : _Operation_1_2
	{
		public _SNE_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = ((variableValue != variableValue2) ? 1.0 : 0.0);
			return index + 1;
		}
	}

	private class _SAP_Operation : _Operation_1_3
	{
		public _SAP_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code, string registerArgument3Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code, registerArgument3Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			double variableValue3 = _Argument3.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = ((Math.Abs(variableValue - variableValue2) <= Math.Max(variableValue3 * Math.Max(Math.Abs(variableValue), Math.Abs(variableValue2)), 1.1210387714598537E-44)) ? 1.0 : 0.0);
			return index + 1;
		}
	}

	private class _SNA_Operation : _Operation_1_3
	{
		public _SNA_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code, string registerArgument3Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code, registerArgument3Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			double variableValue3 = _Argument3.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = ((Math.Abs(variableValue - variableValue2) > Math.Max(variableValue3 * Math.Max(Math.Abs(variableValue), Math.Abs(variableValue2)), 1.1210387714598537E-44)) ? 1.0 : 0.0);
			return index + 1;
		}
	}

	private class _SNAN_Operation : _Operation_1_1
	{
		public _SNAN_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = (double.IsNaN(variableValue) ? 1.0 : 0.0);
			return index + 1;
		}
	}

	private class _SNANZ_Operation : _Operation_1_1
	{
		public _SNANZ_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = (double.IsNaN(variableValue) ? 0.0 : 1.0);
			return index + 1;
		}
	}

	private class _SLTZ_Operation : _SLT_Operation
	{
		public _SLTZ_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, "0")
		{
		}
	}

	private class _SGTZ_Operation : _SGT_Operation
	{
		public _SGTZ_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, "0")
		{
		}
	}

	private class _SLEZ_Operation : _SLE_Operation
	{
		public _SLEZ_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, "0")
		{
		}
	}

	private class _SGEZ_Operation : _SGE_Operation
	{
		public _SGEZ_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, "0")
		{
		}
	}

	private class _SEQZ_Operation : _SEQ_Operation
	{
		public _SEQZ_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, "0")
		{
		}
	}

	private class _SNEZ_Operation : _SNE_Operation
	{
		public _SNEZ_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, "0")
		{
		}
	}

	private class _SAPZ_Operation : _SAP_Operation
	{
		public _SAPZ_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, "0", registerArgument2Code)
		{
		}
	}

	private class _SNAZ_Operation : _SNA_Operation
	{
		public _SNAZ_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, "0", registerArgument2Code)
		{
		}
	}

	private class _EXT_Operation : _Operation_1_3
	{
		public _EXT_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string arg1Code, string arg2Code, string arg3Code)
			: base(chip, lineNumber, registerStoreCode, arg1Code, arg2Code, arg3Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			long variableLong = _Argument1.GetVariableLong(_AliasTarget.Register, signed: false);
			int variableInt = _Argument2.GetVariableInt(_AliasTarget.Register);
			int variableInt2 = _Argument3.GetVariableInt(_AliasTarget.Register);
			if (variableInt2 <= 0)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftUnderflow, _LineNumber);
			}
			if (variableInt < 0)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftUnderflow, _LineNumber);
			}
			if (variableInt >= 53)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftOverflow, _LineNumber);
			}
			if (variableInt2 > 53 || variableInt + variableInt2 > 53)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.PayloadOverflow, _LineNumber);
			}
			long num = variableLong & 0x1FFFFFFFFFFFFFL;
			ulong num2 = (ulong)(((variableInt2 == 53) ? 9007199254740991L : ((1L << variableInt2) - 1)) << variableInt);
			ulong l = ((ulong)num & num2) >> variableInt;
			_Chip._Registers[variableIndex] = LongToDouble((long)l);
			return index + 1;
		}
	}

	private class _INS_Operation : _Operation_1_3
	{
		public _INS_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string arg1Code, string arg2Code, string arg3Code)
			: base(chip, lineNumber, registerStoreCode, arg1Code, arg2Code, arg3Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			long variableLong = _Argument1.GetVariableLong(_AliasTarget.Register, signed: false);
			int variableInt = _Argument2.GetVariableInt(_AliasTarget.Register);
			int variableInt2 = _Argument3.GetVariableInt(_AliasTarget.Register);
			if (variableInt2 <= 0)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftUnderflow, _LineNumber);
			}
			if (variableInt < 0)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftUnderflow, _LineNumber);
			}
			if (variableInt >= 53)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftOverflow, _LineNumber);
			}
			if (variableInt2 > 53 || variableInt + variableInt2 > 53)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.PayloadOverflow, _LineNumber);
			}
			ulong num = 9007199254740991uL;
			long num2 = DoubleToLong(_Chip._Registers[variableIndex], signed: false) & (long)num;
			ulong num3 = (ulong)variableLong & num;
			ulong num4 = ((variableInt2 == 53) ? num : ((ulong)((1L << variableInt2) - 1)));
			ulong num5 = num4 << variableInt;
			long num6 = num2 & (long)(~num5);
			ulong num7 = ((num3 & num4) << variableInt) & num5;
			ulong l = ((ulong)num6 | num7) & num;
			_Chip._Registers[variableIndex] = LongToDouble((long)l);
			return index + 1;
		}
	}

	private class _AND_Operation : _Operation_1_2
	{
		public _AND_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			long variableLong = _Argument1.GetVariableLong(_AliasTarget.Register);
			long variableLong2 = _Argument2.GetVariableLong(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = LongToDouble(variableLong & variableLong2);
			return index + 1;
		}
	}

	private class _OR_Operation : _Operation_1_2
	{
		public _OR_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			long variableLong = _Argument1.GetVariableLong(_AliasTarget.Register);
			long variableLong2 = _Argument2.GetVariableLong(_AliasTarget.Register);
			long num = variableLong;
			long num2 = variableLong2;
			long l = num | num2;
			_Chip._Registers[variableIndex] = LongToDouble(l);
			return index + 1;
		}
	}

	private class _XOR_Operation : _Operation_1_2
	{
		public _XOR_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			long variableLong = _Argument1.GetVariableLong(_AliasTarget.Register);
			long variableLong2 = _Argument2.GetVariableLong(_AliasTarget.Register);
			long num = variableLong;
			long num2 = variableLong2;
			_Chip._Registers[variableIndex] = LongToDouble(num ^ num2);
			return index + 1;
		}
	}

	private class _NOR_Operation : _Operation_1_2
	{
		public _NOR_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			long variableLong = _Argument1.GetVariableLong(_AliasTarget.Register);
			long variableLong2 = _Argument2.GetVariableLong(_AliasTarget.Register);
			long num = variableLong;
			long num2 = variableLong2;
			_Chip._Registers[variableIndex] = LongToDouble(~(num | num2));
			return index + 1;
		}
	}

	private class _NOT_Operation : _Operation_1_1
	{
		public _NOT_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgumentCode)
			: base(chip, lineNumber, registerStoreCode, registerArgumentCode)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			long l = ~_Argument1.GetVariableLong(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = LongToDouble(l);
			return index + 1;
		}
	}

	private class _MUL_Operation : _Operation_1_2
	{
		public _MUL_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = variableValue * variableValue2;
			return index + 1;
		}
	}

	private class _DIV_Operation : _Operation_1_2
	{
		public _DIV_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = variableValue / variableValue2;
			return index + 1;
		}
	}

	private class _MOD_Operation : _Operation_1_2
	{
		public _MOD_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			double num = variableValue;
			double num2 = variableValue2;
			double num3 = num % num2;
			if (num3 < 0.0)
			{
				num3 += num2;
			}
			_Chip._Registers[variableIndex] = num3;
			return index + 1;
		}
	}

	private class _J_Operation : _JR_Operation
	{
		public _J_Operation(ProgrammableChip chip, int lineNumber, string jumpAddressCode)
			: base(chip, lineNumber, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, -index);
		}
	}

	private class _BLT_Operation : _BRLT_Operation
	{
		public _BLT_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, -index);
		}
	}

	private class _BGT_Operation : _BRGT_Operation
	{
		public _BGT_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, -index);
		}
	}

	private class _BLE_Operation : _BRLE_Operation
	{
		public _BLE_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, -index);
		}
	}

	private class _BGE_Operation : _BRGE_Operation
	{
		public _BGE_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, -index);
		}
	}

	private class _BLTZ_Operation : _BLT_Operation
	{
		public _BLTZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BGEZ_Operation : _BGE_Operation
	{
		public _BGEZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BLEZ_Operation : _BLE_Operation
	{
		public _BLEZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BGTZ_Operation : _BGT_Operation
	{
		public _BGTZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BDSE_Operation : _BRDSE_Operation
	{
		public _BDSE_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string jumpAddressCode)
			: base(chip, lineNumber, deviceCode, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, -index);
		}
	}

	private class _BDNS_Operation : _BRDNS_Operation
	{
		public _BDNS_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string jumpAddressCode)
			: base(chip, lineNumber, deviceCode, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, -index);
		}
	}

	private class _BEQ_Operation : _BREQ_Operation
	{
		public _BEQ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, -index);
		}
	}

	private class _BNE_Operation : _BRNE_Operation
	{
		public _BNE_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, -index);
		}
	}

	private class _BAP_Operation : _BRAP_Operation
	{
		public _BAP_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string registerArgument3Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, registerArgument3Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, -index);
		}
	}

	private class _BNA_Operation : _BRNA_Operation
	{
		public _BNA_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string registerArgument3Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, registerArgument3Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, -index);
		}
	}

	private class _BEQZ_Operation : _BEQ_Operation
	{
		public _BEQZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BNEZ_Operation : _BNE_Operation
	{
		public _BNEZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BAPZ_Operation : _BAP_Operation
	{
		public _BAPZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", registerArgument2Code, jumpAddressCode)
		{
		}
	}

	private class _BNAZ_Operation : _BNA_Operation
	{
		public _BNAZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", registerArgument2Code, jumpAddressCode)
		{
		}
	}

	private class _JAL_Operation : _J_Operation
	{
		public _JAL_Operation(ProgrammableChip chip, int lineNumber, string jumpAddressCode)
			: base(chip, lineNumber, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			_Chip._Registers[_Chip._ReturnAddressIndex] = index + 1;
			return base.Execute(index);
		}
	}

	private class _BDSEAL_Operation : _BDSE_Operation
	{
		public _BDSEAL_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string jumpAddressCode)
			: base(chip, lineNumber, deviceCode, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			int result = Execute(index, out hasJumped, -index);
			if (hasJumped)
			{
				_Chip._Registers[_Chip._ReturnAddressIndex] = index + 1;
			}
			return result;
		}
	}

	private class _BDNSAL_Operation : _BDNS_Operation
	{
		public _BDNSAL_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string jumpAddressCode)
			: base(chip, lineNumber, deviceCode, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			int result = Execute(index, out hasJumped, -index);
			if (hasJumped)
			{
				_Chip._Registers[_Chip._ReturnAddressIndex] = index + 1;
			}
			return result;
		}
	}

	private class _BLTAL_Operation : _BLT_Operation
	{
		public _BLTAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			int result = Execute(index, out hasJumped, -index);
			if (hasJumped)
			{
				_Chip._Registers[_Chip._ReturnAddressIndex] = index + 1;
			}
			return result;
		}
	}

	private class _BGEAL_Operation : _BGE_Operation
	{
		public _BGEAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			int result = Execute(index, out hasJumped, -index);
			if (hasJumped)
			{
				_Chip._Registers[_Chip._ReturnAddressIndex] = index + 1;
			}
			return result;
		}
	}

	private class _BLEAL_Operation : _BLE_Operation
	{
		public _BLEAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			int result = Execute(index, out hasJumped, -index);
			if (hasJumped)
			{
				_Chip._Registers[_Chip._ReturnAddressIndex] = index + 1;
			}
			return result;
		}
	}

	private class _BGTAL_Operation : _BGT_Operation
	{
		public _BGTAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			int result = Execute(index, out hasJumped, -index);
			if (hasJumped)
			{
				_Chip._Registers[_Chip._ReturnAddressIndex] = index + 1;
			}
			return result;
		}
	}

	private class _BLTZAL_Operation : _BLTAL_Operation
	{
		public _BLTZAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BGEZAL_Operation : _BGEAL_Operation
	{
		public _BGEZAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BLEZAL_Operation : _BLEAL_Operation
	{
		public _BLEZAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BGTZAL_Operation : _BGTAL_Operation
	{
		public _BGTZAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BEQAL_Operation : _BEQ_Operation
	{
		public _BEQAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			int result = Execute(index, out hasJumped, -index);
			if (hasJumped)
			{
				_Chip._Registers[_Chip._ReturnAddressIndex] = index + 1;
			}
			return result;
		}
	}

	private class _BNEAL_Operation : _BNE_Operation
	{
		public _BNEAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			int result = Execute(index, out hasJumped, -index);
			if (hasJumped)
			{
				_Chip._Registers[_Chip._ReturnAddressIndex] = index + 1;
			}
			return result;
		}
	}

	private class _BAPAL_Operation : _BAP_Operation
	{
		public _BAPAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string registerArgument3Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, registerArgument3Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			int result = Execute(index, out hasJumped, -index);
			if (hasJumped)
			{
				_Chip._Registers[_Chip._ReturnAddressIndex] = index + 1;
			}
			return result;
		}
	}

	private class _BNAAL_Operation : _BNA_Operation
	{
		public _BNAAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string registerArgument3Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, registerArgument3Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			int result = Execute(index, out hasJumped, -index);
			if (hasJumped)
			{
				_Chip._Registers[_Chip._ReturnAddressIndex] = index + 1;
			}
			return result;
		}
	}

	private class _BEQZAL_Operation : _BEQAL_Operation
	{
		public _BEQZAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BNEZAL_Operation : _BNEAL_Operation
	{
		public _BNEZAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BAPZAL_Operation : _BAPAL_Operation
	{
		public _BAPZAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", registerArgument2Code, jumpAddressCode)
		{
		}
	}

	private class _BNAZAL_Operation : _BNAAL_Operation
	{
		public _BNAZAL_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			int result = Execute(index, out hasJumped, -index);
			if (hasJumped)
			{
				_Chip._Registers[_Chip._ReturnAddressIndex] = index + 1;
			}
			return result;
		}
	}

	private class _JR_Operation : _Operation_J_0
	{
		public _JR_Operation(ProgrammableChip chip, int lineNumber, string jumpAddressCode)
			: base(chip, lineNumber, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, 0);
		}

		public int Execute(int index, int offset)
		{
			return index + offset + _JumpIndex.GetVariableValue(_AliasTarget.Register);
		}
	}

	private class _BRLT_Operation : _Operation_J_2
	{
		public _BRLT_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			return Execute(index, out hasJumped);
		}

		public int Execute(int index, int offset)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, offset);
		}

		public int Execute(int index, out bool hasJumped, int offset = 0)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			if (variableValue < variableValue2)
			{
				hasJumped = true;
				return index + offset + _JumpIndex.GetVariableValue(_AliasTarget.Register);
			}
			hasJumped = false;
			return index + 1;
		}
	}

	private class _BRGT_Operation : _Operation_J_2
	{
		public _BRGT_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			return Execute(index, out hasJumped);
		}

		public int Execute(int index, int offset)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, offset);
		}

		public int Execute(int index, out bool hasJumped, int offset = 0)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			if (variableValue > variableValue2)
			{
				hasJumped = true;
				return index + offset + _JumpIndex.GetVariableValue(_AliasTarget.Register);
			}
			hasJumped = false;
			return index + 1;
		}
	}

	private class _BRLE_Operation : _Operation_J_2
	{
		public _BRLE_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			return Execute(index, out hasJumped);
		}

		public int Execute(int index, int offset)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, offset);
		}

		public int Execute(int index, out bool hasJumped, int offset = 0)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			if (variableValue <= variableValue2)
			{
				hasJumped = true;
				return index + offset + _JumpIndex.GetVariableValue(_AliasTarget.Register);
			}
			hasJumped = false;
			return index + 1;
		}
	}

	private class _BRGE_Operation : _Operation_J_2
	{
		public _BRGE_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			return Execute(index, out hasJumped);
		}

		public int Execute(int index, int offset)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, offset);
		}

		public int Execute(int index, out bool hasJumped, int offset = 0)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			if (variableValue >= variableValue2)
			{
				hasJumped = true;
				return index + offset + _JumpIndex.GetVariableValue(_AliasTarget.Register);
			}
			hasJumped = false;
			return index + 1;
		}
	}

	private class _BRLTZ_Operation : _BRLT_Operation
	{
		public _BRLTZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BRGEZ_Operation : _BRGE_Operation
	{
		public _BRGEZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BRLEZ_Operation : _BRLE_Operation
	{
		public _BRLEZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BRGTZ_Operation : _BRGT_Operation
	{
		public _BRGTZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BRDSE_Operation : _Operation_J_0
	{
		protected readonly IDeviceVariable _DeviceIndex;

		public _BRDSE_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string jumpAddressCode)
			: base(chip, lineNumber, jumpAddressCode)
		{
			_DeviceIndex = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			return Execute(index, out hasJumped);
		}

		public int Execute(int index, int offset)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, offset);
		}

		public int Execute(int index, out bool hasJumped, int offset = 0)
		{
			if (_DeviceIndex.GetDevice(_Chip.CircuitHousing) == null)
			{
				hasJumped = false;
				return index + 1;
			}
			hasJumped = true;
			return index + offset + _JumpIndex.GetVariableValue(_AliasTarget.Register);
		}
	}

	private class _BRDNS_Operation : _Operation_J_0
	{
		protected readonly IDeviceVariable _DeviceIndex;

		public _BRDNS_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string jumpAddressCode)
			: base(chip, lineNumber, jumpAddressCode)
		{
			_DeviceIndex = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			return Execute(index, out hasJumped);
		}

		public int Execute(int index, int offset)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, offset);
		}

		public int Execute(int index, out bool hasJumped, int offset = 0)
		{
			if (_DeviceIndex.GetDevice(_Chip.CircuitHousing) == null)
			{
				hasJumped = true;
				return index + offset + _JumpIndex.GetVariableValue(_AliasTarget.Register);
			}
			hasJumped = false;
			return index + 1;
		}
	}

	private class _BREQ_Operation : _Operation_J_2
	{
		public _BREQ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			return Execute(index, out hasJumped);
		}

		public int Execute(int index, int offset)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, offset);
		}

		public int Execute(int index, out bool hasJumped, int offset = 0)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			if (variableValue == variableValue2)
			{
				hasJumped = true;
				return index + offset + _JumpIndex.GetVariableValue(_AliasTarget.Register);
			}
			hasJumped = false;
			return index + 1;
		}
	}

	private class _BRNE_Operation : _Operation_J_2
	{
		public _BRNE_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			return Execute(index, out hasJumped);
		}

		public int Execute(int index, int offset)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, offset);
		}

		public int Execute(int index, out bool hasJumped, int offset = 0)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			if (variableValue != variableValue2)
			{
				hasJumped = true;
				return index + offset + _JumpIndex.GetVariableValue(_AliasTarget.Register);
			}
			hasJumped = false;
			return index + 1;
		}
	}

	private class _BRAP_Operation : _Operation_J_3
	{
		public _BRAP_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string registerArgument3Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, registerArgument3Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, 0);
		}

		public int Execute(int index, int offset)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, offset);
		}

		public int Execute(int index, out bool hasJumped, int offset)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			double variableValue3 = _Argument3.GetVariableValue(_AliasTarget.Register);
			if (Math.Abs(variableValue - variableValue2) <= Math.Max(variableValue3 * Math.Max(Math.Abs(variableValue), Math.Abs(variableValue2)), 1.1210387714598537E-44))
			{
				hasJumped = true;
				return index + offset + _JumpIndex.GetVariableValue(_AliasTarget.Register);
			}
			hasJumped = false;
			return index + 1;
		}
	}

	private class _BRNA_Operation : _Operation_J_3
	{
		public _BRNA_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string registerArgument3Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, registerArgument2Code, registerArgument3Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, 0);
		}

		public int Execute(int index, int offset)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, offset);
		}

		public int Execute(int index, out bool hasJumped, int offset)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			double variableValue3 = _Argument3.GetVariableValue(_AliasTarget.Register);
			if (Math.Abs(variableValue - variableValue2) > Math.Max(variableValue3 * Math.Max(Math.Abs(variableValue), Math.Abs(variableValue2)), 1.1210387714598537E-44))
			{
				hasJumped = true;
				return index + offset + _JumpIndex.GetVariableValue(_AliasTarget.Register);
			}
			hasJumped = false;
			return index + 1;
		}
	}

	private class _BREQZ_Operation : _BREQ_Operation
	{
		public _BREQZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BNAN_Operation : _BRNAN_Operation
	{
		public _BNAN_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			return Execute(index, -index);
		}
	}

	private class _BRNAN_Operation : _Operation_J_1
	{
		public _BRNAN_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, jumpAddressCode)
		{
		}

		public override int Execute(int index)
		{
			bool hasJumped;
			return Execute(index, out hasJumped);
		}

		public int Execute(int index, int offset)
		{
			bool hasJumped;
			return Execute(index, out hasJumped, offset);
		}

		public int Execute(int index, out bool hasJumped, int offset = 0)
		{
			if (double.IsNaN(_Argument1.GetVariableValue(_AliasTarget.Register)))
			{
				hasJumped = true;
				return index + offset + _JumpIndex.GetVariableValue(_AliasTarget.Register);
			}
			hasJumped = false;
			return index + 1;
		}
	}

	private class _BRNEZ_Operation : _BRNE_Operation
	{
		public _BRNEZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", jumpAddressCode)
		{
		}
	}

	private class _BRAPZ_Operation : _BRAP_Operation
	{
		public _BRAPZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", registerArgument2Code, jumpAddressCode)
		{
		}
	}

	private class _BRNAZ_Operation : _BRNA_Operation
	{
		public _BRNAZ_Operation(ProgrammableChip chip, int lineNumber, string registerArgument1Code, string registerArgument2Code, string jumpAddressCode)
			: base(chip, lineNumber, registerArgument1Code, "0", registerArgument2Code, jumpAddressCode)
		{
		}
	}

	private class _SQRT_Operation : _Operation_1_1
	{
		public _SQRT_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Sqrt(variableValue);
			return index + 1;
		}
	}

	private class _ROUND_Operation : _Operation_1_1
	{
		public _ROUND_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Round(variableValue);
			return index + 1;
		}
	}

	private class _TRUNC_Operation : _Operation_1_1
	{
		public _TRUNC_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Truncate(variableValue);
			return index + 1;
		}
	}

	private class _CEIL_Operation : _Operation_1_1
	{
		public _CEIL_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Ceiling(variableValue);
			return index + 1;
		}
	}

	private class _FLOOR_Operation : _Operation_1_1
	{
		public _FLOOR_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Floor(variableValue);
			return index + 1;
		}
	}

	private class _MAX_Operation : _Operation_1_2
	{
		public _MAX_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Max(variableValue, variableValue2);
			return index + 1;
		}
	}

	private class _MIN_Operation : _Operation_1_2
	{
		public _MIN_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Min(variableValue, variableValue2);
			return index + 1;
		}
	}

	private class _POW_Operation : _Operation_1_2
	{
		public _POW_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Pow(variableValue, variableValue2);
			return index + 1;
		}
	}

	private class _LERP_Operation : _Operation_1_3
	{
		public _LERP_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code, string registerArgument3Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code, registerArgument3Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			double variableValue3 = _Argument3.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = RocketMath.Lerp(variableValue, variableValue2, variableValue3);
			return index + 1;
		}
	}

	private class _ABS_Operation : _Operation_1_1
	{
		public _ABS_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Abs(variableValue);
			return index + 1;
		}
	}

	private class _LOG_Operation : _Operation_1_1
	{
		public _LOG_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Log(variableValue);
			return index + 1;
		}
	}

	private class _EXP_Operation : _Operation_1_1
	{
		public _EXP_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Exp(variableValue);
			return index + 1;
		}
	}

	private class _RAND_Operation : _Operation_1_0
	{
		private static readonly System.Random _RandomNumberGenerator;

		static _RAND_Operation()
		{
			_RandomNumberGenerator = new System.Random();
		}

		public _RAND_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode)
			: base(chip, lineNumber, registerStoreCode)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = _RandomNumberGenerator.NextDouble();
			return index + 1;
		}
	}

	private class _HCF_Operation : _Operation
	{
		public _HCF_Operation(ProgrammableChip chip, int lineNumber)
			: base(chip, lineNumber)
		{
		}

		public override int Execute(int index)
		{
			_Chip.HaltAndCatchFire();
			throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ChipCatchingFire, _LineNumber);
		}
	}

	private class _YIELD_Operation : _Operation
	{
		public _YIELD_Operation(ProgrammableChip chip, int lineNumber)
			: base(chip, lineNumber)
		{
		}

		public override int Execute(int index)
		{
			return -index - 1;
		}
	}

	private class _NOOP_Operation : _Operation
	{
		public _NOOP_Operation(ProgrammableChip chip, int lineNUmber)
			: base(chip, lineNUmber)
		{
		}

		public override int Execute(int index)
		{
			return index + 1;
		}
	}

	private class _POP_Operation : _PEEK_Operation
	{
		public _POP_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode)
			: base(chip, lineNumber, registerStoreCode)
		{
		}

		public override int Execute(int index)
		{
			_Chip._Registers[_Chip._StackPointerIndex] -= 1.0;
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			int num = (int)Math.Round(_Chip._Registers[_Chip._StackPointerIndex]);
			if (num < 0)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.StackUnderFlow, _LineNumber);
			}
			if (num >= _Chip._Stack.Length)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.StackOverFlow, _LineNumber);
			}
			_Chip._Registers[variableIndex] = _Chip._Stack[num];
			if (_Chip.CircuitHousing is CircuitHousing circuitHousing)
			{
				circuitHousing._MemoryLight?.Flash(LogicMemoryState.Write);
			}
			return index + 1;
		}
	}

	private class _PEEK_Operation : _Operation_1_0
	{
		public _PEEK_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode)
			: base(chip, lineNumber, registerStoreCode)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			int num = (int)Math.Round(_Chip._Registers[_Chip._StackPointerIndex]) - 1;
			if (num < 0)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.StackUnderFlow, _LineNumber);
			}
			if (num >= _Chip._Stack.Length)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.StackOverFlow, _LineNumber);
			}
			_Chip._Registers[variableIndex] = _Chip._Stack[num];
			if (_Chip.CircuitHousing is CircuitHousing circuitHousing)
			{
				circuitHousing._MemoryLight?.Flash(LogicMemoryState.Read);
			}
			return index + 1;
		}
	}

	private class _PUSH_Operation : _Operation
	{
		protected DoubleValueVariable _Argument1;

		public _PUSH_Operation(ProgrammableChip chip, int lineNumber, string argument1Code)
			: base(chip, lineNumber)
		{
			_Argument1 = new DoubleValueVariable(chip, lineNumber, argument1Code, InstructionInclude.MaskDoubleValue, throwException: false);
		}

		public override int Execute(int index)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			int num = (int)Math.Round(_Chip._Registers[_Chip._StackPointerIndex]);
			if (num < 0)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.StackUnderFlow, _LineNumber);
			}
			if (num >= _Chip._Stack.Length)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.StackOverFlow, _LineNumber);
			}
			_Chip._Stack[num] = variableValue;
			_Chip._Registers[_Chip._StackPointerIndex] += 1.0;
			if (_Chip.CircuitHousing is CircuitHousing circuitHousing)
			{
				circuitHousing._MemoryLight?.Flash(LogicMemoryState.Write);
			}
			return index + 1;
		}
	}

	private class _CLR_Operation : _Operation
	{
		protected readonly DeviceIndexVariable _DeviceIndex;

		public _CLR_Operation(ProgrammableChip chip, int lineNumber, string deviceCode)
			: base(chip, lineNumber)
		{
			_DeviceIndex = new DeviceIndexVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDeviceIndex, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableIndex = _DeviceIndex.GetVariableIndex(_AliasTarget.Device);
			(((_Chip.CircuitHousing.GetLogicableFromIndex(variableIndex) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber)) as IMemoryWritable) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotReadable, _LineNumber)).ClearMemory();
			return index + 1;
		}
	}

	private class _CLRD_Operation : _Operation
	{
		protected readonly IntValuedVariable _DeviceId;

		public _CLRD_Operation(ProgrammableChip chip, int lineNumber, string referenceId)
			: base(chip, lineNumber)
		{
			_DeviceId = new IntValuedVariable(chip, lineNumber, referenceId, InstructionInclude.MaskDoubleValue, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableValue = _DeviceId.GetVariableValue(_AliasTarget.Register);
			(((_Chip.CircuitHousing.GetLogicableFromId(variableValue) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber)) as IMemoryWritable) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotWriteable, _LineNumber)).ClearMemory();
			return index + 1;
		}
	}

	private class _GET_Operation : _Operation_1_0
	{
		protected readonly IDeviceVariable _DeviceRef;

		protected readonly IntValuedVariable _StackIndex;

		public _GET_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string deviceCode, string stackIndexCode)
			: base(chip, lineNumber, registerStoreCode)
		{
			_DeviceRef = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
			_StackIndex = new IntValuedVariable(chip, lineNumber, stackIndexCode, InstructionInclude.MaskDoubleValue, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			int variableValue = _StackIndex.GetVariableValue(_AliasTarget.Register);
			if (!((_DeviceRef.GetDevice(_Chip.CircuitHousing) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber)) is IMemoryReadable memoryReadable))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotReadable, _LineNumber);
			}
			_Chip._Registers[variableIndex] = memoryReadable.ReadMemory(variableValue);
			return index + 1;
		}
	}

	private class _PUT_Operation : _Operation
	{
		protected readonly IDeviceVariable _DeviceRef;

		protected readonly DoubleValueVariable _Argument1;

		protected readonly IntValuedVariable _StackIndex;

		public _PUT_Operation(ProgrammableChip chip, int lineNumber, string argument1Code, string deviceCode, string stackIndexCode)
			: base(chip, lineNumber)
		{
			_Argument1 = new DoubleValueVariable(chip, lineNumber, argument1Code, InstructionInclude.MaskDoubleValue, throwException: false);
			_DeviceRef = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
			_StackIndex = new IntValuedVariable(chip, lineNumber, stackIndexCode, InstructionInclude.MaskDoubleValue, throwException: false);
		}

		public override int Execute(int index)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			int variableValue2 = _StackIndex.GetVariableValue(_AliasTarget.Register);
			if (!((_DeviceRef.GetDevice(_Chip.CircuitHousing) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber)) is IMemoryWritable memoryWritable))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotWriteable, _LineNumber);
			}
			try
			{
				memoryWritable.WriteMemory(variableValue2, variableValue);
				_Chip.CircuitHousing.HasPut();
			}
			catch (Exception ex)
			{
				if (!(ex is NullReferenceException))
				{
					if (!(ex is StackUnderflowException))
					{
						if (ex is StackOverflowException)
						{
							throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.StackOverFlow, _LineNumber);
						}
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.Unknown, _LineNumber);
					}
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.StackUnderFlow, _LineNumber);
				}
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber);
			}
			return index + 1;
		}
	}

	private class _GETD_Operation : _Operation_I
	{
		protected readonly IntValuedVariable _StackIndex;

		public _GETD_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string idCode, string stackIndexCode)
			: base(chip, lineNumber, registerStoreCode, idCode)
		{
			_StackIndex = new IntValuedVariable(chip, lineNumber, stackIndexCode, InstructionInclude.MaskDoubleValue, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			int variableValue = _DeviceId.GetVariableValue(_AliasTarget.Register);
			int variableValue2 = _StackIndex.GetVariableValue(_AliasTarget.Register);
			if (!((_Chip.CircuitHousing.GetLogicableFromId(variableValue) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber)) is IMemoryReadable memoryReadable))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotReadable, _LineNumber);
			}
			_Chip._Registers[variableIndex] = memoryReadable.ReadMemory(variableValue2);
			return index + 1;
		}
	}

	private class _PUTD_Operation : _Operation
	{
		protected readonly DoubleValueVariable _Argument1;

		protected readonly IntValuedVariable _DeviceId;

		protected readonly IntValuedVariable _StackIndex;

		public _PUTD_Operation(ProgrammableChip chip, int lineNumber, string argument1Code, string referenceId, string stackIndexCode)
			: base(chip, lineNumber)
		{
			_Argument1 = new DoubleValueVariable(chip, lineNumber, argument1Code, InstructionInclude.MaskDoubleValue, throwException: false);
			_DeviceId = new IntValuedVariable(chip, lineNumber, referenceId, InstructionInclude.MaskDoubleValue, throwException: false);
			_StackIndex = new IntValuedVariable(chip, lineNumber, stackIndexCode, InstructionInclude.MaskDoubleValue, throwException: false);
		}

		public override int Execute(int index)
		{
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			int variableValue2 = _DeviceId.GetVariableValue(_AliasTarget.Register);
			int variableValue3 = _StackIndex.GetVariableValue(_AliasTarget.Register);
			if (!((_Chip.CircuitHousing.GetLogicableFromId(variableValue2) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber)) is IMemoryWritable memoryWritable))
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotWriteable, _LineNumber);
			}
			try
			{
				memoryWritable.WriteMemory(variableValue3, variableValue);
				_Chip.CircuitHousing.HasPut();
			}
			catch (Exception ex)
			{
				if (!(ex is NullReferenceException))
				{
					if (!(ex is StackUnderflowException))
					{
						if (ex is StackOverflowException)
						{
							throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.StackOverFlow, _LineNumber);
						}
						throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.Unknown, _LineNumber);
					}
					throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.StackUnderFlow, _LineNumber);
				}
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber);
			}
			return index + 1;
		}
	}

	private class _POKE_Operation : _Operation
	{
		protected IntValuedVariable _Index;

		protected DoubleValueVariable _Value;

		public _POKE_Operation(ProgrammableChip chip, int lineNumber, string stackIndexCode, string valueCode)
			: base(chip, lineNumber)
		{
			_Index = new IntValuedVariable(chip, lineNumber, stackIndexCode, InstructionInclude.MaskDoubleValue, throwException: false);
			_Value = new DoubleValueVariable(chip, lineNumber, valueCode, InstructionInclude.MaskDoubleValue, throwException: false);
		}

		public override int Execute(int index)
		{
			double variableValue = _Value.GetVariableValue(_AliasTarget.Register);
			int variableValue2 = _Index.GetVariableValue(_AliasTarget.Register);
			if (variableValue2 < 0)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.StackUnderFlow, _LineNumber);
			}
			if (variableValue2 >= _Chip._Stack.Length)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.StackOverFlow, _LineNumber);
			}
			_Chip._Stack[variableValue2] = variableValue;
			return index + 1;
		}
	}

	private class _SELECT_Operation : _Operation_1_3
	{
		public _SELECT_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string argument1Code, string argument2Code, string argument3Code)
			: base(chip, lineNumber, registerStoreCode, argument1Code, argument2Code, argument3Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			double variableValue3 = _Argument3.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = ((variableValue != 0.0) ? variableValue2 : variableValue3);
			return index + 1;
		}
	}

	private class _SLEEP_Operation : _Operation
	{
		protected readonly DoubleValueVariable _SleepDuration;

		public float LastTimeSet;

		public double SleepDurationRemaining = double.NaN;

		public _SLEEP_Operation(ProgrammableChip chip, int lineNumber, string sleepDurationCode)
			: base(chip, lineNumber)
		{
			_SleepDuration = new DoubleValueVariable(chip, lineNumber, sleepDurationCode, InstructionInclude.MaskDoubleValue, throwException: false);
		}

		public override int Execute(int index)
		{
			if (double.IsNaN(SleepDurationRemaining))
			{
				LastTimeSet = GameManager.GameTime;
				SleepDurationRemaining = _SleepDuration.GetVariableValue(_AliasTarget.Register);
				return -index;
			}
			float num = GameManager.GameTime - LastTimeSet;
			SleepDurationRemaining -= num;
			if (SleepDurationRemaining < 0.0)
			{
				LastTimeSet = 0f;
				SleepDurationRemaining = double.NaN;
				return index + 1;
			}
			LastTimeSet = GameManager.GameTime;
			return -index;
		}
	}

	private class _SRL_Operation : _Operation_1_2
	{
		public _SRL_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			long variableLong = _Argument1.GetVariableLong(_AliasTarget.Register, signed: false);
			int variableInt = _Argument2.GetVariableInt(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = LongToDouble(variableLong >> variableInt);
			return index + 1;
		}
	}

	private class _SRA_Operation : _Operation_1_2
	{
		public _SRA_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			long variableLong = _Argument1.GetVariableLong(_AliasTarget.Register);
			int variableInt = _Argument2.GetVariableInt(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = LongToDouble(variableLong >> variableInt);
			return index + 1;
		}
	}

	private class _SLA_SLL_Operation : _Operation_1_2
	{
		public _SLA_SLL_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			long variableLong = _Argument1.GetVariableLong(_AliasTarget.Register);
			int variableInt = _Argument2.GetVariableInt(_AliasTarget.Register);
			long l = variableLong << variableInt;
			_Chip._Registers[variableIndex] = LongToDouble(l);
			return index + 1;
		}
	}

	private class _ROL_Operation : _Operation_1_2
	{
		public _ROL_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			long num = _Argument1.GetVariableLong(_AliasTarget.Register, signed: false) & 0x3FFFFFFFFFFFFFL;
			int num2 = (_Argument2.GetVariableInt(_AliasTarget.Register) % 54 + 54) % 54;
			long l = ((num << num2) | (num >> 54 - num2)) & 0x3FFFFFFFFFFFFFL;
			_Chip._Registers[variableIndex] = LongToDouble(l);
			return index + 1;
		}
	}

	private class _ROR_Operation : _Operation_1_2
	{
		public _ROR_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			long num = _Argument1.GetVariableLong(_AliasTarget.Register, signed: false) & 0x3FFFFFFFFFFFFFL;
			int num2 = (_Argument2.GetVariableInt(_AliasTarget.Register) % 54 + 54) % 54;
			long l = ((num >> num2) | (num << 54 - num2)) & 0x3FFFFFFFFFFFFFL;
			_Chip._Registers[variableIndex] = LongToDouble(l);
			return index + 1;
		}
	}

	private class _SGN_Operation : _Operation_1_1
	{
		public _SGN_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = ((variableValue > 0.0) ? 1.0 : ((variableValue < 0.0) ? (-1.0) : 0.0));
			return index + 1;
		}
	}

	private class _CLAMP_Operation : _Operation_1_3
	{
		public _CLAMP_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code, string registerArgument3Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code, registerArgument3Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			double variableValue3 = _Argument3.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Min(Math.Max(variableValue, variableValue2), variableValue3);
			return index + 1;
		}
	}

	private class _SIN_Operation : _Operation_1_1
	{
		public _SIN_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Sin(variableValue);
			return index + 1;
		}
	}

	private class _ASIN_Operation : _Operation_1_1
	{
		public _ASIN_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Asin(variableValue);
			return index + 1;
		}
	}

	private class _TAN_Operation : _Operation_1_1
	{
		public _TAN_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Tan(variableValue);
			return index + 1;
		}
	}

	private class _ATAN_Operation : _Operation_1_1
	{
		public _ATAN_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Atan(variableValue);
			return index + 1;
		}
	}

	private class _ATAN2_Operation : _Operation_1_2
	{
		public _ATAN2_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code, string registerArgument2Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code, registerArgument2Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			double variableValue2 = _Argument2.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Atan2(variableValue, variableValue2);
			return index + 1;
		}
	}

	private class _COS_Operation : _Operation_1_1
	{
		public _COS_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Cos(variableValue);
			return index + 1;
		}
	}

	private class _ACOS_Operation : _Operation_1_1
	{
		public _ACOS_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string registerArgument1Code)
			: base(chip, lineNumber, registerStoreCode, registerArgument1Code)
		{
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			_Chip._Registers[variableIndex] = Math.Acos(variableValue);
			return index + 1;
		}
	}

	private class _BDNVS_Operation : _Operation_J_0
	{
		protected readonly IDeviceVariable _DeviceRef;

		protected readonly EnumValuedVariable<LogicType> _LogicType;

		public _BDNVS_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string logicTypeCode, string argument1Code)
			: base(chip, lineNumber, argument1Code)
		{
			_DeviceRef = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
			_LogicType = new EnumValuedVariable<LogicType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicType, throwException: false);
		}

		public override int Execute(int index)
		{
			LogicType variableValue = _LogicType.GetVariableValue(_AliasTarget.Register);
			if (variableValue == LogicType.None)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.LogicTypeIsNone, _LineNumber);
			}
			if (!(_DeviceRef.GetDevice(_Chip.CircuitHousing) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber)).CanLogicWrite(variableValue))
			{
				return _JumpIndex.GetVariableValue(_AliasTarget.Register);
			}
			return index + 1;
		}
	}

	private class _BDNVL_Operation : _Operation_J_0
	{
		protected readonly IDeviceVariable _DeviceRef;

		protected readonly EnumValuedVariable<LogicType> _LogicType;

		public _BDNVL_Operation(ProgrammableChip chip, int lineNumber, string deviceCode, string logicTypeCode, string argument1Code)
			: base(chip, lineNumber, argument1Code)
		{
			_DeviceRef = _Operation._MakeDeviceVariable(chip, lineNumber, deviceCode);
			_LogicType = new EnumValuedVariable<LogicType>(chip, lineNumber, logicTypeCode, InstructionInclude.MaskDoubleValue | InstructionInclude.LogicType, throwException: false);
		}

		public override int Execute(int index)
		{
			LogicType variableValue = _LogicType.GetVariableValue(_AliasTarget.Register);
			if (variableValue == LogicType.None)
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.LogicTypeIsNone, _LineNumber);
			}
			if (!(_DeviceRef.GetDevice(_Chip.CircuitHousing) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber)).CanLogicRead(variableValue))
			{
				return _JumpIndex.GetVariableValue(_AliasTarget.Register);
			}
			return index + 1;
		}
	}

	private class _RMAP_Operation : _Operation_1_1
	{
		protected readonly DeviceIndexVariable _DeviceIndex;

		public _RMAP_Operation(ProgrammableChip chip, int lineNumber, string registerStoreCode, string deviceCode, string argument1Code)
			: base(chip, lineNumber, registerStoreCode, argument1Code)
		{
			_DeviceIndex = new DeviceIndexVariable(chip, lineNumber, deviceCode, InstructionInclude.MaskDeviceIndex, throwException: false);
		}

		public override int Execute(int index)
		{
			int variableIndex = _Store.GetVariableIndex(_AliasTarget.Register);
			int variableIndex2 = _DeviceIndex.GetVariableIndex(_AliasTarget.Device);
			double variableValue = _Argument1.GetVariableValue(_AliasTarget.Register);
			int prefabHashFromReagentHash = (((_Chip.CircuitHousing.GetLogicableFromIndex(variableIndex2) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, _LineNumber)) as IRequireReagent) ?? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.MemoryNotReadable, _LineNumber)).GetPrefabHashFromReagentHash((int)variableValue);
			_Chip._Registers[variableIndex] = LongToDouble(prefabHashFromReagentHash);
			return index + 1;
		}
	}

	public class ScriptEnum<T> : IScriptEnum where T : struct, Enum, IConvertible
	{
		private readonly InstructionInclude _includeType;

		private readonly T[] _types;

		private readonly int _typeHash;

		private readonly string[] _names;

		private readonly Func<T, bool> _isDeprecated;

		private readonly Func<T, string> _getDescription;

		private readonly string _color;

		public ScriptEnum(InstructionInclude logicType, Func<T, bool> isDeprecated, Func<T, string> getDescription = null)
		{
			_includeType = logicType;
			_types = (T[])Enum.GetValues(typeof(T));
			_names = Enum.GetNames(typeof(T));
			_isDeprecated = isDeprecated;
			_color = "orange";
			_getDescription = getDescription;
			_typeHash = Animator.StringToHash(typeof(T).Name);
		}

		public int Count()
		{
			return _types.Length;
		}

		public bool IsDeprecated(int i)
		{
			return _isDeprecated?.Invoke(_types[i]) ?? false;
		}

		public HelpReference MakePage(int i, HelpReference prefab, RectTransform parent)
		{
			T val = _types[i];
			Func<T, bool> isDeprecated = _isDeprecated;
			if (isDeprecated != null && isDeprecated(val))
			{
				return null;
			}
			HelpReference helpReference = UnityEngine.Object.Instantiate(prefab, parent);
			int num = Convert.ToInt32(val);
			helpReference.Text.text = "<color=" + _color + ">" + _names[i] + "</color>";
			helpReference.Text2.text = "<color=#808080>" + typeof(T).Name + "</color>";
			helpReference.ReferenceValue1 = Animator.StringToHash(_names[i]);
			helpReference.ReferenceValue2 = Animator.StringToHash(typeof(T).Name);
			string text = _getDescription?.Invoke(val) ?? string.Empty;
			helpReference.TextString = _names[i];
			helpReference.TypeString = typeof(T).Name;
			helpReference.DescString = text;
			text = "<color=yellow>" + num.ToString("0." + new string('#', 339), CultureInfo.CurrentCulture) + "</color><br>" + text;
			if (string.IsNullOrEmpty(text))
			{
				helpReference.Description.gameObject.SetActive(value: false);
			}
			else
			{
				helpReference.Description.text = text;
			}
			return helpReference;
		}

		public bool TryParse(string searchText)
		{
			T result;
			return Enum.TryParse<T>(searchText, out result);
		}

		public bool IsHashType(int hash)
		{
			return _typeHash == hash;
		}

		public void Parse(ref string masterString)
		{
			for (int i = 0; i < _types.Length; i++)
			{
				string text = _names[i];
				Func<T, bool> isDeprecated = _isDeprecated;
				if (isDeprecated != null && isDeprecated(_types[i]))
				{
					text = "<s>" + text + "</s>";
				}
				masterString = masterString.ReplaceWholeWord(_names[i], string.Format("<color={1}>{0}</color>", text, _color));
			}
		}

		public void Execute(ref bool isValueSet, ref double value, string code, InstructionInclude propertiesToUse)
		{
			if (!isValueSet && (propertiesToUse & _includeType) != InstructionInclude.None && Enum.IsDefined(typeof(T), code))
			{
				value = Convert.ToInt32(Enum.Parse(typeof(T), code));
				isValueSet = true;
			}
		}

		public void Execute(ref bool isValueSet, ref int value, string code, InstructionInclude propertiesToUse)
		{
			if (!isValueSet && (propertiesToUse & _includeType) != InstructionInclude.None && Enum.IsDefined(typeof(T), code))
			{
				value = Convert.ToInt32(Enum.Parse(typeof(T), code));
				isValueSet = true;
			}
		}
	}

	public class BasicEnum<T> : IScriptEnum where T : struct, Enum, IConvertible
	{
		private readonly T[] _types;

		private readonly string[] _names;

		private readonly string _color;

		private readonly string _typeString;

		private readonly int _typeHash;

		private readonly Func<T, bool> _isDeprecated;

		public BasicEnum(string typeString = "", Func<T, bool> isDeprecated = null)
		{
			_types = (T[])Enum.GetValues(typeof(T));
			_names = Enum.GetNames(typeof(T));
			_color = "#20B2AA";
			_typeString = typeString;
			if (!string.IsNullOrEmpty(typeString))
			{
				for (int i = 0; i < _names.Length; i++)
				{
					_names[i] = _typeString + "." + _names[i];
				}
			}
			_typeHash = Animator.StringToHash(typeString);
			_isDeprecated = isDeprecated;
		}

		public HelpReference MakePage(int i, HelpReference prefab, RectTransform parent)
		{
			T val = _types[i];
			Func<T, bool> isDeprecated = _isDeprecated;
			if (isDeprecated != null && isDeprecated(val))
			{
				return null;
			}
			HelpReference helpReference = UnityEngine.Object.Instantiate(prefab, parent);
			int num = Convert.ToInt32(val);
			helpReference.Text.text = "<color=" + _color + ">" + _names[i] + "</color>";
			helpReference.Text2.text = "<color=#808080>Constant</color>";
			helpReference.ReferenceValue1 = Animator.StringToHash(_names[i]);
			helpReference.ReferenceValue2 = Animator.StringToHash(_typeString);
			helpReference.DescString = string.Empty;
			helpReference.Description.text = "<color=yellow>" + num.ToString("0." + new string('#', 339), CultureInfo.CurrentCulture) + "</color><br>";
			helpReference.TextString = _names[i];
			helpReference.TypeString = _typeString;
			return helpReference;
		}

		public int Count()
		{
			return _types.Length;
		}

		public bool IsDeprecated(int i)
		{
			return _isDeprecated?.Invoke(_types[i]) ?? false;
		}

		public bool TryParse(string searchText)
		{
			T result;
			return Enum.TryParse<T>(searchText, out result);
		}

		public bool IsHashType(int hash)
		{
			return _typeHash == hash;
		}

		public void Parse(ref string masterString)
		{
			for (int i = 0; i < _types.Length; i++)
			{
				string text = _names[i];
				Func<T, bool> isDeprecated = _isDeprecated;
				if (isDeprecated != null && isDeprecated(_types[i]))
				{
					text = "<s>" + text + "</s>";
				}
				masterString = masterString.ReplaceWholeWord(text, string.Format("<color={1}>{0}</color>", text, _color));
			}
		}

		public void Execute(ref bool isValueSet, ref double value, string code, InstructionInclude propertiesToUse)
		{
			if (isValueSet || (propertiesToUse & InstructionInclude.Enum) == 0)
			{
				return;
			}
			if (!string.IsNullOrEmpty(_typeString))
			{
				string[] array = code.Split('.');
				if (array.Length != 2 || !array[0].Equals(_typeString, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
				code = array[1];
			}
			if (Enum.IsDefined(typeof(T), code))
			{
				value = Convert.ToInt32(Enum.Parse(typeof(T), code));
				isValueSet = true;
			}
		}

		public void Execute(ref bool isValueSet, ref int value, string code, InstructionInclude propertiesToUse)
		{
			if (isValueSet || (propertiesToUse & InstructionInclude.Enum) == 0)
			{
				return;
			}
			if (!string.IsNullOrEmpty(_typeString))
			{
				string[] array = code.Split('.');
				if (array.Length != 2 || !array[0].Equals(_typeString, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
				code = array[1];
			}
			if (Enum.IsDefined(typeof(T), code))
			{
				value = Convert.ToInt32(Enum.Parse(typeof(T), code));
				isValueSet = true;
			}
		}
	}

	[SerializeField]
	[ReadOnly]
	private readonly double[] _Registers = new double[18];

	private readonly int _StackPointerIndex = 16;

	private readonly int _ReturnAddressIndex = 17;

	private readonly double[] _Stack = new double[512];

	public AsciiString SourceCode = AsciiString.Empty;

	private readonly Dictionary<string, _AliasValue> _Aliases = new Dictionary<string, _AliasValue>();

	private readonly Dictionary<string, int> _JumpTags = new Dictionary<string, int>();

	private readonly Dictionary<string, double> _Defines = new Dictionary<string, double>();

	[ByteArraySync]
	private ushort _ErrorLineNumberSynced;

	private static EnumCollection<ProgrammableChipException.ICExceptionType, byte> _exceptionTypes;

	[ByteArraySync]
	private byte _ErrorTypeSynced;

	private ushort _compileErrorLineNumber;

	private ProgrammableChipException.ICExceptionType _compileErrorType;

	private int _NextAddr;

	private readonly List<_LineOfCode> _LinesOfCode = new List<_LineOfCode>();

	private int _executeIndex;

	public const string _strCommand = "<color=yellow>{0}</color>";

	public const string _strDevice = "<color=green>d?</color>";

	public const string _strLogicType = "<color=orange>var</color>";

	public const string _strNumber = "<color=white>num</color>";

	public const string _strInteger = "<color=white>int</color>";

	public const string _strOr = "<color=#585858FF>|</color>";

	public const string _strRegister = "<color=#0080FFFF>r?</color>";

	public const string _strString = "<color=white>str</color>";

	public const string _strAny = "<color=#0080FFFF>r?</color>|<color=white>num</color>";

	public const string _strRegOrDev = "<color=#0080FFFF>r?</color>|<color=green>d?</color>";

	public const string _strReagentMode = "<color=orange>reagentMode</color>";

	public const string _strReagent = "<color=white>reagent</color>";

	public const string _strType = "<color=white>type</color>";

	public const string _strBatchMode = "<color=orange>batchMode</color>";

	public const string _strValues = "<color=lightblue>...</color>";

	private const string FORMAT_VARIABLE = "<color=orange>{0}</color>";

	private const string FORMAT_NUMBER = "<color=lightblue>{0}</color>";

	private const string FORMAT_TEXT = "<color=white>{0}</color>";

	private static readonly HelpString STRING;

	private static readonly HelpString DEVICE_INDEX;

	private static readonly HelpString REGISTER;

	private static readonly HelpString INTEGER;

	private static readonly HelpString NUMBER;

	private static readonly HelpString REF_ID;

	private static readonly HelpString OR;

	private static readonly HelpString LOGIC_TYPE;

	private static readonly HelpString LOGIC_SLOT_TYPE;

	private static readonly HelpString BATCH_MODE;

	private static readonly HelpString DEVICE_HASH;

	private static readonly HelpString NAME_HASH;

	private static readonly HelpString SLOT_INDEX;

	private static readonly HelpString REAGENT_MODE;

	public const char REGISTER_CHAR = 'r';

	public const char DEVICE_CHAR = 'd';

	public const string BASE_UNIT_STRING = "db";

	public const int BASE_UNIT_INDEX = int.MaxValue;

	public const int BASE_NETWORK_INDEX = int.MinValue;

	public const int FIRST_AVAILABLE_NETWORK = int.MaxValue;

	public const string RETURN_ADDRESS_STRING = "ra";

	public const string STACK_POINTER_STRING = "sp";

	public const char HEX_CHAR = '$';

	public const char BINARY_CHAR = '%';

	public const char COMMENT_CHAR = '#';

	public const char NETWORK_CHAR = ':';

	public static Constant[] AllConstants;

	private const int _RotateWidth = 54;

	private const long _RotateMask = 18014398509481983L;

	public static List<IScriptEnum> InternalEnums;

	public char[] SourceCodeCharArray { get; set; }

	public int SourceCodeWritePointer { get; set; }

	public string ErrorLineNumberString => StringManager.Get(_ErrorLineNumberSynced);

	public ulong LastEditedId { get; set; }

	private ushort _ErrorLineNumber
	{
		get
		{
			return _ErrorLineNumberSynced;
		}
		set
		{
			if (_ErrorLineNumberSynced != value)
			{
				_ErrorLineNumberSynced = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	public string ErrorTypeString => _exceptionTypes.GetName(_ErrorType);

	private ProgrammableChipException.ICExceptionType _ErrorType
	{
		get
		{
			return (ProgrammableChipException.ICExceptionType)_ErrorTypeSynced;
		}
		set
		{
			byte b = (byte)value;
			if (b != _ErrorTypeSynced)
			{
				_ErrorTypeSynced = b;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	public bool CompilationError => CompileErrorType != ProgrammableChipException.ICExceptionType.None;

	private ushort CompileErrorLineNumber
	{
		get
		{
			return _compileErrorLineNumber;
		}
		set
		{
			_compileErrorLineNumber = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	private ProgrammableChipException.ICExceptionType CompileErrorType
	{
		get
		{
			return _compileErrorType;
		}
		set
		{
			_compileErrorType = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	private ICircuitHolder CircuitHousing => base.ParentSlot?.Parent as ICircuitHolder;

	public float MemoryUsed => SourceCode.Length;

	public double LineNumber
	{
		get
		{
			return _NextAddr;
		}
		set
		{
			try
			{
				_NextAddr = (int)Math.Clamp((uint)value, 0L, _LinesOfCode.Count - 1);
			}
			catch
			{
			}
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.LogicIntegratedCircuitsCategory);
	}

	public static string UnpackAscii6(double packedDouble, bool signed)
	{
		ulong num = (ulong)DoubleToLong(packedDouble, signed);
		int num2 = 0;
		ulong num3 = num;
		while (num3 != 0L && num2 < 8)
		{
			num2++;
			num3 >>= 8;
		}
		char[] array = new char[num2];
		for (int num4 = num2 - 1; num4 >= 0; num4--)
		{
			array[num4] = (char)(num & 0xFF);
			num >>= 8;
		}
		return new string(array);
	}

	public static double PackAscii6(string text, int lineNumber)
	{
		if (string.IsNullOrEmpty(text))
		{
			throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.InvalidStringNull, lineNumber);
		}
		if (text.Length > 6)
		{
			throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.InvalidStringLength, lineNumber);
		}
		long num = 0L;
		foreach (char c in text)
		{
			if (c > '\u007f')
			{
				throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.InvalidStringNonAscii, lineNumber);
			}
			num = (num << 8) | (byte)c;
		}
		return num;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.LineNumber)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.LineNumber)
		{
			return _NextAddr;
		}
		return base.GetLogicValue(logicType);
	}

	static ProgrammableChip()
	{
		_exceptionTypes = new EnumCollection<ProgrammableChipException.ICExceptionType, byte>(toProper: false);
		STRING = new HelpString("str", "white");
		DEVICE_INDEX = new HelpString("d?", "green");
		REGISTER = new HelpString("r?", "#0080FFFF");
		INTEGER = new HelpString("int", "#20B2AA");
		NUMBER = new HelpString("num", "#20B2AA");
		REF_ID = new HelpString("id", "#20B2AA");
		OR = new HelpString("or", "|", "#585858FF");
		LOGIC_TYPE = new HelpString("logicType", "orange");
		LOGIC_SLOT_TYPE = new HelpString("logicSlotType", "orange");
		BATCH_MODE = new HelpString("batchMode", "orange");
		DEVICE_HASH = new HelpString("deviceHash", "#20B2AA");
		NAME_HASH = new HelpString("nameHash", "#20B2AA");
		SLOT_INDEX = new HelpString("slotIndex", "#20B2AA");
		REAGENT_MODE = new HelpString("reagentMode", "orange");
		AllConstants = new Constant[9]
		{
			new Constant("nan", "A constant representing 'not a number'. This constant technically provides a 'quiet' NaN, a signal NaN from some instructions will result in an exception and halt execution", double.NaN, addValueToDescription: false),
			new Constant("pinf", "A constant representing a positive infinite value", double.PositiveInfinity, addValueToDescription: false),
			new Constant("ninf", "A constant representing a negative infinite value", double.NegativeInfinity, addValueToDescription: false),
			new Constant("pi", "A constant representing ratio of the circumference of a circle to its diameter, provided in double precision", Math.PI),
			new Constant("tau", "A constant representing the ratio of the circumference of a circle to its radius, provided in double precision", Math.PI * 2.0),
			new Constant("deg2rad", "Degrees to radians conversion constant", 0.01745329238474369),
			new Constant("rad2deg", "Radians to degrees conversion constant", 57.295780181884766),
			new Constant("epsilon", "A constant representing the smallest positive subnormal > 0", double.Epsilon, addValueToDescription: false),
			new Constant("rgas", "Universal gas constant (J/(mol*K))", 8.31446261815324)
		};
		InternalEnums = new List<IScriptEnum>
		{
			new ScriptEnum<LogicType>(InstructionInclude.LogicType, LogicBase.IsDeprecated, LogicBase.GetLogicDescription),
			new ScriptEnum<LogicSlotType>(InstructionInclude.LogicSlotType, LogicBase.IsDeprecated, LogicBase.GetLogicDescription),
			new ScriptEnum<LogicReagentMode>(InstructionInclude.LogicReagentMode, LogicBase.IsDeprecated),
			new ScriptEnum<LogicBatchMethod>(InstructionInclude.LogicBatchMethod, LogicBase.IsDeprecated),
			new BasicEnum<LogicType>("LogicType", LogicBase.IsDeprecated),
			new BasicEnum<LogicSlotType>("LogicSlotType", LogicBase.IsDeprecated),
			new BasicEnum<SoundAlert>("Sound"),
			new BasicEnum<LogicTransmitterMode>("TransmitterMode"),
			new BasicEnum<ElevatorMode>("ElevatorMode"),
			new BasicEnum<ColorType>("Color"),
			new BasicEnum<EntityState>("EntityState"),
			new BasicEnum<AirControlMode>("AirControl"),
			new BasicEnum<DaylightSensor.DaylightSensorMode>("DaylightSensorMode"),
			new BasicEnum<ConditionOperation>(),
			new BasicEnum<AirConditioningMode>("AirCon"),
			new BasicEnum<VentDirection>("Vent"),
			new BasicEnum<FiltrationMode>("FiltrationMode"),
			new BasicEnum<PowerMode>("PowerMode"),
			new BasicEnum<RobotMode>("RobotMode"),
			new BasicEnum<SortingClass>("SortingClass"),
			new BasicEnum<Slot.Class>("SlotClass"),
			new BasicEnum<Chemistry.GasType>("GasType"),
			new BasicEnum<RocketMode>("RocketMode"),
			new BasicEnum<ReEntryProfile>("ReEntryProfile"),
			new BasicEnum<SorterInstruction>("SorterInstruction"),
			new BasicEnum<PrinterInstruction>("PrinterInstruction"),
			new BasicEnum<TraderInstruction>("TraderInstruction"),
			new BasicEnum<ShuttleType>("ShuttleType"),
			new BasicEnum<HashType>("HashType"),
			new BasicEnum<LogicDisplay.DisplayMode>("DisplayMode"),
			new BasicEnum<SettingDisplayMode>("SettingDisplayMode"),
			new BasicEnum<NodeType>("NodeType")
		};
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteUInt16(_ErrorLineNumber);
			writer.WriteByte((byte)_ErrorType);
			writer.WriteUInt16(CompileErrorLineNumber);
			writer.WriteByte((byte)CompileErrorType);
		}
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteAscii(SourceCode);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			_ErrorLineNumber = reader.ReadUInt16();
			_ErrorType = (ProgrammableChipException.ICExceptionType)reader.ReadByte();
			CompileErrorLineNumber = reader.ReadUInt16();
			CompileErrorType = (ProgrammableChipException.ICExceptionType)reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			SetSourceCode(reader.ReadAscii());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteUInt16(_ErrorLineNumber);
		writer.WriteByte((byte)_ErrorType);
		writer.WriteUInt16(CompileErrorLineNumber);
		writer.WriteByte((byte)CompileErrorType);
		writer.WriteAscii(SourceCode);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_ErrorLineNumber = reader.ReadUInt16();
		_ErrorType = (ProgrammableChipException.ICExceptionType)reader.ReadByte();
		CompileErrorLineNumber = reader.ReadUInt16();
		CompileErrorType = (ProgrammableChipException.ICExceptionType)reader.ReadByte();
		SetSourceCode(reader.ReadAscii());
	}

	public void SendUpdate()
	{
		if (NetworkManager.IsClient)
		{
			ISourceCode.SendSourceCodeToServer(SourceCode, base.ReferenceId);
		}
		else if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 256;
		}
	}

	public override string GetQuantityText()
	{
		return StringGenerator.GetString((int)MemoryUsed, Unit.ProgrammableChip);
	}

	public void SetSourceCode(string sourceCode, ICircuitHolder parent)
	{
		parent.ClearError();
		SetSourceCode(sourceCode);
	}

	public void SetSourceCode(string sourceCode)
	{
		_LinesOfCode.Clear();
		_Aliases.Clear();
		_Defines.Clear();
		_JumpTags.Clear();
		_ErrorType = ProgrammableChipException.ICExceptionType.None;
		_ErrorLineNumber = 0;
		CompileErrorType = ProgrammableChipException.ICExceptionType.None;
		CompileErrorLineNumber = 0;
		_Registers[_StackPointerIndex] = 0.0;
		if (string.IsNullOrEmpty(sourceCode))
		{
			sourceCode = string.Empty;
		}
		SourceCode = new AsciiString(sourceCode);
		if (CircuitHousing != null)
		{
			CircuitHousing.ClearError();
			new _ALIAS_Operation(this, 0, "db", $"d{int.MaxValue}").Execute(0, updateLabels: false);
			new _ALIAS_Operation(this, 0, "sp", $"r{_StackPointerIndex}").Execute(0);
			new _ALIAS_Operation(this, 0, "ra", $"r{_ReturnAddressIndex}").Execute(0);
		}
		string[] array = sourceCode.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				if (array[i].IndexOf('#') == 0)
				{
					_LinesOfCode.Add(new _LineOfCode(this, string.Empty, i));
				}
				else
				{
					_LinesOfCode.Add(new _LineOfCode(this, array[i], i));
				}
			}
			catch (ProgrammableChipException ex)
			{
				CircuitHousing?.RaiseError(1);
				CompileErrorLineNumber = ex.LineNumber;
				CompileErrorType = ex.ExceptionType;
				break;
			}
			catch (Exception)
			{
				CircuitHousing?.RaiseError(1);
				CompileErrorLineNumber = (ushort)i;
				CompileErrorType = ProgrammableChipException.ICExceptionType.Unknown;
				break;
			}
		}
		_NextAddr = 0;
		base.ParentSlot?.RefreshQuantity();
	}

	public AsciiString GetSourceCode()
	{
		return SourceCode;
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (!(attack.SourceItem is Labeller labeller))
		{
			return base.AttackWith(attack, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = ActionStrings.Rename
		};
		if (!labeller.OnOff)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
		}
		if (!labeller.IsOperable)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
		}
		if (!doAction)
		{
			return delayedActionInstance;
		}
		labeller.Rename(this);
		return delayedActionInstance;
	}

	public void Execute(int runCount)
	{
		if (_NextAddr < 0 || _NextAddr >= _LinesOfCode.Count || _LinesOfCode.Count == 0)
		{
			return;
		}
		int nextAddr = _NextAddr;
		int num = runCount;
		while (num-- > 0 && _NextAddr >= 0 && _NextAddr < _LinesOfCode.Count)
		{
			nextAddr = _NextAddr;
			try
			{
				_Operation operation = _LinesOfCode[_NextAddr].Operation;
				_NextAddr = operation.Execute(_NextAddr);
			}
			catch (ProgrammableChipException ex)
			{
				CircuitHousing?.RaiseError(1);
				_ErrorLineNumber = ex.LineNumber;
				_ErrorType = ex.ExceptionType;
				_NextAddr = nextAddr;
				break;
			}
			catch (Exception)
			{
				if (CircuitHousing != null)
				{
					CircuitHousing.RaiseError(1);
				}
				_ErrorLineNumber = (ushort)nextAddr;
				_ErrorType = ProgrammableChipException.ICExceptionType.Unknown;
				_NextAddr = nextAddr;
				break;
			}
			if (CircuitHousing != null)
			{
				_ErrorLineNumber = 0;
				_ErrorType = ProgrammableChipException.ICExceptionType.None;
				CircuitHousing.RaiseError(0);
			}
			if (_NextAddr < 0)
			{
				_NextAddr = -_NextAddr;
				break;
			}
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new ProgrammableChipSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (!(savedData is ProgrammableChipSaveData programmableChipSaveData))
		{
			return;
		}
		SetSourceCode(programmableChipSaveData.SourceCode);
		for (int i = 0; i < programmableChipSaveData.Registers.Length; i++)
		{
			_Registers[i] = programmableChipSaveData.Registers[i];
		}
		_NextAddr = programmableChipSaveData.NextAddr;
		_Aliases.Clear();
		if (programmableChipSaveData.NewAliasesKeys != null)
		{
			for (int j = 0; j < programmableChipSaveData.NewAliasesKeys.Length; j++)
			{
				_AliasValue value = new _AliasValue((_AliasTarget)programmableChipSaveData.NewAliasesValuesTarget[j], programmableChipSaveData.NewAliasesValuesIndex[j]);
				_Aliases.Add(programmableChipSaveData.NewAliasesKeys[j], value);
			}
		}
		if (programmableChipSaveData.DeviceLables != null)
		{
			for (int k = 0; k < programmableChipSaveData.DeviceLables.Count; k++)
			{
				_Aliases.Add(programmableChipSaveData.DeviceLables[k], new _AliasValue(_AliasTarget.Device, k));
			}
		}
		if (programmableChipSaveData.AliasesValues != null)
		{
			for (int l = 0; l < programmableChipSaveData.AliasesValues.Length; l++)
			{
				_Aliases.Add(programmableChipSaveData.AliasesKeys[l], new _AliasValue(_AliasTarget.Register, programmableChipSaveData.AliasesValues[l]));
			}
		}
		_JumpTags.Clear();
		if (programmableChipSaveData.JumpTagsKeys != null)
		{
			for (int m = 0; m < programmableChipSaveData.JumpTagsKeys.Length; m++)
			{
				_JumpTags.Add(programmableChipSaveData.JumpTagsKeys[m], programmableChipSaveData.JumpTagsValues[m]);
			}
		}
		if (programmableChipSaveData.Stack != null)
		{
			for (int n = 0; n < _Stack.Length && n < programmableChipSaveData.Stack.Length; n++)
			{
				_Stack[n] = programmableChipSaveData.Stack[n];
			}
		}
		_Defines.Clear();
		if (programmableChipSaveData.DefineKeys != null)
		{
			for (int num = 0; num < programmableChipSaveData.DefineKeys.Length; num++)
			{
				_Defines.Add(programmableChipSaveData.DefineKeys[num], programmableChipSaveData.DefineValues[num]);
			}
		}
		if (_NextAddr >= 0 && _NextAddr < _LinesOfCode.Count && _LinesOfCode[_NextAddr].Operation is _SLEEP_Operation)
		{
			_SLEEP_Operation obj = (_SLEEP_Operation)_LinesOfCode[_NextAddr].Operation;
			obj.LastTimeSet = GameManager.GameTime;
			obj.SleepDurationRemaining = programmableChipSaveData.SleepDurationRemaining;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (!(savedData is ProgrammableChipSaveData programmableChipSaveData))
		{
			return;
		}
		programmableChipSaveData.Registers = new double[_Registers.Length];
		for (int i = 0; i < _Registers.Length; i++)
		{
			programmableChipSaveData.Registers[i] = _Registers[i];
		}
		programmableChipSaveData.SourceCode = GetSourceCode().ToString();
		programmableChipSaveData.NextAddr = _NextAddr;
		programmableChipSaveData.NewAliasesKeys = new string[_Aliases.Count];
		programmableChipSaveData.NewAliasesValuesTarget = new int[_Aliases.Count];
		programmableChipSaveData.NewAliasesValuesIndex = new int[_Aliases.Count];
		int num = 0;
		foreach (KeyValuePair<string, _AliasValue> alias in _Aliases)
		{
			programmableChipSaveData.NewAliasesKeys[num] = alias.Key;
			programmableChipSaveData.NewAliasesValuesTarget[num] = (int)alias.Value.Target;
			programmableChipSaveData.NewAliasesValuesIndex[num] = alias.Value.Index;
			num++;
		}
		programmableChipSaveData.JumpTagsKeys = new string[_JumpTags.Count];
		programmableChipSaveData.JumpTagsValues = new int[_JumpTags.Count];
		num = 0;
		foreach (KeyValuePair<string, int> jumpTag in _JumpTags)
		{
			programmableChipSaveData.JumpTagsKeys[num] = jumpTag.Key;
			programmableChipSaveData.JumpTagsValues[num] = jumpTag.Value;
			num++;
		}
		programmableChipSaveData.DefineKeys = new string[_Defines.Count];
		programmableChipSaveData.DefineValues = new double[_Defines.Count];
		num = 0;
		foreach (KeyValuePair<string, double> define in _Defines)
		{
			programmableChipSaveData.DefineKeys[num] = define.Key;
			programmableChipSaveData.DefineValues[num] = define.Value;
			num++;
		}
		int num2 = _Stack.Length;
		int num3 = _Stack.Length;
		while (num3 > 0 && _Stack[num3 - 1] == 0.0)
		{
			num2--;
			num3--;
		}
		programmableChipSaveData.Stack = new double[num2];
		for (int j = 0; j < num2; j++)
		{
			programmableChipSaveData.Stack[j] = _Stack[j];
		}
		if (_NextAddr >= 0 && _NextAddr < _LinesOfCode.Count && _LinesOfCode[_NextAddr].Operation is _SLEEP_Operation)
		{
			_SLEEP_Operation sLEEP_Operation = (_SLEEP_Operation)_LinesOfCode[_NextAddr].Operation;
			programmableChipSaveData.SleepDurationRemaining = sLEEP_Operation.SleepDurationRemaining;
		}
	}

	public static string GetIntroString()
	{
		return string.Format("These functions are generally typed per line of instruction to your Integrated Circuit (IC). Below are a list of the functions. {0} refers to a device, the ? character replaced with either the screw number, or 'b' for base unit. For example 'd0' or 'db'. {1} refers to a register, the ? refers to the number of the register, such as 'r0'. Additional r in front allows indirect referencing. {2} refers to a logic variable, whether slot or general.", "<color=green>d?</color>", "<color=#0080FFFF>r?</color>", "<color=orange>var</color>");
	}

	private static string MakeString(ScriptCommand command, string color, int paramCount, params HelpString[] strings)
	{
		bool flag = paramCount >= 0;
		string text = (flag ? string.Empty : ("<color=" + color + ">" + EnumCollections.ScriptCommands.GetName(command) + "</color>"));
		if (paramCount < 0)
		{
			paramCount = 0;
		}
		for (int i = paramCount; i < strings.Length; i++)
		{
			text = text + " " + strings[i];
		}
		if (flag)
		{
			string pattern = "<color\\s*=\\s*['\"]?(?<color>[^'\">]+)['\"]?\\s*>(?<content>.*?)<\\/color>";
			string replacement = "<color=" + color + ">${content}</color>";
			text = Regex.Replace(text, pattern, replacement);
		}
		return text;
	}

	public static string SetString(string format, string command)
	{
		return string.Format(format, $"<color=yellow>{command}</color>", "<color=green>d?</color>", "<color=orange>var</color>", "<color=white>num</color>", "<color=white>int</color>", "<color=#585858FF>|</color>", "<color=#0080FFFF>r?</color>", "<color=white>str</color>", "<color=#0080FFFF>r?</color>|<color=white>num</color>", "<color=#0080FFFF>r?</color>|<color=green>d?</color>", "<color=orange>reagentMode</color>", "<color=white>reagent</color>", "<color=white>type</color>", "<color=orange>batchMode</color>", "<color=lightblue>...</color>");
	}

	public static string SetString(string format, ScriptCommand command)
	{
		return SetString(format, command.ToString());
	}

	public static string GetCommandExample(ScriptCommand command, string color = "yellow", int spaceCount = -1)
	{
		switch (command)
		{
		case ScriptCommand.clr:
			return MakeString(command, color, spaceCount, DEVICE_INDEX);
		case ScriptCommand.clrd:
			return MakeString(command, color, spaceCount, (REGISTER + NUMBER).Var("id"));
		case ScriptCommand.get:
			return MakeString(command, color, spaceCount, REGISTER, (DEVICE_INDEX + REGISTER + REF_ID).Var("device"), (REGISTER + NUMBER).Var("address"));
		case ScriptCommand.put:
			return MakeString(command, color, spaceCount, (DEVICE_INDEX + REGISTER + REF_ID).Var("device"), (REGISTER + NUMBER).Var("address"), (REGISTER + NUMBER).Var("value"));
		case ScriptCommand.getd:
			return MakeString(command, color, spaceCount, REGISTER, (REGISTER + REF_ID).Var("id"), (REGISTER + NUMBER).Var("address"));
		case ScriptCommand.putd:
			return MakeString(command, color, spaceCount, (REGISTER + REF_ID).Var("id"), (REGISTER + NUMBER).Var("address"), (REGISTER + NUMBER).Var("value"));
		case ScriptCommand.l:
			return MakeString(command, color, spaceCount, REGISTER, (DEVICE_INDEX + REGISTER + REF_ID).Var("device"), LOGIC_TYPE);
		case ScriptCommand.ld:
			return MakeString(command, color, spaceCount, REGISTER, (REGISTER + REF_ID).Var("id"), LOGIC_TYPE);
		case ScriptCommand.s:
			return MakeString(command, color, spaceCount, (DEVICE_INDEX + REGISTER + REF_ID).Var("device"), LOGIC_TYPE, REGISTER);
		case ScriptCommand.sd:
			return MakeString(command, color, spaceCount, (REGISTER + REF_ID).Var("id"), LOGIC_TYPE, REGISTER);
		case ScriptCommand.ss:
			return MakeString(command, color, spaceCount, (DEVICE_INDEX + REGISTER + REF_ID).Var("device"), SLOT_INDEX, LOGIC_SLOT_TYPE, REGISTER);
		case ScriptCommand.sbs:
			return MakeString(command, color, spaceCount, DEVICE_HASH, SLOT_INDEX, LOGIC_SLOT_TYPE, REGISTER);
		case ScriptCommand.lb:
			return MakeString(command, color, spaceCount, REGISTER, DEVICE_HASH, LOGIC_TYPE, BATCH_MODE);
		case ScriptCommand.lbn:
			return MakeString(command, color, spaceCount, REGISTER, DEVICE_HASH, NAME_HASH, LOGIC_TYPE, BATCH_MODE);
		case ScriptCommand.lbs:
			return MakeString(command, color, spaceCount, REGISTER, DEVICE_HASH, SLOT_INDEX, LOGIC_SLOT_TYPE, BATCH_MODE);
		case ScriptCommand.lbns:
			return MakeString(command, color, spaceCount, REGISTER, DEVICE_HASH, NAME_HASH, SLOT_INDEX, LOGIC_SLOT_TYPE, BATCH_MODE);
		case ScriptCommand.sb:
			return MakeString(command, color, spaceCount, DEVICE_HASH, LOGIC_TYPE, REGISTER);
		case ScriptCommand.sbn:
			return MakeString(command, color, spaceCount, DEVICE_HASH, NAME_HASH, LOGIC_TYPE, REGISTER);
		case ScriptCommand.ls:
			return MakeString(command, color, spaceCount, REGISTER, (DEVICE_INDEX + REGISTER + REF_ID).Var("device"), SLOT_INDEX, LOGIC_SLOT_TYPE);
		case ScriptCommand.lr:
			return MakeString(command, color, spaceCount, REGISTER, (DEVICE_INDEX + REGISTER + REF_ID).Var("device"), REAGENT_MODE, INTEGER);
		case ScriptCommand.define:
			return MakeString(command, color, spaceCount, STRING, NUMBER);
		case ScriptCommand.alias:
			return MakeString(command, color, spaceCount, STRING, REGISTER + DEVICE_INDEX);
		case ScriptCommand.add:
		case ScriptCommand.sub:
		case ScriptCommand.slt:
		case ScriptCommand.sgt:
		case ScriptCommand.sle:
		case ScriptCommand.sge:
		case ScriptCommand.seq:
		case ScriptCommand.sne:
		case ScriptCommand.and:
		case ScriptCommand.or:
		case ScriptCommand.xor:
		case ScriptCommand.nor:
		case ScriptCommand.mul:
		case ScriptCommand.div:
		case ScriptCommand.mod:
		case ScriptCommand.max:
		case ScriptCommand.min:
		case ScriptCommand.sapz:
		case ScriptCommand.snaz:
		case ScriptCommand.atan2:
		case ScriptCommand.srl:
		case ScriptCommand.sra:
		case ScriptCommand.sll:
		case ScriptCommand.sla:
		case ScriptCommand.pow:
		case ScriptCommand.rol:
		case ScriptCommand.ror:
			return MakeString(command, color, spaceCount, REGISTER, (REGISTER + NUMBER).Var("a"), (REGISTER + NUMBER).Var("b"));
		case ScriptCommand.sap:
		case ScriptCommand.sna:
		case ScriptCommand.select:
		case ScriptCommand.ext:
		case ScriptCommand.ins:
			return MakeString(command, color, spaceCount, REGISTER, (REGISTER + NUMBER).Var("a"), (REGISTER + NUMBER).Var("b"), (REGISTER + NUMBER).Var("c"));
		case ScriptCommand.j:
		case ScriptCommand.jal:
		case ScriptCommand.jr:
			return MakeString(command, color, spaceCount, INTEGER);
		case ScriptCommand.bltz:
		case ScriptCommand.bgez:
		case ScriptCommand.blez:
		case ScriptCommand.bgtz:
		case ScriptCommand.bltzal:
		case ScriptCommand.bgezal:
		case ScriptCommand.blezal:
		case ScriptCommand.bgtzal:
		case ScriptCommand.brltz:
		case ScriptCommand.brgez:
		case ScriptCommand.brlez:
		case ScriptCommand.brgtz:
		case ScriptCommand.beqz:
		case ScriptCommand.bnez:
		case ScriptCommand.breqz:
		case ScriptCommand.brnez:
		case ScriptCommand.beqzal:
		case ScriptCommand.bnezal:
		case ScriptCommand.brnan:
		case ScriptCommand.bnan:
			return MakeString(command, color, spaceCount, (REGISTER + NUMBER).Var("a"), (REGISTER + NUMBER).Var("b"));
		case ScriptCommand.poke:
			return MakeString(command, color, spaceCount, (REGISTER + NUMBER).Var("address"), (REGISTER + NUMBER).Var("value"));
		case ScriptCommand.beq:
		case ScriptCommand.bne:
		case ScriptCommand.beqal:
		case ScriptCommand.bneal:
		case ScriptCommand.breq:
		case ScriptCommand.brne:
		case ScriptCommand.blt:
		case ScriptCommand.bgt:
		case ScriptCommand.ble:
		case ScriptCommand.bge:
		case ScriptCommand.brlt:
		case ScriptCommand.brgt:
		case ScriptCommand.brle:
		case ScriptCommand.brge:
		case ScriptCommand.bltal:
		case ScriptCommand.bgtal:
		case ScriptCommand.bleal:
		case ScriptCommand.bgeal:
		case ScriptCommand.bapz:
		case ScriptCommand.bnaz:
		case ScriptCommand.brapz:
		case ScriptCommand.brnaz:
		case ScriptCommand.bapzal:
		case ScriptCommand.bnazal:
			return MakeString(command, color, spaceCount, (REGISTER + NUMBER).Var("a"), (REGISTER + NUMBER).Var("b"), (REGISTER + NUMBER).Var("c"));
		case ScriptCommand.bap:
		case ScriptCommand.bna:
		case ScriptCommand.brap:
		case ScriptCommand.brna:
		case ScriptCommand.bapal:
		case ScriptCommand.bnaal:
			return MakeString(command, color, spaceCount, (REGISTER + NUMBER).Var("a"), (REGISTER + NUMBER).Var("b"), (REGISTER + NUMBER).Var("c"), (REGISTER + NUMBER).Var("d"));
		case ScriptCommand.lerp:
			return MakeString(command, color, spaceCount, REGISTER, (REGISTER + NUMBER).Var("a"), (REGISTER + NUMBER).Var("b"), (REGISTER + NUMBER).Var("c"));
		case ScriptCommand.clamp:
			return MakeString(command, color, spaceCount, REGISTER, (REGISTER + NUMBER).Var("a"), (REGISTER + NUMBER).Var("min"), (REGISTER + NUMBER).Var("max"));
		case ScriptCommand.move:
		case ScriptCommand.sqrt:
		case ScriptCommand.round:
		case ScriptCommand.trunc:
		case ScriptCommand.ceil:
		case ScriptCommand.floor:
		case ScriptCommand.abs:
		case ScriptCommand.log:
		case ScriptCommand.exp:
		case ScriptCommand.sltz:
		case ScriptCommand.sgtz:
		case ScriptCommand.slez:
		case ScriptCommand.sgez:
		case ScriptCommand.seqz:
		case ScriptCommand.snez:
		case ScriptCommand.sin:
		case ScriptCommand.asin:
		case ScriptCommand.tan:
		case ScriptCommand.atan:
		case ScriptCommand.cos:
		case ScriptCommand.acos:
		case ScriptCommand.snan:
		case ScriptCommand.snanz:
		case ScriptCommand.not:
		case ScriptCommand.sgn:
			return MakeString(command, color, spaceCount, REGISTER, (REGISTER + NUMBER).Var("a"));
		case ScriptCommand.rand:
			return MakeString(command, color, spaceCount, REGISTER);
		case ScriptCommand.yield:
		case ScriptCommand.hcf:
			return MakeString(command, color, spaceCount);
		case ScriptCommand.label:
			return MakeString(command, color, spaceCount, DEVICE_INDEX, STRING);
		case ScriptCommand.push:
		case ScriptCommand.sleep:
			return MakeString(command, color, spaceCount, (REGISTER + NUMBER).Var("a"));
		case ScriptCommand.peek:
		case ScriptCommand.pop:
			return MakeString(command, color, spaceCount, REGISTER);
		case ScriptCommand.sdse:
		case ScriptCommand.sdns:
			return MakeString(command, color, spaceCount, REGISTER, (DEVICE_INDEX + REGISTER + REF_ID).Var("device"));
		case ScriptCommand.bdse:
		case ScriptCommand.bdns:
		case ScriptCommand.brdse:
		case ScriptCommand.brdns:
		case ScriptCommand.bdseal:
		case ScriptCommand.bdnsal:
			return MakeString(command, color, spaceCount, (DEVICE_INDEX + REGISTER + REF_ID).Var("device"), (REGISTER + NUMBER).Var("a"));
		case ScriptCommand.rmap:
			return MakeString(command, color, spaceCount, REGISTER, DEVICE_INDEX, (REGISTER + NUMBER).Var("reagentHash"));
		case ScriptCommand.bdnvl:
		case ScriptCommand.bdnvs:
			return MakeString(command, color, spaceCount, (DEVICE_INDEX + REGISTER + REF_ID).Var("device"), LOGIC_TYPE, (REGISTER + NUMBER).Var("a"));
		default:
			throw new ArgumentOutOfRangeException(Localization.GetInterface("ScriptCommandCommand"), command, null);
		}
	}

	private async UniTaskVoid HaltAndCatchFireFromThread()
	{
		await UniTask.SwitchToMainThread();
		HaltAndCatchFire();
	}

	private void HaltAndCatchFire()
	{
		if (GameManager.IsThread)
		{
			HaltAndCatchFireFromThread().Forget();
			return;
		}
		base.IsBurning = true;
		OnFireStart();
		CircuitHousing?.HaltAndCatchFire();
		Achievements.AchieveHaltAndCatchFire();
	}

	public void Reset()
	{
		_NextAddr = 0;
		_Registers[_StackPointerIndex] = 0.0;
		_ErrorType = ProgrammableChipException.ICExceptionType.None;
		_ErrorLineNumber = 0;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		passiveTooltip.Extended += GetErrorCode();
		return passiveTooltip;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.AppendLine(GameStrings.ItemInSlotValue.AsString(ToTooltip(), GetQuantityText()));
		extendedText.Append(GetErrorCode());
		return extendedText;
	}

	public string GetErrorCode()
	{
		if (CompilationError)
		{
			return GameStrings.ProgrammableChipErrorCode.AsString(_exceptionTypes.GetName(CompileErrorType), StringManager.Get(CompileErrorLineNumber)) + "\n";
		}
		if (_ErrorType != ProgrammableChipException.ICExceptionType.None)
		{
			return GameStrings.ProgrammableChipErrorCode.AsString(_exceptionTypes.GetName(_ErrorType), StringManager.Get(_ErrorLineNumberSynced)) + "\n";
		}
		return string.Empty;
	}

	public void AppendErrorsToActionInstance(DelayedActionInstance actionInstance)
	{
		if (CompilationError)
		{
			actionInstance.AppendStateMessage(GameStrings.ProgrammableChipErrorCode, _exceptionTypes.GetName(CompileErrorType), StringManager.Get(CompileErrorLineNumber));
		}
		else if (_ErrorType != ProgrammableChipException.ICExceptionType.None)
		{
			actionInstance.AppendStateMessage(GameStrings.ProgrammableChipErrorCode, ErrorTypeString, ErrorLineNumberString);
		}
	}

	public static double LongToDouble(long l)
	{
		bool num = (l & 0x20000000000000L) != 0;
		l &= 0x1FFFFFFFFFFFFFL;
		if (num)
		{
			l |= -9007199254740992L;
		}
		return l;
	}

	public static long DoubleToLong(double d, bool signed)
	{
		long num = (long)(d % 9007199254740992.0);
		if (!signed)
		{
			num &= 0x3FFFFFFFFFFFFFL;
		}
		return num;
	}

	public double ReadMemory(int address)
	{
		if (address < 0)
		{
			throw new StackUnderflowException();
		}
		if (address >= _Stack.Length)
		{
			throw new StackOverflowException();
		}
		return _Stack[address];
	}

	public void WriteMemory(int address, double value)
	{
		if (address < 0)
		{
			throw new StackUnderflowException();
		}
		if (address >= _Stack.Length)
		{
			throw new StackOverflowException();
		}
		_Stack[address] = value;
	}

	public void ClearMemory()
	{
		for (int i = 0; i < _Stack.Length; i++)
		{
			_Stack[i] = 0.0;
		}
	}

	public int GetStackSize()
	{
		return _Stack.Length;
	}

	public static string StripColorTags(string input)
	{
		return Regex.Replace(input, "<color\\s*=\\s*['\"]?(?<color>[^'\">]+)['\"]?\\s*>(?<content>.*?)<\\/color>", "${content}");
	}

	public static string GetCommandDescription(ScriptCommand command)
	{
		return command switch
		{
			ScriptCommand.l => Localization.GetInterface("ScriptCommandL"), 
			ScriptCommand.ld => Localization.GetInterface("ScriptCommandLD"), 
			ScriptCommand.sb => Localization.GetInterface("ScriptCommandSB"), 
			ScriptCommand.lb => Localization.GetInterface("ScriptCommandLB"), 
			ScriptCommand.lbs => Localization.GetInterface("ScriptCommandLBS"), 
			ScriptCommand.lbn => Localization.GetInterface("ScriptCommandLBN"), 
			ScriptCommand.lbns => Localization.GetInterface("ScriptCommandLBNS"), 
			ScriptCommand.s => Localization.GetInterface("ScriptCommandS"), 
			ScriptCommand.sd => Localization.GetInterface("ScriptCommandSD"), 
			ScriptCommand.ss => Localization.GetInterface("ScriptCommandSS"), 
			ScriptCommand.sbs => Localization.GetInterface("ScriptCommandSBS"), 
			ScriptCommand.sbn => GameStrings.ScriptDescriptionSbn.DisplayString, 
			ScriptCommand.ls => Localization.GetInterface("ScriptCommandLS"), 
			ScriptCommand.lr => Localization.GetInterface("ScriptCommandLR"), 
			ScriptCommand.alias => Localization.GetInterface("ScriptCommandAlias"), 
			ScriptCommand.define => Localization.GetInterface("ScriptCommandDefine"), 
			ScriptCommand.move => Localization.GetInterface("ScriptCommandMove"), 
			ScriptCommand.add => Localization.GetInterface("ScriptCommandAdd"), 
			ScriptCommand.sub => Localization.GetInterface("ScriptCommandSub"), 
			ScriptCommand.sdse => Localization.GetInterface("ScriptCommandSdse"), 
			ScriptCommand.sdns => Localization.GetInterface("ScriptCommandSdns"), 
			ScriptCommand.slt => Localization.GetInterface("ScriptCommandSlt"), 
			ScriptCommand.sgt => Localization.GetInterface("ScriptCommandSgt"), 
			ScriptCommand.sle => Localization.GetInterface("ScriptCommandSle"), 
			ScriptCommand.sge => Localization.GetInterface("ScriptCommandSge"), 
			ScriptCommand.seq => Localization.GetInterface("ScriptCommandSeq"), 
			ScriptCommand.sne => Localization.GetInterface("ScriptCommandSne"), 
			ScriptCommand.sap => Localization.GetInterface("ScriptCommandSap"), 
			ScriptCommand.sna => Localization.GetInterface("ScriptCommandSna"), 
			ScriptCommand.sltz => Localization.GetInterface("ScriptCommandSltz"), 
			ScriptCommand.sgtz => Localization.GetInterface("ScriptCommandSgtz"), 
			ScriptCommand.slez => Localization.GetInterface("ScriptCommandSlez"), 
			ScriptCommand.sgez => Localization.GetInterface("ScriptCommandSgez"), 
			ScriptCommand.seqz => Localization.GetInterface("ScriptCommandSeqz"), 
			ScriptCommand.snez => Localization.GetInterface("ScriptCommandSnez"), 
			ScriptCommand.sapz => Localization.GetInterface("ScriptCommandSapz"), 
			ScriptCommand.snaz => Localization.GetInterface("ScriptCommandSnaz"), 
			ScriptCommand.mul => Localization.GetInterface("ScriptCommandMul"), 
			ScriptCommand.div => Localization.GetInterface("ScriptCommandDiv"), 
			ScriptCommand.mod => Localization.GetInterface("ScriptCommandMod"), 
			ScriptCommand.j => Localization.GetInterface("ScriptCommandJ"), 
			ScriptCommand.bdse => Localization.GetInterface("ScriptCommandBdse"), 
			ScriptCommand.bdns => Localization.GetInterface("ScriptCommandBdns"), 
			ScriptCommand.blt => Localization.GetInterface("ScriptCommandBlt"), 
			ScriptCommand.bgt => Localization.GetInterface("ScriptCommandBgt"), 
			ScriptCommand.ble => Localization.GetInterface("ScriptCommandBle"), 
			ScriptCommand.bge => Localization.GetInterface("ScriptCommandBge"), 
			ScriptCommand.beq => Localization.GetInterface("ScriptCommandBeq"), 
			ScriptCommand.brnan => Localization.GetInterface("ScriptCommandBrnan"), 
			ScriptCommand.bnan => Localization.GetInterface("ScriptCommandBnan"), 
			ScriptCommand.bne => Localization.GetInterface("ScriptCommandBne"), 
			ScriptCommand.bap => Localization.GetInterface("ScriptCommandBap"), 
			ScriptCommand.bna => Localization.GetInterface("ScriptCommandBna"), 
			ScriptCommand.bltz => Localization.GetInterface("ScriptCommandBltz"), 
			ScriptCommand.bgez => Localization.GetInterface("ScriptCommandBgez"), 
			ScriptCommand.blez => Localization.GetInterface("ScriptCommandBlez"), 
			ScriptCommand.bgtz => Localization.GetInterface("ScriptCommandBgtz"), 
			ScriptCommand.beqz => Localization.GetInterface("ScriptCommandBeqz"), 
			ScriptCommand.bnez => Localization.GetInterface("ScriptCommandBnez"), 
			ScriptCommand.bapz => Localization.GetInterface("ScriptCommandBapz"), 
			ScriptCommand.bnaz => Localization.GetInterface("ScriptCommandBnaz"), 
			ScriptCommand.jr => Localization.GetInterface("ScriptCommandJr"), 
			ScriptCommand.brdse => Localization.GetInterface("ScriptCommandBrdse"), 
			ScriptCommand.brdns => Localization.GetInterface("ScriptCommandBrdns"), 
			ScriptCommand.brlt => Localization.GetInterface("ScriptCommandBrlt"), 
			ScriptCommand.brgt => Localization.GetInterface("ScriptCommandBrgt"), 
			ScriptCommand.brle => Localization.GetInterface("ScriptCommandBrle"), 
			ScriptCommand.brge => Localization.GetInterface("ScriptCommandBrge"), 
			ScriptCommand.breq => Localization.GetInterface("ScriptCommandBreq"), 
			ScriptCommand.brne => Localization.GetInterface("ScriptCommandBrne"), 
			ScriptCommand.brap => Localization.GetInterface("ScriptCommandBrap"), 
			ScriptCommand.brna => Localization.GetInterface("ScriptCommandBrna"), 
			ScriptCommand.brltz => Localization.GetInterface("ScriptCommandBrltz"), 
			ScriptCommand.brgez => Localization.GetInterface("ScriptCommandBrgez"), 
			ScriptCommand.brlez => Localization.GetInterface("ScriptCommandBrlez"), 
			ScriptCommand.brgtz => Localization.GetInterface("ScriptCommandBrgtz"), 
			ScriptCommand.breqz => Localization.GetInterface("ScriptCommandBreqz"), 
			ScriptCommand.brnez => Localization.GetInterface("ScriptCommandBrnez"), 
			ScriptCommand.brapz => Localization.GetInterface("ScriptCommandBrapz"), 
			ScriptCommand.brnaz => Localization.GetInterface("ScriptCommandBrnaz"), 
			ScriptCommand.jal => Localization.GetInterface("ScriptCommandJal"), 
			ScriptCommand.bdseal => Localization.GetInterface("ScriptCommandBdseal"), 
			ScriptCommand.bdnsal => Localization.GetInterface("ScriptCommandBdnsal"), 
			ScriptCommand.bltal => Localization.GetInterface("ScriptCommandBltal"), 
			ScriptCommand.bgtal => Localization.GetInterface("ScriptCommandBgtal"), 
			ScriptCommand.bleal => Localization.GetInterface("ScriptCommandBleal"), 
			ScriptCommand.bgeal => Localization.GetInterface("ScriptCommandBgeal"), 
			ScriptCommand.beqal => Localization.GetInterface("ScriptCommandBeqal"), 
			ScriptCommand.bneal => Localization.GetInterface("ScriptCommandBneal"), 
			ScriptCommand.bapal => Localization.GetInterface("ScriptCommandBapal"), 
			ScriptCommand.bnaal => Localization.GetInterface("ScriptCommandBnaal"), 
			ScriptCommand.bltzal => Localization.GetInterface("ScriptCommandBltzal"), 
			ScriptCommand.bgezal => Localization.GetInterface("ScriptCommandBgezal"), 
			ScriptCommand.blezal => Localization.GetInterface("ScriptCommandBlezal"), 
			ScriptCommand.bgtzal => Localization.GetInterface("ScriptCommandBgtzal"), 
			ScriptCommand.beqzal => Localization.GetInterface("ScriptCommandBeqzal"), 
			ScriptCommand.bnezal => Localization.GetInterface("ScriptCommandBnezal"), 
			ScriptCommand.bapzal => Localization.GetInterface("ScriptCommandBapzal"), 
			ScriptCommand.bnazal => Localization.GetInterface("ScriptCommandBnazal"), 
			ScriptCommand.sqrt => Localization.GetInterface("ScriptCommandSqrt"), 
			ScriptCommand.round => Localization.GetInterface("ScriptCommandRound"), 
			ScriptCommand.trunc => Localization.GetInterface("ScriptCommandTrunc"), 
			ScriptCommand.ceil => Localization.GetInterface("ScriptCommandCeil"), 
			ScriptCommand.floor => Localization.GetInterface("ScriptCommandFloor"), 
			ScriptCommand.max => Localization.GetInterface("ScriptCommandMax"), 
			ScriptCommand.min => Localization.GetInterface("ScriptCommandMin"), 
			ScriptCommand.abs => Localization.GetInterface("ScriptCommandAbs"), 
			ScriptCommand.log => Localization.GetInterface("ScriptCommandLog"), 
			ScriptCommand.exp => Localization.GetInterface("ScriptCommandExp"), 
			ScriptCommand.rand => Localization.GetInterface("ScriptCommandRand"), 
			ScriptCommand.yield => Localization.GetInterface("ScriptCommandYield"), 
			ScriptCommand.label => Localization.GetInterface("ScriptCommandLabel"), 
			ScriptCommand.peek => Localization.GetInterface("ScriptCommandPeek"), 
			ScriptCommand.push => Localization.GetInterface("ScriptCommandPush"), 
			ScriptCommand.poke => GameStrings.ScriptDescriptionPoke.DisplayString, 
			ScriptCommand.pop => Localization.GetInterface("ScriptCommandPop"), 
			ScriptCommand.get => GameStrings.ScriptDescriptionGet.DisplayString, 
			ScriptCommand.pow => GameStrings.ScriptDescriptionPow.DisplayString, 
			ScriptCommand.clr => GameStrings.ScriptDescriptionClr.DisplayString, 
			ScriptCommand.clrd => GameStrings.ScriptDescriptionClrD.DisplayString, 
			ScriptCommand.put => GameStrings.ScriptDescriptionPut.DisplayString, 
			ScriptCommand.getd => GameStrings.ScriptDescriptionGetD.DisplayString, 
			ScriptCommand.putd => GameStrings.ScriptDescriptionPutD.DisplayString, 
			ScriptCommand.ext => GameStrings.ScriptDescriptionExt.DisplayString, 
			ScriptCommand.ins => GameStrings.ScriptDescriptionIns.DisplayString, 
			ScriptCommand.lerp => GameStrings.ScriptDescriptionLerp.DisplayString, 
			ScriptCommand.hcf => Localization.GetInterface("ScriptCommandHcf"), 
			ScriptCommand.select => Localization.GetInterface("ScriptCommandSelect"), 
			ScriptCommand.sleep => Localization.GetInterface("ScriptCommandSleep"), 
			ScriptCommand.sin => Localization.GetInterface("ScriptCommandSin"), 
			ScriptCommand.asin => Localization.GetInterface("ScriptCommandASin"), 
			ScriptCommand.tan => Localization.GetInterface("ScriptCommandTan"), 
			ScriptCommand.atan => Localization.GetInterface("ScriptCommandATan"), 
			ScriptCommand.cos => Localization.GetInterface("ScriptCommandCos"), 
			ScriptCommand.acos => Localization.GetInterface("ScriptCommandACos"), 
			ScriptCommand.atan2 => Localization.GetInterface("ScriptCommandATan2"), 
			ScriptCommand.snan => Localization.GetInterface("ScriptCommandSnan"), 
			ScriptCommand.snanz => Localization.GetInterface("ScriptCommandSnanz"), 
			ScriptCommand.srl => GameStrings.ScriptDescriptionSrl.DisplayString, 
			ScriptCommand.sra => GameStrings.ScriptDescriptionSra.DisplayString, 
			ScriptCommand.sll => GameStrings.ScriptDescriptionSll.DisplayString, 
			ScriptCommand.sla => GameStrings.ScriptDescriptionSla.DisplayString, 
			ScriptCommand.not => GameStrings.ScriptDescriptionNot.DisplayString, 
			ScriptCommand.and => GameStrings.ScriptDescriptionAnd.DisplayString, 
			ScriptCommand.or => GameStrings.ScriptDescriptionOr.DisplayString, 
			ScriptCommand.xor => GameStrings.ScriptDescriptionXor.DisplayString, 
			ScriptCommand.nor => GameStrings.ScriptDescriptionNor.DisplayString, 
			ScriptCommand.rmap => GameStrings.ScriptDescriptionRMap.DisplayString, 
			ScriptCommand.bdnvl => GameStrings.ScriptDescriptionBdnvl.DisplayString, 
			ScriptCommand.bdnvs => GameStrings.ScriptDescriptionBdnvs.DisplayString, 
			ScriptCommand.sgn => GameStrings.ScriptDescriptionSgn.DisplayString, 
			ScriptCommand.clamp => GameStrings.ScriptDescriptionClamp.DisplayString, 
			ScriptCommand.rol => GameStrings.ScriptDescriptionRol.DisplayString, 
			ScriptCommand.ror => GameStrings.ScriptDescriptionRor.DisplayString, 
			_ => throw new ArgumentOutOfRangeException(Localization.GetInterface("ScriptCommandCommand"), command, null), 
		};
	}
}
