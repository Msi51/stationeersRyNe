using UnityEngine;

namespace TerrainSystem;

public interface INode
{
	byte Density { get; }

	bool IsValid { get; }

	VoxelNodeType NodeType { get; }

	int GetSize();

	void DrawDebug();

	void DrawChild(Vector3Int parentNodePosition, int childIndex);
}
