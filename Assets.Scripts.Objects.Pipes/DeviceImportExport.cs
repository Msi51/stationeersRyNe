using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class DeviceImportExport : DeviceImport
{
	[Header("Export")]
	[ReadOnly]
	public ExportAnimationComponent ExportAnimationComponent;

	[ReadOnly]
	public int ExportLayerIndex;

	protected int ExportCount;

	[HideInInspector]
	public int ExportConnectionId = -1;

	[ReadOnly]
	public Chute ExportChute;

	[ReadOnly]
	public DynamicThing ExportingThing;

	private int _previousExportAnimStateHash;

	private BinaryAnimState _previousExportAnimState;

	private static readonly float AnimCompletePercent = 0.99f;

	private bool _forceUpdateExportAnimState;

	private int _exportState;

	public Connection ExportConnection => OpenEnds[ExportConnectionId];

	public virtual Slot ExportSlot
	{
		get
		{
			List<Slot> slots = Slots;
			if (slots == null || slots.Count <= 1)
			{
				return null;
			}
			return Slots[1];
		}
	}

	public float ExportSlotQuantity
	{
		get
		{
			if (ExportingThing == null)
			{
				return 0f;
			}
			if (ExportingThing is Stackable stackable)
			{
				return stackable.Quantity;
			}
			if (ExportingThing is Consumable consumable)
			{
				return consumable.Quantity;
			}
			return 0f;
		}
	}

	public bool IsExportClosing
	{
		get
		{
			if (Exporting == 0)
			{
				return !IsCurrentExportAnimComplete;
			}
			return false;
		}
	}

	public bool IsExportOpening
	{
		get
		{
			if (Exporting == 1)
			{
				return !IsCurrentExportAnimComplete;
			}
			return false;
		}
	}

	public bool IsExportClosed
	{
		get
		{
			if (Exporting == 0)
			{
				return IsCurrentExportAnimComplete;
			}
			return false;
		}
	}

	public bool IsExportOpen
	{
		get
		{
			if (Exporting == 1)
			{
				return IsCurrentExportAnimComplete;
			}
			return false;
		}
	}

	protected bool IsExportChuteBlocked
	{
		get
		{
			if ((bool)ExportChute)
			{
				return ExportChute.TransportSlot.Occupant;
			}
			return false;
		}
	}

	public virtual bool CanBeginExport
	{
		get
		{
			if (IsExportClosed)
			{
				return ExportingThing != null;
			}
			return false;
		}
	}

	public virtual bool CanCompleteExport
	{
		get
		{
			if (IsExportOpen)
			{
				if (!(ExportChute == null))
				{
					return ExportChute.TransportSlot.Occupant == null;
				}
				return true;
			}
			return false;
		}
	}

	public virtual bool IsNextExportReady
	{
		get
		{
			if (IsExportClosed)
			{
				return ExportingThing == null;
			}
			return false;
		}
	}

	protected bool IsCurrentExportAnimComplete { get; private set; }

	public override int Exporting
	{
		get
		{
			return _exportState;
		}
		set
		{
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DeviceImportExportSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is DeviceImportExportSaveData deviceImportExportSaveData)
		{
			ExportCount = deviceImportExportSaveData.ExportCount;
			SetExportState(deviceImportExportSaveData.ExportState);
			if (ExportAnimationComponent != null)
			{
				ExportAnimationComponent.RefreshState(skipAnimation: true);
			}
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is DeviceImportExportSaveData deviceImportExportSaveData)
		{
			deviceImportExportSaveData.ExportCount = ExportCount;
			deviceImportExportSaveData.ExportState = _exportState;
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (ExportAnimationComponent != null)
		{
			ExportAnimationComponent.RefreshState(skipAnimation);
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (!HasExportState || ExportLayerIndex < 0)
		{
			return;
		}
		OnServerExportTick();
		if (CanCompleteExport)
		{
			if (ExportingThing != null)
			{
				if (ExportChute != null)
				{
					ExportChute.SetNeighbor(this);
					OnServer.MoveToSlot(ExportingThing, ExportChute.TransportSlot);
				}
				else
				{
					OnServer.MoveToWorld(ExportingThing, ExportSlot, 1f);
				}
				ExportCount++;
			}
			OnServer.Interact(base.InteractExport, 0);
		}
		if (ExportAnimationComponent != null)
		{
			BinaryAnimState currentState = ExportAnimationComponent.CurrentState;
			if (currentState == BinaryAnimState.None)
			{
				_forceUpdateExportAnimState = true;
				IsCurrentExportAnimComplete = false;
			}
			if (currentState != _previousExportAnimState || _forceUpdateExportAnimState)
			{
				_forceUpdateExportAnimState = false;
				switch (currentState)
				{
				case BinaryAnimState.OffToOn:
				case BinaryAnimState.OnToOff:
					IsCurrentExportAnimComplete = false;
					break;
				case BinaryAnimState.Off:
				case BinaryAnimState.On:
					IsCurrentExportAnimComplete = true;
					switch (Exporting)
					{
					case 0:
						OnExportClosingComplete();
						break;
					case 1:
						OnExportOpeningComplete();
						break;
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
				case BinaryAnimState.None:
					break;
				}
			}
			_previousExportAnimState = currentState;
			return;
		}
		AnimatorStateInfo currentAnimatorStateInfo = BaseAnimator.GetCurrentAnimatorStateInfo(ExportLayerIndex);
		if (currentAnimatorStateInfo.fullPathHash != _previousExportAnimStateHash || _forceUpdateExportAnimState)
		{
			IsCurrentExportAnimComplete = false;
			_forceUpdateExportAnimState = false;
			_previousExportAnimStateHash = currentAnimatorStateInfo.fullPathHash;
		}
		else if (!IsCurrentExportAnimComplete && currentAnimatorStateInfo.normalizedTime >= AnimCompletePercent)
		{
			IsCurrentExportAnimComplete = true;
			switch (Exporting)
			{
			case 0:
				OnExportClosingComplete();
				break;
			case 1:
				OnExportOpeningComplete();
				break;
			}
		}
	}

	protected virtual void OnServerExportTick()
	{
	}

	public void SetExportState(int value)
	{
		if (_exportState != value && HasExportState)
		{
			_exportState = value;
			if ((bool)BaseAnimator)
			{
				BaseAnimator.SetInteger(Interactable.ExportState, _exportState);
			}
			IsCurrentExportAnimComplete = false;
			_forceUpdateExportAnimState = true;
		}
	}

	public virtual void OnExportClosingComplete()
	{
	}

	public virtual void OnExportOpeningComplete()
	{
	}

	public override void UpdateChutes()
	{
		base.UpdateChutes();
		if (ExportConnectionId != -1)
		{
			ExportChute = ExportConnection.GetChute();
		}
	}

	public override void OnInteractableStateChanged(Interactable interactable, int newState, int oldState)
	{
		if (interactable.Action == InteractableType.Export)
		{
			SetExportState(newState);
		}
		else
		{
			base.OnInteractableStateChanged(interactable, newState, oldState);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild.ParentSlot == ExportSlot)
		{
			ExportingThing = newChild;
			if (GameManager.RunSimulation && ExportSlot.Interactable != null && (bool)ExportSlot.Interactable.Collider)
			{
				Vector3 size = ExportSlot.Interactable.Collider.bounds.size;
				Vector3 size2 = newChild.Bounds.size;
				float num = Mathf.Min(size.x / size2.x, size.y / size2.y, size.z / size2.z, 1f);
				newChild.ThingTransform.localScale = Vector3.one * num;
			}
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (ExportingThing == previousChild)
		{
			ExportingThing = null;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.ExportCount)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.ExportCount)
		{
			return ExportCount;
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
			ExportCount = 0;
		}
		base.SetLogicValue(logicType, value);
	}
}
