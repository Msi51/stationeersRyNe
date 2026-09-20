using System;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Util;

namespace Objects.Pipes;

public class AccessBridge : Device, ISmartRotatable
{
	[SerializeField]
	private GenericAssignableAnimComponent platformAnimComponent;

	[SerializeField]
	private GenericAssignableAnimComponent doorAnimComponent;

	private CancellationTokenWrapper _activateCancellation = new CancellationTokenWrapper();

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action != InteractableType.Activate)
		{
			return base.GetContextualName(interactable);
		}
		if (!IsOpen)
		{
			return ActionStrings.Extend;
		}
		return ActionStrings.Retract;
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (platformAnimComponent != null)
		{
			platformAnimComponent.RefreshState(skipAnimation);
		}
		if (doorAnimComponent != null)
		{
			doorAnimComponent.RefreshState(skipAnimation);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Activate && interactable.State == 1)
		{
			_activateCancellation.Cancel();
			_activateCancellation.Initialize();
			WaitThenStop(_activateCancellation.Token).Forget();
		}
	}

	public override CanConstructInfo CanConstruct()
	{
		CanConstructInfo result = base.CanConstruct();
		if (!result.CanConstruct)
		{
			return result;
		}
		Span<Grid3> span = stackalloc Grid3[GridBounds._grids.Length];
		GridBounds.GetLocalGrids(base.ThingTransformPosition, base.ThingTransformRotation, span);
		Span<Grid3> span2 = span;
		for (int i = 0; i < span2.Length; i++)
		{
			Grid3 localGrid = span2[i];
			Cell cell = base.GridController.GetCell(localGrid);
			CanConstructInfo result2 = CanConstructCell(cell, base.ThingTransformPosition);
			if (!result2.CanConstruct)
			{
				return result2;
			}
		}
		return CanConstructInfo.ValidPlacement;
	}

	private async UniTaskVoid WaitThenStop(CancellationToken token)
	{
		await UniTask.Delay(550, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		if (GameManager.GameState == GameState.Running)
		{
			OnServer.Interact(base.InteractActivate, 0);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		switch (interactable.Action)
		{
		case InteractableType.Activate:
			if (!Powered)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if (!OnOff)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
			}
			if (Activate == 1)
			{
				return delayedActionInstance.Fail(GameStrings.GlobalAlreadyInUse);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(base.InteractActivate, 1);
			OnServer.Interact(base.InteractOpen, (!IsOpen) ? 1 : 0);
			return delayedActionInstance.Succeed();
		case InteractableType.Open:
			if (!Powered)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if (!OnOff)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return delayedActionInstance.Succeed();
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		_activateCancellation?.Cancel();
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
