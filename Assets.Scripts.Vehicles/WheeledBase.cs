using System.Collections.Generic;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Vehicles;

public class WheeledBase : DraggableThing, IPlayableArea, IPlayerVehicle, IReferencable, IEvaluable
{
	public List<Wheel> Wheels = new List<Wheel>();

	[ReadOnly]
	public float CurrentSteeringAngle;

	[ReadOnly]
	public float CurrentMotorPower;

	[ReadOnly]
	public float CurrentBrakePower;

	public float ForceScale = 1000f;

	public float SteeringPower = 50f;

	public float MotorPower = 20f;

	public float BrakePower = 5f;

	public float SteeringSpeed = 1f;

	public float MotorSpeed = 100f;

	public float BrakeSpeed = 100f;

	public float MaxTurnAngle = 35f;

	public RoverUpdate LastAnimationUpdate;

	public float TargetSteeringAngle { get; set; }

	public float TargetMotorPower { get; set; }

	public float TargetBrakePower { get; set; }

	public PlayableAreaRule PlayableAreaState { get; private set; }

	public Vector2 LastValidPlayablePosition { get; set; }

	public override void BuildOwnerUpdate(RocketBinaryWriter writer)
	{
		base.BuildOwnerUpdate(writer);
		new RoverUpdate(this).Write(writer);
	}

	public override void ProcessOwnerUpdate(RocketBinaryReader reader)
	{
		base.ProcessOwnerUpdate(reader);
		RoverUpdate lastAnimationUpdate = new RoverUpdate(this);
		lastAnimationUpdate.Read(reader);
		LastAnimationUpdate = lastAnimationUpdate;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		new RoverUpdate(this).Write(writer);
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		RoverUpdate lastAnimationUpdate = new RoverUpdate(this);
		lastAnimationUpdate.Read(reader);
		LastAnimationUpdate = lastAnimationUpdate;
	}

	public float GetPitch()
	{
		Vector3 right = ThingTransform.right;
		right.y = 0f;
		right *= Mathf.Sign(ThingTransform.up.y);
		return Vector3.Angle(Vector3.Cross(right, Vector3.up).normalized, ThingTransform.forward) * Mathf.Sign(ThingTransform.forward.y);
	}

	public float GetRoll()
	{
		Vector3 forward = ThingTransform.forward;
		forward.y = 0f;
		forward *= Mathf.Sign(ThingTransform.up.y);
		return Vector3.Angle(Vector3.Cross(Vector3.up, forward).normalized, ThingTransform.right) * Mathf.Sign(ThingTransform.right.y);
	}

	private void EnableWheelColliders(bool isEnabled)
	{
		foreach (Wheel wheel in Wheels)
		{
			if (wheel.WheelCollider != null)
			{
				wheel.WheelCollider.enabled = isEnabled;
			}
		}
	}

	public override void ProcessPhysicsUpdate(DynamicThingPosition updateData)
	{
		LastPhysicsUpdate = updateData;
	}

	public override void PhysicsUpdate()
	{
		base.PhysicsUpdate();
		PlayableAreaState = CheckPlayableArea();
		if (PlayableAreaState == PlayableAreaRule.Valid)
		{
			LastValidPlayablePosition = new Vector2(base.Position.x, base.Position.z);
		}
		EnableWheelColliders(HasAuthority);
		CurrentSteeringAngle = Mathf.Lerp(CurrentSteeringAngle, TargetSteeringAngle, Time.fixedDeltaTime * ForceScale);
		CurrentBrakePower = Mathf.Lerp(CurrentBrakePower, TargetBrakePower, Time.fixedDeltaTime * ForceScale);
		CurrentMotorPower = Mathf.Lerp(CurrentMotorPower, TargetMotorPower, Time.fixedDeltaTime * ForceScale);
		for (int i = 0; i < Wheels.Count; i++)
		{
			Wheel wheel = Wheels[i];
			if (HasAuthority)
			{
				wheel.Apply(CurrentMotorPower, CurrentBrakePower, CurrentSteeringAngle);
				if (!IsOccluded)
				{
					wheel.AnimateAuthority();
					wheel.WheelAudio(GetAudioEvent(wheel.RumbleAudioHash), GetAudioEvent(wheel.SkidAudioHash));
				}
			}
			else if (!IsOccluded)
			{
				wheel.Animate(LastAnimationUpdate.GetWheelLocalRotation(i));
				wheel.WheelAudio(GetAudioEvent(wheel.RumbleAudioHash), GetAudioEvent(wheel.SkidAudioHash));
			}
		}
	}
}
