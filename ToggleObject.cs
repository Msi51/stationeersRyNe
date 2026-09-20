using UnityEngine;

public class ToggleObject : MonoBehaviour
{
	public GameObject TargetObject;

	public void OnClick()
	{
		TargetObject.SetActive(!TargetObject.activeInHierarchy);
	}
}
