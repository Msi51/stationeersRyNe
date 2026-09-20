using System.Text;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Objects.RoboticArm;

public class RoboticArmDockCargo : RoboticArmDock, IProxySlot
{
	[SerializeField]
	private DialKnob _slotIndexKnob;

	[SerializeField]
	private MeshRenderer iconRenderer;

	private const int MAX_SLOT_INDEX = 50;

	private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");

	private ILogicable _targetLogicable;

	public const int PROXY_SLOT_ID = 255;

	private int _currentSlotIndex;

	private Slot HandSlot => Slots[0];

	private ILogicable TargetLogicable
	{
		get
		{
			return _targetLogicable;
		}
		set
		{
			_targetLogicable = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 1024;
			}
			RefreshIcon();
		}
	}

	private int CurrentSlotIndex
	{
		get
		{
			return _currentSlotIndex;
		}
		set
		{
			_currentSlotIndex = value;
			UpdateKnobPosition();
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	protected override void AnimateDownFinished()
	{
		if (GameManager.RunSimulation)
		{
			DoContextualAction();
		}
	}

	private async UniTaskVoid WaitThenSetActivate()
	{
		await UniTask.Delay(200);
		OnServer.Interact(base.InteractActivate, 1);
	}

	private void DoContextualAction()
	{
		SmallCell armInteractionCell = GetArmInteractionCell();
		if (armInteractionCell?.Device == null || armInteractionCell.Device.Slots.Count <= CurrentSlotIndex)
		{
			WaitThenSetActivate().Forget();
			return;
		}
		Slot slot = armInteractionCell.Device.Slots[CurrentSlotIndex];
		if (!CanAccessSlot(slot))
		{
			WaitThenSetActivate().Forget();
			return;
		}
		if (HandSlot.Contains<DynamicThing>(out var occupant))
		{
			DoHandOccupied(slot, occupant);
		}
		else
		{
			DoHandEmpty(slot);
		}
		WaitThenSetActivate().Forget();
	}

	private void DoHandOccupied(Slot slot, DynamicThing inHand)
	{
		if (slot.IsAllowedType(inHand))
		{
			if (slot.IsEmpty())
			{
				OnServer.MoveToSlot(inHand, slot);
			}
			else if (slot.IsSwappable)
			{
				OnServer.SwapSlots(slot.Parent.ReferenceId, base.ReferenceId, slot.SlotIndex, HandSlot.SlotIndex);
			}
		}
	}

	private void DoHandEmpty(Slot slot)
	{
		if (!slot.IsEmpty())
		{
			OnServer.MoveToSlot(slot.Get(), HandSlot);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		UpdateKnobPosition();
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		UpdateKnobPosition();
	}

	private void UpdateKnobPosition()
	{
		_slotIndexKnob.SetState((float)CurrentSlotIndex / 50f);
	}

	public void RefreshIcon()
	{
		if (!IsCursor)
		{
			AwaitToggleIcon().Forget();
		}
	}

	private async UniTaskVoid AwaitToggleIcon()
	{
		await UniTask.SwitchToMainThread();
		SetIcon();
	}

	private void SetIcon()
	{
		if ((bool)iconRenderer && !base.BeingDestroyed && base.CurrentBuildStateIndex >= 0)
		{
			Slot slot = GetSlot(255);
			iconRenderer.transform.gameObject.SetActive(slot != null && slot.IsNotEmpty() && CanAccessSlot(slot));
			if (slot != null && slot.IsNotEmpty())
			{
				Material material = iconRenderer.material;
				material.mainTexture = slot.Get().GetThumbnail().texture;
				iconRenderer.material.SetTexture(EmissionMap, material.mainTexture);
			}
		}
	}

	private bool CanAccessSlot(Slot slot)
	{
		if (slot == null)
		{
			return false;
		}
		if (slot.Type == Slot.Class.Plant)
		{
			return false;
		}
		if (slot.IsInteractable && !slot.IsLocked)
		{
			return !slot.HidesOccupant;
		}
		return false;
	}

	protected override void SetTargetSmallGrid()
	{
		if (base.CurrentBypass != null)
		{
			TargetLogicable = null;
		}
		else
		{
			TargetLogicable = GetArmInteractionCell()?.Device;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.TargetSlotIndex => true, 
			LogicType.TargetPrefabHash => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.TargetSlotIndex)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.TargetSlotIndex)
		{
			CurrentSlotIndex = Mathf.Clamp((int)value, 0, 50);
		}
		else
		{
			base.SetLogicValue(logicType, value);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.TargetSlotIndex => CurrentSlotIndex, 
			LogicType.TargetPrefabHash => ((double?)TargetLogicable?.GetPrefabHash()) ?? 0.0, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override int GetNextSlotId(int slotIndex, bool isForward)
	{
		if (Slots == null)
		{
			return -1;
		}
		if (Slots.Count == 0)
		{
			return 0;
		}
		int num = slotIndex;
		if (num == 255)
		{
			if (!isForward)
			{
				return Slots.Count - 1;
			}
			return 0;
		}
		num += (isForward ? 1 : (-1));
		if (num < 0 || num >= Slots.Count)
		{
			num = 255;
		}
		return num;
	}

	public override Slot GetSlot(int slotIndex)
	{
		Slot slot;
		if (slotIndex == 255)
		{
			ILogicable targetLogicable = TargetLogicable;
			if (targetLogicable != null && targetLogicable.HasAnySlots)
			{
				slot = TargetLogicable.GetSlot(CurrentSlotIndex);
				if (!CanAccessSlot(slot))
				{
					return null;
				}
				return slot;
			}
		}
		slot = base.GetSlot(slotIndex);
		if (!CanAccessSlot(slot))
		{
			return null;
		}
		return slot;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		Slot slot = TargetLogicable?.GetSlot(CurrentSlotIndex);
		if (!CanAccessSlot(slot))
		{
			slot = null;
		}
		string arg = ((slot != null) ? slot.DisplayName.AsColor("yellow") : ((string)GameStrings.None));
		string arg2 = (slot?.Get<DynamicThing>())?.ToTooltip() ?? "";
		GameStrings.TargetSlotInfo.AppendFormat(extendedText, arg, arg2);
		return extendedText;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		return interactable.Action switch
		{
			InteractableType.Button4 => HandleDecrementIndex(interactable, doAction), 
			InteractableType.Button5 => HandleIncrementIndex(interactable, doAction), 
			_ => base.InteractWith(interactable, interaction, doAction), 
		};
	}

	private DelayedActionInstance HandleIncrementIndex(Interactable interactable, bool doAction)
	{
		return ChangeSlotIndex(interactable, doAction, 1);
	}

	private DelayedActionInstance HandleDecrementIndex(Interactable interactable, bool doAction)
	{
		return ChangeSlotIndex(interactable, doAction, -1);
	}

	private DelayedActionInstance ChangeSlotIndex(Interactable interactable, bool doAction, int increment)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (!OnOff)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
		}
		if (!Powered)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
		}
		if (Activate == 1)
		{
			return delayedActionInstance.Fail(GameStrings.RoboticArmBusy);
		}
		if (doAction && GameManager.RunSimulation)
		{
			CurrentSlotIndex = Mathf.Clamp(CurrentSlotIndex + increment, 0, 50);
		}
		delayedActionInstance.ActionMessage = GameStrings.RoboticArmCargoSetSlotIndex.AsString(StringManager.Get(CurrentSlotIndex));
		return delayedActionInstance.Succeed();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RoboticArmDockCargoSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		if (thingSaveData is RoboticArmDockCargoSaveData roboticArmDockCargoSaveData)
		{
			roboticArmDockCargoSaveData.CurrentSlotIndex = CurrentSlotIndex;
		}
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		if (thingSaveData is RoboticArmDockCargoSaveData roboticArmDockCargoSaveData)
		{
			_currentSlotIndex = roboticArmDockCargoSaveData.CurrentSlotIndex;
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)CurrentSlotIndex);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		CurrentSlotIndex = reader.ReadByte();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteByte((byte)CurrentSlotIndex);
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Network.WritePackedId(writer, TargetLogicable);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			CurrentSlotIndex = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Network.ReadPackedId(reader, out var referenceId);
			TargetLogicable = Thing.Find<ILogicable>(referenceId);
		}
	}
}
