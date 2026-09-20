using Assets.Scripts.Vehicles;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class RoverFrame : Structure
{
	public Rover RoverPrefab;

	public override void UpdateStateVisualizer(bool visualOnly = false)
	{
		if (base.CurrentBuildStateIndex < BuildStates.Count - 1 && !base.CurrentBuildState.CanManufacture)
		{
			base.UpdateStateVisualizer(visualOnly);
		}
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		if (GameManager.RunSimulation && newState == BuildStates.Count - 1)
		{
			Transform thingTransform = ThingTransform;
			OnServer.Destroy(this);
			OnServer.Create<Rover>(RoverPrefab, thingTransform.position, thingTransform.rotation);
		}
	}
}
