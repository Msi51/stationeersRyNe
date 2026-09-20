using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class ReplacementTool : MonoBehaviour
{
	public Item ParentPrefab;

	public Item PrefabToReplace;

	public void OnValidate()
	{
		ParentPrefab = GetComponent<Item>();
		if ((bool)ParentPrefab)
		{
			ParentPrefab.ReplacementTool = this;
		}
	}
}
