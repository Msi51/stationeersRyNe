using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Effects;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class Door : Device, IAirlockDevice, ISmartRotatable
{
	public static string[] ButtonModeStrings = Enum.GetNames(typeof(ButtonMode));

	private double _setting;

	[SerializeField]
	private Transform soundPosition;

	[SerializeField]
	private DoorAnimComponent doorAnimComponent;

	[Header("Door")]
	public List<TextMesh> DoorLabels = new List<TextMesh>();

	[Range(0f, 512f)]
	[Tooltip("The maximum number of characters that can be used in the labeler.")]
	public int MaxLabelLength = 256;

	[Range(0f, 512f)]
	public int MaxWordLength = 256;

	public bool UnPowered;

	public Light[] StatusLights;

	public HashSet<Motherboard> LinkedMotherboards = new HashSet<Motherboard>();

	protected Grid3 _faceGrid;

	protected Grid3 _rearGrid;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public List<Collider> DoorColliders = new List<Collider>();

	private static readonly int OffHash = Animator.StringToHash("Off");

	private static readonly int LockHash = Animator.StringToHash("Lock");

	private static readonly int UnlockHash = Animator.StringToHash("Unlock");

	[SerializeField]
	private MaterialChanger statusLightsMaterialChanger;

	[SerializeField]
	private MaterialChanger keypadMaterialChanger;

	private static readonly Color StatusLightsActive = Color.green;

	private static readonly Color StatusLightsLock = Color.red;

	private static readonly Color StatusLightsUnlock = Color.white;

	public static readonly int CloseNoPowerHash = Animator.StringToHash("closenopower");

	public static readonly int OpenNoPowerHash = Animator.StringToHash("opennopower");

	public static readonly int CloseHash = Animator.StringToHash("close");

	public static readonly int OpenHash = Animator.StringToHash("open");

	private static int _keypadOpen = Animator.StringToHash("KeypadPos");

	private static int _keypadClose = Animator.StringToHash("KeypadNeg");

	public override string[] ModeStrings => ButtonModeStrings;

	[ByteArraySync]
	public double Setting
	{
		get
		{
			return _setting;
		}
		set
		{
			_setting = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public override Transform SoundPosition => soundPosition ?? base.SoundPosition;

	public AirlockControlBase AirlockController { get; set; }

	public override bool CanAirPass
	{
		get
		{
			if (!IsOpen || base.NeverAirPass)
			{
				return base.CanAirPass;
			}
			return true;
		}
	}

	public override bool CanLightPass
	{
		get
		{
			if (!IsOpen)
			{
				return base.CanLightPass;
			}
			return true;
		}
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		doorAnimComponent?.RefreshState(skipAnimation);
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteDouble(Setting);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			Setting = reader.ReadDouble();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteDouble(Setting);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Setting = reader.ReadDouble();
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.DoorCategory);
	}

	public override string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.DoorCategory;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DoorSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is DoorSaveData doorSaveData)
		{
			Setting = doorSaveData.Setting;
		}
		Interactable interactPowered = base.InteractPowered;
		ToggleStatusLight(interactPowered != null && interactPowered.State == 1, IsLocked ? StatusLightsLock : StatusLightsUnlock);
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is DoorSaveData doorSaveData)
		{
			doorSaveData.Setting = Setting;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => true, 
			LogicType.Idle => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => Setting, 
			LogicType.Idle => (!base.IsDeviceActive) ? 1 : 0, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		if (logicType == LogicType.Setting)
		{
			Setting = value.Clamp(0.0, 1.0);
		}
	}

	public override void OnRenamed()
	{
		base.OnRenamed();
		SetDoorLabel();
	}

	public override void OnStartRender()
	{
		base.OnStartRender();
		foreach (TextMesh doorLabel in DoorLabels)
		{
			doorLabel.gameObject.SetActive(value: true);
		}
	}

	public override void OnStopRender()
	{
		base.OnStopRender();
		foreach (TextMesh doorLabel in DoorLabels)
		{
			doorLabel.gameObject.SetActive(value: false);
		}
	}

	public override void OnLinkWithBoard(Motherboard motherboard)
	{
		base.OnLinkWithBoard(motherboard);
		LinkedMotherboards.Add(motherboard);
		if (motherboard is AirlockControlBase)
		{
			AirlockController = (AirlockControlBase)motherboard;
		}
	}

	public override void OnUnlinkWithBoard(Motherboard motherboard)
	{
		base.OnUnlinkWithBoard(motherboard);
		LinkedMotherboards.Remove(motherboard);
		if (motherboard == AirlockController)
		{
			AirlockController = null;
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (!base.AllowInteraction || !base.IsStructureCompleted)
		{
			return base.AttackWith(attack, doAction);
		}
		Crowbar crowbar = attack.SourceItem as Crowbar;
		if ((bool)crowbar && (attack.TargetCollider == null || DoorColliders.Contains(attack.TargetCollider) || attack.TargetCollider.transform == ThingTransform))
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = crowbar.DoorForceOpenDuration,
				ActionMessage = (IsOpen ? ActionStrings.ForceClose : ActionStrings.ForceOpen)
			};
			if (attack.TargetCollider != null && attack.TargetCollider.transform != ThingTransform)
			{
				delayedActionInstance.Selection = GetSelection(attack.TargetCollider);
			}
			if (IsLocked)
			{
				delayedActionInstance.IsDisabled = true;
				delayedActionInstance.AppendStateMessage(GameStrings.DoorUnableToForceOpenLocked);
				return delayedActionInstance;
			}
			if (!OnOff || !Powered)
			{
				if (doAction && GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractOpen, (!IsOpen) ? 1 : 0);
				}
				return delayedActionInstance;
			}
			delayedActionInstance.IsDisabled = true;
			delayedActionInstance.AppendStateMessage(GameStrings.DoorUnableToForceOpenPowered);
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}

	private string[] FormatWords(string input)
	{
		List<string> list = input.Substring(0, Mathf.Min(DisplayName.Length, MaxLabelLength)).ToUpper().Split(' ')
			.ToList();
		for (int num = list.Count - 1; num >= 0; num--)
		{
			List<string> list2 = new List<string>();
			string text = list[num];
			if (text.Length > MaxWordLength)
			{
				list.RemoveAt(num);
				while (text.Length > MaxWordLength)
				{
					string text2 = text.Substring(0, Mathf.Min(text.Length, MaxWordLength));
					list2.Add(text2);
					text = text.Substring(text2.Length);
				}
				list2.Add(text);
				list.InsertRange(num, list2);
			}
		}
		return list.ToArray();
	}

	public void SetDoorLabel()
	{
		if (DoorLabels.Count == 0)
		{
			return;
		}
		string[] array = FormatWords(DisplayName);
		List<string> list = new List<string>();
		string text = "";
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if (i + 1 == array.Length && i == 1)
			{
				list.Add(text);
				list.Add(array[i]);
				text = string.Empty;
				break;
			}
			if (num != 0)
			{
				text += " ";
			}
			num += array[i].Length;
			if (num >= 10)
			{
				text += array[i];
				num = 0;
				list.Add(text);
				text = string.Empty;
			}
			else
			{
				text += array[i];
			}
		}
		if (text != string.Empty)
		{
			list.Add(text);
		}
		bool flag = false;
		string text2 = string.Empty;
		for (int j = 0; j < list.Count; j++)
		{
			string text3 = list[j];
			if (j > 0)
			{
				text2 += "\n";
			}
			if (!flag && text3.Length <= 7)
			{
				text2 += $"<size=90>{text3}</size>";
				flag = true;
			}
			else
			{
				text2 += text3;
			}
		}
		foreach (TextMesh doorLabel in DoorLabels)
		{
			doorLabel.richText = true;
			doorLabel.text = text2;
		}
	}

	public override void OnAnimationStart()
	{
		base.OnAnimationStart();
		ToggleStatusLight(Powered, StatusLightsActive);
		OnDeviceActive();
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		ToggleStatusLight(Powered, IsLocked ? StatusLightsLock : StatusLightsUnlock);
		OnDeviceIdle();
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		SetDoorLabel();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SetDoorLabel();
	}

	public Structure IsSideBlocked(bool allStructual = false)
	{
		Grid3 grid = new Grid3(base.ThingTransformPosition);
		foreach (Structure faceStructure in base.GridController.GetFaceStructures(grid))
		{
			Grid3 grid2 = new Grid3(faceStructure.ThingTransformPosition);
			if (allStructual && faceStructure.StructureCollisionType == CollisionType.BlockGrid)
			{
				return faceStructure;
			}
			if (!(grid != grid2) && (faceStructure.IsDoor || allStructual))
			{
				return faceStructure;
			}
		}
		return null;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		switch (interactable.Action)
		{
		case InteractableType.Open:
			if (GameManager.RunSimulation && Mode == 1)
			{
				Setting = interactable.State;
			}
			base.GridController.UpdateAirState(this);
			break;
		case InteractableType.Lock:
		case InteractableType.Powered:
			ToggleStatusLight(Powered, IsLocked ? StatusLightsLock : StatusLightsUnlock);
			keypadMaterialChanger?.ChangeState((!Powered) ? OffHash : (IsLocked ? LockHash : UnlockHash));
			break;
		}
	}

	public void ToggleStatusLight(bool toggle, Color color)
	{
		Light[] statusLights = StatusLights;
		foreach (Light obj in statusLights)
		{
			obj.enabled = toggle;
			obj.color = color;
		}
		if ((bool)statusLightsMaterialChanger)
		{
			int id = ((!Powered) ? Defines.Animator.NotPowered : ((color == StatusLightsActive) ? Defines.Animator.Active : ((!(color == StatusLightsLock)) ? Defines.Animator.Powered : Defines.Animator.Lock)));
			statusLightsMaterialChanger.ChangeState(id);
		}
	}

	public Structure IsSideBlocked(Vector3 direction, bool allStructual = false)
	{
		Grid3 grid = new Grid3(direction * 2f + base.transform.position);
		foreach (Structure faceStructure in base.GridController.GetFaceStructures(grid))
		{
			Grid3 grid2 = new Grid3(faceStructure.ThingTransformPosition);
			if (allStructual && faceStructure.StructureCollisionType == CollisionType.BlockGrid)
			{
				return faceStructure;
			}
			if (!(grid != grid2) && (faceStructure.IsDoor || allStructual))
			{
				return faceStructure;
			}
		}
		return null;
	}

	public override CanConstructInfo CanConstruct()
	{
		if ((bool)IsSideBlocked(allStructual: true))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementBlockedByStructure.DisplayString);
		}
		return base.CanConstruct();
	}

	public void PlayOpenDoorSound()
	{
		PlaySound(Powered ? OpenHash : OpenNoPowerHash);
	}

	public void PlayCloseDoorSound()
	{
		PlaySound(Powered ? CloseHash : CloseNoPowerHash);
	}

	public override bool PreventInteraction(out DelayedActionInstance failResult, Interactable interactable, Interaction interaction)
	{
		if (interactable.Action == InteractableType.Open)
		{
			failResult = null;
			return false;
		}
		return base.PreventInteraction(out failResult, interactable, interaction);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Open)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (!UnPowered)
			{
				if (!Powered)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				if (!OnOff)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
			}
			switch ((ButtonMode)Mode)
			{
			case ButtonMode.Operate:
				if (IsLocked && (bool)AirlockController)
				{
					delayedActionInstance.ActionMessage = ActionStrings.Cycle;
					delayedActionInstance.AppendStateMessage(GameStrings.AirLockIsCurrently, AirlockController.ToTooltip(), AirlockController.GetStateString().ToProper());
					if (!AirlockController.CanToggle)
					{
						return delayedActionInstance.Fail();
					}
					if (!IsAuthorized(interaction.SourceThing))
					{
						return delayedActionInstance.Fail(GameStrings.AccessCardUnableToInteract);
					}
					if (doAction && Powered)
					{
						PlayPooledAudioSound((interactable.State == 0) ? _keypadOpen : _keypadClose, interactable.Collider.transform.localPosition);
					}
					if (doAction && GameManager.RunSimulation)
					{
						AirlockController.ButtonCycleAirlock();
					}
					return delayedActionInstance;
				}
				if (!IsAuthorized(interaction.SourceThing))
				{
					return delayedActionInstance.Fail(GameStrings.AccessCardUnableToInteract);
				}
				if (IsLocked)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceLocked);
				}
				if (doAction)
				{
					PlayPooledAudioSound((interactable.State == 0) ? _keypadOpen : _keypadClose, interactable.Collider.transform.localPosition);
				}
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				OnServer.Interact(base.InteractOpen, (!IsOpen) ? 1 : 0);
				break;
			case ButtonMode.Logic:
				delayedActionInstance.AppendStateMessage(GameStrings.LogicCurrentlySetTo, ToTooltip());
				if (!IsAuthorized(interaction.SourceThing))
				{
					return delayedActionInstance.Fail(GameStrings.AccessCardUnableToInteract);
				}
				if (IsLocked)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceLocked);
				}
				if (doAction && GameManager.RunSimulation)
				{
					Setting = ((!IsOpen) ? 1 : 0);
				}
				break;
			}
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public new List<Connection> GetOpenEnds()
	{
		return null;
	}

	public new int ConnectedCount()
	{
		return 0;
	}

	public new int GetOpenEndsCount()
	{
		return 0;
	}

	public new float GetGridSize()
	{
		return 2f;
	}
}
