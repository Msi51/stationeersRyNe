using System.Collections.Generic;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Objects;

public abstract class BlendedAnimComponent : GameBase
{
	[SerializeField]
	protected Thing parentThing;

	[SerializeField]
	protected List<KeyFrameData> states = new List<KeyFrameData>();

	[SerializeField]
	protected Transform animatedTransform;

	private bool _initialised;

	private void Awake()
	{
		Init();
	}

	protected virtual void Init()
	{
		if (!_initialised)
		{
			_initialised = true;
			if (parentThing == null)
			{
				parentThing = GetComponentInParent<Thing>();
			}
		}
	}

	public void SetParentThing(Thing parent)
	{
		parentThing = parent;
	}

	protected virtual void PlaySound()
	{
	}

	protected virtual bool ShouldPlaySound(KeyFrameData newState)
	{
		return true;
	}

	public void SetState(float ratio)
	{
		if (!(parentThing is Structure { IsStructureCompleted: false }))
		{
			ratio = Mathf.Clamp01(ratio);
			KeyFrameData newState = ((states.Count == 2) ? KeyFrameData.Lerp(states[0], states[1], ratio) : KeyFrameData.Lerp(states, ratio));
			if (ShouldPlaySound(newState))
			{
				PlaySound();
			}
			animatedTransform.localPosition = newState.Position;
			animatedTransform.localRotation = Quaternion.Euler(newState.Rotation);
			if (newState.Scale != Vector3.one)
			{
				animatedTransform.localScale = newState.Scale;
			}
		}
	}
}
