using System;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class DeviceImportExport2 : DeviceImportExport
{
	[Header("Export2")]
	public ExportAnimationComponent Export2AnimationComponent;

	[ReadOnly]
	public int Export2LayerIndex;

	[HideInInspector]
	public int ExportConnectionId2 = -1;

	[ReadOnly]
	public Chute ExportChute2;

	[ReadOnly]
	public DynamicThing ExportingThing2;

	public Transform ExportConveyorPosition2;

	private int _previousExport2AnimStateHash;

	private BinaryAnimState _previousExport2AnimState;

	private static readonly float AnimCompletePercent = 0.99f;

	private bool _forceUpdateExport2AnimState;

	private int _export2State;

	public Connection ExportConnection2 => OpenEnds[ExportConnectionId2];

	public Slot ExportSlot2 => Slots[2];

	public bool IsExport2Closing
	{
		get
		{
			if (Exporting2 == 0)
			{
				return !IsCurrentExport2AnimComplete;
			}
			return false;
		}
	}

	public bool IsExport2Opening
	{
		get
		{
			if (Exporting2 == 1)
			{
				return !IsCurrentExport2AnimComplete;
			}
			return false;
		}
	}

	public bool IsExport2Closed
	{
		get
		{
			if (Exporting2 == 0)
			{
				return IsCurrentExport2AnimComplete;
			}
			return false;
		}
	}

	public bool IsExport2Open
	{
		get
		{
			if (Exporting2 == 1)
			{
				return IsCurrentExport2AnimComplete;
			}
			return false;
		}
	}

	public virtual bool CanBeginExport2
	{
		get
		{
			if (IsExport2Closed)
			{
				return ExportingThing2 != null;
			}
			return false;
		}
	}

	public virtual bool CanCompleteExport2
	{
		get
		{
			if (IsExport2Open)
			{
				if (!(ExportChute2 == null))
				{
					return ExportChute2.TransportSlot.Occupant == null;
				}
				return true;
			}
			return false;
		}
	}

	public bool IsNextExport2Ready
	{
		get
		{
			if (IsExport2Closed)
			{
				return ExportingThing2 == null;
			}
			return false;
		}
	}

	protected bool IsCurrentExport2AnimComplete { get; private set; }

	public override int Exporting2
	{
		get
		{
			return _export2State;
		}
		set
		{
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DeviceImportExport2SaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is DeviceImportExport2SaveData deviceImportExport2SaveData)
		{
			SetExport2State(deviceImportExport2SaveData.Export2State);
			if (Export2AnimationComponent != null)
			{
				Export2AnimationComponent.RefreshState(skipAnimation: true);
			}
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is DeviceImportExport2SaveData deviceImportExport2SaveData)
		{
			deviceImportExport2SaveData.Export2State = _export2State;
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (!HasExport2State)
		{
			return;
		}
		OnServerExport2Tick();
		if (CanCompleteExport2)
		{
			if (ExportingThing2 != null)
			{
				if (ExportChute2 != null)
				{
					ExportChute2.SetNeighbor(this);
					OnServer.MoveToSlot(ExportingThing2, ExportChute2.TransportSlot);
				}
				else
				{
					OnServer.MoveToWorld(ExportingThing2, ExportSlot2, 1f);
				}
				ExportCount++;
			}
			OnServer.Interact(base.InteractExport2, 0);
		}
		if (Export2AnimationComponent != null)
		{
			BinaryAnimState currentState = Export2AnimationComponent.CurrentState;
			if (currentState != _previousExport2AnimState || _forceUpdateExport2AnimState)
			{
				_forceUpdateExport2AnimState = false;
				switch (currentState)
				{
				case BinaryAnimState.None:
					IsCurrentExport2AnimComplete = true;
					break;
				case BinaryAnimState.OffToOn:
				case BinaryAnimState.OnToOff:
					IsCurrentExport2AnimComplete = false;
					break;
				case BinaryAnimState.Off:
				case BinaryAnimState.On:
					IsCurrentExport2AnimComplete = true;
					switch (Exporting)
					{
					case 0:
						OnExport2ClosingComplete();
						break;
					case 1:
						OnExport2OpeningComplete();
						break;
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
				}
			}
			_previousExport2AnimState = currentState;
			return;
		}
		AnimatorStateInfo currentAnimatorStateInfo = BaseAnimator.GetCurrentAnimatorStateInfo(Export2LayerIndex);
		if (currentAnimatorStateInfo.fullPathHash != _previousExport2AnimStateHash || _forceUpdateExport2AnimState)
		{
			IsCurrentExport2AnimComplete = false;
			_forceUpdateExport2AnimState = false;
			_previousExport2AnimStateHash = currentAnimatorStateInfo.fullPathHash;
		}
		else if (!IsCurrentExport2AnimComplete && currentAnimatorStateInfo.normalizedTime >= AnimCompletePercent)
		{
			IsCurrentExport2AnimComplete = true;
			switch (Exporting)
			{
			case 0:
				OnExport2ClosingComplete();
				break;
			case 1:
				OnExport2OpeningComplete();
				break;
			}
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (Export2AnimationComponent != null)
		{
			Export2AnimationComponent.RefreshState(skipAnimation);
		}
	}

	protected virtual void OnServerExport2Tick()
	{
	}

	public void SetExport2State(int value)
	{
		if (_export2State != value && HasExport2State)
		{
			_export2State = value;
			if ((bool)BaseAnimator)
			{
				BaseAnimator.SetInteger(Interactable.Export2State, _export2State);
			}
			IsCurrentExport2AnimComplete = false;
			_forceUpdateExport2AnimState = true;
		}
	}

	public virtual void OnExport2ClosingComplete()
	{
	}

	public virtual void OnExport2OpeningComplete()
	{
	}

	public override void UpdateChutes()
	{
		base.UpdateChutes();
		if (ExportConnectionId2 != -1)
		{
			ExportChute2 = ExportConnection2.GetChute();
		}
	}

	public override void OnInteractableStateChanged(Interactable interactable, int newState, int oldState)
	{
		if (interactable.Action == InteractableType.Export2)
		{
			SetExport2State(newState);
		}
		else
		{
			base.OnInteractableStateChanged(interactable, newState, oldState);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (GameManager.RunSimulation && newChild.ParentSlot == ExportSlot2)
		{
			ExportingThing2 = newChild;
			if (ExportSlot2.Interactable != null && (bool)ExportSlot2.Interactable.Collider)
			{
				Vector3 size = ExportSlot2.Interactable.Collider.bounds.size;
				Vector3 size2 = newChild.Bounds.size;
				float num = Mathf.Min(size.x / size2.x, size.y / size2.y, size.z / size2.z, 1f);
				newChild.ThingTransform.localScale = Vector3.one * num;
			}
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (GameManager.RunSimulation && !(ExportingThing2 != previousChild))
		{
			ExportingThing2 = null;
		}
	}
}
