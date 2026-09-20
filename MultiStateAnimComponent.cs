using System.Threading;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class MultiStateAnimComponent : InteractableAnimComponent
{
	[SerializeField]
	protected AnimKeyFrameCollection[] states;

	private int _targetIndex;

	private MultiAnimState CurrentState { get; set; }

	private int TargetIndex
	{
		get
		{
			return _targetIndex;
		}
		set
		{
			if (states != null && value < states.Length)
			{
				_targetIndex = value;
			}
		}
	}

	public float TimelineLength
	{
		get
		{
			float num = 0f;
			if (states == null)
			{
				return num;
			}
			AnimKeyFrameCollection[] array = states;
			for (int i = 0; i < array.Length; i++)
			{
				foreach (ObjectAnimData animDatum in array[i].AnimData)
				{
					if (animDatum != null)
					{
						num = Mathf.Max(num, animDatum.tOffset);
					}
				}
			}
			return num;
		}
	}

	protected override bool ShouldUpdateAnimation()
	{
		if (parentThing == null || states == null)
		{
			return false;
		}
		if (GameManager.GameState != GameState.Running)
		{
			return true;
		}
		if (CurrentState == MultiAnimState.None)
		{
			return true;
		}
		if (InteractableState != TargetIndex)
		{
			return InteractableState < states.Length;
		}
		return false;
	}

	protected override void AnimateImmediate()
	{
		TargetIndex = InteractableState;
		states[TargetIndex].Apply();
		CurrentState = MultiAnimState.AtTarget;
	}

	protected override async UniTask Animate(CancellationToken token)
	{
		TargetIndex = InteractableState;
		CurrentState = MultiAnimState.MoveToTarget;
		OnAnimationStart();
		while (GameManager.GameState == GameState.Running && CurrentState == MultiAnimState.MoveToTarget && parentThing != null && !parentThing.BeingDestroyed)
		{
			CurrentState = (states[TargetIndex].MoveTowards(time) ? MultiAnimState.AtTarget : MultiAnimState.MoveToTarget);
			await UniTask.WaitForEndOfFrame(token);
		}
		OnAnimationCompleted();
	}

	public void SampleAtTime(float t)
	{
		if (states == null || states.Length == 0)
		{
			return;
		}
		int num = 0;
		AnimKeyFrameCollection[] array = states;
		foreach (AnimKeyFrameCollection animKeyFrameCollection in array)
		{
			num = Mathf.Max(num, animKeyFrameCollection.AnimData.Count);
		}
		for (int j = 0; j < num; j++)
		{
			ObjectAnimData objectAnimData = null;
			ObjectAnimData objectAnimData2 = null;
			float num2 = float.NegativeInfinity;
			float num3 = float.PositiveInfinity;
			Transform transform = null;
			array = states;
			foreach (AnimKeyFrameCollection animKeyFrameCollection2 in array)
			{
				if (j >= animKeyFrameCollection2.AnimData.Count)
				{
					continue;
				}
				ObjectAnimData objectAnimData3 = animKeyFrameCollection2.AnimData[j];
				if (objectAnimData3 != null)
				{
					if (objectAnimData3.transform != null)
					{
						transform = objectAnimData3.transform;
					}
					float tOffset = objectAnimData3.tOffset;
					if (tOffset <= t && tOffset >= num2)
					{
						num2 = tOffset;
						objectAnimData = objectAnimData3;
					}
					if (tOffset >= t && tOffset <= num3)
					{
						num3 = tOffset;
						objectAnimData2 = objectAnimData3;
					}
				}
			}
			if (!(transform == null))
			{
				if (objectAnimData == null)
				{
					objectAnimData = objectAnimData2;
				}
				if (objectAnimData2 == null)
				{
					objectAnimData2 = objectAnimData;
				}
				if (objectAnimData != null)
				{
					float num4 = objectAnimData2.tOffset - objectAnimData.tOffset;
					float t2 = ((num4 > Mathf.Epsilon) ? Mathf.Clamp01((t - objectAnimData.tOffset) / num4) : 0f);
					transform.localPosition = Vector3.Lerp(objectAnimData.position, objectAnimData2.position, t2);
					transform.localRotation = Quaternion.Lerp(Quaternion.Euler(objectAnimData.rotation), Quaternion.Euler(objectAnimData2.rotation), t2);
					transform.localScale = Vector3.Lerp(objectAnimData.scale, objectAnimData2.scale, t2);
				}
			}
		}
	}
}
