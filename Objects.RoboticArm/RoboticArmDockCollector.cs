using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Effects;
using Objects.Electrical;
using Trading;
using UnityEngine;

namespace Objects.RoboticArm;

public class RoboticArmDockCollector : RoboticArmDock, IPhysical, IProfile, IDensePoolable, ICollector, IReferencable, IEvaluable
{
	private readonly DensePoolReference<IPhysical> _densePoolReference = new DensePoolReference<IPhysical>(Thing.PhysicalPoolActive);

	[SerializeField]
	private MaterialSetChanger _materialSetChanger;

	[SerializeField]
	protected Transform _endPosition;

	private readonly List<Item> _proximityItems = new List<Item>(128);

	public static readonly string[] VentDirectionStrings = EnumCollections.VentDirection.Names;

	[SerializeField]
	private SwitchMode switchMode;

	public static readonly List<ICollector> AllCollectors = new List<ICollector>(64);

	private const float REGISTRATION_SQUARE_DISTANCE = 10f;

	private const float MIN_TIME_BETWEEN_EJECTIONS = 0.2f;

	private const float MAX_TIME_BETWEEN_EJECTIONS = 0.4f;

	private float _ejectCoolDown;

	private const float COLLECTION_SQUARE_DISTANCE = 1f;

	private (bool playing, VentDirection direction) _audioState = (playing: false, direction: VentDirection.Outward);

	private byte _armDisplayState;

	private const float MAXFORCE = 600f;

	private const float MINFORCE = 300f;

	private const float DISTANCE_FACTOR = 3f;

	private int _stackIndex = -1;

	private const int SLOT_COUNT = 20;

	public Room Room => GetRoom();

	public VentDirection VentDirection => (VentDirection)Mode;

	public override string[] ModeStrings => RoboticArmDockAtmos.VentDirectionStrings;

	public Vector3 CollectionPosition => EjectPosition;

	public float RegistrationSquareDistance
	{
		get
		{
			if (!IsOperable || base.ArmState != ArmState.Down)
			{
				return 0f;
			}
			return 10f;
		}
	}

	public override Transform SoundPosition => _endPosition;

	private byte ArmDisplayState
	{
		get
		{
			return _armDisplayState;
		}
		set
		{
			_armDisplayState = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public bool RunPhysicsUpdate => true;

	public Vector3 EjectPosition { get; private set; }

	public override bool OnAddToPool(object densePool, int slot)
	{
		if (_densePoolReference.CanAddToPool(densePool))
		{
			return _densePoolReference.AddToPool(densePool, slot);
		}
		return base.OnAddToPool(densePool, slot);
	}

	public override void OnRemoveFromPool(object densePool)
	{
		base.OnRemoveFromPool(densePool);
		_densePoolReference.OnRemovedFrom(densePool);
	}

	public new static void ClearAll()
	{
		AllCollectors.Clear();
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (switchMode != null)
		{
			switchMode.RefreshState(skipAnimation);
		}
	}

	public void RegisterItem(Item item)
	{
		lock (_proximityItems)
		{
			if (!_proximityItems.Contains(item))
			{
				_proximityItems.Add(item);
			}
			item.RegisteredCollector = this;
		}
	}

	public void DeRegisterItem(Item item)
	{
		lock (_proximityItems)
		{
			_proximityItems.Remove(item);
			item.RegisteredCollector = null;
		}
	}

	private void EjectItem()
	{
		if (!(_ejectCoolDown > Time.time))
		{
			Item nextOutputItem = GetNextOutputItem();
			if (nextOutputItem != null)
			{
				_ejectCoolDown = Time.time + Random.Range(0.2f, 0.4f);
				OnServer.MoveToWorld(nextOutputItem, EjectPosition, Random.rotation);
			}
		}
	}

	public void CollectNearbyItem()
	{
		lock (_proximityItems)
		{
			for (int i = 0; i < _proximityItems.Count; i++)
			{
				Item item = _proximityItems[i];
				if (item == null || item.ParentSlot != null)
				{
					_proximityItems.RemoveAt(i);
					if (item != null)
					{
						item.RegisteredCollector = null;
					}
				}
				else
				{
					if (!(Vector3.SqrMagnitude(item.Position - EjectPosition) < 1f))
					{
						continue;
					}
					for (int j = 0; j < Slots.Count; j++)
					{
						if (!Slots[j].Contains<Item>())
						{
							OnServer.MoveToSlot(item, Slots[j]);
							AudioEvent.Create(this, Defines.Sounds.CollectorIngest);
							break;
						}
					}
					break;
				}
			}
		}
	}

	private bool IsStationaryAtDock()
	{
		if (!ArmIsStationary(out var railNodeIndex))
		{
			return false;
		}
		return base.RoboticArmNetwork.RailNodeList[railNodeIndex].Rail is IRoboticArmBypass;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Mode)
		{
			return HandleMode(interactable, interaction, doAction);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	protected override DelayedActionInstance HandleActivate(Interactable interactable, bool doAction)
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
		if (Error != 0)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceError);
		}
		if (base.IsMoving || Activate == 1)
		{
			return delayedActionInstance.Fail(GameStrings.RoboticArmBusy);
		}
		if (!IsStationaryAtDock() && IsFull() && VentDirection == VentDirection.Inward)
		{
			return delayedActionInstance.Fail(GameStrings.RoboticArmFull);
		}
		if (!IsStationaryAtDock() && IsEmpty() && VentDirection == VentDirection.Outward)
		{
			return delayedActionInstance.Fail(GameStrings.RoboticArmEmpty);
		}
		if (doAction)
		{
			OnServer.Interact(base.InteractActivate, 1);
		}
		return delayedActionInstance.Succeed();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		InteractableType action = interactable.Action;
		if (action == InteractableType.Powered || action == InteractableType.OnOff || action == InteractableType.Mode)
		{
			HandleOperatingAudio();
		}
	}

	protected override void AnimateDownFinished()
	{
		if (GameManager.RunSimulation && IsFull() && VentDirection == VentDirection.Inward)
		{
			OnServer.Interact(base.InteractActivate, 1);
			return;
		}
		HandleOperatingAudio();
		CacheEjectPosition();
	}

	protected override void AnimateUpStarted()
	{
		base.AnimateUpStarted();
		HandleOperatingAudio();
	}

	protected override void AnimateUpFinished()
	{
		base.AnimateUpFinished();
		CacheEjectPosition();
		HandleOperatingAudio();
	}

	private void HandleOperatingAudio(bool force = false)
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		bool flag = base.ArmState == ArmState.Down && IsOperable;
		if (flag && (force || !_audioState.playing || VentDirection != _audioState.direction))
		{
			switch (VentDirection)
			{
			case VentDirection.Inward:
				GetAudioEvent(Defines.Sounds.CollectorInwardsStart).Trigger();
				GetAudioEvent(Defines.Sounds.CollectorInwardsLoop).Trigger();
				GetAudioEvent(Defines.Sounds.CollectorOutwardsLoop).Stop(immediate: true);
				break;
			case VentDirection.Outward:
				GetAudioEvent(Defines.Sounds.CollectorOutwardsStart).Trigger();
				GetAudioEvent(Defines.Sounds.CollectorOutwardsLoop).Trigger();
				GetAudioEvent(Defines.Sounds.CollectorInwardsLoop).Stop(immediate: true);
				break;
			}
		}
		else if (!flag && (force || _audioState.playing))
		{
			GetAudioEvent(Defines.Sounds.CollectorInwardsLoop).Stop();
			GetAudioEvent(Defines.Sounds.CollectorOutwardsLoop).Stop();
		}
		_audioState = (playing: flag, direction: VentDirection);
	}

	private DelayedActionInstance HandleMode(Interactable interactable, Interaction interaction, bool doAction)
	{
		DelayedActionInstance obj = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (doAction)
		{
			OnServer.Interact(interactable, (Mode != 1) ? 1 : 0);
		}
		return obj.Succeed();
	}

	private bool IsFull()
	{
		List<Slot> slots = Slots;
		return !slots[slots.Count - 1].IsEmpty();
	}

	private bool IsEmpty()
	{
		return Slots[0].IsEmpty();
	}

	private void DoContextualArmAction()
	{
		SmallCell targetCell = RoboticArmActionHelper.GetTargetCell(_arm.Transform);
		if (targetCell != null)
		{
			Device device = targetCell.Device;
			if (device is ChuteBin || device is ChuteInlet)
			{
				if (VentDirection != VentDirection.Outward)
				{
					return;
				}
				Item nextOutputItem = GetNextOutputItem();
				if (targetCell.Device is ChuteBin chuteBin)
				{
					if (nextOutputItem != null && chuteBin.AllowInput)
					{
						OnServer.MoveToSlot(nextOutputItem, chuteBin.InputSlot);
					}
				}
				else if (targetCell.Device is ChuteInlet chuteInlet && nextOutputItem != null && chuteInlet.AllowInput)
				{
					OnServer.MoveToSlot(nextOutputItem, chuteInlet.InputSlot);
				}
				RetractIfEmpty();
				return;
			}
			if (targetCell.Device is ChuteExportBin chuteExportBin)
			{
				if (VentDirection == VentDirection.Inward)
				{
					Slot nextFreeSlot = GetNextFreeSlot();
					if (nextFreeSlot != null && chuteExportBin.IsOpen && chuteExportBin.TransportSlot.Contains<Item>())
					{
						OnServer.MoveToSlot(chuteExportBin.TransportSlot.Get<Item>(), nextFreeSlot);
					}
					RetractIfFull();
				}
				return;
			}
		}
		switch (VentDirection)
		{
		case VentDirection.Inward:
			CollectNearbyItem();
			RetractIfFull();
			break;
		case VentDirection.Outward:
			EjectItem();
			RetractIfEmpty();
			break;
		}
	}

	private void RetractIfFull()
	{
		if (IsFull() && base.ArmState == ArmState.Down && VentDirection == VentDirection.Inward)
		{
			OnServer.Interact(base.InteractActivate, 1);
		}
	}

	private void RetractIfEmpty()
	{
		if (IsEmpty() && base.ArmState == ArmState.Down && VentDirection == VentDirection.Outward)
		{
			OnServer.Interact(base.InteractActivate, 1);
		}
	}

	private void CheckArmDisplayState()
	{
		byte armDisplayState = GetArmDisplayState();
		if (ArmDisplayState != armDisplayState)
		{
			ArmDisplayState = armDisplayState;
			RefreshArmDisplayMaterial();
		}
	}

	private void RefreshArmDisplayMaterial()
	{
		_materialSetChanger.TargetSetIndex = ArmDisplayState;
		RefreshAnimState(skipAnimation: true);
	}

	private byte GetArmDisplayState()
	{
		if (!Powered || !OnOff)
		{
			return 0;
		}
		byte b = 1;
		int[] array = new int[3] { 5, 12, 19 };
		foreach (int index in array)
		{
			if (Slots[index].IsEmpty())
			{
				break;
			}
			b++;
		}
		return b;
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		CheckArmDisplayState();
		if (base.IsStructureCompleted && IsOperable && base.ArmState == ArmState.Down)
		{
			DoContextualArmAction();
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RoboticArmDockCollectorSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.Append(GameStrings.RoboticArmVentDirection.AsString(VentDirectionStrings[(int)VentDirection].AsColor("yellow"))).AppendLine();
		switch (ArmDisplayState)
		{
		case 1:
		case 2:
		case 3:
			extendedText.Append(GameStrings.RoboticArmState.AsString(GameStrings.RoboticArmArmDisplayStateHasSpace.AsColor("yellow"))).AppendLine();
			break;
		case 4:
			extendedText.Append(GameStrings.RoboticArmState.AsString(GameStrings.RoboticArmArmDisplayStateFull.AsColor("yellow"))).AppendLine();
			break;
		}
		return extendedText;
	}

	protected override void InitialiseSaveData(ref ThingSaveData thingSaveData)
	{
		base.InitialiseSaveData(ref thingSaveData);
		_ = thingSaveData is RoboticArmDockCollectorSaveData;
	}

	public override void DeserializeSave(ThingSaveData thingSaveData)
	{
		base.DeserializeSave(thingSaveData);
		_ = thingSaveData is RoboticArmDockCollectorSaveData;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteByte(ArmDisplayState);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			ArmDisplayState = reader.ReadByte();
			RefreshArmDisplayMaterial();
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		HandleOperatingAudio(force: true);
	}

	public void PhysicsUpdate()
	{
		if (!IsOperable || base.ArmState != ArmState.Down || VentDirection != VentDirection.Inward)
		{
			return;
		}
		lock (_proximityItems)
		{
			for (int num = _proximityItems.Count - 1; num >= 0; num--)
			{
				Item item = _proximityItems[num];
				float num2 = Vector3.Magnitude(EjectPosition - item.Position);
				Vector3 vector = Vector3.Normalize(Vector3.Lerp(EjectPosition, base.ArmWorldPosition, (num2 - 1f) / 2f) - item.Position);
				if (item.RigidBody != null)
				{
					float num3 = Mathf.Lerp(300f, 600f, num2 / 3f);
					item.RigidBody.AddForce(vector * (num3 * Time.fixedDeltaTime));
				}
			}
		}
	}

	public Item GetNextOutputItem()
	{
		for (int num = Slots.Count - 1; num >= 0; num--)
		{
			if (Slots[num].Contains<Item>(out var occupant))
			{
				return occupant;
			}
		}
		return null;
	}

	private void CacheEjectPosition()
	{
		EjectPosition = _endPosition.position;
	}

	protected override void MoveOnRail()
	{
		base.MoveOnRail();
		CacheEjectPosition();
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		AllCollectors.Add(this);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!Singleton<GameManager>.IsQuitting && !IsCursor && GameManager.GameState != GameState.None)
		{
			AllCollectors.Remove(this);
		}
	}

	public override void RailNetworkUpdated()
	{
		base.RailNetworkUpdated();
		HandleOperatingAudio();
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		_stackIndex = newChild.ParentSlot.SlotIndex;
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		_stackIndex--;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Quantity => true, 
			LogicType.Ratio => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Quantity => _stackIndex + 1, 
			LogicType.Ratio => (float)(_stackIndex + 1) / 20f, 
			_ => base.GetLogicValue(logicType), 
		};
	}
}
