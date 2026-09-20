using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using UnityEngine;
using Util;

namespace Assets.Scripts.Objects;

public class WindowShutterController : Device, IWindowShutter, ISmartRotatable
{
	[SerializeField]
	private MaterialChanger keypadDisplay;

	private CancellationTokenWrapper _updateStateCancellation = new CancellationTokenWrapper();

	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.FlatExhaustive;

	public int[] OpenEndsPermutation = new int[4] { 0, 1, 2, 3 };

	public Grid3[] NeighbourPositions { get; } = new Grid3[12];

	public Grid3 RegisteredGridPosition => RegisteredLocalGrid;

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (keypadDisplay != null)
		{
			if (!OnOff || !Powered)
			{
				keypadDisplay.ChangeState(Defines.Animator.Off);
			}
			else if (IsLocked)
			{
				keypadDisplay.ChangeState(Defines.Animator.Lock);
			}
			else
			{
				keypadDisplay.ChangeState(Defines.Animator.OnPowered);
			}
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (interactable.Action == InteractableType.Open)
		{
			if (!OnOff || !Powered || Error == 1)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceOffOrUnpowered.AsString(DisplayName));
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnInteractableStateChanged(Interactable interactable, int newState, int oldState)
	{
		base.OnInteractableStateChanged(interactable, newState, oldState);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Open && newState != oldState && OnOff && Powered && IsOperable && Error == 0)
		{
			_updateStateCancellation.CancelAndInitialize();
			UpdateState(_updateStateCancellation.Token).Forget();
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		ShutteredWindow.RegisterNeighbourPositions(this);
	}

	private async UniTaskVoid UpdateState(CancellationToken cancellationToken)
	{
		await UniTask.SwitchToThreadPool();
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		HashSet<IWindowShutter> connected = FindConnectedWindows(cancellationToken);
		if (!cancellationToken.IsCancellationRequested)
		{
			await UniTask.SwitchToMainThread(cancellationToken);
			if (!cancellationToken.IsCancellationRequested)
			{
				ApplyOpenState(connected);
			}
		}
	}

	private HashSet<IWindowShutter> FindConnectedWindows(CancellationToken cancellationToken)
	{
		HashSet<IWindowShutter> visited = new HashSet<IWindowShutter>(128);
		ShutteredWindow.FindNeighbours(this, ref visited, cancellationToken);
		return visited;
	}

	private void ApplyOpenState(HashSet<IWindowShutter> connectedWindowShutters)
	{
		foreach (IWindowShutter connectedWindowShutter in connectedWindowShutters)
		{
			if (connectedWindowShutter is ShutteredWindow { BeingDestroyed: false, IsStructureCompleted: not false } shutteredWindow)
			{
				OnServer.Interact(shutteredWindow.InteractOpen, IsOpen ? 1 : 0);
			}
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		_updateStateCancellation.Cancel();
	}

	public override CanConstructInfo CanConstruct()
	{
		CanConstructInfo result = IsBlockClear();
		if (!result.CanConstruct)
		{
			return result;
		}
		return base.CanConstruct();
	}

	private CanConstructInfo IsBlockClear()
	{
		Cell cell = base.GridController.GetCell(GetGrid());
		if (cell == null)
		{
			return CanConstructInfo.ValidPlacement;
		}
		CanConstructInfo result = IsBlockPosition(cell, Quaternion.AngleAxis(90f, ThingTransform.right));
		if (!result.CanConstruct)
		{
			return result;
		}
		CanConstructInfo result2 = IsBlockPosition(cell, Quaternion.AngleAxis(-90f, ThingTransform.right));
		if (!result2.CanConstruct)
		{
			return result2;
		}
		CanConstructInfo result3 = IsBlockPosition(cell, Quaternion.AngleAxis(90f, ThingTransform.up));
		if (!result3.CanConstruct)
		{
			return result3;
		}
		CanConstructInfo result4 = IsBlockPosition(cell, Quaternion.AngleAxis(-90f, ThingTransform.up));
		if (!result4.CanConstruct)
		{
			return result4;
		}
		CanConstructInfo result5 = IsBlockPosition(cell, Quaternion.AngleAxis(180f, ThingTransform.right));
		if (!result5.CanConstruct)
		{
			return result5;
		}
		CanConstructInfo result6 = IsBlockPosition(cell, Quaternion.AngleAxis(-180f, ThingTransform.right));
		if (!result6.CanConstruct)
		{
			return result6;
		}
		return CanConstructInfo.ValidPlacement;
	}

	private CanConstructInfo IsBlockPosition(Cell cell, Quaternion offsetRotation)
	{
		return CanConstructCell(cell, GetBlockPosition(offsetRotation));
	}

	private Vector3 GetBlockPosition(Quaternion offsetRotation)
	{
		return GetGrid() + ThingTransform.rotation * Bounds.center + offsetRotation * -ThingTransform.forward;
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Open)
		{
			if (OnOff && Powered)
			{
				int state = (int)Mathf.Clamp((float)value, 0f, 1f);
				OnServer.Interact(base.InteractOpen, state);
			}
		}
		else
		{
			base.SetLogicValue(logicType, value);
		}
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
