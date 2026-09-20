using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Sensor : SmallDevice, ISmartRotatable
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public List<Motherboard> LinkedMotherboards = new List<Motherboard>();

	private UniTask _notTriggeredTask;

	private CancellationTokenSource _taskCancel;

	public virtual bool IsTriggered => false;

	public virtual void SetMotherboards(bool isTriggered)
	{
		foreach (Motherboard linkedMotherboard in LinkedMotherboards)
		{
			if (linkedMotherboard is Circuitboard circuitboard)
			{
				circuitboard.RemoteToggle(isTriggered);
			}
		}
	}

	public override void OnDestroy()
	{
		ResetSensor();
		base.OnDestroy();
	}

	public async UniTask WaitUntilNotTriggered()
	{
		if (GameManager.GameState == GameState.None)
		{
			return;
		}
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(this, InteractableType.Activate, 1);
		}
		SetMotherboards(isTriggered: true);
		while (IsTriggered)
		{
			await UniTask.Delay(100, ignoreTimeScale: false, PlayerLoopTiming.Update, _taskCancel.Token);
		}
		if (GameManager.GameState != GameState.None && !_taskCancel.IsCancellationRequested)
		{
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(this, InteractableType.Activate, 0);
			}
			SetMotherboards(isTriggered: false);
		}
	}

	public void ActivateSensor()
	{
		if (Activate == 0 && _notTriggeredTask.Status != UniTaskStatus.Pending)
		{
			_taskCancel = new CancellationTokenSource();
			_notTriggeredTask = WaitUntilNotTriggered();
		}
	}

	public void ResetSensor()
	{
		if (GameManager.GameState == GameState.Running && GameManager.RunSimulation)
		{
			if (_notTriggeredTask.Status == UniTaskStatus.Pending)
			{
				_taskCancel.Cancel();
			}
			SetMotherboards(isTriggered: false);
			OnServer.Interact(this, InteractableType.Activate, 0);
		}
	}

	public override void OnLinkWithBoard(Motherboard motherboard)
	{
		base.OnLinkWithBoard(motherboard);
		LinkedMotherboards.Add(motherboard);
	}

	public override void OnUnlinkWithBoard(Motherboard motherboard)
	{
		base.OnUnlinkWithBoard(motherboard);
		LinkedMotherboards.Remove(motherboard);
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
