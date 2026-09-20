using System;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class ChuteBin : SmallDevice, ISmartRotatable, IRobotInput
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	[SerializeField]
	private ChuteBinAnimComponent chuteBinAnimComponent;

	public Chute OutputChute;

	public bool AllowInput
	{
		get
		{
			if (IsOpen && !InputSlot.Occupant)
			{
				return base.AllowInteraction;
			}
			return false;
		}
	}

	public Slot InputSlot => Slots[0];

	private bool ReadyToMove => chuteBinAnimComponent.CurrentState == BinaryAnimState.Off;

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		Span<SmallCellRef> buf = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected<Chute>(buf, ref count);
		if (count > 0)
		{
			OutputChute = buf[0].Get<Chute>();
		}
		chuteBinAnimComponent.RefreshState(skipAnimation: true);
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (InputSlot.Occupant == newChild)
		{
			Vector3 size = InputSlot.Interactable.Collider.bounds.size;
			Vector3 size2 = newChild.Bounds.size;
			float num = Mathf.Min(size.x / size2.x, size.y / size2.y, size.z / size2.z, 1f);
			newChild.ThingTransform.localScale = Vector3.one * num;
		}
	}

	public override void OnNeighborPlaced(SmallGrid neighbor)
	{
		base.OnNeighborPlaced(neighbor);
		Chute chute = neighbor as Chute;
		if (!(chute == null))
		{
			OutputChute = chute;
		}
	}

	public override void OnNeighborRemoved(SmallGrid neighbor)
	{
		base.OnNeighborRemoved(neighbor);
		if (!(neighbor as Chute == null))
		{
			OutputChute = null;
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (GameManager.RunSimulation && !IsOpen && base.AllowInteraction && Error != 1 && OnOff && Powered && ReadyToMove)
		{
			if (InputSlot.Occupant != null && OutputChute != null)
			{
				OutputChute.SetNeighbor(this);
				OnServer.MoveToSlot(InputSlot.Occupant, OutputChute.TransportSlot);
			}
			OnServer.Interact(base.InteractOpen, 1);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		chuteBinAnimComponent.RefreshState(skipAnimation);
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = permutation;
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
