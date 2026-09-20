using UnityEngine;

namespace Assets.Scripts.Util;

public static class FindChildName
{
	public static Transform Get(Transform target, string _name)
	{
		Transform[] componentsInChildren = target.GetComponentsInChildren<Transform>();
		foreach (Transform transform in componentsInChildren)
		{
			if (transform.gameObject.name.Contains(_name))
			{
				return transform;
			}
		}
		return null;
	}

	public static Transform GetExact(Transform target, string _name)
	{
		Transform[] componentsInChildren = target.GetComponentsInChildren<Transform>();
		foreach (Transform transform in componentsInChildren)
		{
			if (transform.gameObject.name.Equals(_name))
			{
				return transform;
			}
		}
		return null;
	}

	public static void SetLayerRecursively(GameObject go, int layer)
	{
		go.layer = layer;
		Transform transform = go.transform;
		for (int i = 0; i < transform.childCount; i++)
		{
			SetLayerRecursively(transform.GetChild(i).gameObject, layer);
		}
	}
}
