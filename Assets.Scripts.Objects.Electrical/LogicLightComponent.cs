using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicLightComponent : GameBase
{
	[SerializeField]
	private Material MaterialRead;

	[SerializeField]
	private Material MaterialWrite;

	[SerializeField]
	private Material MaterialOff;

	[SerializeField]
	private MeshRenderer ButtonRenderer;

	[SerializeField]
	private LogicUnitBase ParentLogicUnit;

	private LogicMemoryState _state;

	private LogicMemoryState _syncState;

	private static readonly System.Random Rand = new System.Random();

	private float _timeOut;

	private bool _watching;

	public void Awake()
	{
		if (ParentLogicUnit == null)
		{
			ParentLogicUnit = GetComponentInParent<LogicUnitBase>();
		}
	}

	public LogicMemoryState GetSyncState()
	{
		return _syncState;
	}

	private void RefreshState()
	{
		if (!GameManager.IsMainThread)
		{
			RefreshOnMainThread().Forget();
			return;
		}
		if (!ParentLogicUnit.Powered)
		{
			ButtonRenderer.material = MaterialOff;
			return;
		}
		MeshRenderer buttonRenderer = ButtonRenderer;
		buttonRenderer.material = _state switch
		{
			LogicMemoryState.Read => MaterialRead, 
			LogicMemoryState.Write => MaterialWrite, 
			_ => MaterialOff, 
		};
		_syncState = _state;
		ParentLogicUnit.NetworkUpdateFlags |= 512;
	}

	private async UniTaskVoid RefreshOnMainThread()
	{
		await UniTask.SwitchToMainThread();
		RefreshState();
	}

	public void Flash(LogicMemoryState state)
	{
		if (state == LogicMemoryState.Off)
		{
			_state = state;
			RefreshState();
			return;
		}
		_state = state;
		_timeOut = Mathf.Max((float)Rand.Next(30, 80) * 0.001f, _timeOut);
		RefreshState();
		if (!_watching)
		{
			FinishFlash().Forget();
		}
	}

	private async UniTaskVoid FinishFlash()
	{
		for (_watching = true; _timeOut > 0f; _timeOut -= Time.deltaTime)
		{
			await UniTask.NextFrame();
		}
		_watching = false;
		_state = LogicMemoryState.Off;
		RefreshState();
	}

	public void Reset()
	{
		_timeOut = 0f;
		_state = LogicMemoryState.Off;
		_syncState = LogicMemoryState.Off;
		RefreshState();
	}
}
