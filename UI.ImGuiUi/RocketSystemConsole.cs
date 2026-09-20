using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UI.ImGuiUi;

public class RocketSystemConsole : IDisposable
{
	private readonly string _title;

	private readonly string _inputPrefix;

	private bool _keepAlive;

	private bool _isReady;

	private readonly Thread _inputThread;

	private readonly Thread _unityThread;

	private readonly StringBuilder _inputString = new StringBuilder();

	private readonly Queue<string> _inputQueue = new Queue<string>();

	private readonly Queue<(string, ConsoleColor)> _preReadyOutputQueue = new Queue<(string, ConsoleColor)>();

	public event Action<string> OnInputReceived;

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetConsoleOutputCP(uint wCodePageID);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetConsoleCP(uint wCodePageID);

	public RocketSystemConsole(string title)
	{
		if (Application.platform != RuntimePlatform.LinuxServer && Application.platform != RuntimePlatform.WindowsServer)
		{
			throw new Exception("Don't use this outside of dedicated server builds");
		}
		Application.targetFrameRate = 25;
		_inputPrefix = "> ";
		_keepAlive = true;
		_title = title;
		Console.Title = title;
		Console.OutputEncoding = Encoding.UTF8;
		SetConsoleOutputCP(65001u);
		SetConsoleCP(65001u);
		_inputThread = new Thread(ConsoleInputThread);
		_unityThread = Thread.CurrentThread;
		Tick();
	}

	public void Ready(string readyMessage)
	{
		_isReady = true;
		if (!Environment.GetCommandLineArgs().Contains("-noclear"))
		{
			Clear();
		}
		PrintToConsole("***" + readyMessage + "***", ConsoleColor.Green);
		while (_preReadyOutputQueue.Count > 0)
		{
			var (output, colour) = _preReadyOutputQueue.Dequeue();
			PrintToConsole(output, colour);
		}
		PrintToConsole("Ready", ConsoleColor.Green);
		_inputThread.Start();
		Console.Title = _title;
		Console.OutputEncoding = Encoding.UTF8;
	}

	private async Task Tick()
	{
		while (_keepAlive)
		{
			if (Thread.CurrentThread == _unityThread)
			{
				lock (_inputQueue)
				{
					while (_inputQueue.Count > 0)
					{
						this.OnInputReceived?.Invoke(_inputQueue.Dequeue());
					}
				}
			}
			await Task.Delay(100);
		}
	}

	private void ConsoleInputThread()
	{
		_inputString.Append(_inputPrefix);
		while (_keepAlive)
		{
			if (!Console.CursorVisible || !Console.KeyAvailable)
			{
				continue;
			}
			ConsoleKeyInfo consoleKeyInfo = Console.ReadKey();
			switch (consoleKeyInfo.Key)
			{
			case ConsoleKey.Enter:
				OnEnter();
				continue;
			case ConsoleKey.Backspace:
				OnBackspace();
				continue;
			case ConsoleKey.Escape:
				OnEscape();
				continue;
			}
			if (consoleKeyInfo.KeyChar != 0)
			{
				_inputString.Append(consoleKeyInfo.KeyChar);
			}
			RedrawInputLine();
		}
	}

	private void OnBackspace()
	{
		if (_inputString.Length <= 1)
		{
			ClearInputStringBuilder();
			RedrawInputLine();
			return;
		}
		int num = _inputString.ToString().IndexOf(_inputPrefix, StringComparison.Ordinal);
		string text;
		if (num < 0)
		{
			text = _inputPrefix;
		}
		else
		{
			string text2 = _inputString.ToString();
			int num2 = num;
			text = text2.Substring(num2, text2.Length - 1 - num2);
		}
		string value = text;
		_inputString.Clear();
		_inputString.Append(value);
		RedrawInputLine();
	}

	private void OnEscape()
	{
		ClearLine();
		ClearInputStringBuilder();
	}

	private void OnEnter()
	{
		if (_inputString.Length != 0)
		{
			Console.SetCursorPosition(0, Console.CursorTop);
			Console.ForegroundColor = ConsoleColor.White;
			string item = _inputString.ToString().Replace(_inputPrefix, string.Empty).Trim();
			lock (_inputQueue)
			{
				_inputQueue.Enqueue(item);
			}
			PrintToConsole(_inputString.ToString());
			ClearInputStringBuilder();
			RedrawInputLine();
		}
	}

	private static void ClearLine()
	{
		Console.SetCursorPosition(0, Console.CursorTop);
		int count = Math.Clamp(Console.BufferWidth - 1, 0, int.MaxValue);
		Console.Write(new string(' ', count));
		Console.SetCursorPosition(0, Console.CursorTop);
	}

	private void RedrawInputLine()
	{
		Console.ForegroundColor = ConsoleColor.White;
		ClearLine();
		Console.Write(_inputString.ToString());
	}

	private void ClearInputStringBuilder()
	{
		_inputString.Clear();
		_inputString.Append(_inputPrefix);
	}

	public void PrintToConsole(string output, ConsoleColor colour = ConsoleColor.White)
	{
		if (!_isReady)
		{
			_preReadyOutputQueue?.Enqueue((output, colour));
			return;
		}
		Console.ForegroundColor = colour;
		string[] array = (output ?? string.Empty).Split('\n');
		foreach (string value in array)
		{
			ClearLine();
			Console.WriteLine(value);
		}
		Console.ResetColor();
		RedrawInputLine();
	}

	public void Clear()
	{
		if (Application.platform != RuntimePlatform.LinuxPlayer)
		{
			Console.Clear();
			ClearInputStringBuilder();
			RedrawInputLine();
		}
	}

	private void ReleaseUnmanagedResources()
	{
		_keepAlive = false;
	}

	public void Dispose()
	{
		ReleaseUnmanagedResources();
		GC.SuppressFinalize(this);
	}
}
