using System.Runtime.CompilerServices;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.GridSystem;

public readonly struct BoundRef<T>(long referenceId, Bounds bounds, Grid3 position, Quaternion rotation, int colorIndex, float distanceSquaredToCamera) where T : IReferencable
{
	public readonly long ReferenceId = referenceId;

	public readonly Bounds Bounds = bounds;

	public readonly Grid3 Position = position;

	public readonly Quaternion Rotation = rotation;

	public readonly int ColorIndex = colorIndex;

	public readonly float DistanceSquaredToCamera = distanceSquaredToCamera;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Get()
	{
		return Thing.Find<T>(ReferenceId);
	}
}
