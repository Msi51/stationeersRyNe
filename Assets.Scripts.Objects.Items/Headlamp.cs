using System.Text;
using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Headlamp : PowerTool, IWearable, IReferencable, IEvaluable, IWearableLight
{
	[SerializeField]
	private GameObject _light;

	[SerializeField]
	private MaterialChanger _lightMaterialChanger;

	private bool _isCastingShadows = true;

	private bool _isShadowChangeScheduled;

	private new const float RENDER_DISTANCE = 100f;

	public override StringBuilder GetSlotTooltip()
	{
		StringBuilder slotTooltip = base.GetSlotTooltip();
		if (base.ParentSlot?.Parent is SuitStorage && (bool)base.Battery)
		{
			slotTooltip.AppendLine(GameStrings.ContainsItemInSlotAtQuantity.AsString(base.Battery.ToTooltip(), base.BatterySlot.ToTooltip(), base.Battery.GetQuantityText().AsColor("yellow")));
		}
		return slotTooltip;
	}

	public override void Awake()
	{
		base.Awake();
		if (!IsCursor)
		{
			Thing.AllIWearableLights.Add(this);
		}
	}

	public override void OnDestroy()
	{
		Thing.AllIWearableLights.Remove(this);
		base.OnDestroy();
	}

	public override void SetOcclusion()
	{
		base.SetOcclusion();
		if ((object)InventoryManager.Parent != null && !_isShadowChangeScheduled && OcclusionManager.ShadowQualitySetting != ShadowQuality.Disable)
		{
			bool flag = base.Position.DistanceSquared(InventoryManager.WorldPosition) < (float)OcclusionManager.HelmetLightShadowDistanceSq();
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

	protected override float GetRenderMaxDistanceSquared()
	{
		if (base.ParentSlot != null && RootParentHuman?.HelmetSlot == base.ParentSlot)
		{
			return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
		}
		return base.GetRenderMaxDistanceSquared();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Powered || interactable.Action == InteractableType.OnOff)
		{
			bool flag = OnOff && Powered;
			_light.SetActive(flag);
			_lightMaterialChanger.ChangeState(flag ? Defines.Animator.On : Defines.Animator.Off);
		}
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (OnOff && GameManager.RunSimulation && parent is Structure)
		{
			OnServer.Interact(base.InteractOnOff, 0);
		}
		SetGameObjectLayer();
	}

	public override void OnParentLocalityChange()
	{
		base.OnParentLocalityChange();
		SetGameObjectLayer();
	}

	private void SetGameObjectLayer()
	{
		LayerMask layerMask = (((bool)RootParentHuman && RootParentHuman.IsLocalPlayer) ? Layers.PlayerInvisible : Layers.Default);
		foreach (ThingRenderer renderer in Renderers)
		{
			renderer.BaseLayer = layerMask;
			renderer.SetLayer(layerMask);
		}
	}

	public void SetWearableVisibility(bool clothingOn)
	{
	}

	public void RefreshVisibility()
	{
	}
}
