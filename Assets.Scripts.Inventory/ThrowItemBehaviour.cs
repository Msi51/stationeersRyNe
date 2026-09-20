using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Pipes;
using Sound;
using UI;
using UnityEngine;

namespace Assets.Scripts.Inventory;

public class ThrowItemBehaviour : MonoBehaviour
{
	[SerializeField]
	private Human _human;

	[SerializeField]
	private Animator _parentAnimator;

	[SerializeField]
	[ReadOnly]
	private float _throwForce;

	[SerializeField]
	private float _maxThrowForce = 6f;

	private UIProgressionBar _uiProgression;

	private PooledAudioSource _throwingAudio;

	private bool _canThrow;

	private static bool IsUsingSmartTool
	{
		get
		{
			if ((bool)InventoryManager.Instance)
			{
				return InventoryManager.Instance.IsUsingSmartTool;
			}
			return false;
		}
	}

	private static SlotDisplay ActiveHand
	{
		get
		{
			if (!InventoryManager.Instance)
			{
				return null;
			}
			return InventoryManager.Instance.ActiveHand;
		}
	}

	private float ThrowPercent => _throwForce / _maxThrowForce;

	private void Start()
	{
		_uiProgression = InventoryManager.Instance.UIProgressionBar;
	}

	public void DropKeyDown()
	{
		_canThrow = true;
		if (_human.ExitAnything() || IsUsingSmartTool || _human.HasRecentParent || !ActiveHand.Slot.Occupant)
		{
			return;
		}
		if (ActiveHand.Slot.Occupant is IDraggable)
		{
			_throwForce = 0f;
			Throw();
			return;
		}
		_throwForce = 0f;
		_parentAnimator.SetBool(MovementController.ThrowingHash, value: true);
		_uiProgression.SetVisible(isVisble: true);
		if ((bool)_throwingAudio)
		{
			_throwingAudio.Stop(UIAudioManager.ThrowingHash);
		}
		_throwingAudio = UIAudioManager.Play(UIAudioManager.ThrowingHash);
	}

	public void DropKeyUp()
	{
		if (IsUsingSmartTool || _human.HasRecentParent || !ActiveHand.Slot.Occupant)
		{
			ClearThrowState();
		}
		else if (_canThrow)
		{
			Throw();
		}
	}

	private void Throw()
	{
		OnServer.MoveToWorld(ActiveHand.Slot.Get(), _throwForce);
		InventoryManager.Instance.CheckCancelMultiConstructor();
		_parentAnimator.SetBool(MovementController.ThrowingHash, value: false);
		_uiProgression.SetVisible(isVisble: false);
		if ((bool)_throwingAudio)
		{
			_throwingAudio.Stop();
		}
		_throwingAudio = null;
		if (ThrowPercent < 0.4f)
		{
			UIAudioManager.Play(UIAudioManager.ThrowSmallHash);
		}
		else if (ThrowPercent < 0.7f)
		{
			UIAudioManager.Play(UIAudioManager.ThrowMediumHash);
		}
		else
		{
			UIAudioManager.Play(UIAudioManager.ThrowBigHash);
		}
	}

	public void DropKeyHeld()
	{
		HandleDropOfCryoTube();
		if (IsUsingSmartTool || !(ActiveHand.Slot.Occupant is Item) || _human.HasRecentParent)
		{
			_canThrow = false;
			ClearThrowState();
		}
		else if (_canThrow)
		{
			_throwForce += Time.deltaTime * _maxThrowForce;
			if (_throwForce > _maxThrowForce)
			{
				_throwForce = _maxThrowForce;
			}
			_uiProgression.SetProgress(ThrowPercent, "Throw", ActiveHand.Slot.Occupant.DisplayName);
		}
	}

	private void ClearThrowState()
	{
		_parentAnimator.SetBool(MovementController.ThrowingHash, value: false);
		_uiProgression.SetVisible(isVisble: false);
		if ((bool)_throwingAudio)
		{
			_throwingAudio.Stop();
			_throwingAudio = null;
		}
	}

	private void HandleDropOfCryoTube()
	{
		if (_human.IsChild && _human.ParentSlot.Parent is CryoTube)
		{
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(new RequestInteractionToServer
				{
					InteractThingId = _human.ParentSlot.Parent.netId,
					InteractionId = _human.ParentSlot.Parent.InteractOpen.InteractableId,
					NewState = 1
				});
			}
			else
			{
				OnServer.Interact(_human.ParentSlot.Parent.InteractOpen, 1);
			}
		}
	}
}
