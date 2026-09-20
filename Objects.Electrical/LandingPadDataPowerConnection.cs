using System.Threading;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Cysharp.Threading.Tasks;
using Util;

namespace Objects.Electrical;

public class LandingPadDataPowerConnection : LandingPadModularDevice
{
	private CancellationTokenWrapper _activateCancellation = new CancellationTokenWrapper();

	public override string[] ModeStrings => LandingPadCenter.LandingPadModeStrings;

	public override bool HasReadableAtmosphere => true;

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Powered)
		{
			base.LandingPadCenter?.RefreshPadPower();
		}
		if (interactable.Action == InteractableType.Activate && GameManager.RunSimulation)
		{
			if (base.LandingPadCenter != null)
			{
				OnServer.Interact(base.LandingPadCenter.InteractActivate, Activate);
			}
			if (Activate != 0)
			{
				_activateCancellation.Cancel();
				_activateCancellation.Initialize();
				ResetActivate(_activateCancellation.Token).Forget();
			}
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (interactable.Action == InteractableType.Activate)
		{
			if (IsLocked)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceLocked);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation && Activate == 0)
			{
				OnServer.Interact(base.InteractActivate, 1);
			}
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	private async UniTaskVoid ResetActivate(CancellationToken token)
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		await UniTask.Delay(500, DelayType.DeltaTime, PlayerLoopTiming.Update, token);
		await UniTask.WaitUntil(delegate
		{
			GameState gameState = GameManager.GameState;
			return gameState == GameState.Running || gameState == GameState.None;
		}, PlayerLoopTiming.Update, token);
		if (GameManager.GameState == GameState.Running)
		{
			OnServer.Interact(base.InteractActivate, 0);
			if ((bool)base.LandingPadCenter)
			{
				base.LandingPadCenter.SetLogicValue(LogicType.Activate, 0.0);
			}
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		base.LandingPadCenter?.RefreshPadPower();
	}

	public override void OnDestroy()
	{
		LandingPadCenter landingPadCenter = base.LandingPadCenter;
		base.OnDestroy();
		landingPadCenter?.RefreshPadPower();
	}

	public override void OnStructureNetworkUpdated()
	{
	}

	public override double GasRatio(LogicType logicType)
	{
		return AtmosphereHelper.GasRatio(logicType, base.LandingPadNetwork?.Atmosphere);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Mode || logicType == LogicType.Vertical || logicType == LogicType.ContactTypeId)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Vertical => true, 
			LogicType.Mode => false, 
			_ => base.CanLogicWrite(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Mode:
		case LogicType.Vertical:
			if (base.LandingPadCenter != null)
			{
				return base.LandingPadCenter.GetLogicValue(logicType);
			}
			return -1.0;
		case LogicType.Activate:
			if (base.LandingPadCenter != null)
			{
				return base.LandingPadCenter.GetLogicValue(logicType);
			}
			break;
		case LogicType.Combustion:
			if (base.LandingPadNetwork?.Atmosphere?.Sparked != true)
			{
				return 0.0;
			}
			return 1.0;
		case LogicType.TotalMoles:
			return (base.LandingPadNetwork?.Atmosphere?.TotalMoles.ToDouble()).GetValueOrDefault();
		case LogicType.Pressure:
			return (base.LandingPadNetwork?.Atmosphere?.PressureGassesAndLiquids.ToDouble()).GetValueOrDefault();
		case LogicType.Temperature:
			return (base.LandingPadNetwork?.Atmosphere?.Temperature.ToDouble()).GetValueOrDefault();
		case LogicType.ContactTypeId:
			return (base.LandingPadCenter?.CurrentTradingContact?.DataInstance?.TraderData?.IdHash).GetValueOrDefault();
		}
		return base.GetLogicValue(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		if (logicType == LogicType.Vertical && base.LandingPadCenter != null)
		{
			base.LandingPadCenter.SetLogicValue(logicType, value);
		}
	}
}
