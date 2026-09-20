using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Sound;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class WallLight : SmallDevice, IAirlockDevice, ISmartRotatable, ILight, IShadowLight, IDensePoolable
{
	private const float WALL_LIGHT_RENDER_DISTANCE = 40f;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	[Header("Wall Light")]
	[SerializeField]
	private Light light;

	[SerializeField]
	private Light terrainLight;

	[SerializeField]
	protected LensFlare lodFlare;

	private readonly DensePoolReference<IShadowLight> _shadowLightPool = new DensePoolReference<IShadowLight>(OcclusionManager.AllShadowLights);

	private static readonly int WallLightHumHash = Animator.StringToHash("WallLightHum");

	private PooledAudioSource _poweredAudio;

	private bool _isPlayingSound;

	protected bool isCastingShadows = true;

	protected bool isShadowChangeScheduled;

	public Light Light => light;

	public override float AudioDistanceSquared => 25f;

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
				if (_isPlayingSound)
				{
					_poweredAudio = PlayPooledAudioSound(WallLightHumHash, light.transform.localPosition);
					return;
				}
				_poweredAudio?.Stop();
				_poweredAudio = null;
			}
		}
	}

	public bool IsShadowCandidate { get; private set; }

	public float ShadowDistanceSquared { get; private set; }

	protected virtual bool NeverCastShadows => false;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(40f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(40f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override bool OnAddToPool(object densePool, int slot)
	{
		if (_shadowLightPool.CanAddToPool(densePool))
		{
			return _shadowLightPool.AddToPool(densePool, slot);
		}
		return base.OnAddToPool(densePool, slot);
	}

	public override void OnRemoveFromPool(object densePool)
	{
		_shadowLightPool.OnRemovedFrom(densePool);
		base.OnRemoveFromPool(densePool);
	}

	public override void Awake()
	{
		base.Awake();
		Thing.AllILights.Add(this);
		OcclusionManager.AllShadowLights.Add(this);
		if (!NeverCastShadows)
		{
			return;
		}
		foreach (ThingLight light in Lights)
		{
			if ((bool)light?.Light)
			{
				light.Light.shadows = LightShadows.None;
			}
		}
	}

	public override void OnDestroy()
	{
		IsPlayingSound = false;
		base.OnDestroy();
		Thing.AllILights.Remove(this);
		OcclusionManager.AllShadowLights.Remove(this);
		IsShadowCandidate = false;
	}

	public override void UpdateAudio(float deltaTime)
	{
		if (!IsOccluded && OnOff && Powered && (bool)light)
		{
			IsPlayingSound = IsAudible();
		}
		else
		{
			IsPlayingSound = false;
		}
		if ((bool)_poweredAudio && !_poweredAudio.IsActive)
		{
			_poweredAudio = null;
		}
	}

	public override CanConstructInfo CanConstruct()
	{
		CanMountResult canMountResult = CanMountOnWall();
		if (canMountResult.result == WallMountResult.Valid)
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(canMountResult.ResultMessage());
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		if (GameManager.IsValidColor(index))
		{
			SetLightsCustomColor();
			SetCustomColor(OnOff && Powered);
		}
	}

	public virtual void SetLightsCustomColor()
	{
		foreach (ThingLight light in Lights)
		{
			light.Light.color = CustomColor.Light;
		}
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		if ((bool)PaintableMaterial && !CustomColor.Emissive)
		{
			CustomColor = GameManager.GetColorSwatch(PaintableMaterial);
		}
		SetCustomColor(OnOff && Powered);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.OnOff)
		{
			if (IsLocked)
			{
				return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceLocked);
			}
			if (!doAction)
			{
				return DelayedActionInstance.Success(interactable.ContextualName);
			}
			OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
			return DelayedActionInstance.Success(interactable.ContextualName);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.LightCategory);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.OnOff || interactable.Action == InteractableType.Powered)
		{
			ToggleLightsOn(OnOff && Powered);
		}
		if (interactable.Action == InteractableType.OnOff && IsAudible())
		{
			Thing.PlayPooledAudioSound(this, OnOff ? Defines.Sounds.SwitchOn : Defines.Sounds.SwitchOff, Vector3.zero, 0.5f);
		}
	}

	protected virtual void ToggleLightsOn(bool on)
	{
		if (!base.HasBaseAnimator)
		{
			if (GetOpenEndsPermutation() != null)
			{
				light.enabled = on;
			}
			if ((bool)terrainLight)
			{
				terrainLight.enabled = on;
			}
			if ((bool)lodFlare)
			{
				lodFlare.gameObject.SetActive(on);
			}
			SetCustomColor(on);
		}
	}

	public override void SetOcclusion()
	{
		base.SetOcclusion();
		if (!NeverCastShadows)
		{
			ShadowDistanceSquared = base.Position.DistanceSquared(InventoryManager.WorldPosition);
			IsShadowCandidate = (object)InventoryManager.Parent != null && OcclusionManager.ShadowQualitySetting != ShadowQuality.Disable && OnOff && Powered && !IsOccluded && ShadowDistanceSquared < (float)OcclusionManager.LightShadowDistanceSq;
		}
	}

	public void SetShadowCasting(bool shouldCastShadows)
	{
		if (!isShadowChangeScheduled && !NeverCastShadows && !base.IsBeingDestroyed && shouldCastShadows != isCastingShadows)
		{
			isShadowChangeScheduled = true;
			ShadowChange(shouldCastShadows).Forget();
		}
	}

	protected async UniTaskVoid ShadowChange(bool newShouldCastShadows)
	{
		if (NeverCastShadows)
		{
			newShouldCastShadows = false;
		}
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
		if (newShouldCastShadows)
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
			if ((bool)light?.Light)
			{
				light.Light.shadows = shadows;
			}
		}
		if ((bool)terrainLight)
		{
			ThingShadowMode thingShadowMode = OcclusionManager.ThingShadowMode;
			if (thingShadowMode == ThingShadowMode.Medium || thingShadowMode == ThingShadowMode.Low)
			{
				terrainLight.shadows = LightShadows.None;
			}
		}
		isCastingShadows = newShouldCastShadows;
		isShadowChangeScheduled = false;
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SetCustomColor(OnOff && Powered);
		ShadowChange(newShouldCastShadows: false).Forget();
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
}
