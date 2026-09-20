using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects;

[Serializable]
public class Slot : SlotDisplayBase
{
	public delegate void Event();

	public enum Class : ushort
	{
		[XmlEnum("None")]
		None,
		[XmlEnum("Helmet")]
		Helmet,
		[XmlEnum("Suit")]
		Suit,
		[XmlEnum("Back")]
		Back,
		[XmlEnum("GasFilter")]
		GasFilter,
		[XmlEnum("GasCanister")]
		GasCanister,
		[XmlEnum("Motherboard")]
		Motherboard,
		[XmlEnum("Circuitboard")]
		Circuitboard,
		[XmlEnum("DataDisk")]
		DataDisk,
		[XmlEnum("Organ")]
		Organ,
		[XmlEnum("Ore")]
		Ore,
		[XmlEnum("Plant")]
		Plant,
		[XmlEnum("Uniform")]
		Uniform,
		[XmlEnum("Entity")]
		Entity,
		[XmlEnum("Battery")]
		Battery,
		[XmlEnum("Egg")]
		Egg,
		[XmlEnum("Belt")]
		Belt,
		[XmlEnum("Tool")]
		Tool,
		[XmlEnum("Appliance")]
		Appliance,
		[XmlEnum("Ingot")]
		Ingot,
		[XmlEnum("Torpedo")]
		Torpedo,
		[XmlEnum("Cartridge")]
		Cartridge,
		[XmlEnum("AccessCard")]
		AccessCard,
		[XmlEnum("Magazine")]
		Magazine,
		[XmlEnum("Circuit")]
		Circuit,
		[XmlEnum("Bottle")]
		Bottle,
		[XmlEnum("ProgrammableChip")]
		ProgrammableChip,
		[XmlEnum("Glasses")]
		Glasses,
		[XmlEnum("CreditCard")]
		CreditCard,
		[XmlEnum("DirtCanister")]
		DirtCanister,
		[XmlEnum("SensorProcessingUnit")]
		SensorProcessingUnit,
		[XmlEnum("LiquidCanister")]
		LiquidCanister,
		[XmlEnum("LiquidBottle")]
		LiquidBottle,
		[XmlEnum("Wreckage")]
		Wreckage,
		[XmlEnum("SoundCartridge")]
		SoundCartridge,
		[XmlEnum("DrillHead")]
		DrillHead,
		[XmlEnum("ScanningHead")]
		ScanningHead,
		[XmlEnum("Flare")]
		Flare,
		[XmlEnum("Blocked")]
		Blocked,
		[XmlEnum("SuitMod")]
		SuitMod,
		[XmlEnum("Crate")]
		Crate,
		[XmlEnum("Portables")]
		Portables,
		[XmlEnum("RocketPayload")]
		RocketPayload,
		[XmlEnum("AutoInjector")]
		AutoInjector
	}

	public static int ProgrammableChipHash = Animator.StringToHash("ProgrammableChip");

	public static int LeftHandHash = Animator.StringToHash("LeftHand");

	public static int RightHandHash = Animator.StringToHash("RightHand");

	public static int UniformHash = Animator.StringToHash("Uniform");

	public static int LungsHash = Animator.StringToHash("Lungs");

	public static int BrainHash = Animator.StringToHash("Brain");

	public static int StomachHash = Animator.StringToHash("Stomach");

	public static int CaptainSeatHash = Animator.StringToHash("CaptainsSeat");

	private DynamicThing _dynamicThing;

	public string StringKey;

	[ReadOnly]
	public int StringHash;

	public Class Type;

	[Tooltip("When entries exist, things must match the Type and also be one of these hashes.")]
	public int[] SpecificTypePrefabHashes = Array.Empty<int>();

	public Transform Location;

	[ReadOnly]
	public Thing Parent;

	public MovementController.Mode EntityControlMode = MovementController.Mode.Seated;

	[Tooltip("Whether the slot should use the internal atmosphere for its parent")]
	public bool UseInternalAtmosphere;

	[Tooltip("Will the contents of the slot retain its realworld scale?")]
	public bool RealWorldScale;

	[Tooltip("It will scale the size of the item in the slot")]
	public float ScaleMultiplier = 1f;

	[Tooltip("Will the contents of the slot cast shadows?")]
	public bool OccupantCastsShadows = true;

	[FormerlySerializedAs("IsHidden")]
	[Tooltip("Will the contents of the slot be shown?")]
	public bool HidesOccupant;

	[Tooltip("Will the contents of the slot be shown when sitting down?")]
	public bool IsHiddenInSeat;

	[Tooltip("Can the slot be interacted with via the interaction system?")]
	public bool IsInteractable = true;

	[Tooltip("Can the slot be interacted to swap items?")]
	public bool IsSwappable = true;

	[Tooltip("Can the slot be used to drag objects?")]
	public bool AllowDragging;

	[Tooltip("Is the slot locked?")]
	public bool IsLocked;

	public InteractableType Action;

	[NonSerialized]
	[ReadOnly]
	public Interactable Interactable;

	[NonSerialized]
	[ReadOnly]
	public Sprite SlotTypeIcon;

	public BoxCollider Collider;

	[ReadOnly]
	public Vector3 Size;

	private int _index = -1;

	private static Dictionary<int, Sprite> _slotTypeLookup = new Dictionary<int, Sprite>();

	public bool OccupantAlwaysVisible { get; set; }

	[Obsolete("Use Get(), Get<T>() or Contains<T>() instead")]
	public DynamicThing Occupant => _dynamicThing;

	public bool IsHandSlot
	{
		get
		{
			if (StringHash != LeftHandHash)
			{
				return StringHash == RightHandHash;
			}
			return true;
		}
	}

	public string DisplayName
	{
		get
		{
			if (StringHash == 0)
			{
				return string.Empty;
			}
			return Localization.GetName(this);
		}
	}

	public int SlotIndex
	{
		get
		{
			if (Parent == null)
			{
				return -1;
			}
			if (_index < 0)
			{
				_index = Parent.Slots.IndexOf(this);
			}
			return _index;
		}
	}

	public SlotDisplayButton Button
	{
		get
		{
			if (Display == null)
			{
				return null;
			}
			return Display.SlotDisplayButton;
		}
	}

	public bool IsSortable
	{
		get
		{
			Class type = Type;
			return type == Class.None || type == Class.Ore;
		}
	}

	public event Event OnOccupantChange;

	public event Event OnPlayerInventoryWindow;

	public event Event OnEnter;

	public event Event OnExit;

	public DynamicThing Get()
	{
		return _dynamicThing;
	}

	public T Get<T>() where T : IReferencable
	{
		DynamicThing dynamicThing = _dynamicThing;
		if (dynamicThing is T)
		{
			return (T)(object)((dynamicThing is T) ? dynamicThing : null);
		}
		return default(T);
	}

	public bool Contains<T>() where T : IReferencable
	{
		return _dynamicThing is T;
	}

	public bool Contains<T>(out T occupant) where T : IReferencable
	{
		if (!(_dynamicThing is T val))
		{
			occupant = default(T);
			return false;
		}
		occupant = val;
		return true;
	}

	public bool Contains<T>(T occupant) where T : IReferencable
	{
		if ((object)_dynamicThing == null || occupant == null)
		{
			return false;
		}
		if (_dynamicThing is T val)
		{
			return val.ReferenceId == occupant.ReferenceId;
		}
		return false;
	}

	public static bool Contains<T>(Slot slot1, Slot slot2) where T : IReferencable
	{
		if (!slot1.Contains<T>())
		{
			return slot2.Contains<T>();
		}
		return true;
	}

	public static bool Contains<T>(Slot slot1, Slot slot2, Slot slot3) where T : IReferencable
	{
		if (!slot1.Contains<T>() && !slot2.Contains<T>())
		{
			return slot3.Contains<T>();
		}
		return true;
	}

	public static bool Contains<T>(Slot slot1, Slot slot2, Slot slot3, Slot slot4) where T : IReferencable
	{
		if (!slot1.Contains<T>() && !slot2.Contains<T>() && !slot3.Contains<T>())
		{
			return slot4.Contains<T>();
		}
		return true;
	}

	public static bool Contains<T>(List<Slot> slots) where T : IReferencable
	{
		if (slots == null)
		{
			return false;
		}
		foreach (Slot slot in slots)
		{
			if (slot.Contains<T>())
			{
				return true;
			}
		}
		return false;
	}

	public void Empty()
	{
		Take(null);
	}

	public void Take([NotNull] DynamicThing child)
	{
		if (NetworkManager.IsServer)
		{
			if ((bool)child)
			{
				child.NetworkUpdateFlags |= 1;
			}
			if (child != _dynamicThing && (bool)_dynamicThing)
			{
				_dynamicThing.NetworkUpdateFlags |= 1;
			}
		}
		if (!(_dynamicThing == child))
		{
			_dynamicThing = child;
			this.OnOccupantChange?.Invoke();
		}
	}

	public bool IsNotEmpty()
	{
		return (object)_dynamicThing != null;
	}

	public bool IsEmpty()
	{
		return (object)_dynamicThing == null;
	}

	public void ParentWindowVisibilityChange()
	{
		this.OnPlayerInventoryWindow?.Invoke();
	}

	public static bool CanInsert(DynamicThing thing, Slot destinationSlot)
	{
		if (thing == null || destinationSlot == null || destinationSlot.IsLocked || !destinationSlot.Occupant)
		{
			return false;
		}
		if (thing.SlotType == destinationSlot.Type || destinationSlot.Occupant.PrefabHash == thing.PrefabHash)
		{
			return false;
		}
		if (!destinationSlot.Occupant.HasSlots)
		{
			return false;
		}
		foreach (Slot slot in destinationSlot.Occupant.Slots)
		{
			if (AllowMove(thing, slot))
			{
				return true;
			}
		}
		return false;
	}

	public void PlaySlotEnterUiSound()
	{
		if (GameManager.GameState != GameState.Running)
		{
			return;
		}
		switch (Type)
		{
		case Class.Helmet:
			UIAudioManager.Play(UIAudioManager.UiEquipHelmetHash);
			return;
		case Class.Suit:
			UIAudioManager.Play(UIAudioManager.UiEquipSuitHash);
			return;
		case Class.Back:
			UIAudioManager.Play(UIAudioManager.UiEquipBackHash);
			return;
		case Class.Uniform:
			UIAudioManager.Play(UIAudioManager.UiEquipUniformHash);
			return;
		case Class.Belt:
			UIAudioManager.Play(UIAudioManager.UiEquipBeltHash);
			return;
		case Class.Glasses:
			UIAudioManager.Play(UIAudioManager.UiEquipGlassesHash);
			return;
		case Class.Battery:
			UIAudioManager.Play(UIAudioManager.InstallBatteryHash);
			return;
		case Class.GasFilter:
			UIAudioManager.Play(UIAudioManager.InstallFilterHash);
			return;
		case Class.GasCanister:
			UIAudioManager.Play(UIAudioManager.InstallCanisterHash);
			return;
		case Class.LiquidCanister:
			UIAudioManager.Play(UIAudioManager.InstallLiquidCanisterHash);
			return;
		case Class.LiquidBottle:
			UIAudioManager.Play(UIAudioManager.InstallLiquidCanisterHash);
			return;
		}
		Human rootParentHuman = Parent.RootParentHuman;
		if (rootParentHuman != null && rootParentHuman.IsLocalPlayer)
		{
			if (IsHandSlot)
			{
				UIAudioManager.Play(UIAudioManager.UiObjectIntoHandHash);
			}
			else
			{
				UIAudioManager.Play(UIAudioManager.LocalPlayerSlotEnterSoundHash);
			}
		}
		else
		{
			UIAudioManager.Play(UIAudioManager.SlotEnterSoundHash);
		}
	}

	public static bool CanMerge(DynamicThing source, Slot destinationSlot)
	{
		if (destinationSlot.IsLocked)
		{
			return false;
		}
		if (destinationSlot.Occupant.PrefabHash != source.PrefabHash)
		{
			if (!(destinationSlot.Occupant is MiningBelt miningBelt))
			{
				return false;
			}
			return miningBelt.CanMergeAsOre(source);
		}
		if (source is IMergeable mergeable && destinationSlot.Contains<IMergeable>(out var occupant))
		{
			return mergeable.CanStack(occupant);
		}
		return false;
	}

	public static bool AllowSwap(Slot sourceSlot, DynamicThing destination)
	{
		if (sourceSlot.IsLocked)
		{
			return false;
		}
		if (!sourceSlot.Occupant && !destination)
		{
			return false;
		}
		if ((bool)sourceSlot.Occupant && sourceSlot.Occupant is DraggableThing)
		{
			return false;
		}
		CanEnterResult canEnterResult = destination.CanEnter(sourceSlot);
		bool flag = sourceSlot.Type == Class.None || sourceSlot.Type == destination.SlotType;
		if (!canEnterResult || !flag)
		{
			return false;
		}
		return true;
	}

	public static bool AllowSwap(Slot sourceSlot, Slot destinationSlot)
	{
		if (sourceSlot.IsLocked || destinationSlot.IsLocked)
		{
			return false;
		}
		if (!sourceSlot.Occupant && !destinationSlot.Occupant)
		{
			return false;
		}
		if ((bool)sourceSlot.Occupant)
		{
			CanEnterResult canEnterResult = sourceSlot.Occupant.CanEnter(destinationSlot);
			bool flag = destinationSlot.Type == Class.None || destinationSlot.Type == sourceSlot.Occupant.SlotType;
			if (destinationSlot.Occupant is MiningBelt && sourceSlot.Occupant is Ore)
			{
				flag = true;
			}
			bool flag2 = sourceSlot.Occupant is DraggableThing;
			if (!canEnterResult || !flag || flag2)
			{
				return false;
			}
		}
		if ((bool)destinationSlot.Occupant)
		{
			CanEnterResult canEnterResult2 = destinationSlot.Occupant.CanEnter(sourceSlot);
			bool flag3 = sourceSlot.Type == Class.None || sourceSlot.Type == destinationSlot.Occupant.SlotType;
			if (!canEnterResult2 || !flag3)
			{
				return false;
			}
		}
		return true;
	}

	public static bool AllowMove(DynamicThing thing, Slot destinationSlot)
	{
		if (!thing || destinationSlot == null)
		{
			return false;
		}
		if (destinationSlot.IsLocked || (bool)destinationSlot.Occupant)
		{
			return false;
		}
		CanEnterResult canEnterResult = thing.CanEnter(destinationSlot);
		bool flag = destinationSlot.Type == Class.None || destinationSlot.Type == thing.SlotType;
		bool flag2 = thing is DraggableThing;
		if (!canEnterResult || !flag || flag2)
		{
			return false;
		}
		return true;
	}

	public void OnEnterEvent()
	{
		this.OnEnter?.Invoke();
		if (InventoryManager.ActiveHandSlot != null)
		{
			InventoryManager.OnActiveEvent();
		}
	}

	public override string ToString()
	{
		return GetSafeName();
	}

	public void OnExitEvent()
	{
		if (this.OnExit != null)
		{
			this.OnExit();
		}
	}

	public string ToTooltip()
	{
		return "<color=yellow>" + GetSafeName() + "</color>";
	}

	public string TypeTooltip()
	{
		return "<color=yellow>" + EnumCollections.SlotClasses.GetName(Type) + "</color>";
	}

	public string GetSafeName()
	{
		if (StringHash != 0)
		{
			return DisplayName;
		}
		if (Interactable != null)
		{
			return Interactable.ContextualName;
		}
		return EnumCollections.InteractableTypes.GetName(Action);
	}

	public bool IsAllowedType(DynamicThing dynamicThing)
	{
		if (dynamicThing == null)
		{
			return false;
		}
		if (Parent is DynamicGasCanister dynamicGasCanister && dynamicThing is GasCanister gasCanister)
		{
			return dynamicGasCanister.IsAllowed(gasCanister);
		}
		if (Type != Class.None)
		{
			return Type == dynamicThing.SlotType;
		}
		return true;
	}

	public void RefreshState()
	{
		if (Display != null)
		{
			Display.RefreshState();
		}
	}

	public void RefreshQuantity()
	{
		if (Display != null)
		{
			Display.RefreshQuantity().Forget();
		}
	}

	public void RefreshDamage()
	{
		if (Display != null)
		{
			Display.RefreshDamage().Forget();
		}
	}

	public void RefreshSlotDisplay()
	{
		if (Display != null && !(Parent == null) && !(Parent.RootParent == null) && this == Display.Slot)
		{
			Display.RefreshDisplay();
		}
	}

	public static void PopulateSlotTypeSprites()
	{
		Sprite[] array = Resources.LoadAll<Sprite>("UI/SlotTypes");
		if (array == null || array.Length == 0)
		{
			return;
		}
		foreach (Class value in EnumUtil.GetValues<Class>())
		{
			string text = $"sloticon-{value}".ToLower();
			Sprite[] array2 = array;
			foreach (Sprite sprite in array2)
			{
				if (sprite.name == text)
				{
					_slotTypeLookup.Add((int)value, sprite);
				}
			}
		}
	}

	public static Sprite GetSlotTypeSprite(Class slotType)
	{
		_slotTypeLookup.TryGetValue((int)slotType, out var value);
		return value;
	}

	public void Initialize()
	{
		SlotTypeIcon = GetSlotTypeSprite(Type);
	}

	public void PlayerMoveToSlot(DynamicThing thingToMove)
	{
		InventoryManager.Instance.CheckCancelMultiConstructor();
		DoSwapActiveHand();
		Slot parentSlot = thingToMove.ParentSlot;
		Slot slot = Parent.Slots[SlotIndex];
		OnServer.MoveToSlot(thingToMove, slot);
		PlaySlotEnterUiSound();
		if (KeyManager.GetButton(KeyMap.MoveAll))
		{
			TryMoveAll(thingToMove, parentSlot, slot);
		}
		else if (KeyManager.GetButton(KeyMap.MoveAllOfType))
		{
			TryMoveAllOfType(thingToMove, parentSlot, slot);
		}
	}

	private void TryMoveAll(DynamicThing thingToMove, Slot fromSlot, Slot endSlot)
	{
		if (!(thingToMove is Item item) || fromSlot == null || endSlot == null || fromSlot.Parent is Entity)
		{
			return;
		}
		int num = endSlot.SlotIndex + 1;
		int num2 = num;
		List<Slot> slots = endSlot.Parent.Slots;
		if (num2 >= slots[slots.Count - 1].SlotIndex)
		{
			num = 0;
		}
		List<Slot> list = endSlot.Parent.Slots.RotateFrom(num);
		foreach (Slot slot2 in fromSlot.Parent.Slots)
		{
			if (!slot2.Contains<Item>(out var occupant))
			{
				continue;
			}
			for (int i = 0; i < list.Count; i++)
			{
				Slot slot = list[i];
				if (slot.IsEmpty() && (slot.Type == Class.None || slot.Type == item.SlotType) && slot.IsSwappable && AllowMove(occupant, slot))
				{
					OnServer.MoveToSlot(occupant, slot);
					list.RemoveAt(i);
					break;
				}
			}
		}
	}

	private void TryMoveAllOfType(DynamicThing thingToMove, Slot fromSlot, Slot endSlot)
	{
		if (!KeyManager.GetButton(KeyMap.MoveAllOfType) || !(thingToMove is Item item) || fromSlot == null || endSlot == null || fromSlot.Parent is Entity)
		{
			return;
		}
		int num = endSlot.SlotIndex + 1;
		int num2 = num;
		List<Slot> slots = endSlot.Parent.Slots;
		if (num2 >= slots[slots.Count - 1].SlotIndex)
		{
			num = 0;
		}
		List<Slot> list = endSlot.Parent.Slots.RotateFrom(num);
		foreach (Slot slot2 in fromSlot.Parent.Slots)
		{
			if (!slot2.Contains<Item>(out var occupant) || occupant?.PrefabHash != item.PrefabHash)
			{
				continue;
			}
			for (int i = 0; i < list.Count; i++)
			{
				Slot slot = list[i];
				if (slot.IsEmpty() && (slot.Type == Class.None || slot.Type == item.SlotType) && slot.IsSwappable && AllowMove(item, slot))
				{
					OnServer.MoveToSlot(occupant, slot);
					list.RemoveAt(i);
					break;
				}
			}
		}
	}

	public void PlayerSwapToWorld(DynamicThing dynamicThing)
	{
		InventoryManager.Instance.CheckCancelMultiConstructor();
		DoSwapActiveHand();
		OnServer.SwapSlots(Parent.netId, dynamicThing.netId, SlotIndex, -1);
		PlaySlotEnterUiSound();
	}

	public void PlayerMergeToSlot(IMergeable stackable)
	{
		if (stackable != null)
		{
			if (Contains<IMergeable>(out var occupant))
			{
				Thing.Merge(occupant, stackable);
				PlaySlotEnterUiSound();
			}
			else if (Occupant is MiningBelt miningBelt && miningBelt.MergeAsOre(stackable))
			{
				PlaySlotEnterUiSound();
			}
		}
	}

	public void PlayerSwapToSlot(Slot slot)
	{
		InventoryManager.Instance.CheckCancelMultiConstructor();
		OnServer.SwapSlots(Parent.ReferenceId, slot.Parent.ReferenceId, SlotIndex, slot.SlotIndex);
		if (slot.IsHandSlot)
		{
			slot.PlaySlotEnterUiSound();
		}
		else
		{
			PlaySlotEnterUiSound();
		}
	}

	public void PlayerMoveToWorld()
	{
		OnServer.MoveToWorld(Occupant);
	}

	public void PlayerInsertToFreeSlot(DynamicThing slotOccupant)
	{
		if (!Occupant || slotOccupant.SlotType == Type || Occupant.PrefabHash == slotOccupant.PrefabHash || !Occupant.HasSlots)
		{
			return;
		}
		foreach (Slot slot in Occupant.Slots)
		{
			if (AllowMove(slotOccupant, slot))
			{
				OnServer.MoveToSlot(slotOccupant, slot);
				slot.PlaySlotEnterUiSound();
				break;
			}
		}
	}

	private void DoSwapActiveHand()
	{
		if (Display == InventoryManager.Instance.InactiveHand && !InventoryManager.Instance.IsUsingSmartTool)
		{
			Human.LocalHuman.SwapHands();
			InventoryManager.Instance.ActiveHand.RefreshAnimation();
			InventoryManager.Instance.InactiveHand.RefreshAnimation();
		}
	}

	public bool TakesSpecificItem(int prefabHash)
	{
		if (SpecificTypePrefabHashes == null || SpecificTypePrefabHashes.Length == 0)
		{
			return true;
		}
		for (int i = 0; i < SpecificTypePrefabHashes.Length; i++)
		{
			if (SpecificTypePrefabHashes[i] == prefabHash)
			{
				return true;
			}
		}
		return false;
	}

	public void SortContents()
	{
		if (Contains<DynamicThing>(out var occupant))
		{
			long netId = occupant.netId;
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(new SortContentsMessage
				{
					ThingId1 = netId
				});
			}
			else
			{
				OnServer.SortContents(occupant);
			}
		}
	}
}
