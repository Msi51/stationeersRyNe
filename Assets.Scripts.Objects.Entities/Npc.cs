using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Entities;

public class Npc : Entity
{
	[Header("Npc Info")]
	[Space(50f)]
	public float Acceleration = 4f;

	public float MaxSpeed = 2f;

	public float TurnSpeed = 3f;

	public float JumpStrength = 1f;

	public float StepUpJumpStrength = 5f;

	[SerializeField]
	protected float StateTimer;

	[Header("PathFinding")]
	[ReadOnly]
	public List<GridPathfinder.NpcPathGrid> PathList;

	protected bool IsBusy;

	protected Vector3 AimVector;

	protected Vector3 TargetGrid;

	protected float StationaryTime;

	protected float StationaryTolerance;

	protected float LastPathChange;

	private Quaternion _lookQuat;

	protected Animator ControllingAnimator;

	public static int VelocityHash = Animator.StringToHash("Velocity");

	public static int AliveHash = Animator.StringToHash("Alive");

	public override void Awake()
	{
		base.Awake();
		ControllingAnimator = GetComponent<Animator>();
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, new VolumeLitres(3.0), 0L);
		}
	}

	public virtual void DoAttack()
	{
	}

	public virtual void StartAttack()
	{
	}

	public override void UpdateEachFrame()
	{
		if (!WorldManager.IsGamePaused)
		{
			base.UpdateEachFrame();
			if (!IsOccluded)
			{
				Animator.SetFloat(VelocityHash, RigidBody.velocity.magnitude);
				Animator.SetBool(AliveHash, base.State == EntityState.Alive);
			}
		}
	}

	public override void PhysicsUpdate()
	{
		base.PhysicsUpdate();
		if (GameManager.RunSimulation && base.State == EntityState.Alive && PathList != null && PathList.Count > 0)
		{
			if (RigidBody.useGravity)
			{
				NavigatePath();
			}
			else
			{
				NoGravityNavigation();
			}
		}
	}

	public virtual void HandleStateTimer()
	{
		if (StateTimer >= 0f && base.State == EntityState.Alive)
		{
			StateTimer -= 1f;
		}
	}

	protected void NoGravityNavigation()
	{
		if (RigidBody.useGravity || PathList == null || PathList.Count <= 0)
		{
			return;
		}
		TargetGrid = base.GridController.LocalToWorld(PathList[0].Grid);
		TargetGrid -= new Vector3(0f, 1f, 0f);
		AimVector = TargetGrid - base.ThingTransformPosition;
		if (Time.fixedTime - LastPathChange > 10f)
		{
			PathList = null;
			IsBusy = false;
			StateTimer = 1f;
			LastPathChange = Time.fixedTime;
			return;
		}
		_lookQuat = Quaternion.LookRotation(AimVector);
		if (Vector3.Distance(TargetGrid, base.ThingTransformPosition) < 1f && PathList.Count > 0)
		{
			PathList.RemoveAt(0);
			LastPathChange = Time.fixedTime;
		}
		ThingTransform.rotation = Quaternion.Lerp(ThingTransform.rotation, _lookQuat, Time.deltaTime * TurnSpeed);
		if (RigidBody.velocity.magnitude < MaxSpeed)
		{
			RigidBody.AddForce(AimVector.normalized * Acceleration);
		}
		else
		{
			RigidBody.AddForce(-RigidBody.velocity * 5f);
		}
	}

	public override void FollowPath(RoomManager.PathfindingTask pathfindingTask)
	{
		if (pathfindingTask.Result != null && pathfindingTask.Result.Count != 0)
		{
			PathList = pathfindingTask.Result;
		}
	}

	protected void NavigatePath()
	{
		if (PathList != null && PathList.Count > 0)
		{
			Vector3 vector = ((base.WorldAtmosphere != null) ? (base.WorldAtmosphere.Direction * AtmosphereDampeningScale) : Vector3.zero);
			Vector3 force = Vector3.zero;
			TargetGrid = base.GridController.LocalToWorld(PathList[0].Grid);
			AimVector = TargetGrid - base.ThingTransformPosition;
			Vector3 vector2 = Vector3.ProjectOnPlane(AimVector, Vector3.up);
			if (PathList.Count > 0 && Mathf.Abs(Vector3.Dot(Vector3.up, (PathList[0].Grid - base.ThingTransformPosition.ToGrid()).ToVector3().normalized)) > 0.9f && PathList.Count > 1)
			{
				PathList.RemoveAt(0);
			}
			if (base.VelocityMagnitude < 0.5f)
			{
				StationaryTime += Time.fixedDeltaTime;
			}
			else if (StationaryTime >= 0f)
			{
				StationaryTime -= Time.fixedDeltaTime;
			}
			if (StationaryTime > StationaryTolerance)
			{
				force = Vector3.up * Random.Range(0.5f, 1f * JumpStrength);
				if (PathList.Count > 0)
				{
					PathList.RemoveAt(0);
				}
				StationaryTime = 0f;
			}
			if (Time.fixedTime - LastPathChange > 5f)
			{
				PathList = null;
				IsBusy = false;
				StateTimer = 1f;
				LastPathChange = Time.fixedTime;
				return;
			}
			Vector3 forward = Vector3.ProjectOnPlane((RigidBody.velocity.y < 0f) ? new Vector3(RigidBody.velocity.x, 0f, RigidBody.velocity.z) : RigidBody.velocity, Vector3.up);
			if (forward.magnitude > 0.5f)
			{
				_lookQuat = Quaternion.LookRotation(forward);
			}
			if (PathList.Count > 0 && PathList[0].Grid == GridPosition)
			{
				PathList.RemoveAt(0);
				LastPathChange = Time.fixedTime;
			}
			ThingTransform.rotation = Quaternion.Lerp(ThingTransform.rotation, _lookQuat, Time.deltaTime * TurnSpeed);
			if (base.VelocityMagnitude < MaxSpeed)
			{
				if (force.y > 0.1f)
				{
					RigidBody.AddForce(force, ForceMode.VelocityChange);
					force = Vector3.zero;
				}
				else if (Physics.Raycast(base.Position, -ThingTransform.up, Collider.bounds.extents.y + 0.25f))
				{
					RigidBody.AddForce(vector2.normalized * (Acceleration * RigidBody.mass) + vector);
				}
			}
			else
			{
				RigidBody.AddForce(-RigidBody.velocity * 5f);
			}
		}
		if (PathList == null || PathList.Count == 0)
		{
			if (StateTimer < 2f)
			{
				StateTimer = 2f;
			}
			if (PathList == null || (PathList != null && PathList.Count < 1))
			{
				PathList = null;
				IsBusy = false;
				StateTimer = 2f;
			}
		}
	}

	public virtual void RegisterPathFindingTask(bool force = false)
	{
	}
}
