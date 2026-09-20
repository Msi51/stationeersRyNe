using System.Text;
using System.Threading;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Tablet : PowerTool
{
	[ReadOnly]
	public Cartridge Cartridge;

	public GameObject Screen;

	private static float ZOffset = -2f;

	private Transform _screenTransform;

	private UniTask _inputHandler;

	private static readonly int EquipTabletHash = Animator.StringToHash("EquipTablet");

	private static readonly int UnEquipTabletHash = Animator.StringToHash("UnEquipTablet");

	private Slot _cartridgeSlot;

	private float _scrollData;

	public static readonly int ScrollUpHash = Animator.StringToHash("ScrollUp");

	public static readonly int ScrollDownHash = Animator.StringToHash("ScrollDown");

	private readonly DensePoolReference<ICircuitHolder> _circuitHolderPool = new DensePoolReference<ICircuitHolder>(CircuitHolders.AllCircuitHolders);

	private Slot CartridgeSlot => _cartridgeSlot;

	public float ActionTime
	{
		get
		{
			if (!Cartridge)
			{
				return 0f;
			}
			return Cartridge.ScanTime;
		}
	}

	public override int EquipSoundHash => EquipTabletHash;

	public override int UnEquipSoundHash => UnEquipTabletHash;

	private bool InUse
	{
		get
		{
			if (RootParent.HasAuthority && (bool)Cartridge && OnOff && IsOperable)
			{
				return InventoryManager.ActiveHandSlot?.Occupant == this;
			}
			return false;
		}
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (_cartridgeSlot.IsNotEmpty())
		{
			extendedText.AppendLine(GameStrings.SlotContainsItem.AsString(_cartridgeSlot.ToTooltip(), _cartridgeSlot.Get().ToTooltip()));
		}
		return extendedText;
	}

	public override void Awake()
	{
		base.Awake();
		_screenTransform = Screen.GetComponent<Transform>();
		_cartridgeSlot = Slots.Find((Slot s) => s.Type == Slot.Class.Cartridge);
		if (!IsCursor && this is ICircuitHolder iCircuitHolder)
		{
			CircuitHolders.Register(iCircuitHolder);
		}
	}

	public override bool OnAddToPool(object densePool, int slot)
	{
		if (_circuitHolderPool.CanAddToPool(densePool))
		{
			return _circuitHolderPool.AddToPool(densePool, slot);
		}
		return base.OnAddToPool(densePool, slot);
	}

	public override void OnRemoveFromPool(object densePool)
	{
		base.OnRemoveFromPool(densePool);
		_circuitHolderPool.OnRemovedFrom(densePool);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsCursor && this is ICircuitHolder iCircuitHolder)
		{
			CircuitHolders.Deregister(iCircuitHolder);
		}
	}

	public void InActiveHand()
	{
		InputHandling();
	}

	private async UniTask HandleScreenInput()
	{
		if (!Cartridge)
		{
			return;
		}
		CancellationToken cancel = base.GameObject.GetCancellationTokenOnDestroy();
		while (InUse)
		{
			_scrollData = CursorManager.ScrollbarData.y;
			if (_scrollData > 0f)
			{
				Cartridge.OnTabletScrollUp();
			}
			if (_scrollData < 0f)
			{
				Cartridge.OnTabletScrollDown();
			}
			Cartridge.OnScroll(CursorManager.ScrollbarData);
			await UniTask.NextFrame(cancel);
			if (base.BeingDestroyed || cancel.IsCancellationRequested)
			{
				break;
			}
		}
	}

	public override void OnStartRender()
	{
		base.OnStartRender();
		CheckScreen();
	}

	public override void OnStopRender()
	{
		base.OnStopRender();
		CheckScreen();
	}

	public void TransferScreen()
	{
		if ((bool)Cartridge)
		{
			Transform obj = Cartridge.RectTransform.transform;
			obj.SetParent(_screenTransform.transform, worldPositionStays: false);
			obj.localRotation = Quaternion.identity;
			Vector3 zero = Vector3.zero;
			zero.z = ZOffset;
			obj.localPosition = zero;
			obj.localScale = Vector3.one;
			Cartridge.RectTransform.offsetMin = Vector2.zero;
			Cartridge.RectTransform.offsetMax = Vector2.zero;
		}
	}

	public void RemoveScreen(Cartridge oldCartridge)
	{
		if ((bool)oldCartridge && !base.BeingDestroyed && !oldCartridge.BeingDestroyed)
		{
			oldCartridge.RectTransform.transform.SetParent(oldCartridge.ThingTransform, worldPositionStays: false);
			oldCartridge.RectTransform.transform.localRotation = Quaternion.identity;
			oldCartridge.RectTransform.transform.localPosition = Vector3.zero;
			oldCartridge.RectTransform.transform.localScale = Vector3.one;
		}
	}

	protected void InputHandling()
	{
		if (_inputHandler.Status != UniTaskStatus.Pending && InUse)
		{
			_inputHandler = HandleScreenInput();
		}
	}

	public override void OnParentLocalityChange()
	{
		base.OnParentLocalityChange();
		CheckError();
		CheckScreen();
		InputHandling();
	}

	public virtual void CheckError()
	{
		if (GameManager.RunSimulation)
		{
			if (Cartridge == null && Error == 0)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if ((bool)Cartridge && Error == 1)
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
	}

	protected void CheckScreen()
	{
		if (Screen != null)
		{
			Screen.SetActive(!IsOccluded && OnOff && Powered);
		}
	}

	public override void SetVisibility(bool isVisible, bool hideOnPlayer = false, bool isRecursive = false, bool shouldUpdateLayers = true)
	{
		base.SetVisibility(isVisible, hideOnPlayer, isRecursive, shouldUpdateLayers);
		if (Screen != null)
		{
			Screen.SetActive(isVisible && OnOff && Powered);
		}
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		InputHandling();
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		Cartridge cartridge = newChild as Cartridge;
		if (cartridge != null && Cartridge == null)
		{
			Cartridge = cartridge;
		}
		CheckError();
		InputHandling();
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		CheckError();
		CheckScreen();
		InputHandling();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (previousChild == Cartridge)
		{
			Cartridge = null;
			CheckError();
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		CheckError();
		CheckScreen();
		if (interactable.Action == InteractableType.OnOff && OnOff)
		{
			InputHandling();
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 1f
		};
		Cartridge cartridge = attack.SourceItem as Cartridge;
		if ((bool)cartridge)
		{
			delayedActionInstance.ActionMessage = ActionStrings.Insert;
			delayedActionInstance.OverrideTitle = attack.SourceItem.GetPassiveTooltip(null).Title;
			if (!base.AllowInteraction)
			{
				return delayedActionInstance.Fail(GameStrings.ThingCanNotInsertInteractionsDisabled, attack.SourceItem.ToTooltip(), base.Battery.ToTooltip());
			}
			if ((bool)Cartridge)
			{
				return delayedActionInstance.Fail(GameStrings.ThingCanNotInsertAlreadyContains, attack.SourceItem.ToTooltip(), Cartridge.ToTooltip());
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.MoveToSlot(cartridge, CartridgeSlot);
			}
			return delayedActionInstance.Succeed();
		}
		if ((bool)(attack.SourceItem as Screwdriver))
		{
			delayedActionInstance.ActionMessage = ActionStrings.Remove;
			delayedActionInstance.OverrideTitle = (CartridgeSlot.Occupant ? CartridgeSlot.Occupant.GetPassiveTooltip(null).Title : CartridgeSlot.GetSafeName());
			if (!Cartridge && (bool)base.Battery)
			{
				return base.AttackWith(attack, doAction);
			}
			if (!Cartridge)
			{
				return delayedActionInstance.Fail(GameStrings.CartridgeNoCartridgeInSlot, CartridgeSlot.ToTooltip(), CartridgeSlot.TypeTooltip());
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.MoveToSlotOrWorld(Cartridge, attack.OtherHand);
			}
			return delayedActionInstance.Succeed();
		}
		return base.AttackWith(attack, doAction);
	}

	public void Scan(Thing thing)
	{
		if ((bool)Cartridge && OnOff && IsOperable)
		{
			Cartridge.OnTabletScanned(thing);
		}
	}

	public void OnDocked()
	{
		foreach (Interactable interactable in Interactables)
		{
			if (interactable != null && !(interactable.Collider == null))
			{
				interactable.Collider.enabled = true;
			}
		}
	}
}
