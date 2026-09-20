using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.GridSystem;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class ShutteredWindow : WallTransparent, IWindowShutter
{
	[SerializeField]
	private ShutterAnimComponent shutterAnimation;

	[SerializeField]
	private ShutterMaterialAnimComponent shutterMaterialAnimation;

	public Grid3 RegisteredGridPosition => RegisteredLocalGrid;

	public override bool CanLightPass
	{
		get
		{
			if (base.CurrentBuildStateIndex >= BuildStates.Count - 1)
			{
				return IsOpen;
			}
			return true;
		}
	}

	public Grid3[] NeighbourPositions { get; } = new Grid3[12];

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (shutterAnimation != null)
		{
			shutterAnimation.RefreshState(skipAnimation);
		}
		if (shutterMaterialAnimation != null)
		{
			shutterMaterialAnimation.RefreshState(skipAnimation);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Open)
		{
			List<BuildState> buildStates = BuildStates;
			buildStates[buildStates.Count - 1].BlockLight = !IsOpen;
		}
	}

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

	public static void FindNeighbours(IWindowShutter shutter, ref HashSet<IWindowShutter> visited, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		Grid3[] neighbourPositions = shutter.NeighbourPositions;
		foreach (Grid3 localFacePosition in neighbourPositions)
		{
			foreach (Structure faceStructure in GridController.World.GetFaceStructures(localFacePosition))
			{
				if (faceStructure is IWindowShutter windowShutter && !visited.Contains(windowShutter))
				{
					visited.Add(windowShutter);
					FindNeighbours(windowShutter, ref visited, cancellationToken);
				}
			}
		}
	}
}
