using Assets.Scripts.GridSystem;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class DispersalTowerVentExtension : StructureExtension
{
	[SerializeField]
	private Transform gasExit;

	public WorldGrid VentGrid { get; private set; }

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		VentGrid = new WorldGrid(gasExit.position);
	}
}
