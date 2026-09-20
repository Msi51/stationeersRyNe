using System;

[Flags]
public enum KeyInputState : byte
{
	All = 0,
	Game = 2,
	Paused = 4,
	Typing = 8,
	Cinematic = 0x10
}
