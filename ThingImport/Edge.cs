using System;
using UnityEngine;

namespace ThingImport;

[Serializable]
public struct Edge(Vector3 a, Vector3 b)
{
	public Vector3 A = a;

	public Vector3 B = b;
}
