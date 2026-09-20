using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Emotes;

public class EmoteController : MonoBehaviour
{
	public static int EmoteIndexHash = Animator.StringToHash("EmoteIndex");

	public static int EmoteFaceIndexHash = Animator.StringToHash("EmoteFaceIndex");

	[SerializeField]
	private Animator _animator;

	[SerializeField]
	private Animator _faceAnimator;

	[SerializeField]
	private Human _human;

	public void DoEmote(EmoteData data)
	{
		switch (data.EmoteType)
		{
		case EmoteType.Body:
			SetAnimatorParam(_animator, EmoteIndexHash, data);
			break;
		case EmoteType.Face:
			SetAnimatorParam(_faceAnimator, EmoteFaceIndexHash, data);
			break;
		}
	}

	public void OnEmote(EmoteData data)
	{
		DoEmote(data);
		AnimationEmoteMessage animationEmoteMessage = new AnimationEmoteMessage
		{
			AnimationIndex = data.AnimationIndex,
			HumanNetId = _human.netId,
			Delay = data.ResetDelay,
			EmoteType = (byte)data.EmoteType
		};
		if (NetworkManager.IsClient)
		{
			animationEmoteMessage.SendToServer();
		}
		else if (NetworkManager.IsServer)
		{
			animationEmoteMessage.SendToClients();
		}
	}

	private void SetAnimatorParam(Animator animator, int parameterIndex, EmoteData data)
	{
		animator.SetInteger(parameterIndex, data.AnimationIndex);
		ResetAnimationParam(animator, data.ResetDelay, parameterIndex).Forget();
	}

	private async UniTaskVoid ResetAnimationParam(Animator animator, int time, int parameterIndex)
	{
		await UniTask.Delay(time);
		animator.SetInteger(parameterIndex, 0);
	}

	public void SetExpression()
	{
		InventoryManager.ParentHuman.CosmeticsBehaviour.SetExpression(BlendShapeType.Happy, tween: true);
	}
}
