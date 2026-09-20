using System.Collections.Generic;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public struct ScannerMeshBatch
{
	public Mesh Mesh;

	public List<Matrix4x4> Matrices;

	public List<Vector4> Colors;

	public bool IsValid
	{
		get
		{
			if (Mesh != null && Matrices != null)
			{
				return Colors != null;
			}
			return false;
		}
	}

	public static ScannerMeshBatch Create(Structure structure)
	{
		Mesh mesh = structure.Renderers[0].MeshFilter.mesh;
		if (structure is Pipe { IsBurst: not PipeBurst.None, HasBrokenMesh: not false } pipe)
		{
			mesh = pipe.BurstMesh;
		}
		return new ScannerMeshBatch
		{
			Mesh = mesh,
			Matrices = new List<Matrix4x4> { structure.GetBatchMatrix() },
			Colors = new List<Vector4> { structure.CustomColor.Color }
		};
	}
}
