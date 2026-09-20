using Assets.Scripts.GridSystem;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class ShutteredWindowConnector : Wall, IWindowShutter
{
	public Grid3 RegisteredGridPosition => RegisteredLocalGrid;

	public Grid3[] NeighbourPositions { get; } = new Grid3[12];

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		RegisterNeighbourPositions(this);
	}

	public static void RegisterNeighbourPositions(IWindowShutter iWindowShutter)
	{
		Vector3 up = iWindowShutter.Transform.up;
		Vector3 right = iWindowShutter.Transform.right;
		Vector3 forward = iWindowShutter.Transform.forward;
		iWindowShutter.NeighbourPositions[0] = iWindowShutter.RegisteredGridPosition + new Grid3(up * 2f);
		iWindowShutter.NeighbourPositions[1] = iWindowShutter.RegisteredGridPosition + new Grid3(-up * 2f);
		iWindowShutter.NeighbourPositions[2] = iWindowShutter.RegisteredGridPosition + new Grid3(right * 2f);
		iWindowShutter.NeighbourPositions[3] = iWindowShutter.RegisteredGridPosition + new Grid3(-right * 2f);
		iWindowShutter.NeighbourPositions[4] = iWindowShutter.RegisteredGridPosition + new Grid3(up + forward);
		iWindowShutter.NeighbourPositions[5] = iWindowShutter.RegisteredGridPosition + new Grid3(-up + forward);
		iWindowShutter.NeighbourPositions[6] = iWindowShutter.RegisteredGridPosition + new Grid3(right + forward);
		iWindowShutter.NeighbourPositions[7] = iWindowShutter.RegisteredGridPosition + new Grid3(-right + forward);
		iWindowShutter.NeighbourPositions[8] = iWindowShutter.RegisteredGridPosition + new Grid3(up - forward);
		iWindowShutter.NeighbourPositions[9] = iWindowShutter.RegisteredGridPosition + new Grid3(-up - forward);
		iWindowShutter.NeighbourPositions[10] = iWindowShutter.RegisteredGridPosition + new Grid3(right - forward);
		iWindowShutter.NeighbourPositions[11] = iWindowShutter.RegisteredGridPosition + new Grid3(-right - forward);
	}
}
