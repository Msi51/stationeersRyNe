using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public static class Logicable
{
	public static List<string> LogicSlotTypeStrings = Enum.GetNames(typeof(LogicSlotType)).ToList();

	public static ScriptCommand[] CommandTypes = Enum.GetValues(typeof(ScriptCommand)) as ScriptCommand[];

	public static LogicType[] LogicTypes = Enum.GetValues(typeof(LogicType)) as LogicType[];

	public static LogicSlotType[] LogicSlotTypes = Enum.GetValues(typeof(LogicSlotType)) as LogicSlotType[];

	public static LogicReagentMode[] LogicReagentModes = Enum.GetValues(typeof(LogicReagentMode)) as LogicReagentMode[];

	public static LogicBatchMethod[] LogicBatchMethods = Enum.GetValues(typeof(LogicBatchMethod)) as LogicBatchMethod[];

	private static string[] LogicTypeNames = Enum.GetNames(typeof(LogicType));

	private static string[] LogicSlotTypeNames = Enum.GetNames(typeof(LogicSlotType));

	private static int[] LogicTypeNamesRedirects;

	private static int[] LogicSlotTypeNamesRedirects;

	private static List<ILogicable> _workingList = new List<ILogicable>();

	public static void Initialize()
	{
		List<ScriptCommand> list = EnumCollections.ScriptCommands.Values.ToList();
		list.Sort((ScriptCommand a, ScriptCommand b) => a.ToString().Length.CompareTo(b.ToString().Length));
		list.Reverse();
		Localization.OrderedScriptCommands = list;
		LogicTypeNamesRedirects = new int[LogicTypeNames.Length];
		for (int num = 0; num < LogicTypeNames.Length; num++)
		{
			LogicTypeNamesRedirects[num] = num;
		}
		Array.Sort(LogicTypeNamesRedirects, (int a, int b) => string.Compare(LogicTypeNames[a], LogicTypeNames[b], StringComparison.Ordinal));
		LogicSlotTypeNamesRedirects = new int[LogicSlotTypeNames.Length];
		for (int num2 = 0; num2 < LogicSlotTypeNames.Length; num2++)
		{
			LogicSlotTypeNamesRedirects[num2] = num2;
		}
		Array.Sort(LogicSlotTypeNamesRedirects, (int a, int b) => string.Compare(LogicSlotTypeNames[a], LogicSlotTypeNames[b], StringComparison.Ordinal));
	}

	public static List<ILogicable> RecalculateSortedDevicesList(CableNetwork cableNetwork)
	{
		if (cableNetwork == null)
		{
			return null;
		}
		List<ILogicable> list = new List<ILogicable>();
		list.AddRange(cableNetwork.DataDeviceList);
		list.Sort((ILogicable a, ILogicable b) => a.DisplayName.CompareTo(b.DisplayName));
		return list;
	}

	public static T GetNextReadOrWritable<T>(ILogicable thisLogicable, T currentDevice, List<ILogicable> deviceList, bool isForward, bool allowNull = false) where T : ILogicable
	{
		return _GetNext(thisLogicable, IOCheck.ReadOrWritable, currentDevice, deviceList, isForward, allowNull);
	}

	public static T GetNextReadable<T>(ILogicable thisLogicable, T currentDevice, List<ILogicable> deviceList, bool isForward, bool allowNull = false) where T : ILogicable
	{
		return _GetNext(thisLogicable, IOCheck.Readable, currentDevice, deviceList, isForward, allowNull);
	}

	public static T GetNextWritable<T>(ILogicable thisLogicable, T currentDevice, List<ILogicable> deviceList, bool isForward, bool allowNull = false) where T : ILogicable
	{
		return _GetNext(thisLogicable, IOCheck.Writable, currentDevice, deviceList, isForward, allowNull);
	}

	private static T _GetNext<T>(ILogicable thisLogicable, IOCheck iocheck, T currentDevice, List<ILogicable> deviceList, bool isForward, bool allowNull = false) where T : ILogicable
	{
		if (deviceList == null || deviceList.Count == 0)
		{
			return default(T);
		}
		T result = default(T);
		int count = deviceList.Count;
		int num = ((currentDevice != null) ? deviceList.FindIndex((ILogicable d) => object.Equals(d, currentDevice)) : ((!isForward) ? (-1) : count));
		int num2 = count;
		while (num2-- > 0)
		{
			num += ((!isForward) ? 1 : (-1));
			if (allowNull && num > count)
			{
				return default(T);
			}
			if (allowNull && num < 0)
			{
				return default(T);
			}
			num %= count;
			if (num < 0)
			{
				num += count;
			}
			ILogicable logicable = deviceList[num];
			if (logicable == null || !(logicable is T) || object.Equals(logicable, thisLogicable))
			{
				continue;
			}
			switch (iocheck)
			{
			case IOCheck.Readable:
				if (!logicable.IsLogicReadable())
				{
					continue;
				}
				break;
			case IOCheck.Writable:
				if (!logicable.IsLogicWritable())
				{
					continue;
				}
				break;
			case IOCheck.ReadOrWritable:
				if (!logicable.IsLogicReadable() && !logicable.IsLogicWritable())
				{
					continue;
				}
				break;
			default:
				return default(T);
			}
			return (T)logicable;
		}
		return result;
	}

	public static T _GetNextValidThing<T>(ILogicable thisLogicable, IOCheck iocheck, T currentDevice, List<ILogicable> deviceList, bool isForward, bool allowNull = false) where T : ILogicable
	{
		if (deviceList == null || deviceList.Count == 0)
		{
			return default(T);
		}
		return _GetNext(thisLogicable, iocheck, currentDevice, deviceList, isForward, allowNull);
	}

	public static T _GetNextCanAudioInput<T>(ILogicable thisLogicable, IOCheck iocheck, T currentDevice, List<ILogicable> deviceList, bool isForward, bool allowNull = false) where T : IAudioInput
	{
		if (deviceList == null || deviceList.Count == 0)
		{
			return default(T);
		}
		return _GetNext(thisLogicable, iocheck, currentDevice, deviceList, isForward, allowNull);
	}

	public static T GetNextValidReadable<T>(ILogicable thisLogicable, T currentDevice, List<ILogicable> deviceList, bool isForward, bool allowNull = false) where T : ILogicable
	{
		return _GetNextValidThing(thisLogicable, IOCheck.Readable, currentDevice, deviceList, isForward, allowNull);
	}

	public static T GetNextAudioInput<T>(ILogicable thisLogicable, T currentDevice, List<ILogicable> deviceList, bool isForward, bool allowNull = false) where T : IAudioInput
	{
		return _GetNextCanAudioInput(thisLogicable, IOCheck.Readable, currentDevice, deviceList, isForward, allowNull);
	}

	public static Device GetNextReadableType(int currentTypeHash, ILogicable currentPrefabHash, List<ILogicable> deviceList, bool isForward)
	{
		return _GetNextType(IOCheck.Readable, currentTypeHash, currentPrefabHash, deviceList, isForward);
	}

	public static Device GetNextWritableType(int currentTypeHash, ILogicable currentPrefabHash, List<ILogicable> deviceList, bool isForward)
	{
		return _GetNextType(IOCheck.Writable, currentTypeHash, currentPrefabHash, deviceList, isForward);
	}

	private static Device _GetNextType(IOCheck iocheck, int currentTypeHash, ILogicable currentPrefabHash, List<ILogicable> deviceList, bool isForward)
	{
		if (deviceList == null || deviceList.Count == 0)
		{
			return null;
		}
		List<int> list = new List<int>(deviceList.Count);
		for (int i = 0; i < deviceList.Count; i++)
		{
			ILogicable logicable = deviceList[i];
			int prefabHash = logicable.GetPrefabHash();
			if (list.Contains(prefabHash))
			{
				continue;
			}
			switch (iocheck)
			{
			case IOCheck.Readable:
				if (!logicable.IsLogicReadable())
				{
					continue;
				}
				break;
			case IOCheck.Writable:
				if (!logicable.IsLogicWritable())
				{
					continue;
				}
				break;
			default:
				return null;
			}
			list.Add(prefabHash);
		}
		int num = currentTypeHash;
		int num2 = ((currentPrefabHash == null) ? (list.Count - 1) : list.FindIndex((int t) => currentTypeHash == t));
		num2 += (isForward ? 1 : (-1));
		num2 %= list.Count;
		if (num2 < 0)
		{
			num2 += list.Count;
		}
		num = list[num2];
		if (num != currentTypeHash)
		{
			return Prefab.Find(num) as Device;
		}
		return null;
	}

	public static int GetNextSlot(int slotIndex, Device currentDevice, bool isForward)
	{
		if (currentDevice == null)
		{
			return -1;
		}
		return currentDevice.GetNextSlotId(slotIndex, isForward);
	}

	public static Slot GetSlot(int slotIndex, Device currentDevice)
	{
		if (currentDevice == null)
		{
			return null;
		}
		return currentDevice.GetSlot(slotIndex);
	}

	public static LogicType GetNextReadableValue(LogicType currentType, Device device, bool isForward)
	{
		return _GetNextValue(IOCheck.Readable, currentType, device, isForward);
	}

	public static LogicType GetNextWritableValue(LogicType currentType, Device device, bool isForward)
	{
		return _GetNextValue(IOCheck.Writable, currentType, device, isForward);
	}

	private static LogicType _GetNextValue(IOCheck iocheck, LogicType currentType, Device device, bool isForward)
	{
		if (device == null)
		{
			return LogicType.None;
		}
		int num = Array.IndexOf(LogicTypes, currentType);
		int num2 = num;
		do
		{
			num += ((!isForward) ? 1 : (-1));
			num %= LogicTypes.Length;
			if (num < 0)
			{
				num += LogicTypes.Length;
			}
			switch (iocheck)
			{
			case IOCheck.Readable:
				if (device.CanLogicRead(LogicTypes[num]))
				{
					break;
				}
				continue;
			case IOCheck.Writable:
				if (device.CanLogicWrite(LogicTypes[num]))
				{
					break;
				}
				continue;
			default:
				return LogicType.None;
			}
			return LogicTypes[num];
		}
		while (num != num2);
		return LogicType.None;
	}

	public static LogicSlotType GetNextReadableValue(LogicSlotType currentType, Device device, int slotIndex, bool isForward)
	{
		return _GetNextValue(IOCheck.Readable, currentType, device, slotIndex, isForward);
	}

	private static LogicSlotType _GetNextValue(IOCheck iocheck, LogicSlotType currentType, Device device, int slotIndex, bool isForward)
	{
		if (device == null || device.GetSlot(slotIndex) == null)
		{
			return LogicSlotType.None;
		}
		int num = Array.IndexOf(LogicSlotTypeNamesRedirects, (int)currentType);
		int num2 = num;
		do
		{
			num += ((!isForward) ? 1 : (-1));
			num %= LogicSlotTypeNamesRedirects.Length;
			if (num < 0)
			{
				num += LogicSlotTypeNamesRedirects.Length;
			}
			if (iocheck == IOCheck.Readable)
			{
				if (device.CanLogicRead(LogicSlotTypes[LogicSlotTypeNamesRedirects[num]], slotIndex))
				{
					return (LogicSlotType)LogicSlotTypeNamesRedirects[num];
				}
				continue;
			}
			return LogicSlotType.None;
		}
		while (num != num2);
		return LogicSlotType.None;
	}

	public static Reagent GetNextReagent(Reagent reagent, bool isForward = true)
	{
		int num = Reagent.AllReagentsSorted.FindIndex((Reagent r) => object.Equals(r, reagent));
		num += (isForward ? 1 : (-1));
		num %= Reagent.AllReagentsSorted.Count;
		if (num < 0)
		{
			num += Reagent.AllReagentsSorted.Count;
		}
		return Reagent.AllReagentsSorted[num];
	}

	public static Thing.DelayedActionInstance _TryGetNextLogicDevice(Interactable interactable, Interaction interaction, ref ILogicable device, List<ILogicable> inputNetworkDeviceList, bool doAction, byte screwIndex)
	{
		Thing.DelayedActionInstance delayedActionInstance = new Thing.DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		Thing parent = interactable.Parent;
		if ((object)parent == null)
		{
			return delayedActionInstance.Succeed();
		}
		if (!(interaction.SourceSlot.Occupant is Screwdriver))
		{
			return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
		}
		ILogicable nextReadable = GetNextReadable(parent as ILogicable, device, inputNetworkDeviceList, interaction.AltKey, allowNull: true);
		delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, (nextReadable != null) ? nextReadable.ToTooltip() : GameStrings.LogicNoDevice.AsString());
		if (!KeyManager.GetButton(KeyMap.QuantityModifier))
		{
			delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
		}
		if (!doAction)
		{
			return delayedActionInstance.Succeed();
		}
		parent.PlayPooledAudioSound(Defines.Sounds.ScrewdriverSound, Vector3.zero);
		if (GameManager.RunSimulation)
		{
			device = nextReadable;
			if (NetworkManager.IsServer)
			{
				switch (screwIndex)
				{
				case 0:
					parent.NetworkUpdateFlags |= 1024;
					break;
				case 1:
					parent.NetworkUpdateFlags |= 2048;
					break;
				case 2:
					parent.NetworkUpdateFlags |= 4096;
					break;
				case 3:
					parent.NetworkUpdateFlags |= 8192;
					break;
				case 4:
					parent.NetworkUpdateFlags |= 16384;
					break;
				case 5:
					parent.NetworkUpdateFlags |= 32768;
					break;
				}
			}
		}
		return delayedActionInstance.Succeed();
	}
}
