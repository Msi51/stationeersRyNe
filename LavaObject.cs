using System.Collections.Generic;
using UnityEngine;

public class LavaObject : GameBase
{
	public static List<LavaObject> AllLavaObjects = new List<LavaObject>();

	[SerializeField]
	private MeshRenderer _meshRenderer;

	[SerializeField]
	private Transform _meshTransform;

	[SerializeField]
	private Transform _pivotTransform;

	private float _maxBoundsSize;

	public Transform PivotTransform => _pivotTransform;

	public void Initialize()
	{
		base.name = $"LavaObject_{AllLavaObjects.Count}";
		AllLavaObjects.Add(this);
	}

	public static void ClearAll()
	{
		for (int num = AllLavaObjects.Count - 1; num >= 0; num--)
		{
			LavaObject lavaObject = AllLavaObjects[num];
			if ((object)lavaObject != null)
			{
				Object.Destroy(lavaObject.gameObject);
				AllLavaObjects.Remove(lavaObject);
			}
		}
	}

	public void SetSize(Vector3 size)
	{
		_pivotTransform.localScale = size;
	}

	public void ResetTransform()
	{
		_pivotTransform.localPosition = Vector3.zero;
		_pivotTransform.localScale = Vector3.one;
		_pivotTransform.rotation = Quaternion.identity;
	}
}
