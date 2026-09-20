using UnityEngine;

namespace TerrainSystem;

public class MeshStamp : GameBase
{
	[SerializeField]
	private MeshFilter _meshFilter;

	[SerializeField]
	private MeshRenderer _meshRenderer;

	[SerializeField]
	private MeshCollider _meshCollider;

	[SerializeField]
	private Transform _meshTransform;

	[SerializeField]
	private Transform _pivotTransform;

	private float _maxBoundsSize;

	public Transform PivotTransform => _pivotTransform;

	public Bounds GetBounds()
	{
		return _meshCollider.bounds;
	}

	public void SetMesh(Mesh mesh)
	{
		if (!(mesh == _meshFilter.sharedMesh))
		{
			_meshFilter.sharedMesh = mesh;
			_meshCollider.sharedMesh = mesh;
			_pivotTransform.localScale = Vector3.one;
			Vector3 size = _meshCollider.bounds.size;
			_maxBoundsSize = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
			Vector3 center = _meshCollider.bounds.center;
			Vector3 vector = _meshTransform.InverseTransformPoint(center);
			_meshTransform.localPosition = -vector;
		}
	}

	public void SetSize(Vector3 size)
	{
		if (!Mathf.Approximately(_maxBoundsSize, 0f))
		{
			Vector3 localScale = size / _maxBoundsSize;
			_pivotTransform.localScale = localScale;
		}
	}

	public void ResetTransform()
	{
		_pivotTransform.localPosition = Vector3.zero;
		_pivotTransform.localScale = Vector3.one;
		_pivotTransform.rotation = Quaternion.identity;
	}
}
