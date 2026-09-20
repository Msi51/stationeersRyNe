using System.Text;
using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Util;

namespace Objects.Structures;

public class StructureDrinkingFountain : DeviceInput
{
	[Header("Drinking Fountain")]
	[SerializeField]
	private GameObject waterMesh;

	private CancellationTokenWrapper ActivateReset = new CancellationTokenWrapper();

	private const float DRINKING_TIME_MODIFIER = 0.5f;

	public override bool IsInputValid
	{
		get
		{
			if (ConnectedPipeNetwork != null)
			{
				return ConnectedPipeNetwork.IsNetworkValid();
			}
			return false;
		}
	}

	private bool WaterTooHot
	{
		get
		{
			if (ConnectedPipeNetwork?.Atmosphere != null)
			{
				return ConnectedPipeNetwork?.Atmosphere.Temperature > Chemistry.Temperature.ZeroDegrees + new TemperatureKelvin(100.0);
			}
			return false;
		}
	}

	private bool WaterTooCold
	{
		get
		{
			if (ConnectedPipeNetwork?.Atmosphere != null)
			{
				return ConnectedPipeNetwork?.Atmosphere.Temperature < Chemistry.Temperature.ZeroDegrees;
			}
			return false;
		}
	}

	private bool WaterPolluted
	{
		get
		{
			if (ConnectedPipeNetwork?.Atmosphere != null)
			{
				if (!(ConnectedPipeNetwork.Atmosphere.GasMixture.TotalToxins > MoleQuantity.Zero))
				{
					return ConnectedPipeNetwork.Atmosphere.GasMixture.GetTotalMolesLiquids > ConnectedPipeNetwork.Atmosphere.GasMixture.Water.Quantity;
				}
				return true;
			}
			return false;
		}
	}

	private Atmosphere DrinkingAtmosphere => ConnectedPipeNetwork?.Atmosphere;

	protected override bool IsOperable
	{
		get
		{
			bool flag = IsInputValid && !WaterTooCold && !WaterTooHot && !WaterPolluted;
			if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (GameManager.RunSimulation && HasErrorState && Error == 1 && flag)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return flag;
		}
	}

	private static int MaxDrinkTimeMS => (int)HydrateTime(new MoleQuantity(100.0)) * 1000;

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (waterMesh != null && !base.BeingDestroyed)
		{
			waterMesh.SetActive(Activate == 1);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		_ = IsOperable;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			return HandleActivate(interactable, interaction, doAction);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && interactable.Action == InteractableType.Activate && Activate == 1)
		{
			ActivateReset.CancelAndInitialize();
			ResetActivate(ActivateReset.Token).Forget();
		}
	}

	private async UniTaskVoid ResetActivate(CancellationToken token)
	{
		await UniTask.Delay(MaxDrinkTimeMS, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		if (Activate == 1)
		{
			OnServer.Interact(base.InteractActivate, 0);
		}
	}

	public float HydrateAmount(Entity consumer)
	{
		float b = consumer.GetHydrationStorage() - consumer.Hydration;
		b = Mathf.Max(0f, b);
		return b / 5f;
	}

	private static float HydrateTime(MoleQuantity molesToDrink)
	{
		return molesToDrink.ToFloat() * HydrationBase.HydrationPerMole * 0.5f;
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			return GameStrings.DrinkFromFountain;
		}
		return base.GetContextualName(interactable);
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (WaterTooCold)
		{
			extendedText.AppendLine(GameStrings.DeviceWaterTooCold);
		}
		if (WaterTooHot)
		{
			extendedText.AppendLine(GameStrings.DeviceWaterTooHot);
		}
		if (WaterPolluted)
		{
			extendedText.AppendLine(GameStrings.DeviceWaterPolluted);
		}
		return base.GetExtendedText();
	}

	private DelayedActionInstance HandleActivate(Interactable interactable, Interaction interaction, bool doAction)
	{
		float num = HydrateAmount(interaction.SourceThing as Human);
		MoleQuantity val = (Mathf.Approximately(num, 0f) ? MoleQuantity.Zero : new MoleQuantity(55.55555555555556 * (double)num));
		MoleQuantity moleQuantity = DrinkingAtmosphere?.GasMixture.Water.Quantity ?? MoleQuantity.Zero;
		val = RocketMath.Min(val, moleQuantity);
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = HydrateTime(val),
			ActionMessage = interactable.ContextualName,
			ActionSoundHash = Item.DrinkingHash,
			ActionCompleteSoundHash = Item.DrinkingFinishedHash
		};
		if (!Powered)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
		}
		if (!OnOff)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
		}
		if (WaterTooCold)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceWaterTooCold);
		}
		if (WaterTooHot)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceWaterTooHot);
		}
		if (WaterPolluted)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceWaterPolluted);
		}
		if (moleQuantity < Chemistry.MINIMUM_QUANTITY_MOLES)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceNotEnoughWater);
		}
		if (Error == 1)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceError);
		}
		Human human = interaction.SourceThing as Human;
		if (Mathf.Approximately(num, 0f))
		{
			return delayedActionInstance.Fail(GameStrings.DrinkNotThirsty);
		}
		if (!human.CanDrink())
		{
			return delayedActionInstance.Fail(GameStrings.DifficultyCannotDrinkThroughHelmet);
		}
		if (!GameManager.IsBatchMode && human == InventoryManager.Parent)
		{
			if (KeyManager.GetMouse("Primary") && Activate == 0)
			{
				Thing.Interact(base.InteractActivate, 1);
			}
			else if (!KeyManager.GetMouse("Primary") && Activate == 1)
			{
				Thing.Interact(base.InteractActivate, 0);
			}
		}
		if (!doAction)
		{
			return delayedActionInstance.Succeed();
		}
		if (GameManager.RunSimulation)
		{
			GasMixture gasMixture = new GasMixture(ConnectedPipeNetwork.Atmosphere.GasMixture.Water);
			gasMixture.Scale((val / gasMixture.GetTotalMolesLiquids).ToFloat());
			AtmosphericEventInstance.CreateRemove(ConnectedPipeNetwork.Atmosphere, gasMixture);
			human.Hydrate(gasMixture.Water);
		}
		OnServer.Interact(base.InteractActivate, 0);
		return delayedActionInstance.Succeed();
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Activate || logicType == LogicType.Setting || logicType - 23 <= LogicType.Power)
		{
			return false;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Activate || logicType == LogicType.Setting)
		{
			return false;
		}
		return base.CanLogicWrite(logicType);
	}
}
