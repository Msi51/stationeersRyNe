using System.Collections;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class NetworkTime : Singleton<NetworkTime>
{
	public static float _serverTimeOffset;

	private readonly SyncTimeMessage _msg = new SyncTimeMessage();

	private const float ServerTimeUpdatePeriod = 60f;

	private readonly WaitForSeconds _yieldObj = new WaitForSeconds(60f);

	private Coroutine _timeUpdateCoroutine;

	public static float time => GameManager.GameTime + _serverTimeOffset;

	public void StartTimeSync()
	{
		if (GameManager.RunSimulation)
		{
			_timeUpdateCoroutine = StartCoroutine(ScheduledNetworkTimeUpdate());
		}
	}

	public void StopTimeSync()
	{
		if (GameManager.RunSimulation && _timeUpdateCoroutine != null)
		{
			StopCoroutine(_timeUpdateCoroutine);
		}
	}

	private IEnumerator ScheduledNetworkTimeUpdate()
	{
		while (true)
		{
			_msg.ServerTime = GameManager.GameTime;
			NetworkServer.SendToClients(_msg, NetworkChannel.GeneralTraffic, -1L);
			yield return _yieldObj;
		}
	}

	public static void UpdateServerTimeOffset(float serverTime)
	{
		_serverTimeOffset = serverTime - GameManager.GameTime;
	}
}
