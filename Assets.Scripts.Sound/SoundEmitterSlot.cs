using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Sound;

public struct SoundEmitterSlot
{
	public enum EmitterType
	{
		Default,
		Pipe,
		Wall,
		Tank,
		DynamicCanister
	}

	public Thing Thing;

	public float CoolDownTime;

	public static readonly float MinCoolDownTime = 5f;

	public static readonly float MaxCoolDownTime = 15f;

	public EmitterType EmitType;

	public bool IsFree => Time.time >= CoolDownTime;

	public void UpdateEmitterData(Thing emittingThing, EmitterType emitType)
	{
		Thing = emittingThing;
		CoolDownTime = Time.time + Random.Range(MinCoolDownTime, MaxCoolDownTime);
		EmitType = emitType;
	}
}
