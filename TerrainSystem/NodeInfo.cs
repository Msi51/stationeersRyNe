using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TerrainSystem;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct NodeInfo(byte density, VoxelNodeType nodeType, sbyte depth)
{
	public readonly byte Density = density;

	public readonly sbyte Depth = depth;

	public readonly VoxelNodeType NodeType = nodeType;

	public static readonly NodeInfo Invalid = new NodeInfo(0, VoxelNodeType.None, -1);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float DensityAsFloat()
	{
		return VoxelTerrain.DensityToFloat(Density);
	}
}
