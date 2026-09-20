using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networks;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Helmet : HelmetBase, IBatteryPowered, IPowered, IDensePoolable, IReferencable, IEvaluable, IWearableLight
{
	private bool _isCastingShadows = true;

	private bool _isShadowChangeScheduled;

	public Slot BatterySlot => Slots[0];

	public BatteryCell Battery => BatterySlot.Occupant as BatteryCell;

	public override void Awake()
	{
		base.Awake();
		ElectricityManager.Register(this);
		if (!IsCursor)
		{
			Thing.AllIWearableLights.Add(this);
		}
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
			ElectricityManager.Deregister(this);
			Thing.AllIWearableLights.Remove(this);
		}
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

	public void Recharge(float ammount)
	{
		if ((bool)Battery)
		{
			Battery.PowerStored += ammount;
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.PersonalHelmets);
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (GameManager.RunSimulation && !IsCursor && (bool)Battery && !Battery.IsEmpty && GameManager.GameState == GameState.Running)
		{
			Battery.PowerStored -= 5f;
			CheckPowerState();
		}
	}

	private void CheckPowerState()
	{
		if (Powered && (!OnOff || Battery == null || Battery.IsEmpty))
		{
			OnServer.Interact(base.InteractPowered, 0, skipAnimation: true);
		}
		else if (!Powered && OnOff && (bool)Battery && !Battery.IsEmpty)
		{
			OnServer.Interact(base.InteractPowered, 1, skipAnimation: true);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		CheckPowerState();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		CheckPowerState();
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		CheckPowerState();
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		WaitEffects().Forget();
	}

	private async UniTaskVoid WaitEffects()
	{
		await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());
		foreach (ThingLight light in Lights)
		{
			light.Refresh();
		}
	}
}
