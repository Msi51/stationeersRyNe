using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Electrical.Helper;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class IndustrialFiltrationFanExtension : StructureExtension
{
	private const int VENT_GRIDS = 6;

	[SerializeField]
	private List<Transform> ventTransforms = new List<Transform>(6);

	[NonSerialized]
	public readonly List<WorldGrid> VentGrids = new List<WorldGrid>(6);

	[SerializeField]
	private List<Rotator> fans = new List<Rotator>();

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (GameManager.IsBatchMode || !base.IsStructureCompleted || IsOccluded)
		{
			return;
		}
		IndustrialFiltration industrialFiltration = base.ExtendableParent as IndustrialFiltration;
		bool running = industrialFiltration != null && industrialFiltration.Powered && industrialFiltration.OnOff && industrialFiltration.Error == 0 && industrialFiltration.Mode == 1;
		foreach (Rotator fan in fans)
		{
			fan.DoUpdate(running);
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		VentGrids.Clear();
		for (int i = 0; i < ventTransforms.Count; i++)
		{
			Transform transform = ventTransforms[i];
			if (transform != null)
			{
				VentGrids.Add(new WorldGrid(transform.position));
			}
		}
	}
}
