using System.Collections.Generic;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Trading;

public class NonThingOcclusionHandler : MonoBehaviour
{
	public static List<NonThingOcclusionHandler> AllNonThingOcclusionHandlers = new List<NonThingOcclusionHandler>();

	[SerializeField]
	private MonoBehaviour Owner;

	private INonThingOcclusion _owner;

	public Renderer[] Renderers;

	public Light[] Lights;

	[HideInInspector]
	public float OcclusionTimeout;

	private bool _isOccluded;

	private bool _renderChangeScheduled;

	public static void ClearAll()
	{
		AllNonThingOcclusionHandlers.Clear();
	}

	public void CacheRenderers()
	{
		Renderers = GetComponentsInChildren<Renderer>();
		Lights = GetComponentsInChildren<Light>();
	}

	private void Awake()
	{
		if (Owner is INonThingOcclusion owner)
		{
			_owner = owner;
		}
		else
		{
			ConsoleWindow.PrintError("Non thing occlusion handler set up incorrectly for " + base.gameObject.name);
		}
		AllNonThingOcclusionHandlers.Add(this);
	}

	private void OnDestroy()
	{
		AllNonThingOcclusionHandlers.Remove(this);
	}

	public void SetOcclusion()
	{
		if (_owner != null && !_renderChangeScheduled && _owner.CanSetOcclusion())
		{
			bool flag = _owner.GetCachedTransformPosition().DistanceSquared(InventoryManager.WorldPosition) < _owner.GetRenderMaxDistanceSquared();
			if (flag && _isOccluded)
			{
				_renderChangeScheduled = true;
				ScheduleRenderChange(isOccluded: false).Forget();
			}
			else if (!flag && !_isOccluded)
			{
				_renderChangeScheduled = true;
				ScheduleRenderChange(isOccluded: true).Forget();
			}
		}
	}

	private async UniTaskVoid ScheduleRenderChange(bool isOccluded)
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		await UniTask.NextFrame(cancelToken);
		if (!cancelToken.IsCancellationRequested && _owner.CanSetOcclusion())
		{
			SetIsOccluded(isOccluded);
			_renderChangeScheduled = false;
		}
	}

	private void SetIsOccluded(bool value)
	{
		_isOccluded = value;
		Renderer[] renderers = Renderers;
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = !value;
		}
		Light[] lights = Lights;
		for (int i = 0; i < lights.Length; i++)
		{
			lights[i].enabled = !value;
		}
	}
}
