using UnityEngine;

namespace Audio;

public struct UnityReference(GameObject gameObject, Transform transform)
{
	public Transform Transform = transform;

	public GameObject GameObject = gameObject;

	public static UnityReference Create(string name, bool isActive = true)
	{
		GameObject gameObject = new GameObject(name);
		if (!isActive)
		{
			gameObject.gameObject.SetActive(value: false);
		}
		return new UnityReference(gameObject.gameObject, gameObject.transform);
	}
}
