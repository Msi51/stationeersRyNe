using System.Collections.Generic;
using Trading;
using UnityEngine;

namespace TerrainSystem.Lods;

public interface ILodRequester : IReferencable, IEvaluable, IThreadable
{
	bool ShouldRender { get; }

	HashSet<Vector3Int>[] RequestedLods { get; set; }

	LodInfo LodInfo { get; }

	Vector3 CenterPosition { get; }
}
