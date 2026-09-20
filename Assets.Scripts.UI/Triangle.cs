using UnityEngine;

namespace Assets.Scripts.UI;

public class Triangle
{
	public Vector3 Point1;

	public Vector3 Point2;

	public Vector3 Point3;

	public Edge Edge1;

	public Edge Edge2;

	public Edge Edge3;

	public Vector3 Normal;

	public Vector3 Center;

	public Transform Parent;

	public static Vector3 ApplyLocalScale(Vector3 point, Transform t)
	{
		point.x *= t.localScale.x;
		point.y *= t.localScale.y;
		point.z *= t.localScale.z;
		return point;
	}

	public static Vector3 RemoveLocalScale(Vector3 point, Transform t)
	{
		point.x /= t.localScale.x;
		point.y /= t.localScale.y;
		point.z /= t.localScale.z;
		return point;
	}

	public Triangle SetPoints(MeshFilter meshFilter, int triangle)
	{
		Mesh sharedMesh = meshFilter.sharedMesh;
		Point1 = ApplyLocalScale(sharedMesh.vertices[sharedMesh.triangles[triangle]], meshFilter.transform);
		Point2 = ApplyLocalScale(sharedMesh.vertices[sharedMesh.triangles[triangle + 1]], meshFilter.transform);
		Point3 = ApplyLocalScale(sharedMesh.vertices[sharedMesh.triangles[triangle + 2]], meshFilter.transform);
		Edge1 = new Edge
		{
			Point1 = Point1,
			Point2 = Point2,
			Triangle = this
		};
		Edge2 = new Edge
		{
			Point1 = Point2,
			Point2 = Point3,
			Triangle = this
		};
		Edge3 = new Edge
		{
			Point1 = Point3,
			Point2 = Point1,
			Triangle = this
		};
		Center = (Point1 + Point2 + Point3) / 3f;
		Normal = ApplyLocalScale(sharedMesh.normals[sharedMesh.triangles[triangle]], meshFilter.transform);
		Parent = meshFilter.transform;
		return this;
	}

	public Triangle SetPoints(Mesh mesh, int triangle)
	{
		Point1 = mesh.vertices[mesh.triangles[triangle]];
		Point2 = mesh.vertices[mesh.triangles[triangle + 1]];
		Point3 = mesh.vertices[mesh.triangles[triangle + 2]];
		Edge1 = new Edge
		{
			Point1 = Point1,
			Point2 = Point2,
			Triangle = this
		};
		Edge2 = new Edge
		{
			Point1 = Point2,
			Point2 = Point3,
			Triangle = this
		};
		Edge3 = new Edge
		{
			Point1 = Point3,
			Point2 = Point1,
			Triangle = this
		};
		Center = (Point1 + Point2 + Point3) / 3f;
		Normal = mesh.normals[mesh.triangles[triangle]];
		return this;
	}

	public bool IsValid()
	{
		if (!Edge1.IsValid())
		{
			return false;
		}
		if (Edge2.IsValid())
		{
			return Edge3.IsValid();
		}
		return false;
	}

	public Edge[] GetShortestEdges()
	{
		float num = Vector3.Distance(Point1, Point2);
		float num2 = Vector3.Distance(Point2, Point3);
		float num3 = Vector3.Distance(Point3, Point1);
		if (num > num2 && num > num3)
		{
			return new Edge[2] { Edge2, Edge3 };
		}
		if (num2 > num && num2 > num3)
		{
			return new Edge[2] { Edge1, Edge3 };
		}
		return new Edge[2] { Edge2, Edge1 };
	}

	public Edge GetShortestEdge()
	{
		float num = Vector3.Distance(Point1, Point2);
		float num2 = Vector3.Distance(Point2, Point3);
		float num3 = Vector3.Distance(Point3, Point1);
		if (num < num2 && num < num3)
		{
			return Edge1;
		}
		if (num2 < num && num2 < num3)
		{
			return Edge2;
		}
		return Edge3;
	}
}
