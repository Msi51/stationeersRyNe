using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Util;

public class MonoBehaviourListener : Singleton<MonoBehaviourListener>
{
	private List<MonoBehaviourType> MonoBehaviourList;

	private void Awake()
	{
		if (MonoBehaviourList != null)
		{
			return;
		}
		MonoBehaviourList = new List<MonoBehaviourType>();
		foreach (MonoBehaviourType value in Enum.GetValues(typeof(MonoBehaviourType)))
		{
			MonoBehaviourList.Add(value);
		}
	}

	public void SetType(MonoBehaviourType type)
	{
		MonoBehaviourList.Remove(type);
	}

	public bool IsInitialized()
	{
		return MonoBehaviourList.Count == 0;
	}

	public void WaitUntilAllInitialized(Action onComplete, float timeToWait)
	{
		StartCoroutine(WaitUntilInitialized(onComplete, timeToWait));
	}

	private IEnumerator WaitUntilInitialized(Action onComplete, float timeToWait)
	{
		float startTime = Time.time;
		while (!IsInitialized())
		{
			yield return Yielders.EndOfFrame;
			if (timeToWait != 0f && Time.time - startTime > timeToWait)
			{
				onComplete?.Invoke();
				yield break;
			}
		}
		onComplete?.Invoke();
	}
}
