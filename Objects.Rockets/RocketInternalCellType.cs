using System;

namespace Objects.Rockets;

[Flags]
public enum RocketInternalCellType
{
	None = 0,
	Pipes = 1,
	Cables = 2,
	Devices = 4,
	Chutes = 8,
	Umbilical = 0x10,
	Engine = 0x20,
	CargoBay = 0x40,
	CableConnector = 0x80,
	Bulkhead = 0x100
}
