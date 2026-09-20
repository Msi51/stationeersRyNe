using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Events;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class DeviceInputOutputImport : DeviceInputOutput
{
	[Header("Import")]
	[ReadOnly]
	public ImportAnimationComponent ImportAnimationComponent;

	[ReadOnly]
	public List<DynamicThing> ImportQueue = new List<DynamicThing>();

	[ReadOnly]
	public Collider ImportZone;

	[ReadOnly]
	public MachineInputTrigger ImportTrigger;

	private int _importCount;

	[ReadOnly]
	public int ImportLayerIndex;

	[HideInInspector]
	public int ImportConnectionId = -1;

	[ReadOnly]
	public Chute ImportChute;

	[ReadOnly]
	public DynamicThing ImportingThing;

	public Transform ImportConveyorPosition;

	private BinaryAnimState _previousImportAnimState;

	private int _previousImportAnimStateHash;

	private static readonly float AnimCompletePercent = 0.99f;

	private bool _forceUpdateImportAnimState;

	private int _importState;

	private Coroutine _collectHandler;

	public static int MaximumDropStacks = 200;

	public Connection ImportConnection => OpenEnds[ImportConnectionId];

	public virtual Slot ImportSlot
	{
		get
		{
			if (Slots == null || Slots.Count <= 0)
			{
				return null;
			}
			return Slots[0];
		}
	}

	public bool IsImportClosing
	{
		get
		{
			if (Importing == 1)
			{
				return !IsCurrentImportAnimComplete;
			}
			return false;
		}
	}

	public bool IsImportOpening
	{
		get
		{
			if (Importing == 0)
			{
				return !IsCurrentImportAnimComplete;
			}
			return false;
		}
	}

	public bool IsImportClosed
	{
		get
		{
			if (Importing == 1)
			{
				return IsCurrentImportAnimComplete;
			}
			return false;
		}
	}

	public bool IsImportOpen
	{
		get
		{
			if (Importing == 0)
			{
				return IsCurrentImportAnimComplete;
			}
			return false;
		}
	}

	public virtual bool CanBeginImport
	{
		get
		{
			if (IsImportOpen)
			{
				return ImportingThing != null;
			}
			return false;
		}
	}

	public virtual bool CanCompleteImport => IsImportClosed;

	public virtual bool IsNextImportReady
	{
		get
		{
			if (IsImportOpen)
			{
				return ImportingThing == null;
			}
			return false;
		}
	}

	protected virtual bool ShouldImportTick
	{
		get
		{
			if (HasImportState)
			{
				if ((bool)BaseAnimator)
				{
					return ImportLayerIndex >= 0;
				}
				return true;
			}
			return false;
		}
	}

	protected bool IsCurrentImportAnimComplete { get; private set; }

	public override int Importing
	{
		get
		{
			return _importState;
		}
		set
		{
		}
	}

	protected virtual bool SkipCollectingWhenHasImportChute => true;

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DeviceInputOutputImportSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is DeviceInputOutputImportSaveData deviceInputOutputImportSaveData)
		{
			_importCount = deviceInputOutputImportSaveData.ImportCount;
			SetImportState(deviceInputOutputImportSaveData.ImportState);
			if (ImportAnimationComponent != null)
			{
				ImportAnimationComponent.RefreshState(skipAnimation: true);
			}
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is DeviceInputOutputImportSaveData deviceInputOutputImportSaveData)
		{
			deviceInputOutputImportSaveData.ImportCount = _importCount;
			deviceInputOutputImportSaveData.ImportState = _importState;
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (!ShouldImportTick)
		{
			return;
		}
		OnServerImportTick();
		if (ImportAnimationComponent != null)
		{
			BinaryAnimState currentState = ImportAnimationComponent.CurrentState;
			if (currentState == BinaryAnimState.None)
			{
				_forceUpdateImportAnimState = true;
				IsCurrentImportAnimComplete = false;
			}
			if (currentState != _previousImportAnimState || _forceUpdateImportAnimState)
			{
				_forceUpdateImportAnimState = false;
				switch (currentState)
				{
				case BinaryAnimState.OffToOn:
				case BinaryAnimState.OnToOff:
					IsCurrentImportAnimComplete = false;
					break;
				case BinaryAnimState.Off:
				case BinaryAnimState.On:
					IsCurrentImportAnimComplete = true;
					switch (Importing)
					{
					case 0:
						OnImportClosingComplete();
						break;
					case 1:
						OnImportOpeningComplete();
						break;
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
				case BinaryAnimState.None:
					break;
				}
			}
			_previousImportAnimState = currentState;
		}
		else
		{
			if (ImportLayerIndex < 0)
			{
				return;
			}
			AnimatorStateInfo currentAnimatorStateInfo = BaseAnimator.GetCurrentAnimatorStateInfo(ImportLayerIndex);
			if (currentAnimatorStateInfo.fullPathHash != _previousImportAnimStateHash || _forceUpdateImportAnimState)
			{
				IsCurrentImportAnimComplete = false;
				_forceUpdateImportAnimState = false;
				_previousImportAnimStateHash = currentAnimatorStateInfo.fullPathHash;
			}
			else if (!IsCurrentImportAnimComplete && currentAnimatorStateInfo.normalizedTime >= AnimCompletePercent)
			{
				IsCurrentImportAnimComplete = true;
				switch (Importing)
				{
				case 0:
					OnImportOpeningComplete();
					break;
				case 1:
					OnImportClosingComplete();
					break;
				}
			}
		}
	}

	protected virtual void OnServerImportTick()
	{
	}

	public override void OnInputTriggerExit(DynamicThing dynamicThing, MachineInputTrigger trigger)
	{
		base.OnInputTriggerExit(dynamicThing, trigger);
		ImportQueue.Remove(dynamicThing);
	}

	public override void InitializeDevice()
	{
		base.InitializeDevice();
		UpdateChutes();
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (ImportAnimationComponent != null)
		{
			ImportAnimationComponent.RefreshState(skipAnimation);
		}
	}

	public void SetImportState(int value)
	{
		if (_importState != value && HasImportState)
		{
			_importState = value;
			if ((bool)BaseAnimator)
			{
				BaseAnimator.SetInteger(Interactable.ImportState, _importState);
			}
			IsCurrentImportAnimComplete = false;
			_forceUpdateImportAnimState = true;
		}
	}

	public virtual void OnImportClosingComplete()
	{
	}

	public virtual void OnImportOpeningComplete()
	{
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		UpdateChutes();
	}

	public override void OnAddChuteNetwork(ChuteNetwork newNetwork)
	{
		base.OnAddChuteNetwork(newNetwork);
		UpdateChutes();
	}

	public override void OnRemoveChuteNetwork(ChuteNetwork newNetwork)
	{
		base.OnRemoveChuteNetwork(newNetwork);
		UpdateChutes();
	}

	public virtual void UpdateChutes()
	{
		if (ImportConnectionId != -1)
		{
			ImportChute = ImportConnection.GetChute();
		}
	}

	public override void OnInteractableStateChanged(Interactable interactable, int newState, int oldState)
	{
		if (interactable.Action == InteractableType.Import)
		{
			SetImportState(newState);
		}
		else
		{
			base.OnInteractableStateChanged(interactable, newState, oldState);
		}
	}

	public override void OnInputTriggerEnter(DynamicThing dynamicThing, MachineInputTrigger trigger)
	{
		if ((!SkipCollectingWhenHasImportChute || !ImportChute) && (bool)(dynamicThing as Item) && !ImportQueue.Contains(dynamicThing))
		{
			ImportQueue.Add(dynamicThing);
			if (_collectHandler == null)
			{
				_collectHandler = StartCoroutine(HandleCollecting());
			}
		}
	}

	private IEnumerator HandleCollecting()
	{
		while (ImportQueue.Count > 0)
		{
			TryCollect();
			yield return Yielders.EndOfFrame;
		}
		_collectHandler = null;
	}

	public override void OnReleaseReagents()
	{
		base.OnReleaseReagents();
		if (GameManager.GameState == GameState.Running && GameManager.RunSimulation && ReagentMixture != null && ReagentMixture.TotalReagents > 0.0)
		{
			int num = 0;
			while (ReagentMixture.TotalReagents > 0.0 && num < MaximumDropStacks)
			{
				DropReagent(DropType == DropType.Ore);
				num++;
			}
		}
	}

	public virtual void TryCollect()
	{
		if (IsNextImportReady && ImportQueue.Count > 0)
		{
			OnServer.MoveToSlot(ImportQueue[0], ImportSlot);
		}
	}

	public virtual bool TryChuteImport()
	{
		if (ImportChute == null)
		{
			return false;
		}
		DynamicThing occupant = ImportChute.TransportSlot.Occupant;
		if (occupant == null || ImportSlot.Occupant != null)
		{
			return false;
		}
		OnServer.MoveToSlot(occupant, ImportSlot);
		return true;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild.ParentSlot != ImportSlot)
		{
			return;
		}
		ImportingThing = newChild;
		newChild.ScaleToSlot();
		if (GameManager.RunSimulation)
		{
			if (CanBeginImport)
			{
				OnServer.Interact(base.InteractImport, 1);
			}
			if (GameManager.GameState == GameState.Running)
			{
				_importCount++;
			}
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (GameManager.RunSimulation && ImportingThing == previousChild)
		{
			if (CanCompleteImport)
			{
				OnServer.Interact(base.InteractImport, 0);
			}
			ImportingThing = null;
		}
		previousChild.HandleCollisionWith(this, ignore: false);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.ImportCount)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.ImportCount)
		{
			return _importCount;
		}
		return base.GetLogicValue(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.ClearMemory)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.ClearMemory && value > 0.0)
		{
			_importCount = 0;
		}
		base.SetLogicValue(logicType, value);
	}

	public new SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public new void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public new void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public new int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
