using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.FirstPerson;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using Sound;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class GasMask : AtmosphericItem, ILogicable, IReferencable, IEvaluable, IWearableLight, ISoundAlert, IObstructsEating, IWearable, IObstructsDrinking
{
	private const string LIGHTSTRING = "LightString";

	private const string HELMETSTRING = "HelmetString";

	private new const float RENDER_DISTANCE = 100f;

	[Header("GasMask")]
	public Human ParentHuman;

	[Header("First Person Helmet")]
	[SerializeField]
	public FirstPersonHelmet FirstPersonHelmet;

	[Header("Optional components")]
	[SerializeField]
	private InteractableAnimComponent _helmetAnimationComponent;

	[SerializeField]
	private GameObject _spotlight;

	private static string _maskString = "{1} {0}";

	private static string _lightString = "{0} {1}";

	private InteractableType _lastInteractableType;

	private string _lastInteractableString;

	private bool _isCastingShadows = true;

	private bool _isShadowChangeScheduled;

	private static float _lightPowerUsage = 50f;

	private int _helmetFlushHash = Animator.StringToHash("HelmetFlush");

	private byte _soundVolume = 50;

	private byte _soundAlert;

	private PooledAudioSource _playingAudio;

	protected override bool HasPaintableMaskMaterial => true;

	public BatteryCell ParentBattery
	{
		get
		{
			if ((bool)ParentHuman && ParentHuman.HelmetSlot == base.ParentSlot && (bool)ParentHuman.Suit?.AsThing)
			{
				return ParentHuman.Suit.Battery;
			}
			return null;
		}
	}

	public byte SoundVolume
	{
		get
		{
			return _soundVolume;
		}
		set
		{
			_soundVolume = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
			}
			if (_playingAudio != null)
			{
				_playingAudio.GameAudioSource.SetVolumeMultiplier(AudioManager.Find((SoundAlert)SoundAlert).NameHash, (float)(int)SoundVolume / 100f);
			}
		}
	}

	public byte SoundAlert
	{
		get
		{
			return _soundAlert;
		}
		set
		{
			_soundAlert = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
			if (!GameManager.IsBatchMode)
			{
				WaitThenPlay().Forget();
			}
		}
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		if (base.ParentSlot != null && ParentHuman?.HelmetSlot == base.ParentSlot)
		{
			return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
		}
		return base.GetRenderMaxDistanceSquared();
	}

	public override void Awake()
	{
		base.Awake();
		if (!IsCursor)
		{
			Thing.AllIWearableLights.Add(this);
		}
	}

	public override void SetOcclusion()
	{
		base.SetOcclusion();
		if ((object)InventoryManager.Parent != null && !_isShadowChangeScheduled && OcclusionManager.ShadowQualitySetting != ShadowQuality.Disable)
		{
			bool flag = RootParent.Position.DistanceSquared(InventoryManager.WorldPosition) < (float)OcclusionManager.HelmetLightShadowDistanceSq();
			if (!flag && _isCastingShadows)
			{
				_isShadowChangeScheduled = true;
				ScheduleShadowChange(shouldCastShadows: false).Forget();
			}
			else if (flag && !_isCastingShadows)
			{
				_isShadowChangeScheduled = true;
				ScheduleShadowChange(shouldCastShadows: true).Forget();
			}
		}
	}

	private async UniTaskVoid ScheduleShadowChange(bool shouldCastShadows)
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		await UniTask.NextFrame(cancelToken);
		if (base.BeingDestroyed || cancelToken.IsCancellationRequested || GameManager.GameState == GameState.None)
		{
			return;
		}
		LightShadows shadows = LightShadows.None;
		if (shouldCastShadows)
		{
			switch (OcclusionManager.ShadowQualitySetting)
			{
			case ShadowQuality.Disable:
				shadows = LightShadows.None;
				break;
			case ShadowQuality.HardOnly:
				shadows = LightShadows.Hard;
				break;
			case ShadowQuality.All:
				shadows = LightShadows.Soft;
				break;
			default:
				ConsoleWindow.PrintError($"Invalid Shadow Quality Setting: {OcclusionManager.ShadowQualitySetting}");
				break;
			}
		}
		foreach (ThingLight light in Lights)
		{
			if (light?.Light != null)
			{
				light.Light.shadows = shadows;
			}
		}
		_isCastingShadows = shouldCastShadows;
		_isShadowChangeScheduled = false;
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		if (CustomColor != null)
		{
			HandlePaintableMaskMaterial();
		}
	}

	private void HandlePaintableMaskMaterial()
	{
		Material material = Renderers[0].GetRenderer().material;
		CustomColor.ApplyToSuitMaterial(material);
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == _lastInteractableType)
		{
			return _lastInteractableString;
		}
		_lastInteractableType = interactable.Action;
		switch (interactable.Action)
		{
		case InteractableType.OnOff:
			_lastInteractableString = string.Format(_lightString, Localization.GetInterface("LightString"), OnOff ? ActionStrings.Off : ActionStrings.On);
			return _lastInteractableString;
		case InteractableType.Open:
			_lastInteractableString = string.Format(_maskString, Localization.GetInterface("HelmetString"), IsOpen ? ActionStrings.Close : ActionStrings.Open);
			return _lastInteractableString;
		case InteractableType.Lock:
			_lastInteractableString = string.Format(_maskString, Localization.GetInterface("HelmetString"), IsLocked ? ActionStrings.Unlock : ActionStrings.Lock);
			return _lastInteractableString;
		default:
			_lastInteractableString = base.GetContextualName(interactable);
			return _lastInteractableString;
		}
	}

	public override CanEnterResult CanEnter(Slot destinationSlot)
	{
		return base.CanEnter(destinationSlot);
	}

	protected virtual void EnterInventory()
	{
		ListenerEffectManager.SetHelmetFilter(!IsOpen, this);
		LayerMask layerMask = (((bool)ParentHuman && ParentHuman.IsLocalPlayer) ? Layers.PlayerInvisible : Layers.Default);
		foreach (ThingRenderer renderer in Renderers)
		{
			renderer.BaseLayer = layerMask;
			renderer.SetLayer(layerMask);
		}
		if (!GameManager.IsBatchMode && Settings.CurrentData.HelmetOverlay && (bool)InventoryManager.ParentHuman && InventoryManager.ParentHuman.IsLocalPlayer && ParentHuman != null && ParentHuman.IsLocalPlayer && base.ParentSlot != null && base.ParentSlot.Type == SlotType)
		{
			FirstPersonHelmetOverlay.Instance.OnHelmetSlotChange(this);
		}
	}

	public override void OnParentLocalityChange()
	{
		base.OnParentLocalityChange();
		EnterInventory();
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		ParentHuman = parent as Human;
		EnterInventory();
	}

	public override void OnExitInventory(Thing oldParent)
	{
		base.OnExitInventory(oldParent);
		if (ParentHuman == oldParent)
		{
			ParentHuman = null;
		}
		ListenerEffectManager.SetHelmetFilter(isOn: false, this);
		if (oldParent != null && oldParent.HasAuthority)
		{
			for (int i = 0; i < Renderers.Count; i++)
			{
				Renderers[i].BaseLayer = Human.LayerDefault;
				Renderers[i].SetLayer(Human.LayerDefault);
			}
		}
		if (!GameManager.IsBatchMode && Settings.CurrentData.HelmetOverlay && (bool)InventoryManager.ParentHuman && InventoryManager.ParentHuman.HasAuthority && oldParent != null && oldParent.HasAuthority && FirstPersonHelmet != null && FirstPersonHelmetOverlay.CurrentEquippedHelmet != null && FirstPersonHelmetOverlay.CurrentEquippedHelmet == FirstPersonHelmet)
		{
			FirstPersonHelmetOverlay.Instance.OnHelmetSlotChange(null);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!Powered && (object)ParentBattery != null && !ParentBattery.IsEmpty)
		{
			OnServer.Interact(base.InteractPowered, 1);
			return;
		}
		if (Powered && ((object)ParentBattery == null || ParentBattery.IsEmpty))
		{
			OnServer.Interact(base.InteractPowered, 0);
			return;
		}
		if (OnOff && Powered && (object)ParentBattery != null)
		{
			ParentBattery.PowerStored -= _lightPowerUsage;
		}
		if ((bool)ParentHuman && base.ParentSlot == ParentHuman.HelmetSlot && (bool)ParentHuman.Suit?.AsThing)
		{
			AtmosphereHelper.Mix(base.InternalAtmosphere, ParentHuman.Suit.InternalAtmosphere, AtmosphereHelper.MatterState.Gas);
		}
		else if (base.InternalAtmosphere.IsValid())
		{
			base.WorldAtmosphere = base.GridController.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
			AtmosphereHelper.Mix(base.InternalAtmosphere, base.WorldAtmosphere, AtmosphereHelper.MatterState.Gas);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (_helmetAnimationComponent != null)
		{
			_helmetAnimationComponent.RefreshState(skipAnimation);
		}
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		WaitEffects().Forget();
	}

	private async UniTaskVoid WaitEffects()
	{
		await UniTask.NextFrame();
		foreach (ThingLight light in Lights)
		{
			light.Refresh();
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new GasMaskSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
	}

	public async UniTaskVoid FlushMaskFromThread()
	{
		await UniTask.SwitchToMainThread();
		FlushMask();
	}

	public void FlushMask()
	{
		if (!GameManager.RunSimulation || base.InternalAtmosphere == null || ((bool)ParentHuman && ParentHuman.HelmetSlot != base.ParentSlot) || !base.GridController.CanContainAtmos(base.WorldGrid) || base.InteractButton1.State == 1)
		{
			return;
		}
		AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, base.InternalAtmosphere.GasMixture);
		AtmosphericEventInstance.Reset(base.InternalAtmosphere);
		if ((bool)ParentHuman && (bool)ParentHuman.Suit?.AsThing)
		{
			AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, ParentHuman.Suit.InternalAtmosphere.GasMixture);
			AtmosphericEventInstance.Reset(ParentHuman.Suit.InternalAtmosphere);
		}
		Entity entity = RootParent as Entity;
		Human human = RootParent as Human;
		if ((bool)entity && (bool)human && human.SpeciesClass != SpeciesClass.Robot)
		{
			if (base.WorldAtmosphere != null)
			{
				AtmosphericEventInstance.CreateAdd(base.WorldAtmosphere, new GasMixture(entity.LungAtmosphere.GasMixture));
			}
			AtmosphericEventInstance.Reset(entity.LungAtmosphere);
		}
		else if ((bool)entity && human == null)
		{
			if (base.WorldAtmosphere != null)
			{
				AtmosphericEventInstance.CreateAdd(base.WorldAtmosphere, new GasMixture(entity.LungAtmosphere.GasMixture));
			}
			AtmosphericEventInstance.Reset(entity.LungAtmosphere);
		}
		OnServer.Interact(base.InteractButton1, 1);
		base.InteractButton1.WaitThenResetInteractableState().Forget();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteByte(SoundVolume);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteByte(SoundAlert);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			SoundVolume = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			SoundAlert = reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte(SoundVolume);
		writer.WriteByte(SoundAlert);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		SoundVolume = reader.ReadByte();
		SoundAlert = reader.ReadByte();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		InteractableType action = interactable.Action;
		if ((action == InteractableType.OnOff || action == InteractableType.Powered) && _spotlight != null)
		{
			_spotlight.SetActive(OnOff && Powered);
		}
		if (interactable.Action == InteractableType.Open)
		{
			ListenerEffectManager.SetHelmetFilter(!IsOpen, this);
		}
		if (!Powered && _playingAudio != null)
		{
			_playingAudio.Stop(immediate: true);
			_playingAudio = null;
		}
		else if (Powered && _playingAudio == null && SoundAlert != 0 && !GameManager.IsBatchMode)
		{
			WaitThenPlay().Forget();
		}
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Volume || logicType - 174 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Volume || logicType == LogicType.SoundAlert)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.Flush:
			if (value > double.Epsilon)
			{
				FlushMaskFromThread().Forget();
			}
			break;
		case LogicType.Volume:
			SoundVolume = (byte)Mathf.Clamp((int)value, 0, 100);
			break;
		case LogicType.SoundAlert:
			SoundAlert = (byte)Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1);
			break;
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Volume => (int)SoundVolume, 
			LogicType.SoundAlert => (int)SoundAlert, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	private async UniTaskVoid WaitThenPlay()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if (_playingAudio != null && PooledAudioSources.Contains(_playingAudio))
		{
			_playingAudio.Stop(immediate: true);
			_playingAudio = null;
		}
		if (SoundAlert != 0)
		{
			_playingAudio = Thing.PlayPooledAudioSound(this, Singleton<AudioManager>.Instance.GetChannelData(Defines.SoundChannel.Small));
		}
	}

	public override bool PreventInteraction(out DelayedActionInstance failResult, Interactable interactable, Interaction interaction)
	{
		InteractableType action = interactable.Action;
		if (action == InteractableType.OnOff || action == InteractableType.Button1)
		{
			failResult = null;
			return false;
		}
		return base.PreventInteraction(out failResult, interactable, interaction);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		switch (interactable.Action)
		{
		case InteractableType.OnOff:
		case InteractableType.Lock:
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return delayedActionInstance.Succeed();
		case InteractableType.Open:
			if (IsLocked)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceLocked);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return delayedActionInstance.Succeed();
		case InteractableType.Button1:
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			FlushMask();
			return delayedActionInstance.Succeed();
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			Thing.AllIWearableLights.Remove(this);
			ListenerEffectManager.SetHelmetFilter(isOn: false, this);
			if ((bool)FirstPersonHelmetOverlay.Instance)
			{
				FirstPersonHelmetOverlay.Instance.DestroyCurrentHelmet();
			}
			base.OnDestroy();
		}
	}

	public override void SetWearVisibility(bool shouldUpdateLayers = true)
	{
		base.SetWearVisibility(shouldUpdateLayers);
		if (FirstPersonHelmet?.Helmet != null)
		{
			FirstPersonHelmet.Helmet.SetLayerRecursive(FirstPersonHelmetOverlay.FirstPersonLayer);
		}
	}

	public void SetWearableVisibility(bool clothingOn)
	{
	}

	public void RefreshVisibility()
	{
	}
}
