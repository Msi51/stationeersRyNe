using Assets.Scripts.GridSystem;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class IndustrialCombustorChimneyExtension : StructureExtension
{
	[SerializeField]
	private Transform chimneyExit;

	public WorldGrid VentGrid { get; private set; }

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		VentGrid = new WorldGrid(chimneyExit.position);
	}
}
