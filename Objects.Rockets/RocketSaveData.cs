using Assets.Scripts.Objects;
using UnityEngine;

namespace Objects.Rockets;

public class RocketSaveData : ReferencableSaveData
{
	public long RocketNetworkId;

	public float Progress;

	public long TargetNodeId;

	public long CurrentNodeId;

	public string CustomName;

	public RocketState RocketState;

	public RocketMode RocketMode;

	public ReEntryProfile ReEntryProfile;

	public bool HasParentTransform;

	public Vector3 RocketTransformPosition;

	public Vector3 ParentedPosition;

	public Vector3 TargetPosition;

	public Vector2Int ParkLocation;

	public float Velocity;

	public float Acceleration;

	public bool AutoShutOff = true;

	public bool AutoLand = true;

	public float MaxRecordedThrust;

	public float OrbitalPosition;

	public RocketSaveData()
	{
	}

	public RocketSaveData(Rocket rocket)
	{
		ReferenceId = rocket?.ReferenceId ?? 0;
		RocketNetworkId = (rocket?.RocketNetwork?.ReferenceId).GetValueOrDefault();
		Progress = rocket?.Progress ?? 0f;
		TargetNodeId = (rocket?.TargetNode?.ReferenceId).GetValueOrDefault();
		CurrentNodeId = (rocket?.CurrentNode?.ReferenceId).GetValueOrDefault();
		CustomName = rocket?.CustomName ?? string.Empty;
		RocketState = rocket?.RocketState ?? RocketState.None;
		HasParentTransform = rocket?.RocketParentTransform != null;
		ParentedPosition = rocket?.LastParentedPosition ?? Vector3.zero;
		RocketTransformPosition = ((rocket?.RocketParentTransform != null) ? rocket.RocketParentTransform.position : Vector3.zero);
		TargetPosition = rocket?.TargetPosition ?? Vector3.zero;
		ParkLocation = rocket?.RocketParkSlot?.Location ?? Vector2Int.zero;
		Velocity = rocket?.Velocity ?? 0f;
		Acceleration = rocket?.Acceleration ?? 0f;
		AutoShutOff = rocket?.AutomatedShutOff ?? false;
		AutoLand = rocket?.AutomatedLanding ?? false;
		ReEntryProfile = rocket?.ReEntryProfile ?? ReEntryProfile.None;
		RocketMode = rocket?.RocketMode ?? RocketMode.Invalid;
		MaxRecordedThrust = rocket?.MaxRecordedThrust ?? 0f;
		OrbitalPosition = rocket?.OrbitalPosition ?? 0f;
	}
}
