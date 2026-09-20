using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AOT;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using Messages;
using UI.ImGuiUi;
using Unity.Mathematics;
using UnityEngine;
using Util.Commands;

namespace Assets.Scripts;

public static class ConsoleWindow
{
	private sealed class ConsoleModal : IModal
	{
		public bool UnlockCursor => true;
	}

	private struct PrematureLog
	{
		internal string output;

		internal ConsoleColor color;

		internal bool clearLine;

		internal bool aged;

		internal bool unformatted;
	}

	private static bool _show;

	private static bool _setSize = true;

	private static Vector2 _inputSize;

	private static bool _inputLastActive;

	private static string _consoleInput = string.Empty;

	private static bool _clearConsoleInput;

	private static ConsoleLine[] _consoleBuffer = Array.Empty<ConsoleLine>();

	private static readonly string[] _commandBuffer = new string[8];

	private static int _customWindowHeight;

	private static bool _useCustomWindowHeight;

	private static bool _snapNextFrame = true;

	private static float _copyToastTime;

	private static List<string> _tabMatches = new List<string>();

	private static int _tabIndex;

	private static string _lastTabResult = string.Empty;

	private static readonly ConsoleModal _cursorModal = new ConsoleModal();

	private static RocketSystemConsole _systemConsoleInput;

	public const ConsoleColor DEFAULT_COLOR = ConsoleColor.White;

	public static readonly uint DefaultColor = ImguiHelper.ColorConvertFloat4ToU32(new Vector4(0.7f, 0.7f, 0.7f, 1f));

	private static readonly Queue<PrematureLog> _prematureLogQueue = new Queue<PrematureLog>();

	private static readonly Dictionary<string, List<List<string>>> _specCache = new Dictionary<string, List<List<string>>>();

	private static readonly List<string> _typeaheadScratch = new List<string>();

	private static string DateString = "HH:mm:ss ";

	public static int MaxLength = 128;

	public static ConsoleCommandScope[] AllCommandScopes = (ConsoleCommandScope[])Enum.GetValues(typeof(ConsoleCommandScope));

	public static string[] ConsoleCommandScopeStrings = Enum.GetNames(typeof(ConsoleCommandScope));

	private static int _commandBufferIndex = -1;

	private static int DEFAULT_CONSOLE_BUFFER_SIZE = 1024;

	private unsafe static readonly ImGuiInputTextCallback _inputCallback = InputCallback;

	private unsafe static readonly ImGuiInputTextCallback _inputHistoryCallback = InputHistoryCallback;

	public static bool IsInitialised { get; private set; }

	public static ConsoleLine[] ConsoleBuffer => _consoleBuffer;

	public static bool IsOpen => _show;

	public static int CommandBufferIndex
	{
		get
		{
			return _commandBufferIndex;
		}
		private set
		{
			value = math.clamp(value, -1, _commandBuffer.Length - 1);
			if (value < 0 || value <= _commandBufferIndex || !string.IsNullOrEmpty(_commandBuffer[value]))
			{
				_commandBufferIndex = value;
			}
		}
	}

	private static IReadOnlyList<string> CommandLineArgs { get; set; }

	private static bool CustomLogFile => CommandLineArgs?.Contains("-logFile") ?? false;

	private static string CommandHistoryPath => Path.Combine(StationSaveUtils.GetSavePath(), "console_history.txt");

	public static void FlashCopyToast()
	{
		_copyToastTime = 1.2f;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void _Init()
	{
		Application.logMessageReceivedThreaded += LogMessage;
		Application.quitting += Shutdown;
		if (GameManager.IsBatchMode)
		{
			CommandLineArgs = Environment.GetCommandLineArgs().Skip(1).ToList();
			string text = string.Join(" ", CommandLineArgs);
			string gameVersion = GameManager.GetGameVersion();
			if (!CustomLogFile)
			{
				_systemConsoleInput = new RocketSystemConsole("Stationeers - " + gameVersion + " " + text);
			}
		}
	}

	public static void Initialize()
	{
		if (!IsInitialised)
		{
			for (int i = 0; i < DEFAULT_CONSOLE_BUFFER_SIZE; i++)
			{
				_consoleBuffer[i] = new ConsoleLine();
			}
			LoadCommandHistory();
			IsInitialised = true;
			DrainPrematureLogQueue();
			if (GameManager.IsBatchMode)
			{
				WaitForGameToBeReadyThenOverrideConsoleInput().Forget();
			}
		}
	}

	private static string ComputeNextTabMatch(string current)
	{
		if (string.IsNullOrEmpty(current))
		{
			return null;
		}
		if (current != _lastTabResult)
		{
			_tabMatches.Clear();
			CollectMatches(current, _tabMatches);
			_tabIndex = 0;
		}
		else
		{
			_tabIndex++;
		}
		if (_tabMatches.Count == 0)
		{
			return null;
		}
		_lastTabResult = _tabMatches[_tabIndex % _tabMatches.Count];
		return _lastTabResult;
	}

	private static void CollectMatches(string current, List<string> into)
	{
		if (string.IsNullOrEmpty(current))
		{
			return;
		}
		string[] array = current.Split(' ');
		if (array.Length == 1)
		{
			foreach (string key in CommandLine.CommandsMap.Keys)
			{
				if (key.StartsWith(array[0], StringComparison.OrdinalIgnoreCase))
				{
					into.Add(key);
				}
			}
			return;
		}
		if (!CommandLine.CommandsMap.TryGetValue(array[0], out var value) || value.Arguments == null)
		{
			return;
		}
		int num = array.Length - 2;
		string text = array[^1];
		string text2 = string.Join(" ", array, 0, array.Length - 1);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		bool flag = false;
		string[] arguments = value.Arguments;
		foreach (string text3 in arguments)
		{
			if (string.IsNullOrEmpty(text3))
			{
				continue;
			}
			foreach (List<string> cachedSpecPath in GetCachedSpecPaths(text3))
			{
				if (num >= cachedSpecPath.Count || !PathMatchesSoFar(cachedSpecPath, array, num))
				{
					continue;
				}
				string text4 = cachedSpecPath[num];
				if (text4 == null)
				{
					flag = true;
				}
				else if (text4.StartsWith(text, StringComparison.OrdinalIgnoreCase))
				{
					string item = text2 + " " + text4;
					if (hashSet.Add(item))
					{
						into.Add(item);
					}
				}
			}
		}
		if (!flag)
		{
			return;
		}
		IEnumerable<string> completions = value.GetCompletions(num, text);
		if (completions == null)
		{
			return;
		}
		foreach (string item3 in completions)
		{
			if (!string.IsNullOrEmpty(item3) && item3.StartsWith(text, StringComparison.OrdinalIgnoreCase))
			{
				string item2 = text2 + " " + item3;
				if (hashSet.Add(item2))
				{
					into.Add(item2);
				}
			}
		}
	}

	private static bool PathMatchesSoFar(List<string> path, string[] tokens, int depth)
	{
		for (int i = 0; i < depth; i++)
		{
			string text = path[i];
			if (text != null && !text.Equals(tokens[i + 1], StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		return true;
	}

	private static List<List<string>> GetCachedSpecPaths(string spec)
	{
		if (_specCache.TryGetValue(spec, out var value))
		{
			return value;
		}
		value = new List<List<string>>
		{
			new List<string>()
		};
		int i = 0;
		ParseSpecInto(spec, ref i, value);
		_specCache[spec] = value;
		return value;
	}

	private static void ParseSpecInto(string spec, ref int i, List<List<string>> paths)
	{
		while (i < spec.Length)
		{
			while (i < spec.Length && char.IsWhiteSpace(spec[i]))
			{
				i++;
			}
			if (i >= spec.Length)
			{
				break;
			}
			switch (spec[i])
			{
			case ']':
			case '|':
				return;
			case '[':
				i++;
				ParseAlternation(spec, ref i, paths);
				continue;
			case '(':
			{
				int num2 = 1;
				i++;
				while (i < spec.Length && num2 > 0)
				{
					if (spec[i] == '(')
					{
						num2++;
					}
					else if (spec[i] == ')')
					{
						num2--;
					}
					i++;
				}
				continue;
			}
			case '<':
			{
				int num = spec.IndexOf('>', i);
				i = ((num >= 0) ? (num + 1) : spec.Length);
				foreach (List<string> path in paths)
				{
					path.Add(null);
				}
				continue;
			}
			}
			int num3 = i;
			while (i < spec.Length && !char.IsWhiteSpace(spec[i]) && "[]<>|()".IndexOf(spec[i]) < 0)
			{
				i++;
			}
			if (i > num3)
			{
				string item = spec.Substring(num3, i - num3);
				foreach (List<string> path2 in paths)
				{
					path2.Add(item);
				}
			}
			else
			{
				i++;
			}
		}
	}

	private static void ParseAlternation(string spec, ref int i, List<List<string>> paths)
	{
		List<List<string>> list = new List<List<string>>(paths.Count);
		foreach (List<string> path in paths)
		{
			list.Add(new List<string>(path));
		}
		paths.Clear();
		bool flag = true;
		while (i < spec.Length && spec[i] != ']')
		{
			if (!flag && spec[i] == '|')
			{
				i++;
			}
			flag = false;
			List<List<string>> list2 = new List<List<string>>(list.Count);
			foreach (List<string> item in list)
			{
				list2.Add(new List<string>(item));
			}
			ParseSpecInto(spec, ref i, list2);
			paths.AddRange(list2);
		}
		if (i < spec.Length)
		{
			i++;
		}
	}

	private static void DrawTypeaheadHint()
	{
		if (string.IsNullOrEmpty(_consoleInput))
		{
			return;
		}
		_typeaheadScratch.Clear();
		CollectMatches(_consoleInput, _typeaheadScratch);
		if (_typeaheadScratch.Count != 0)
		{
			string text = _typeaheadScratch[0];
			if (!text.Equals(_consoleInput, StringComparison.OrdinalIgnoreCase))
			{
				Vector2 itemRectMin = ImGui.GetItemRectMin();
				Vector2 framePadding = ImGui.GetStyle().FramePadding;
				float x = ImGui.CalcTextSize(_consoleInput).x;
				Vector2 pos = new Vector2(itemRectMin.x + framePadding.x + x + 6f, itemRectMin.y + framePadding.y);
				ImGui.GetWindowDrawList().AddText(pos, ImGuiColor.Integer.Grey, "[Tab] " + text);
			}
		}
	}

	private static void LoadCommandHistory()
	{
		try
		{
			string commandHistoryPath = CommandHistoryPath;
			if (File.Exists(commandHistoryPath))
			{
				string[] array = File.ReadAllLines(commandHistoryPath);
				for (int i = 0; i < _commandBuffer.Length && i < array.Length; i++)
				{
					_commandBuffer[i] = array[i];
				}
			}
		}
		catch
		{
		}
	}

	private static void SaveCommandHistory()
	{
		try
		{
			List<string> list = new List<string>(_commandBuffer.Length);
			string[] commandBuffer = _commandBuffer;
			foreach (string text in commandBuffer)
			{
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(text);
				}
			}
			File.WriteAllLines(CommandHistoryPath, list);
		}
		catch
		{
		}
	}

	private static async UniTaskVoid WaitForGameToBeReadyThenOverrideConsoleInput()
	{
		await UniTask.WaitUntil(() => GameManager.IsInitialized);
		await UniTask.Delay(1000);
		if (!CustomLogFile && _systemConsoleInput != null)
		{
			_systemConsoleInput.OnInputReceived += CommandLine.Process;
			_systemConsoleInput.Ready("Stationeers - " + GameManager.GetGameVersion());
		}
	}

	private static void Shutdown()
	{
		Application.logMessageReceivedThreaded -= LogMessage;
	}

	private static void LogMessage(string logStr, string stacktrace, LogType type)
	{
		if (CustomLogFile)
		{
			return;
		}
		string text = EnumCollections.LogTypes.GetName(type).ToUpper();
		string text2 = logStr.ToLower();
		if (type == LogType.Error || type == LogType.Exception)
		{
			Print("[" + text + "] " + text2, ConsoleColor.Red, clearLine: false, aged: false);
			if (!string.IsNullOrEmpty(stacktrace))
			{
				Print(stacktrace);
			}
		}
	}

	public static void UseCustomWindowHeight(bool useCustom, int height = 0)
	{
		_useCustomWindowHeight = useCustom;
		_customWindowHeight = Mathf.Clamp(height, 2, 100) - 2;
	}

	public static void Show()
	{
		if (!_show)
		{
			GameManager.EventBus.Publish(new ShowHideConsoleWindowMessage(show: true));
			_show = true;
			_setSize = true;
			_inputLastActive = true;
			_commandBufferIndex = -1;
			ImGuiManager.SetBlockUguiClicks(block: true);
			MouseModeController.AddModal(_cursorModal);
			KeyManager.SetInputState("ConsoleWindow", KeyInputState.Typing);
			KeyMap._Cancel.KeyUp += Hide;
		}
	}

	public static void Hide()
	{
		if (_show)
		{
			GameManager.EventBus.Publish(new ShowHideConsoleWindowMessage(show: false));
			_show = false;
			_inputLastActive = false;
			_inputSize = Vector2.zero;
			_snapNextFrame = true;
			_lastTabResult = string.Empty;
			_tabMatches.Clear();
			ImGuiManager.SetBlockUguiClicks(block: false);
			MouseModeController.RemoveModal(_cursorModal);
			MouseModeController.Reset();
			KeyManager.RemoveInputState("ConsoleWindow");
			KeyMap._Cancel.KeyUp -= Hide;
		}
	}

	public static void Draw(bool noFade = false)
	{
		HandleInput();
		ImGui.PushFont(ImguiHelper.GetFont(1));
		ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);
		float min = (_show ? Mathf.Min((float)Screen.height * 0.25f, 320f) : 0f);
		float num = Mathf.Clamp(_inputSize.y, min, Screen.height);
		ImGui.SetNextWindowSize(new Vector2(Screen.width, num), ImGuiCond.Always);
		ImGui.SetNextWindowPos(new Vector2(0f, (float)Screen.height - num), ImGuiCond.Always);
		ImGuiWindowFlags imGuiWindowFlags = (ImGuiWindowFlags)303;
		if (!_show)
		{
			imGuiWindowFlags |= (ImGuiWindowFlags)640;
		}
		ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
		ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGuiColor.Integer.Black);
		ImGui.PushStyleColor(ImGuiCol.Border, ImGuiColor.Integer.DarkGrey);
		ImGui.Begin("ConsoleWindow", ref _show, imGuiWindowFlags);
		ImGui.PopStyleColor(2);
		ImGui.PopStyleVar();
		_inputSize = ImGui.GetStyle().WindowPadding * 2f;
		int num2 = _consoleBuffer.Length - 1;
		if (_show)
		{
			if (_copyToastTime > 0f)
			{
				float val = Mathf.Clamp01(_copyToastTime / 0.3f);
				ImGui.PushStyleVar(ImGuiStyleVar.Alpha, val);
				ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColor.Integer.Yellow);
				ImGui.Text("Copied to clipboard");
				ImGui.PopStyleColor();
				ImGui.PopStyleVar();
				_inputSize += new Vector2(0f, ImGui.GetItemRectSize().y);
				_copyToastTime -= Time.unscaledDeltaTime;
			}
			float frameHeightWithSpacing = ImGui.GetFrameHeightWithSpacing();
			ImGui.BeginChild("###ConsoleScroll", new Vector2(0f, 0f - frameHeightWithSpacing), border: false, ImGuiWindowFlags.None);
			if (num2 >= 0)
			{
				for (int num3 = num2; num3 >= 0; num3--)
				{
					_consoleBuffer[num3].Draw(ref _inputSize, _show, noFade);
				}
			}
			bool flag = false;
			if (Input.GetKeyDown(KeyCode.PageUp))
			{
				float windowHeight = ImGui.GetWindowHeight();
				ImGui.SetScrollY(Mathf.Max(0f, ImGui.GetScrollY() - windowHeight));
				flag = true;
			}
			else if (Input.GetKeyDown(KeyCode.PageDown))
			{
				float windowHeight2 = ImGui.GetWindowHeight();
				ImGui.SetScrollY(Mathf.Min(ImGui.GetScrollMaxY(), ImGui.GetScrollY() + windowHeight2));
				flag = true;
			}
			if (!flag && (_snapNextFrame || ImGui.GetScrollY() >= ImGui.GetScrollMaxY()))
			{
				ImGui.SetScrollHereY(1f);
				_snapNextFrame = false;
			}
			ImGui.EndChild();
			if (_setSize)
			{
				SetLength();
			}
			ImGui.Text(">");
			ImGui.SameLine();
			ImGui.PushStyleColor(ImGuiCol.Border, ImGuiColor.Integer.Transparent);
			ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiColor.Integer.Transparent);
			bool flag2 = false;
			if (_inputLastActive)
			{
				if (Input.GetKeyDown(KeyCode.UpArrow))
				{
					CommandBufferIndex++;
					flag2 = true;
				}
				if (Input.GetKeyDown(KeyCode.DownArrow))
				{
					CommandBufferIndex--;
					flag2 = true;
				}
			}
			if (flag2 && CommandBufferIndex >= 0)
			{
				_consoleInput = _commandBuffer[CommandBufferIndex];
				ImGui.InputText("###ConsoleInput", ref _consoleInput, 512u, (ImGuiInputTextFlags)16640, _inputHistoryCallback);
			}
			else
			{
				ImGui.InputText("###ConsoleInput", ref _consoleInput, 512u, ImGuiInputTextFlags.CallbackCompletion, _inputCallback);
				_inputLastActive = ImGui.IsItemActive();
			}
			_inputSize += ImGui.GetItemRectSize();
			ImGui.PopStyleColor(2);
			if (ImGui.IsItemVisible())
			{
				ImGui.SetWindowFocus();
				ImGui.SetKeyboardFocusHere(-1);
			}
			DrawTypeaheadHint();
		}
		else if (num2 >= 0)
		{
			for (int num4 = (_useCustomWindowHeight ? Mathf.Clamp(_customWindowHeight, 0, num2) : num2); num4 >= 0; num4--)
			{
				_consoleBuffer[num4].Draw(ref _inputSize, _show, noFade);
			}
		}
		if (_clearConsoleInput)
		{
			_clearConsoleInput = false;
			_consoleInput = string.Empty;
		}
		ImGui.End();
		ImGui.PopFont();
		ImGui.PopStyleVar();
		TerrainDebugHelper.DrawDebug();
	}

	private static void HandleInput()
	{
		bool keyDown = Input.GetKeyDown(KeyMap.ToggleConsole);
		bool keyDown2 = Input.GetKeyDown(KeyCode.Return);
		if (!_show && keyDown)
		{
			Show();
		}
		else
		{
			if (!_show)
			{
				return;
			}
			if (keyDown)
			{
				_consoleInput = string.Empty;
				Hide();
			}
			else
			{
				if (!keyDown2)
				{
					return;
				}
				if (!string.IsNullOrEmpty(_consoleInput))
				{
					for (int num = 6; num >= 0; num--)
					{
						_commandBuffer[num + 1] = _commandBuffer[num];
					}
					_commandBuffer[0] = _consoleInput;
					SaveCommandHistory();
				}
				_commandBufferIndex = -1;
				Submit();
			}
		}
	}

	public static void Submit(string input)
	{
		_consoleInput = input;
		Submit();
	}

	private static void Submit()
	{
		_snapNextFrame = true;
		_lastTabResult = string.Empty;
		_tabMatches.Clear();
		if (string.IsNullOrEmpty(_consoleInput))
		{
			Hide();
			return;
		}
		Print(_consoleInput, ConsoleColor.Cyan, clearLine: false, aged: true, unformatted: true);
		CommandLine.Process(_consoleInput);
		_clearConsoleInput = true;
	}

	private static void Threads(string[] lineSplit)
	{
		if (!IsInvalidSyntax(lineSplit, 1))
		{
			List<ThreadedManager> active = ThreadedManager.Active;
			PrintAction($"found '{active.Count}' threads ");
			for (int i = 0; i < active.Count; i++)
			{
				ThreadedManager threadedManager = active[i];
				Print($"{i}\t{threadedManager.PaddedName}\tTHREAD INFO NOT AVAILABLE");
			}
		}
	}

	private static void ShowHandler(string[] lineSplit, bool setting, Action<bool> updateSetting)
	{
		bool result = setting;
		if (lineSplit.Length == 1)
		{
			result = !result;
		}
		else if (IsInvalidSyntax(lineSplit, 2) || !Get(lineSplit, 1, "show", out result))
		{
			return;
		}
		setting = result;
		updateSetting(setting);
		PrintAction($"{lineSplit[0]} set to '{result}'");
	}

	private static void AddText(FileStream fs, string value)
	{
		byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(value);
		fs.Write(bytes, 0, bytes.Length);
	}

	public static bool IsInvalidSyntax(string[] lineSplit, int requiredSize, string[] uses = null)
	{
		if (lineSplit.Length < requiredSize)
		{
			if (lineSplit.Length == 0)
			{
				return true;
			}
			PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString("syntax", lineSplit[0]));
			PrintValidScope(lineSplit[0], uses);
			return true;
		}
		return false;
	}

	private static bool IsNotPositiveValue(int value)
	{
		if (value <= 0)
		{
			PrintError(ConsoleStrings.Error.OnlyPositiveAllowed.AsString(StringManager.Get(value)));
			return true;
		}
		return false;
	}

	private static bool Get(string[] lineSplit, int i, string variable, out bool result)
	{
		if (!bool.TryParse(lineSplit[i], out result))
		{
			PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, lineSplit[i]));
			return false;
		}
		return true;
	}

	private static bool Get(string[] lineSplit, int i, string variable, out byte result)
	{
		if (!byte.TryParse(lineSplit[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
		{
			PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, lineSplit[i]));
			return false;
		}
		return true;
	}

	private static bool Get(string[] lineSplit, int i, string variable, out string result)
	{
		result = lineSplit[i];
		if (string.IsNullOrEmpty(result))
		{
			PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, result));
			return false;
		}
		return true;
	}

	private static bool Get(string[] lineSplit, int i, string variable, out ushort result)
	{
		if (!ushort.TryParse(lineSplit[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
		{
			PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, lineSplit[i]));
			return false;
		}
		return true;
	}

	private static bool Get(string[] lineSplit, int i, string variable, out int result)
	{
		if (!int.TryParse(lineSplit[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
		{
			PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, lineSplit[i]));
			return false;
		}
		return true;
	}

	private static bool Get(string[] lineSplit, int i, string variable, out uint result)
	{
		if (!uint.TryParse(lineSplit[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
		{
			PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString(variable, lineSplit[i]));
			return false;
		}
		return true;
	}

	private static bool Get<TEnum>(string[] lineSplit, int i, out TEnum result) where TEnum : struct
	{
		if (!Enum.TryParse<TEnum>(lineSplit[i], ignoreCase: true, out result))
		{
			PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString("sortby", lineSplit[i]));
			return false;
		}
		return true;
	}

	private static void SetLength()
	{
		_setSize = false;
		MaxLength = Math.DivRem((int)ImGui.GetContentRegionAvail().x - (int)ImGui.CalcTextSize(DateString).x, (int)ImGui.CalcTextSize("X").x, out var _);
	}

	private static void PrintValidScope(string command, string[] uses)
	{
		if (uses == null)
		{
			return;
		}
		string text = "allowed scope: ";
		for (int i = 0; i < uses.Length; i++)
		{
			if (i > 0)
			{
				text += " ,";
			}
			text = text + uses[i].ToLower() + "'";
		}
		PrintError(text + " true");
	}

	private static bool Get(string[] lineSplit, int i, out ConsoleCommandScope result, string[] uses = null)
	{
		if (!Enum.TryParse<ConsoleCommandScope>(lineSplit[i], ignoreCase: true, out result))
		{
			PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString("scope", lineSplit[i]));
			PrintValidScope(lineSplit[i], uses);
			return false;
		}
		return true;
	}

	private static bool Get(string[] lineSplit, int i, out float value)
	{
		if (!float.TryParse(lineSplit[i], NumberStyles.Float, CultureInfo.InvariantCulture, out value))
		{
			PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString("value", lineSplit[i]));
			return false;
		}
		return true;
	}

	private static bool Get(string[] lineSplit, int i, out int value)
	{
		if (!int.TryParse(lineSplit[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
		{
			PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString("value", lineSplit[i]));
			return false;
		}
		return true;
	}

	private static bool Get(string[] lineSplit, int i, out DateTime value)
	{
		if (!DateTime.TryParse(lineSplit[i], out value))
		{
			PrintError(ConsoleStrings.Error.CommandArgumentInvalid.AsString("date", lineSplit[i]));
			return false;
		}
		return true;
	}

	private static bool AllowDebugMode(ConsoleCommandScope scope)
	{
		if ((uint)(scope - 2) <= 2u)
		{
			return true;
		}
		return false;
	}

	private static string GetSaveName(string saveName, string[] lineSplit)
	{
		for (int i = 2; i < lineSplit.Length; i++)
		{
			string text = lineSplit[i];
			if (!string.IsNullOrEmpty(text))
			{
				saveName = saveName + " " + text;
			}
		}
		saveName = saveName.Replace("'", "");
		saveName = saveName.Replace("\"", "");
		return saveName;
	}

	private static void StopHost()
	{
		if (!NetworkManager.IsServer)
		{
			PrintError(ConsoleStrings.Error.NotHostingGame.AsString());
			return;
		}
		NetworkServer.StopServer();
		PrintAction($"ending hosting on '{NetworkServer.HostPort}'");
	}

	private static void Disconnect()
	{
		if (!NetworkManager.IsActiveAsClient)
		{
			PrintError(ConsoleStrings.Error.NotConnectedAsClient.AsString());
			return;
		}
		PrintAction("disconnecting from " + NetworkClient.Address + ":" + NetworkClient.Port);
		NetworkClient.Disconnect().Forget();
	}

	public static void ConnectTo(string[] lineSplit)
	{
		if (IsInvalidSyntax(lineSplit, 2) || !Get(lineSplit, 1, "address", out string result))
		{
			return;
		}
		string text = NetworkManager.ResolveIpAddress(result);
		if (string.IsNullOrEmpty(text))
		{
			PrintError(ConsoleStrings.Error.CannotResolveAddressToIp.AsString(result));
			return;
		}
		ushort result2 = ushort.Parse(Settings.CurrentData.GamePort);
		if (lineSplit.Length >= 3 && !Get(lineSplit, 2, "port", out result2))
		{
			return;
		}
		if (lineSplit.Length >= 4)
		{
			if (Get(lineSplit, 3, "localport", out ushort result3))
			{
				PrintAction($"connect to '{text}':'{result2}' using '{result3}'");
				NetworkManager.StartClient(text, result2, result3);
			}
		}
		else
		{
			PrintAction($"connect to '{text}':'{result2}'");
			NetworkManager.StartClient(text, result2, (ushort)(result2 + 1));
		}
	}

	private static string ShowEach(string[] strings, string desc)
	{
		for (int i = 0; i < strings.Length; i++)
		{
			if (i > 0)
			{
				desc += ", ";
			}
			string text = strings[i];
			desc = desc + text.ToLower() + "'";
		}
		return desc;
	}

	public static void ApplySettings()
	{
		ConsoleLine[] array = new ConsoleLine[1024];
		for (int i = 0; i < array.Length; i++)
		{
			if (i >= _consoleBuffer.Length)
			{
				array[i] = new ConsoleLine();
			}
			else
			{
				array[i] = _consoleBuffer[i];
			}
		}
		_consoleBuffer = array;
		GC.Collect();
	}

	public static void ClearConsole()
	{
		if (GameManager.IsBatchMode)
		{
			_systemConsoleInput?.Clear();
		}
		else if (_consoleBuffer != null)
		{
			for (int i = 0; i < _consoleBuffer.Length; i++)
			{
				_consoleBuffer[i]?.Clear();
			}
		}
	}

	private static ConsoleColor GetConsoleColor(uint color)
	{
		if (color == ImGuiColor.Integer.White)
		{
			return ConsoleColor.White;
		}
		if (color == ImGuiColor.Integer.Red)
		{
			return ConsoleColor.Red;
		}
		if (color == ImGuiColor.Integer.Green)
		{
			return ConsoleColor.Green;
		}
		if (color == ImGuiColor.Integer.Yellow)
		{
			return ConsoleColor.Yellow;
		}
		if (color == ImGuiColor.Integer.Grey)
		{
			return ConsoleColor.Gray;
		}
		if (color == ImGuiColor.Integer.DarkGrey)
		{
			return ConsoleColor.DarkGray;
		}
		if (color == ImGuiColor.Integer.SoftMustard)
		{
			return ConsoleColor.DarkYellow;
		}
		if (color == ImGuiColor.Integer.SoftCopper)
		{
			return ConsoleColor.DarkRed;
		}
		if (color == ImGuiColor.Integer.SoftSky)
		{
			return ConsoleColor.Cyan;
		}
		if (color == ImGuiColor.Integer.SoftRose)
		{
			return ConsoleColor.Magenta;
		}
		if (color == ImGuiColor.Integer.SoftSage)
		{
			return ConsoleColor.Green;
		}
		if (color == ImGuiColor.Integer.SoftLilac)
		{
			return ConsoleColor.DarkMagenta;
		}
		if (color == ImGuiColor.Integer.SoftPeach)
		{
			return ConsoleColor.Yellow;
		}
		if (color == ImGuiColor.Integer.SoftMint)
		{
			return ConsoleColor.DarkCyan;
		}
		return ConsoleColor.Gray;
	}

	private static uint GetConsoleColor(ConsoleColor color)
	{
		switch (color)
		{
		case ConsoleColor.White:
			return ImGuiColor.Integer.White;
		case ConsoleColor.DarkRed:
		case ConsoleColor.DarkMagenta:
		case ConsoleColor.Red:
		case ConsoleColor.Magenta:
			return ImGuiColor.Integer.Red;
		case ConsoleColor.Gray:
			return ImGuiColor.Integer.Grey;
		case ConsoleColor.DarkGray:
			return ImGuiColor.Integer.DarkGrey;
		case ConsoleColor.Green:
			return ImGuiColor.Integer.Green;
		case ConsoleColor.DarkGreen:
			return ImGuiColor.Integer.Green;
		case ConsoleColor.DarkYellow:
		case ConsoleColor.Yellow:
			return ImGuiColor.Integer.Yellow;
		case ConsoleColor.DarkBlue:
		case ConsoleColor.DarkCyan:
			return ImGuiColor.Integer.Blue;
		case ConsoleColor.Blue:
		case ConsoleColor.Cyan:
			return ImGuiColor.Integer.LightBlue;
		case ConsoleColor.Black:
			return ImGuiColor.Integer.Black;
		default:
			return ImGuiColor.Integer.Grey;
		}
	}

	public static async UniTaskVoid PrintError(Exception exception)
	{
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		PrintError("Exception: " + exception.Message);
		Print(exception.StackTrace);
	}

	public static void PrintError(string output, bool suppressStacktrace = false)
	{
		Print(output, ConsoleColor.Red, clearLine: false, aged: false);
		if (!suppressStacktrace)
		{
			Print(Environment.StackTrace, ConsoleColor.Gray);
		}
	}

	public static async UniTaskVoid AsyncPrintError(string output, bool suppressStacktrace = false)
	{
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		Print(output, ConsoleColor.Red, clearLine: false, aged: false);
		if (!suppressStacktrace)
		{
			Print(Environment.StackTrace);
		}
	}

	public static void PrintAction(string output, bool aged = false)
	{
		Print(output, ConsoleColor.Yellow, clearLine: false, aged);
	}

	public static void Print(string output, ConsoleColor color = ConsoleColor.White, bool clearLine = false, bool aged = true, bool unformatted = false)
	{
		if (GameManager.IsBatchMode)
		{
			output = $"{DateTime.Now:HH:mm:ss}: {output}";
			if (CustomLogFile)
			{
				switch (color)
				{
				case ConsoleColor.DarkRed:
				case ConsoleColor.DarkMagenta:
				case ConsoleColor.Red:
				case ConsoleColor.Magenta:
					Debug.LogError(output);
					break;
				case ConsoleColor.DarkYellow:
				case ConsoleColor.Yellow:
					Debug.LogWarning(output);
					break;
				default:
					Debug.Log(output);
					break;
				}
			}
			else if (!output.Contains("<color="))
			{
				_systemConsoleInput?.PrintToConsole(output, color);
			}
			return;
		}
		uint consoleColor = GetConsoleColor(color);
		ConsoleLine[] consoleBuffer = _consoleBuffer;
		if (consoleBuffer == null || consoleBuffer.Length <= 0)
		{
			_prematureLogQueue.Enqueue(new PrematureLog
			{
				output = output,
				color = color,
				clearLine = clearLine,
				aged = aged,
				unformatted = unformatted
			});
			return;
		}
		for (int num = _consoleBuffer.Length - 1 - 1; num >= 0; num--)
		{
			_consoleBuffer[num + 1].Apply(_consoleBuffer[num]);
		}
		if (aged)
		{
			_consoleBuffer[0].Set(output, consoleColor, 0f);
		}
		else
		{
			_consoleBuffer[0].Set(output, consoleColor);
		}
	}

	private static void DrainPrematureLogQueue()
	{
		while (_prematureLogQueue.Count > 0)
		{
			PrematureLog prematureLog = _prematureLogQueue.Dequeue();
			Print(prematureLog.output, prematureLog.color, prematureLog.clearLine, prematureLog.aged, prematureLog.unformatted);
		}
	}

	public static void PrintBlock(params (string text, ConsoleColor color)[] lines)
	{
		if (lines == null || lines.Length == 0)
		{
			return;
		}
		if (!GameManager.IsBatchMode)
		{
			ConsoleLine[] consoleBuffer = _consoleBuffer;
			if (consoleBuffer != null && consoleBuffer.Length > 0)
			{
				for (int num = _consoleBuffer.Length - 1 - 1; num >= 0; num--)
				{
					_consoleBuffer[num + 1].Apply(_consoleBuffer[num]);
				}
				StringBuilder stringBuilder = new StringBuilder(lines[0].text ?? string.Empty);
				uint[] array = null;
				if (lines.Length > 1)
				{
					array = new uint[lines.Length - 1];
					for (int i = 1; i < lines.Length; i++)
					{
						stringBuilder.Append('\n').Append(lines[i].text ?? string.Empty);
						array[i - 1] = GetConsoleColor(lines[i].color);
					}
				}
				_consoleBuffer[0].Set(stringBuilder.ToString(), GetConsoleColor(lines[0].color), 0f, array);
				return;
			}
		}
		StringBuilder stringBuilder2 = new StringBuilder();
		for (int j = 0; j < lines.Length; j++)
		{
			if (j > 0)
			{
				stringBuilder2.AppendLine();
			}
			stringBuilder2.Append(lines[j].text);
		}
		Print(stringBuilder2.ToString(), lines[0].color);
	}

	public static void PrintSegmentedBlock(params (string text, ConsoleColor color)[][] lines)
	{
		if (lines == null || lines.Length == 0)
		{
			return;
		}
		if (!GameManager.IsBatchMode)
		{
			ConsoleLine[] consoleBuffer = _consoleBuffer;
			if (consoleBuffer != null && consoleBuffer.Length > 0)
			{
				for (int num = _consoleBuffer.Length - 1 - 1; num >= 0; num--)
				{
					_consoleBuffer[num + 1].Apply(_consoleBuffer[num]);
				}
				ConsoleSegment[][] array = new ConsoleSegment[lines.Length][];
				for (int i = 0; i < lines.Length; i++)
				{
					(string, ConsoleColor)[] array2 = lines[i];
					if (array2 == null)
					{
						array[i] = Array.Empty<ConsoleSegment>();
						continue;
					}
					array[i] = new ConsoleSegment[array2.Length];
					for (int j = 0; j < array2.Length; j++)
					{
						array[i][j] = new ConsoleSegment(array2[j].Item1, GetConsoleColor(array2[j].Item2));
					}
				}
				_consoleBuffer[0].SetSegments(array, 0f);
				return;
			}
		}
		foreach ((string, ConsoleColor)[] array3 in lines)
		{
			if (array3 == null || array3.Length == 0)
			{
				continue;
			}
			StringBuilder stringBuilder = new StringBuilder();
			(string, ConsoleColor)[] array4 = array3;
			for (int l = 0; l < array4.Length; l++)
			{
				(string, ConsoleColor) tuple = array4[l];
				stringBuilder.Append(tuple.Item1);
			}
			ConsoleColor color = ConsoleColor.White;
			array4 = array3;
			for (int l = 0; l < array4.Length; l++)
			{
				(string, ConsoleColor) tuple2 = array4[l];
				if (!string.IsNullOrWhiteSpace(tuple2.Item1))
				{
					color = tuple2.Item2;
					break;
				}
			}
			Print(stringBuilder.ToString(), color);
		}
	}

	public static void PrintTable(string[] headers, IReadOnlyList<(string text, uint color)[]> rows, int columnGap = 2)
	{
		if (rows == null || rows.Count == 0)
		{
			return;
		}
		int num = ((headers != null) ? headers.Length : 0);
		foreach ((string, uint)[] row in rows)
		{
			num = Math.Max(num, row.Length);
		}
		if (num == 0)
		{
			return;
		}
		int[] array = new int[num];
		if (headers != null)
		{
			for (int i = 0; i < headers.Length; i++)
			{
				array[i] = headers[i].Length;
			}
		}
		foreach ((string, uint)[] row2 in rows)
		{
			for (int j = 0; j < row2.Length; j++)
			{
				if (row2[j].Item1.Length > array[j])
				{
					array[j] = row2[j].Item1.Length;
				}
			}
		}
		List<(string, uint)[]> list = new List<(string, uint)[]>(rows.Count + 1);
		if (headers != null)
		{
			(string, uint)[] array2 = new(string, uint)[headers.Length];
			for (int k = 0; k < headers.Length; k++)
			{
				array2[k] = (headers[k], ImGuiColor.Integer.Grey);
			}
			list.Add(PadTableRow(array2, array, columnGap));
		}
		foreach ((string, uint)[] row3 in rows)
		{
			list.Add(PadTableRow(row3, array, columnGap));
		}
		PrintSegmentedBlockRaw(list.ToArray());
	}

	private static (string text, uint color)[] PadTableRow((string text, uint color)[] cells, int[] widths, int columnGap)
	{
		List<(string, uint)> list = new List<(string, uint)>(cells.Length * 2);
		for (int i = 0; i < cells.Length; i++)
		{
			list.Add(cells[i]);
			if (i == cells.Length - 1)
			{
				break;
			}
			int num = widths[i] - cells[i].text.Length + columnGap;
			if (num > 0)
			{
				list.Add((new string(' ', num), ImGuiColor.Integer.Grey));
			}
		}
		return list.ToArray();
	}

	public static void PrintSegmentedBlockRaw(params (string text, uint color)[][] lines)
	{
		if (lines == null || lines.Length == 0)
		{
			return;
		}
		if (!GameManager.IsBatchMode)
		{
			ConsoleLine[] consoleBuffer = _consoleBuffer;
			if (consoleBuffer != null && consoleBuffer.Length > 0)
			{
				for (int num = _consoleBuffer.Length - 1 - 1; num >= 0; num--)
				{
					_consoleBuffer[num + 1].Apply(_consoleBuffer[num]);
				}
				ConsoleSegment[][] array = new ConsoleSegment[lines.Length][];
				for (int i = 0; i < lines.Length; i++)
				{
					(string, uint)[] array2 = lines[i];
					if (array2 == null)
					{
						array[i] = Array.Empty<ConsoleSegment>();
						continue;
					}
					array[i] = new ConsoleSegment[array2.Length];
					for (int j = 0; j < array2.Length; j++)
					{
						array[i][j] = new ConsoleSegment(array2[j].Item1, array2[j].Item2);
					}
				}
				_consoleBuffer[0].SetSegments(array, 0f);
				return;
			}
		}
		foreach ((string, uint)[] array3 in lines)
		{
			if (array3 == null || array3.Length == 0)
			{
				continue;
			}
			StringBuilder stringBuilder = new StringBuilder();
			(string, uint)[] array4 = array3;
			for (int l = 0; l < array4.Length; l++)
			{
				(string, uint) tuple = array4[l];
				stringBuilder.Append(tuple.Item1);
			}
			uint color = ImGuiColor.Integer.White;
			array4 = array3;
			for (int l = 0; l < array4.Length; l++)
			{
				(string, uint) tuple2 = array4[l];
				if (!string.IsNullOrWhiteSpace(tuple2.Item1))
				{
					color = tuple2.Item2;
					break;
				}
			}
			Print(stringBuilder.ToString(), GetConsoleColor(color));
		}
	}

	public static void Print(GameString output)
	{
		Print(output.DisplayString);
	}

	public static void Print(GameString output, uint color)
	{
		Print(output.DisplayString, GetConsoleColor(color));
	}

	public static void Print(GameString output, string value)
	{
		Print(string.Format(output.DisplayString, value));
	}

	public static void Print(GameString output, string value, uint color)
	{
		Print(string.Format(output.DisplayString, value), GetConsoleColor(color));
	}

	public static void Print(GameString output, string value1, string value2)
	{
		Print(string.Format(output.DisplayString, value1, value2));
	}

	public static void Print(GameString output, string value1, string value2, uint color)
	{
		Print(string.Format(output.DisplayString, value1, value2), GetConsoleColor(color));
	}

	public static void Print(GameString output, string value1, string value2, string value3, uint color)
	{
		Print(string.Format(output.DisplayString, value1, value2, value3), GetConsoleColor(color));
	}

	public static void DrawListables(IEnumerable<IListable> enumerable1, IEnumerable<IListable> enumerable2 = null)
	{
		DrawListables(enumerable1?.ToList(), enumerable2?.ToList());
	}

	private static void DrawListables(List<IListable> list1, List<IListable> list2)
	{
		int num = 0;
		int num2 = int.MaxValue;
		int num3 = 0;
		if (list1 != null)
		{
			num3 += list1.Count;
		}
		if (list2 != null)
		{
			num3 += list2.Count;
		}
		int index = 0;
		if (num < 0)
		{
			num = 0;
		}
		if (num >= num3)
		{
			num = num3 - 1;
		}
		num2 += num;
		if (list1 != null)
		{
			index += num;
			if (num >= list1.Count)
			{
				num = list1.Count - 1;
			}
			if (num < 0)
			{
				num = 0;
			}
			for (int i = num; i < list1.Count && i < num2; i++)
			{
				list1[i].DrawInList(ref index);
				index++;
			}
			num2 -= list1.Count;
		}
		if (list2 != null)
		{
			for (int j = 0; j < list2.Count && j < num2; j++)
			{
				list2[j].DrawInList(ref index);
				index++;
			}
		}
	}

	[MonoPInvokeCallback(typeof(ImGuiInputTextCallback))]
	private unsafe static int InputCallback(ImGuiInputTextCallbackData* dataPtr)
	{
		ImGuiInputTextCallbackDataPtr imGuiInputTextCallbackDataPtr = new ImGuiInputTextCallbackDataPtr(dataPtr);
		if (imGuiInputTextCallbackDataPtr.EventFlag != ImGuiInputTextFlags.CallbackCompletion)
		{
			return 0;
		}
		string text = ComputeNextTabMatch(Encoding.UTF8.GetString((byte*)(void*)imGuiInputTextCallbackDataPtr.Buf, imGuiInputTextCallbackDataPtr.BufTextLen));
		if (text == null)
		{
			return 0;
		}
		imGuiInputTextCallbackDataPtr.DeleteChars(0, imGuiInputTextCallbackDataPtr.BufTextLen);
		imGuiInputTextCallbackDataPtr.InsertChars(0, text);
		return 0;
	}

	[MonoPInvokeCallback(typeof(ImGuiInputTextCallback))]
	private unsafe static int InputHistoryCallback(ImGuiInputTextCallbackData* dataPtr)
	{
		ImGuiInputTextCallbackDataPtr imGuiInputTextCallbackDataPtr = new ImGuiInputTextCallbackDataPtr(dataPtr);
		imGuiInputTextCallbackDataPtr.CursorPos = imGuiInputTextCallbackDataPtr.BufTextLen;
		imGuiInputTextCallbackDataPtr.SelectionStart = imGuiInputTextCallbackDataPtr.BufTextLen;
		imGuiInputTextCallbackDataPtr.SelectionEnd = imGuiInputTextCallbackDataPtr.BufTextLen;
		return 0;
	}
}
