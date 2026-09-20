using System;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class ChuteExportBin : SmallDevice, ISmartRotatable
{
	[SerializeField]
	private ChuteBinAnimComponent _chuteBinAnimComponent;

	public Chute InputChute;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public Slot TransportSlot => Slots[0];

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		Span<SmallCellRef> buf = stackalloc SmallCellRef[32];
		int count = 0;
		FillConnected<Chute>(buf, ref count);
		if (count > 0)
		{
			InputChute = buf[0].Get<Chute>();
		}
		_chuteBinAnimComponent.RefreshState(skipAnimation: true);
	}

	public override void OnNeighborPlaced(SmallGrid neighbor)
	{
		base.OnNeighborPlaced(neighbor);
		Chute chute = neighbor as Chute;
		if (!(chute == null))
		{
			InputChute = chute;
		}
	}

	public override void OnNeighborRemoved(SmallGrid neighbor)
	{
		base.OnNeighborRemoved(neighbor);
		if (!(neighbor as Chute == null))
		{
			InputChute = null;
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (!GameManager.RunSimulation || !Powered || !OnOff || Error == 1)
		{
			return;
		}
		if (TransportSlot.IsEmpty() && _chuteBinAnimComponent.CurrentState == BinaryAnimState.On)
		{
			OnServer.Interact(base.InteractOpen, 0);
		}
		if (TransportSlot.IsEmpty() && InputChute != null && InputChute.TransportSlot.IsNotEmpty())
		{
			if (_chuteBinAnimComponent.CurrentState == BinaryAnimState.Off)
			{
				OnServer.MoveToSlot(InputChute.TransportSlot.Occupant, TransportSlot);
			}
		}
		else if (!IsOpen && TransportSlot.IsNotEmpty())
		{
			OnServer.Interact(base.InteractOpen, 1);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		_chuteBinAnimComponent.RefreshState(skipAnimation);
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
