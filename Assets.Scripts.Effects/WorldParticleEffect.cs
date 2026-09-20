using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Effects;

public class WorldParticleEffect : MonoBehaviour
{
	public static HashSet<WorldGrid> GridsInUse = new HashSet<WorldGrid>();

	[Tooltip("Used for particle collision detection")]
	public EffectType ParticleEffectType;

	[ReadOnly]
	public ParticleSystem ParticleSystem;

	[ReadOnly]
	public ParticleSystem.Particle[] Activeparticles;

	[ReadOnly]
	public ParticleSystem.VelocityOverLifetimeModule VelocityModule;

	[ReadOnly]
	public ParticleSystem.MainModule MainModule;

	[ReadOnly]
	public int NumberParticles;

	[ReadOnly]
	public float SunPosition;

	[ReadOnly]
	public ParticleSystem.MinMaxGradient ColorGradient;

	[ReadOnly]
	public Color ColorStart;

	[ReadOnly]
	public Color ColorEnd;

	public AnimationCurve AlphaCurve;

	[Tooltip("Lerp value for adjusting particle velocity. Used to avoid jitter when particles collide with ground")]
	public float VelocitySmooth = 3f;

	public bool ModifyAlphaWithSun;

	private UniTask _setAlphaTask;

	[ReadOnly]
	public List<Vector3> VectorList;

	[ReadOnly]
	public Vector3 CenterPosition;

	[ReadOnly]
	public Vector3 TopPosition;

	[ReadOnly]
	public Vector3 BottomPosition;

	private void Start()
	{
		ParticleSystem = GetComponent<ParticleSystem>();
		VelocityModule = ParticleSystem.velocityOverLifetime;
		Activeparticles = new ParticleSystem.Particle[ParticleSystem.main.maxParticles];
		MainModule = ParticleSystem.main;
		ColorGradient = ParticleSystem.main.startColor;
		ColorStart = ColorGradient.colorMin;
		ColorEnd = ColorGradient.colorMax;
	}

	private async UniTask EffectAlpha(float limit, float timeToReachLowerLimit)
	{
		float timer = 0f;
		while ((double)Math.Abs(ColorGradient.colorMin.a - limit) > 0.05 || Math.Abs(ColorGradient.colorMax.a - limit) > 0.05f)
		{
			timer += Time.deltaTime;
			float t = timer / timeToReachLowerLimit;
			ColorStart = ColorGradient.colorMin;
			ColorStart.a = Mathf.Lerp(ColorStart.a, limit, t);
			ColorGradient.colorMin = ColorStart;
			ColorEnd = ColorGradient.colorMax;
			ColorEnd.a = Mathf.Lerp(ColorEnd.a, limit, t);
			ColorGradient.colorMax = ColorEnd;
			MainModule.startColor = ColorGradient;
			await UniTask.NextFrame();
		}
	}

	private void Update()
	{
		if (!InventoryManager.Parent || WorldManager.IsGamePaused)
		{
			return;
		}
		SunPosition = Vector3.Dot(OrbitalSimulation.WorldSunVector, Vector3.up);
		NumberParticles = ParticleSystem.GetParticles(Activeparticles);
		if (ParticleEffectType == EffectType.Dust)
		{
			for (int i = 0; i < NumberParticles; i++)
			{
				CenterPosition = (Activeparticles[i].position + WorldManager.WindVector).GridCenter();
				TopPosition = CenterPosition + Vector3.up * 4f;
				BottomPosition = CenterPosition + Vector3.down * 2f;
				if (GridsInUse.Contains(new WorldGrid(CenterPosition)))
				{
					Activeparticles[i].remainingLifetime = -1f;
				}
				else
				{
					Activeparticles[i].velocity = Vector3.Lerp(Activeparticles[i].velocity, WorldManager.WindVector / 2f, Time.deltaTime * VelocitySmooth);
				}
			}
		}
		if (ParticleEffectType == EffectType.Rain)
		{
			for (int j = 0; j < NumberParticles; j++)
			{
				CenterPosition = Activeparticles[j].position.GridCenter();
				TopPosition = CenterPosition + Vector3.up * 6f;
			}
		}
		if (ModifyAlphaWithSun && _setAlphaTask.Status != UniTaskStatus.Pending)
		{
			RotatingCelestialBody playerBody = OrbitalSimulation.GetPlayerBody();
			if (playerBody != null)
			{
				_setAlphaTask = EffectAlpha(AlphaCurve.Evaluate(SunPosition), (float)playerBody.GetGameSiderealDayLength().Seconds / 6f);
			}
		}
		ParticleSystem.SetParticles(Activeparticles, NumberParticles);
	}
}
