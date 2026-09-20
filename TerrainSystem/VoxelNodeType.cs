using System;

namespace TerrainSystem;

[Flags]
public enum VoxelNodeType : byte
{
	None = 0,
	Crust = 1,
	Dirt = 2,
	Macro = 4,
	Bedrock = 8
}
