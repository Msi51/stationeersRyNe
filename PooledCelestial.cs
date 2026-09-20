using Assets.Scripts;
using Assets.Scripts.Util;
using UnityEngine;

public class PooledCelestial : CelestialPrefab
{
	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			Debug.LogError("error pooled PooledCelestial '" + base.name + "' was destroyed");
		}
	}
}
