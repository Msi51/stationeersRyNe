using System.Collections.Generic;
using UnityEngine;

public class VoxeliseScene : MonoBehaviour
{
	[SerializeField]
	private GameObject[] _voxelModel;

	[SerializeField]
	private Transform root;

	[SerializeField]
	private Collider removable;

	private Voxeliser _voxeliser;

	public float VoxelRatio = 0.1f;

	public List<GameObject> objects = new List<GameObject>();

	public int slicePos;

	[ContextMenu("Create to scene")]
	public void Create()
	{
		root.gameObject.SetActive(value: true);
		objects.Clear();
		_voxeliser = new Voxeliser(VoxelRatio, default(Bounds), 1, 1);
		GameObject obj = new GameObject("Voxel Root");
		obj.transform.position = root.GetComponent<Collider>().bounds.min;
		_ = obj.transform;
		for (int i = 0; i < _voxeliser.Xsize; i++)
		{
			for (int j = 0; j < _voxeliser.Ysize; j++)
			{
				for (int k = 0; k < _voxeliser.Zsize; k++)
				{
				}
			}
		}
		root.gameObject.SetActive(value: false);
	}

	public void OnValidate()
	{
		foreach (GameObject @object in objects)
		{
			@object.SetActive(Mathf.FloorToInt(@object.transform.localPosition.x) == slicePos);
		}
	}
}
