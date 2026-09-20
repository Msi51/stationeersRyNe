using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical.Helper;
using Objects.DeviceParts;
using Objects.Electrical;
using Trading;
using UnityEngine;

namespace Objects.LandingPads;

public class LandingPadTankConnector : LandingPadModularDevice, IPortablesConnector, IFastenedConnector, IReferencable, IEvaluable
{
	[SerializeField]
	private TwoStateButton _arrowIn;

	[SerializeField]
	private TwoStateButton _arrowOut;

	[SerializeField]
	private GameObject _connectionObject;

	[SerializeField]
	private Rotator _fanRotator;

	[SerializeField]
	private AtmosphereHelper.MatterState pumpType = AtmosphereHelper.MatterState.Gas;

	[SerializeField]
	private float volumeMoved;

	public override float AudioDistanceSquared => 25f;

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.LandingPadCenter != null && base.LandingPadCenter.Error == 0 && base.IsStructureCompleted;
			if (Error == 1)
			{
				if (!flag)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag)
			{
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

	public Slot TankSlot => Slots[0];

	private void RefreshConnector()
	{
		bool active = TankSlot.Contains<IVisuallyConnectable>();
		_connectionObject.SetActive(active);
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		RefreshConnector();
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		RefreshConnector();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		RefreshConnector();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		RefreshConnector();
	}

	public override void OnStructureNetworkUpdated()
	{
		base.OnStructureNetworkUpdated();
		if (GameManager.RunSimulation)
		{
			AssessPower(null, base.LandingPadCenter != null && base.LandingPadCenter.OnOff);
		}
	}

	protected override void AssessPower(CableNetwork cableNetwork, bool isOn)
	{
		SetPower(cableNetwork, isOn);
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		AssessPower(null, base.LandingPadCenter != null && base.LandingPadCenter.OnOff);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		AssessPower(null, base.LandingPadCenter != null && base.LandingPadCenter.OnOff);
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		_arrowIn.ChangeState((OnOff && Powered && Mode == 1) ? 1 : 0, skipAnimation || !base.IsStructureCompleted);
		_arrowOut.ChangeState((OnOff && Powered && Mode == 2) ? 1 : 0, skipAnimation || !base.IsStructureCompleted);
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (!GameManager.IsBatchMode && base.IsStructureCompleted && !IsOccluded)
		{
			_fanRotator.DoUpdate(OnOff && Powered);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		return interactable.Action switch
		{
			InteractableType.Button1 => InteractWithButton1(interactable, interaction, doAction), 
			InteractableType.Button2 => InteractWithButton2(interactable, interaction, doAction), 
			_ => base.InteractWith(interactable, interaction, doAction), 
		};
	}

	private DelayedActionInstance InteractWithButton1(Interactable interactable, Interaction interaction, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (!Powered || !IsOperable)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceIsNotOperable);
		}
		if (GameManager.RunSimulation && doAction)
		{
			int pumpOnOff = ((Mode != 1) ? 1 : 0);
			SetPumpOnOff(pumpOnOff);
		}
		delayedActionInstance.AppendStateMessage(GameStrings.PumpIntoPad);
		return delayedActionInstance.Succeed();
	}

	private DelayedActionInstance InteractWithButton2(Interactable interactable, Interaction interaction, bool doAction)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (!Powered || !IsOperable)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceIsNotOperable);
		}
		if (GameManager.RunSimulation && doAction)
		{
			int pumpOnOff = ((Mode != 2) ? 2 : 0);
			SetPumpOnOff(pumpOnOff);
		}
		delayedActionInstance.AppendStateMessage(GameStrings.PumpIntoTank);
		return delayedActionInstance.Succeed();
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (TankSlot.Contains<DynamicGasCanister>(out var occupant))
		{
			occupant.MainCollider.enabled = true;
		}
		return base.AttackWith(attack, doAction);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!OnOff || !Powered || !IsOperable)
		{
			return;
		}
		PortableAtmospherics portableAtmospherics = TankSlot.Get<PortableAtmospherics>();
		if ((object)portableAtmospherics == null)
		{
			SetPumpOnOff(0);
			return;
		}
		Atmosphere inputAtmos = ((Mode == 1) ? portableAtmospherics.InternalAtmosphere : base.LandingPadNetwork.Atmosphere);
		Atmosphere outputAtmos = ((Mode == 1) ? base.LandingPadNetwork.Atmosphere : portableAtmospherics.InternalAtmosphere);
		switch (pumpType)
		{
		case AtmosphereHelper.MatterState.Gas:
			AtmosphereHelper.MoveVolume(inputAtmos, outputAtmos, new VolumeLitres(volumeMoved), AtmosphereHelper.MatterState.Gas, new MoleQuantity(volumeMoved));
			break;
		case AtmosphereHelper.MatterState.Liquid:
			AtmosphereHelper.MoveLiquidVolume(inputAtmos, outputAtmos, new VolumeLitres(volumeMoved));
			break;
		default:
			ConsoleWindow.PrintError(DisplayName + " unsupported matter state");
			break;
		}
	}

	private void SetPumpOnOff(int mode)
	{
		OnServer.Interact(base.InteractOnOff, (mode != 0) ? 1 : 0);
		OnServer.Interact(base.InteractMode, mode);
	}

	public override string GetContextualName(Interactable interactable)
	{
		return interactable.Action switch
		{
			InteractableType.Button1 => GameStrings.Inward, 
			InteractableType.Button2 => GameStrings.Outward, 
			_ => base.GetContextualName(interactable), 
		};
	}
}
