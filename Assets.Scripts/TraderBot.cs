using System;
using System.Collections;
using Assets.Scripts.Objects.Electrical;
using Trading;
using UnityEngine;

namespace Assets.Scripts;

public class TraderBot : TraderPilot, INonThingOcclusion
{
	public Animator Animator;

	[Tooltip("The maximum amount of time before the next animation play")]
	public int IdleMaximumWaitTime = 20;

	[Tooltip("The minimum amount of time for the idle animation to play")]
	public int IdleMinimumWaitTime = 3;

	[Tooltip("If true the trader bot will play the idle animation, else will not play or will break out")]
	public bool CanPlayIdleAnimation = true;

	private Coroutine _idleCoroutine;

	private static readonly System.Random _idleAnimationChance = new System.Random();

	private static readonly int WaveClipName = Animator.StringToHash("Wave");

	private static readonly int IdleClipName = Animator.StringToHash("Idle");

	private bool _isCurrentlyPlayingIdleAnimation;

	private bool _isCurrentlyPlayingWaveAnimation;

	private const float RENDER_MAX_DISTANCE = 100f;

	public void Awake()
	{
		if ((object)Animator == null)
		{
			Animator = GetComponent<Animator>();
		}
	}

	public void OnEnable()
	{
		Animator.Play(WaveClipName);
	}

	public void OnDisable()
	{
		if (_idleCoroutine != null)
		{
			StopCoroutine(_idleCoroutine);
		}
		_idleCoroutine = null;
	}

	public IEnumerator PlayIdleAnimation()
	{
		while (CanPlayIdleAnimation)
		{
			yield return Yielders.WaitForSeconds(_idleAnimationChance.Next(IdleMinimumWaitTime, IdleMaximumWaitTime));
			if (!_isCurrentlyPlayingIdleAnimation && !_isCurrentlyPlayingWaveAnimation)
			{
				Animator.Play(IdleClipName);
			}
		}
	}

	public void OnIdleAnimationFinished()
	{
		_isCurrentlyPlayingIdleAnimation = false;
	}

	public void OnIdleAnimationStart()
	{
		_isCurrentlyPlayingIdleAnimation = true;
	}

	public void OnWaveAnimationFinished()
	{
		_isCurrentlyPlayingWaveAnimation = false;
		if (CanPlayIdleAnimation && _idleCoroutine == null)
		{
			_idleCoroutine = StartCoroutine(PlayIdleAnimation());
		}
	}

	public void OnWaveAnimationStart()
	{
		_isCurrentlyPlayingWaveAnimation = true;
	}

	public bool CanSetOcclusion()
	{
		return !IsBeingDestroyed;
	}

	public float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public Vector3 GetCachedTransformPosition()
	{
		return CachedTransformPosition;
	}
}
