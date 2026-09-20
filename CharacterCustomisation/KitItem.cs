using System;
using DLC;
using UnityEngine;

namespace CharacterCustomisation;

[CreateAssetMenu(fileName = "New Item", menuName = "Stationeers/Character Customisation/New Item", order = 0)]
public class KitItem : UniqueItem
{
	public UnityEngine.Object Item;

	public DLCType DlcType;

	public SkinnedMeshRenderer SkinnedMeshRenderer;

	public GameObject GetPrefabGameObject()
	{
		UnityEngine.Object item = Item;
		if (!(item is GameObject result))
		{
			if (!(item is Component { gameObject: var gameObject }))
			{
				throw new ArgumentException($"Type '{Item.GetType()}' not supported");
			}
			return gameObject;
		}
		return result;
	}

	public T GetPrefabComponent<T>(bool searchChildren = false) where T : UnityEngine.Object
	{
		GameObject prefabGameObject = GetPrefabGameObject();
		if (prefabGameObject is T result)
		{
			return result;
		}
		if (!searchChildren)
		{
			return prefabGameObject.GetComponent<T>();
		}
		return prefabGameObject.GetComponentInChildren<T>(includeInactive: true);
	}

	public override string ToString()
	{
		return JsonUtility.ToJson(this, prettyPrint: true);
	}

	[ContextMenu("Print Json")]
	private void PrintJson()
	{
		Debug.Log(ToString());
	}
}
