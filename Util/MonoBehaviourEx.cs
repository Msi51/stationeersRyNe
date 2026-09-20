using UnityEngine;

namespace Util;

public static class MonoBehaviourEx
{
	public static void DestroyGameObject(this GameObject gameObject, Object callerContext = null)
	{
		if ((bool)gameObject)
		{
			Object.Destroy(gameObject);
		}
	}

	public static void DestroyComponent(this Component component, Object callerContext = null)
	{
		if ((bool)component)
		{
			Object.Destroy(component);
		}
	}

	public static void SetEnable(this Behaviour behaviour, bool enable, Object callerContext = null)
	{
		behaviour.enabled = enable;
	}

	public static void SetGOActive(this GameObject gameObject, bool active, Object callerContext = null)
	{
		gameObject.SetActive(active);
	}
}
