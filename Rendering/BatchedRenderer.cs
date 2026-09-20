using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using UnityEngine;

namespace Rendering;

[Obsolete]
public class BatchedRenderer : MonoBehaviour
{
	private readonly Dictionary<Grid3, SpatialBucket> _spatialBucketLookup = new Dictionary<Grid3, SpatialBucket>();

	[SerializeField]
	private List<SpatialBucket> spatialBuckets = new List<SpatialBucket>();

	private static readonly float GridSize = 4f;

	private static readonly float Offset = 0.5f;

	public static readonly int DrawCallMaxInstances = 1023;

	public static BatchedRenderer Instance { get; private set; }

	public void Awake()
	{
		Instance = this;
	}

	public void AddSpatialDraw(DrawData drawData, Matrix4x4 localToWorldTransform)
	{
		Grid3 grid = SpatialBucketPosition(localToWorldTransform);
		GetOrAddSpatialBucket(grid).Register(drawData, localToWorldTransform);
	}

	public void RemoveSpatialDraw(DrawData drawData, Matrix4x4 localToWorldTransform)
	{
		Grid3 key = SpatialBucketPosition(localToWorldTransform);
		if (_spatialBucketLookup.TryGetValue(key, out var value))
		{
			value.Deregister(drawData, localToWorldTransform);
			if (value.DrawCalls.Count <= 0)
			{
				_spatialBucketLookup.Remove(key);
				spatialBuckets.Remove(value);
			}
		}
	}

	private static Grid3 SpatialBucketPosition(Matrix4x4 instanceTransform)
	{
		return ExtensionMethods.ToGrid(instanceTransform.GetColumn(3), GridSize, Offset);
	}

	private SpatialBucket GetOrAddSpatialBucket(Grid3 grid)
	{
		SpatialBucket spatialBucket = GetSpatialBucket(grid);
		if (spatialBucket == null)
		{
			spatialBucket = new SpatialBucket();
			_spatialBucketLookup.Add(grid, spatialBucket);
			spatialBuckets.Add(spatialBucket);
		}
		return spatialBucket;
	}

	private SpatialBucket GetSpatialBucket(Grid3 grid)
	{
		_spatialBucketLookup.TryGetValue(grid, out var value);
		return value;
	}

	public void SetFloat(DrawData drawData, Matrix4x4 localToWorldTransform, int propertyID, float value)
	{
		GetSpatialBucket(SpatialBucketPosition(localToWorldTransform))?.SetFloat(drawData, localToWorldTransform, propertyID, value);
	}

	public void SetVector(DrawData drawData, Matrix4x4 localToWorldTransform, int propertyID, Vector4 value)
	{
		GetSpatialBucket(SpatialBucketPosition(localToWorldTransform))?.SetVector(drawData, localToWorldTransform, propertyID, value);
	}

	public void Update()
	{
		foreach (SpatialBucket spatialBucket in spatialBuckets)
		{
			spatialBucket.Draw();
		}
	}

	public static void ClearAll()
	{
		if ((object)Instance != null)
		{
			Instance._spatialBucketLookup.Clear();
			Instance.spatialBuckets.Clear();
		}
	}
}
