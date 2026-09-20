using System.Collections.Generic;
using Assets.Scripts.UI;
using UnityEngine;

namespace ThingImport;

public class BlueprintTemplate : MonoBehaviour
{
	[SerializeField]
	private MeshFilter _meshFilter;

	[SerializeField]
	private Wireframe _wireframe;

	public void Initialize(BlueprintData data)
	{
		if (data == null)
		{
			return;
		}
		_meshFilter.sharedMesh = data.MeshRef.Mesh;
		_wireframe.WireframeEdges = new List<Assets.Scripts.UI.Edge>();
		foreach (EdgeData edge in data.Edges)
		{
			_wireframe.WireframeEdges.Add(new Assets.Scripts.UI.Edge
			{
				Point1 = edge.Point1,
				Point2 = edge.Point2
			});
		}
	}
}
