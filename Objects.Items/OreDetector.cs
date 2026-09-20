using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Sound;
using Assets.Scripts.Voxel;
using Cysharp.Threading.Tasks;
using TerrainSystem;
using UnityEngine;

namespace Objects.Items;

public class OreDetector : PowerTool
{
	private enum IndicatorState
	{
		Off,
		Flashing,
		On
	}

	public MeshRenderer Screen;

	private static float ZOffset = -2f;

	private Transform _screenTransform;

	private UniTask _inputHandler;

	protected static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");

	private static readonly int EquipTabletHash = Animator.StringToHash("EquipTablet");

	private static readonly int UnEquipTabletHash = Animator.StringToHash("UnEquipTablet");

	private float _scrollData;

	public static readonly int ScrollUpHash = Animator.StringToHash("ScrollUp");

	public static readonly int ScrollDownHash = Animator.StringToHash("ScrollDown");

	private static readonly Dictionary<int, MinableType> ModeToMinableMap = new Dictionary<int, MinableType>
	{
		{
			0,
			MinableType.Iron
		},
		{
			1,
			MinableType.Coal
		},
		{
			2,
			MinableType.Copper
		},
		{
			3,
			MinableType.Gold
		},
		{
			4,
			MinableType.Ice
		},
		{
			5,
			MinableType.Nickel
		},
		{
			6,
			MinableType.Lead
		},
		{
			7,
			MinableType.Silver
		},
		{
			8,
			MinableType.Silicon
		},
		{
			9,
			MinableType.Oxite
		},
		{
			10,
			MinableType.Volatiles
		},
		{
			11,
			MinableType.Cobalt
		},
		{
			12,
			MinableType.Nitrice
		}
	};

	public Material SignalInactiveMaterial;

	public Material SignalActiveMaterial;

	public Material SignalFlashingMaterial;

	public MeshRenderer[] signalStrengthIndicators = new MeshRenderer[5];

	private IndicatorState[] indicatorStates = new IndicatorState[5];

	private CancellationTokenSource cts = new CancellationTokenSource();

	private static readonly float MinVolume = 0.5f;

	private static readonly float MaxVolume = 2f;

	public float MinDistance = 2f;

	public float MaxDistance = 6f;

	private static readonly int OreDetectorOperatingHash = Animator.StringToHash("OreDetectorOperating");

	private static readonly float MinAudioPitch = 0.25f;

	private static readonly float MaxAudioPitch = 1f;

	private float _audioPitch = MinAudioPitch;

	private GameAudioSource oreDetectorAudio;

	private bool _isPlayingSound;

	[SerializeField]
	private float _range = 30f;

	[SerializeField]
	[HideInInspector]
	private string[] _modeStrings;

	private string _cachedDistance;

	private float _cachedDirection;

	public override int EquipSoundHash => EquipTabletHash;

	public override int UnEquipSoundHash => UnEquipTabletHash;

	public override float AudioDistanceSquared => 64f;

	private MinableType TrackedMinableType => ModeToMinableMap[Mode];

	private bool InUse
	{
		get
		{
			if (RootParent.HasAuthority && OnOff && IsOperable && base.ParentSlot != null)
			{
				return InventoryManager.ActiveHandSlot?.Occupant == this;
			}
			return false;
		}
	}

	private bool IsPlayingSound
	{
		get
		{
			return _isPlayingSound;
		}
		set
		{
			if (value != _isPlayingSound)
			{
				_isPlayingSound = value;
				if (OnOff && IsOperable && _isPlayingSound)
				{
					float num = (InventoryManager.Parent ? Vector3.SqrMagnitude(InventoryManager.Parent.Position - base.Position) : float.MaxValue);
					float t = Mathf.Clamp01(Mathf.Max(AudioDistanceSquared - num, 0f) / AudioDistanceSquared);
					PlaySound(OreDetectorOperatingHash, Mathf.Lerp(MinVolume, MaxVolume, t), _audioPitch);
					oreDetectorAudio = GetAudioSource(GetAudioEvent(OreDetectorOperatingHash).Channel);
				}
				else
				{
					StopSound(OreDetectorOperatingHash);
				}
			}
		}
	}

	public override string[] ModeStrings => _modeStrings;

	public override void Awake()
	{
		base.Awake();
		_screenTransform = Screen.transform;
		SetupWhenReady().Forget();
	}

	private void SetIndicatorState(int index, IndicatorState state)
	{
		if (indicatorStates[index] != state)
		{
			indicatorStates[index] = state;
			CancelCts();
			switch (state)
			{
			case IndicatorState.Flashing:
				cts = new CancellationTokenSource();
				Flash(index, cts.Token).Forget();
				break;
			case IndicatorState.On:
				signalStrengthIndicators[index].sharedMaterial = SignalActiveMaterial;
				break;
			default:
				signalStrengthIndicators[index].sharedMaterial = SignalInactiveMaterial;
				break;
			}
		}
	}

	private void CancelCts()
	{
		if (cts != null)
		{
			cts.Cancel();
			cts = null;
		}
	}

	private async UniTaskVoid SetupWhenReady()
	{
		while (GameManager.GameState != GameState.Running)
		{
			await UniTask.NextFrame();
		}
		await UniTask.NextFrame();
		InputHandling();
		Redraw();
	}

	public void InActiveHand()
	{
		InputHandling();
	}

	private void ResetSignalStrength()
	{
		for (int i = 0; i < signalStrengthIndicators.Length; i++)
		{
			SetIndicatorState(i, IndicatorState.Off);
		}
		CancelCts();
	}

	private void UpdateSignalStrength()
	{
		Vein nearestVeinOfType = Vein.GetNearestVeinOfType(base.transform.position, _range, TrackedMinableType);
		if (nearestVeinOfType == null)
		{
			ResetSignalStrength();
			_audioPitch = MinAudioPitch;
			return;
		}
		float num = Vein.DistanceToNearestMinableInVein(base.transform.position, nearestVeinOfType);
		if (num > _range)
		{
			ResetSignalStrength();
			_audioPitch = MinAudioPitch;
		}
		else
		{
			UpdateMaterials(num);
			_audioPitch = Mathf.Lerp(MinAudioPitch, MaxAudioPitch, Mathf.Max(_range - num, 0f) / _range);
		}
	}

	private void UpdateMaterials(float distance)
	{
		float num = _range / (float)(signalStrengthIndicators.Length * 2 + 1);
		for (int num2 = signalStrengthIndicators.Length; num2 > 0; num2--)
		{
			IndicatorState state = IndicatorState.Off;
			if (OnOff && IsOperable && distance < _range * ((float)num2 / (float)signalStrengthIndicators.Length))
			{
				state = ((!(distance < _range * ((float)num2 / (float)signalStrengthIndicators.Length) - num)) ? IndicatorState.Flashing : IndicatorState.On);
			}
			SetIndicatorState(signalStrengthIndicators.Length - num2, state);
		}
	}

	private async UniTaskVoid Flash(int flashIndex, CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			signalStrengthIndicators[flashIndex].sharedMaterial = SignalFlashingMaterial;
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
			signalStrengthIndicators[flashIndex].sharedMaterial = SignalInactiveMaterial;
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		}
	}

	private async UniTask HandleScreenInput()
	{
		CancellationToken cancel = base.GameObject.GetCancellationTokenOnDestroy();
		while (InUse)
		{
			_scrollData = CursorManager.ScrollbarData.y;
			if (_scrollData > 0f)
			{
				OnScrollUp();
			}
			if (_scrollData < 0f)
			{
				OnScrollDown();
			}
			if (_scrollData != 0f)
			{
				ResetSignalStrength();
				UpdateSignalStrength();
			}
			await UniTask.NextFrame(cancel);
			if (base.BeingDestroyed || cancel.IsCancellationRequested)
			{
				break;
			}
		}
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		UpdateSignalStrength();
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

	private void OnScrollUp()
	{
		int state = (Mode + 1) % ModeToMinableMap.Count;
		Thing.Interact(base.InteractMode, state);
	}

	private void OnScrollDown()
	{
		int state = (Mode - 1 + ModeToMinableMap.Count) % ModeToMinableMap.Count;
		Thing.Interact(base.InteractMode, state);
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
		CheckScreen();
		InputHandling();
	}

	protected void CheckScreen()
	{
		if (Screen != null)
		{
			Screen.enabled = !IsOccluded && OnOff && Powered;
		}
	}

	public override void SetVisibility(bool isVisible, bool hideOnPlayer = false, bool isRecursive = false, bool shouldUpdateLayers = true)
	{
		base.SetVisibility(isVisible, hideOnPlayer, isRecursive, shouldUpdateLayers);
		if (Screen != null)
		{
			Screen.enabled = !IsOccluded && isVisible && OnOff && Powered;
		}
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		CheckScreen();
		InputHandling();
		SetupWhenReady().Forget();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		CheckScreen();
		if (interactable.Action == InteractableType.OnOff && OnOff)
		{
			InputHandling();
			Redraw();
			UpdateSignalStrength();
		}
		if (interactable.Action == InteractableType.Mode && OnOff)
		{
			Redraw();
			UpdateSignalStrength();
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 1f
		};
		if ((bool)(attack.SourceItem as Cartridge))
		{
			delayedActionInstance.ActionMessage = ActionStrings.Insert;
			delayedActionInstance.OverrideTitle = attack.SourceItem.GetPassiveTooltip(null).Title;
			if (!base.AllowInteraction)
			{
				return delayedActionInstance.Fail(GameStrings.ThingCanNotInsertInteractionsDisabled, attack.SourceItem.ToTooltip(), base.Battery.ToTooltip());
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			return delayedActionInstance.Succeed();
		}
		if ((bool)(attack.SourceItem as Screwdriver))
		{
			if ((bool)base.Battery)
			{
				return base.AttackWith(attack, doAction);
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			return delayedActionInstance.Succeed();
		}
		return base.AttackWith(attack, doAction);
	}

	public override void OnEnterInventory(Thing parent)
	{
		float num = 0.65f;
		base.transform.localScale = Vector3.one * num;
		base.OnEnterInventory(parent);
		HandleScreenInput().Forget();
		Redraw();
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		base.transform.localScale = Vector3.one;
	}

	public virtual void SetIcon()
	{
		if ((bool)Screen && !base.BeingDestroyed)
		{
			bool active = OnOff && IsOperable;
			Screen.transform.gameObject.SetActive(active);
			Material material = Screen.material;
			if (Prefab.TryFind(VoxelTerrain.OrePrefabHash(TrackedMinableType), out var thing))
			{
				material.mainTexture = thing.Thumbnail.texture;
				material.SetTexture(EmissionMap, material.mainTexture);
			}
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Mode)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Mode)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Mode)
		{
			Mode = (int)Math.Clamp(value, 0.0, ModeToMinableMap.Count - 1);
		}
		base.SetLogicValue(logicType, value);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Mode)
		{
			return Mode;
		}
		return base.GetLogicValue(logicType);
	}

	public void Redraw()
	{
		if (!base.BeingDestroyed)
		{
			SetIcon();
		}
	}

	public override void OnPrefabLoad()
	{
		base.OnPrefabLoad();
		List<string> list = new List<string>();
		foreach (KeyValuePair<int, MinableType> item in ModeToMinableMap)
		{
			list.Add(EnumCollections.MinableTypes.GetNameFromValue((int)item.Value));
		}
		_modeStrings = list.ToArray();
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		if (OnOff && IsOperable)
		{
			if (IsOccluded)
			{
				IsPlayingSound = false;
				return;
			}
			float num = (InventoryManager.Parent ? Vector3.SqrMagnitude(InventoryManager.Parent.Position - base.Position) : float.MaxValue);
			float t = Mathf.Clamp01((AudioDistanceSquared - num) / AudioDistanceSquared);
			IsPlayingSound = num < AudioDistanceSquared;
			if ((bool)oreDetectorAudio?.AudioSource && !oreDetectorAudio.PausedForConcurrency && !oreDetectorAudio.AudioSource.isVirtual)
			{
				oreDetectorAudio.SetVolumeMultiplier(OreDetectorOperatingHash, Mathf.Lerp(MinVolume, MaxVolume, t));
				oreDetectorAudio.SetPitchMultiplier(OreDetectorOperatingHash, _audioPitch);
				oreDetectorAudio.maxDistance = Mathf.Lerp(MinDistance, MaxDistance, t);
			}
		}
		else
		{
			IsPlayingSound = false;
		}
	}
}
