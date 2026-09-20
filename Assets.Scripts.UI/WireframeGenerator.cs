using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.UI;

public class WireframeGenerator
{
	public List<Edge> Edges = new List<Edge>();

	private List<Edge> _checkedEdges = new List<Edge>();

	public Mesh CombinedMesh;

	private Transform _transform;

	public WireframeGenerator(Transform transform)
	{
		_transform = transform;
		GenerateEdges();
	}

	public bool IsBadEdge(Edge edge)
	{
		foreach (Edge checkedEdge in _checkedEdges)
		{
			if (edge != checkedEdge && edge.Triangle != checkedEdge.Triangle && edge.Center() == checkedEdge.Center() && Vector3.Distance(checkedEdge.Triangle.Normal, edge.Triangle.Normal) < Edge.MinDistance)
			{
				return true;
			}
			if (!edge.IsValid())
			{
				return true;
			}
			if (!edge.Triangle.IsValid())
			{
				return true;
			}
		}
		return false;
	}

	public Vector3 GetOffset(Transform transform)
	{
		Vector3 result = -transform.position;
		while ((bool)transform.parent)
		{
			result -= transform.position;
			transform = transform.parent;
		}
		return result;
	}

	public void GenerateEdges()
	{
		Edges = new List<Edge>();
		_checkedEdges = new List<Edge>();
		CombinedMesh = new Mesh();
		MeshFilter[] componentsInChildren = _transform.GetComponentsInChildren<MeshFilter>();
		List<CombineInstance> list = new List<CombineInstance>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Mesh sharedMesh = componentsInChildren[i].sharedMesh;
			if (!componentsInChildren[i].GetComponent<Renderer>() || sharedMesh == null)
			{
				continue;
			}
			_ = componentsInChildren[i].transform.position;
			for (int j = 0; j < sharedMesh.subMeshCount; j++)
			{
				List<Vector3> list2 = new List<Vector3>();
				List<int> list3 = new List<int>();
				for (int k = 0; k < sharedMesh.triangles.Length; k++)
				{
					Vector3 item = sharedMesh.vertices[sharedMesh.triangles[k]];
					list2.Add(item);
					list3.Add(k);
				}
				Mesh mesh = new Mesh
				{
					vertices = list2.ToArray(),
					triangles = list3.ToArray()
				};
				mesh.RecalculateNormals();
				mesh.RecalculateBounds();
				CombineInstance item2 = new CombineInstance
				{
					mesh = mesh,
					subMeshIndex = j,
					transform = componentsInChildren[i].transform.localToWorldMatrix
				};
				list.Add(item2);
			}
		}
		CombinedMesh.CombineMeshes(list.ToArray(), mergeSubMeshes: true, useMatrices: true);
		Mathf.RoundToInt((float)CombinedMesh.vertexCount / 3f);
		for (int l = 0; l < CombinedMesh.triangles.Length - 2; l += 3)
		{
			Triangle triangle = new Triangle();
			triangle.SetPoints(CombinedMesh, l);
			_checkedEdges.Add(triangle.Edge1);
			_checkedEdges.Add(triangle.Edge2);
			_checkedEdges.Add(triangle.Edge3);
		}
		int num = 0;
		foreach (Edge checkedEdge in _checkedEdges)
		{
			if (!IsBadEdge(checkedEdge))
			{
				Edges.Add(checkedEdge);
			}
			num++;
		}
	}
}
