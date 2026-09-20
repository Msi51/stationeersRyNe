using System;
using UnityEngine;

namespace Assets.Scripts.UI;

[Serializable]
public class Edge
{
	public static readonly float MinDistance = 0.005f;

	[NonSerialized]
	public Triangle Triangle;

	public Vector3 Point1;

	public Vector3 Point2;

	[NonSerialized]
	public Vector3 CachedPoint1;

	[NonSerialized]
	public Vector3 CachedPoint2;

	public Vector3[] WorldEdge()
	{
		return new Vector3[2]
		{
			Triangle.Parent.TransformPoint(Point1),
			Triangle.Parent.TransformPoint(Point2)
		};
	}

	public Vector3 WorldCenter()
	{
		return Triangle.Parent.TransformPoint(Center());
	}

	public Vector3 WorldNormal()
	{
		return Triangle.Parent.TransformPoint(Center() + Triangle.Normal);
	}

	public Vector3 Center()
	{
		return (Point1 + Point2) / 2f;
	}

	public bool IsValid()
	{
		return Vector3.Distance(Point1, Point2) > MinDistance;
	}

	public bool IsShortEdge()
	{
		Edge[] shortestEdges = Triangle.GetShortestEdges();
		if (shortestEdges[0] == this)
		{
			return true;
		}
		if (shortestEdges[1] == this)
		{
			return true;
		}
		return false;
	}
}
