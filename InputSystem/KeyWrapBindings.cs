using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace InputSystem;

internal static class KeyWrapBindings
{
	private readonly struct Binding
	{
		internal readonly InputPhase phase;

		internal readonly Action callback;

		internal readonly KeyInputState inputState;

		internal Binding(InputPhase phase, Action callback, KeyInputState inputState)
		{
			this.phase = phase;
			this.callback = callback;
			this.inputState = inputState;
		}

		public override string ToString()
		{
			return $"phase: {phase}, state: {inputState}";
		}
	}

	private static readonly Dictionary<KeyWrap, HashSet<Binding>> _bindingsMap = new Dictionary<KeyWrap, HashSet<Binding>>();

	public static void Bind(this KeyWrap keyWrap, InputPhase phase, Action callback, KeyInputState inputStates = KeyInputState.All)
	{
		if (!_bindingsMap.ContainsKey(keyWrap))
		{
			_bindingsMap[keyWrap] = new HashSet<Binding>();
			keyWrap.Event += KeyWrapOnEvent;
		}
		Binding item = new Binding(phase, callback, inputStates);
		_bindingsMap[keyWrap].Add(item);
	}

	private static void KeyWrapOnEvent(InputContext context)
	{
		if (!_bindingsMap.TryGetValue(context.Wrap, out var value))
		{
			return;
		}
		foreach (Binding item in value)
		{
			if (item.phase == context.Phase && (item.inputState == KeyInputState.All || item.inputState.HasFlag(KeyManager.InputState)))
			{
				item.callback?.Invoke();
			}
		}
		CleanUp(context.Wrap);
	}

	private static void CleanUp(KeyWrap keyWrap)
	{
		if (_bindingsMap[keyWrap].Count <= 0)
		{
			keyWrap.Event -= KeyWrapOnEvent;
			_bindingsMap.Remove(keyWrap);
		}
	}

	public static string Print(string[] args)
	{
		if (_bindingsMap.Count == 0)
		{
			return "None";
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (args.Length == 1)
		{
			foreach (KeyValuePair<KeyWrap, HashSet<Binding>> item in _bindingsMap)
			{
				item.Deconstruct(out var key, out var value);
				KeyWrap keyWrap = key;
				HashSet<Binding> hashSet = value;
				KeyCode[] secondaryKeys = keyWrap.SecondaryKeys;
				if (secondaryKeys != null && secondaryKeys.Length > 0)
				{
					stringBuilder.AppendLine(string.Format("Key: {0} + {1}", keyWrap.Key, string.Join(", ", secondaryKeys)));
				}
				else
				{
					stringBuilder.AppendLine($"Key: {keyWrap.Key}");
				}
				foreach (Binding item2 in hashSet)
				{
					stringBuilder.AppendLine($"\t-> Phase: {item2.phase}, For state(s): {item2.inputState}");
				}
			}
			stringBuilder.AppendLine(KeyManager.PrintKeyInputState());
		}
		else if (args[1] == "reset" || args[1] == "clear")
		{
			KeyManager.ResetKeyStateToDefault();
			stringBuilder.AppendLine(KeyManager.PrintKeyInputState());
		}
		else
		{
			stringBuilder.AppendLine("args not recognised: " + string.Join(" ", args));
		}
		return stringBuilder.ToString();
	}
}
