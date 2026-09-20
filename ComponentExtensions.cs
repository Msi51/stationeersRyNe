using UnityEngine;

public static class ComponentExtensions
{
	public static T GetOrAddComponent<T>(this Component obj) where T : Component
	{
		T component = obj.GetComponent<T>();
		if (!component)
		{
			return obj.gameObject.AddComponent<T>();
		}
		return component;
	}
}
