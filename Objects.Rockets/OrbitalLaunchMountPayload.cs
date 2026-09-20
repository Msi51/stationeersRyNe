using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Objects.Rockets.Scanning;
using UnityEngine;

namespace Objects.Rockets;

public class OrbitalLaunchMountPayload : RocketPayload
{
	private const float EDGE_MARGIN = 250f;

	private const float MIN_MOUNT_SEPARATION = 250f;

	private const float STRUCTURE_CLEARANCE = 100f;

	private const int PLACEMENT_ATTEMPTS = 100;

	private static int _clearanceMask;

	private static int ClearanceMask
	{
		get
		{
			if (_clearanceMask == 0)
			{
				return _clearanceMask = 1 << LayerMask.NameToLayer("Default");
			}
			return _clearanceMask;
		}
	}

	public override void OnDeploy()
	{
		RocketPayloadBay rocketPayloadBay = base.ParentSlot?.Parent as RocketPayloadBay;
		if ((bool)CanDeploy() && rocketPayloadBay?.RocketNetwork?.Rocket != null && TryGetFreeDeployPosition(out var vector))
		{
			Thing.Create<LaunchMount>(Animator.StringToHash("StructureLaunchMountOrbital"), vector, Quaternion.identity, 0L);
			OnServer.Destroy(this);
		}
	}

	public override RocketActionResult CanDeploy()
	{
		NodeType? nodeType = (base.ParentSlot?.Parent as RocketPayloadBay)?.RocketNetwork?.Rocket?.CurrentNode?.NodeType;
		if (!nodeType.HasValue || nodeType != NodeType.Entry)
		{
			return RocketActionResult.Failure(GameStrings.RocketDeployFailNotInLowOrbit, this);
		}
		if (!TryGetFreeDeployPosition(out var _))
		{
			return RocketActionResult.Failure(GameStrings.RocketDeployFailNoFreePosition, this);
		}
		return RocketActionResult.Success;
	}

	public static bool TryGetFreeDeployPosition(out Vector3 position)
	{
		Bounds lowOrbitPlayableBounds = Rocket.LowOrbitPlayableBounds;
		Vector3 vector = lowOrbitPlayableBounds.min + Vector3.one * 250f;
		Vector3 vector2 = lowOrbitPlayableBounds.max - Vector3.one * 250f;
		for (int i = 0; i < 100; i++)
		{
			Vector3 vector3 = new Vector3(Random.Range(vector.x, vector2.x), Random.Range(vector.y, vector2.y), Random.Range(vector.z, vector2.z)).GridCenter();
			if (IsClearOfOtherMounts(vector3) && IsClearOfStructures(vector3, lowOrbitPlayableBounds))
			{
				position = vector3;
				return true;
			}
		}
		position = Vector3.zero;
		return false;
	}

	private static bool IsClearOfStructures(Vector3 position, Bounds bounds)
	{
		Vector3 center = new Vector3(position.x, bounds.center.y, position.z);
		Vector3 halfExtents = new Vector3(100f, bounds.size.y * 0.5f, 100f);
		return !Physics.CheckBox(center, halfExtents, Quaternion.identity, ClearanceMask, QueryTriggerInteraction.Ignore);
	}

	private static bool IsClearOfOtherMounts(Vector3 position)
	{
		foreach (SpaceMapNode allSpaceMapNode in SpaceMapNode.AllSpaceMapNodes)
		{
			if (allSpaceMapNode.Owner is LaunchMount { IsOrbital: not false } launchMount && (bool)launchMount)
			{
				Vector3 vector = launchMount.Position - position;
				vector.y = 0f;
				if (vector.sqrMagnitude < 62500f)
				{
					return false;
				}
			}
		}
		return true;
	}
}
