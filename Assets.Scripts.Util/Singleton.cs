using UnityEngine;

namespace Assets.Scripts.Util;

public class Singleton<T> : ManagerBase where T : ManagerBase
{
	private static T _instance;

	private static object _accessLock = new object();

	private static object _createLock = new object();

	private static bool _applicationIsQuitting;

	public static bool IsQuitting => _applicationIsQuitting;

	public static T Instance
	{
		get
		{
			if (_applicationIsQuitting)
			{
				return null;
			}
			lock (_accessLock)
			{
				Create();
				return _instance;
			}
		}
	}

	public static void Create()
	{
		if (_applicationIsQuitting)
		{
			return;
		}
		lock (_createLock)
		{
			if (!(_instance != null))
			{
				_instance = (T)Object.FindObjectOfType(typeof(T), includeInactive: true);
				if (Object.FindObjectsOfType(typeof(T)).Length <= 1 && _instance == null)
				{
					GameObject obj = new GameObject();
					_instance = obj.AddComponent<T>();
					obj.name = "(singleton) " + typeof(T).Name;
				}
			}
		}
	}

	public virtual void OnDestroy()
	{
		_applicationIsQuitting = true;
	}

	public virtual void OnApplicationQuit()
	{
		_applicationIsQuitting = true;
	}
}
