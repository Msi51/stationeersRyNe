using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class DeepMinerSaveData : DeviceInputOutputImportExportCircuitSaveData
{
	public bool IsReachedBedRock { get; set; }

	public Vector3 DrillPosition { get; set; }
}
