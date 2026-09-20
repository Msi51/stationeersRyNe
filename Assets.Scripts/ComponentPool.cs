using System.Collections.Concurrent;
using UnityEngine;

namespace Assets.Scripts;

public class ComponentPool<T> where T : Component
{
	private ConcurrentQueue<T> _objectQueue = new ConcurrentQueue<T>();

	private Transform _poolTransform;

	private T _prefab;

	public ComponentPool(string poolGameObjectName, T prefab, int initialPoolSize = 0)
	{
		_prefab = prefab;
		_poolTransform = new GameObject(poolGameObjectName).transform;
		for (int i = 0; i < initialPoolSize; i++)
		{
			T val = Object.Instantiate(_prefab, _poolTransform);
			val.gameObject.SetActive(value: false);
			_objectQueue.Enqueue(val);
		}
	}

	public T GetObjectFromPool()
	{
		if (!_objectQueue.TryDequeue(out var result))
		{
			result = Object.Instantiate(_prefab, _poolTransform);
		}
		if (result == null || result.gameObject == null)
		{
			return null;
		}
		result.gameObject.SetActive(value: true);
		return result;
	}

	public void ReturnObjectToPool(T component)
	{
		component.gameObject.SetActive(value: false);
		component.gameObject.transform.parent = _poolTransform;
		_objectQueue.Enqueue(component);
	}
}
