using UnityEngine;

namespace Rooms;

public static class Bootstrapper
{
	private static bool _initialized;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Init()
	{
		if (!_initialized)
		{
			_initialized = true;
			GameObject gameObject = new GameObject("~RoomEvaluator");
			gameObject.AddComponent<RoomEvaluator>();
			Object.DontDestroyOnLoad(gameObject);
		}
	}
}
