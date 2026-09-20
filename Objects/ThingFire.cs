using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects;

public class ThingFire : Fire
{
	public static List<ThingFire> AllThingFires = new List<ThingFire>();

	public static readonly ConcurrentDictionary<Thing, ThingFire> ThingFireLookup = new ConcurrentDictionary<Thing, ThingFire>();

	public Thing ParentThing;

	public int LastEmissionFrame;

	private int _randomOffset;

	public static ParticleSystem ThingFireParticleSystem;

	public static ParticleSystem.Particle[] ThingFireParticles;

	public static Vector3[] ThingParticlePositions;

	public static Vector3[] ThingAtmosphereVelocities;

	public static ParticleSystem.ShapeModule ThingFireShapeModule;

	public static ParticleSystem.MainModule ThingFireMainModule;

	public static Transform ThingFireVisualizerTransform;

	public static readonly int MAXThingFireParticles = 5000;

	private static readonly System.Random _random = new System.Random();

	private const float MAX_DISTANCE_TO_RENDER = 50f;

	public override Vector3 Position => ParentThing.Position;

	public override bool IsValid
	{
		get
		{
			if (ParentThing != null)
			{
				return ParentThing.IsBurning;
			}
			return false;
		}
	}

	public ThingFire(Thing thing)
	{
		ParentThing = thing;
		_particleData = default(FlameParticleData);
		_randomOffset = _random.Next(0, 5);
	}

	public static void Initialise(ParticleSystem thingFireParticleSystem)
	{
		ThingFireParticleSystem = thingFireParticleSystem;
		ThingFireVisualizerTransform = ThingFireParticleSystem.transform;
		ThingFireShapeModule = ThingFireParticleSystem.shape;
		ThingFireMainModule = ThingFireParticleSystem.main;
		ThingParticlePositions = new Vector3[MAXThingFireParticles];
		ThingAtmosphereVelocities = new Vector3[MAXThingFireParticles];
		ThingFireParticles = new ParticleSystem.Particle[MAXThingFireParticles];
	}

	public static void Register(ThingFire thingFire)
	{
		if (!thingFire.IsValid)
		{
			return;
		}
		lock (AllThingFires)
		{
			AllThingFires.Add(thingFire);
		}
	}

	public static void DeRegister(ThingFire thingFire)
	{
		lock (AllThingFires)
		{
			AllThingFires.Remove(thingFire);
		}
	}

	public static void UpdateFlames()
	{
		foreach (KeyValuePair<Thing, ThingFire> item in ThingFireLookup)
		{
			item.Key.UpdateFlameVisualizer();
		}
	}

	public override void PrepareEmitterState()
	{
		if (!GameManager.IsBatchMode)
		{
			ThingFireMainModule.startColor = _particleData.Color;
			ThingFireShapeModule.scale = _particleData.Scale;
		}
	}

	public static void SetFlameParticleValues(Thing thing, FlameParticleData data)
	{
		if (ThingFireLookup.TryGetValue(thing, out var value))
		{
			value._particleData = data;
		}
	}

	public static void EmitThingFireParticles()
	{
		lock (AllThingFires)
		{
			if (AllThingFires.Count <= 0)
			{
				return;
			}
			for (int num = AllThingFires.Count - 1; num >= 0; num--)
			{
				try
				{
					ThingFire thingFire = AllThingFires[num];
					if (!thingFire.IsEmitting())
					{
						break;
					}
					if (!(thingFire.ParentThing.SqDistanceFromListener > 2500f))
					{
						ThingFireVisualizerTransform.position = thingFire.EmissionPosition();
						thingFire.PrepareEmitterState();
						ThingFireParticleSystem.Emit(thingFire._particleData.NumberOfEmitters);
						thingFire.LastEmissionFrame = GameManager.FixedUpdateFrame;
					}
				}
				catch (ArgumentOutOfRangeException)
				{
				}
				catch (IndexOutOfRangeException)
				{
				}
			}
		}
	}

	public Vector3 EmissionPosition()
	{
		return RocketMath.RandomInBox(Position - ParentThing.Bounds.extents, Position + ParentThing.Bounds.extents);
	}

	public bool IsEmitting()
	{
		if (IsValid)
		{
			return LastEmissionFrame + 4 < GameManager.FixedUpdateFrame;
		}
		return false;
	}

	public static void Clear()
	{
		lock (AllThingFires)
		{
			AllThingFires.Clear();
		}
		ThingFireLookup.Clear();
	}
}
