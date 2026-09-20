using System.Threading;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using UnityEngine;
using Util;

public class ActivateButton : MonoBehaviour
{
	[SerializeField]
	private MaterialChanger materialChanger;

	[SerializeField]
	private Transform buttonTransform;

	[SerializeField]
	private Thing parentThing;

	private CancellationTokenWrapper _animationCancellation = new CancellationTokenWrapper();

	[SerializeField]
	private Vector3 activate0Position;

	[SerializeField]
	private Vector3 activate1Position;

	[SerializeField]
	private Collider interactionTrigger;

	[SerializeField]
	private float buttonSpeed = 1f;

	public MaterialChanger MaterialChanger => materialChanger;

	public Collider InteractionTrigger => interactionTrigger;

	public BinaryAnimState CurrentAnimState { get; private set; }

	private bool IsStateIncorrect()
	{
		if (parentThing == null)
		{
			return false;
		}
		int activate = parentThing.Activate;
		if (activate == 0 || activate != 1)
		{
			BinaryAnimState currentAnimState = CurrentAnimState;
			if (currentAnimState == BinaryAnimState.None || currentAnimState == BinaryAnimState.OffToOn || currentAnimState == BinaryAnimState.On)
			{
				return true;
			}
		}
		else
		{
			BinaryAnimState currentAnimState = CurrentAnimState;
			if (currentAnimState == BinaryAnimState.None || currentAnimState == BinaryAnimState.Off || currentAnimState == BinaryAnimState.OnToOff)
			{
				return true;
			}
		}
		return false;
	}

	public void Awake()
	{
		if (parentThing == null)
		{
			parentThing = GetComponentInParent<Thing>();
		}
	}

	public void SetParentThing(Thing parent)
	{
		parentThing = parent;
	}

	public void RefreshState(bool skipAnimation = false)
	{
		if (!GameManager.IsBatchMode && !(parentThing == null))
		{
			if (skipAnimation)
			{
				_animationCancellation.Cancel();
				Transform transform = buttonTransform;
				transform.localPosition = parentThing.Activate switch
				{
					0 => activate0Position, 
					1 => activate1Position, 
					_ => activate0Position, 
				};
				CurrentAnimState = ((parentThing.Activate != 1) ? BinaryAnimState.Off : BinaryAnimState.On);
			}
			else if (IsStateIncorrect())
			{
				_animationCancellation.Cancel();
				_animationCancellation.Initialize();
				Animate(_animationCancellation.Token).Forget();
			}
		}
	}

	private async UniTask Animate(CancellationToken token)
	{
		CurrentAnimState = ((parentThing.Activate == 1) ? BinaryAnimState.OffToOn : BinaryAnimState.OnToOff);
		if (CurrentAnimState == BinaryAnimState.OffToOn)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(Defines.Sounds.ActivateButton, buttonTransform.position);
		}
		while (GameManager.GameState == GameState.Running)
		{
			Vector3 vector = ((parentThing.Activate == 1) ? activate1Position : activate0Position);
			bool num = parentThing.Activate == 1;
			Vector3 localPosition = Vector3.MoveTowards(buttonTransform.localPosition, vector, Time.deltaTime * buttonSpeed);
			if ((num ? (localPosition.z < vector.z) : (localPosition.z > vector.z)) || RocketMath.Approximately(vector.z, localPosition.z, 0.0005f))
			{
				buttonTransform.localPosition = vector;
				CurrentAnimState = ((parentThing.Activate != 1) ? BinaryAnimState.Off : BinaryAnimState.On);
				break;
			}
			buttonTransform.localPosition = localPosition;
			await UniTask.WaitForEndOfFrame(token);
		}
	}
}
