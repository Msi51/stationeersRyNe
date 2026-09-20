using UnityEngine;

public class GameBase : MonoBehaviour
{
	public GameObject GameObject;

	public Transform Transform;

	public virtual bool IsVisible
	{
		get
		{
			if ((bool)GameObject)
			{
				return GameObject.activeInHierarchy;
			}
			return false;
		}
	}

	public virtual void SetVisible(bool isVisble)
	{
		if ((bool)GameObject && GameObject.activeSelf != isVisble)
		{
			SetActive(isVisble);
		}
	}

	public virtual void SetActive(bool active)
	{
		GameObject.SetActive(active);
	}
}
