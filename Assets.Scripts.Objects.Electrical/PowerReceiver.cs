using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using JetBrains.Annotations;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class PowerReceiver : WirelessPower, ITransmitable, ILogicable, IReferencable, IEvaluable
{
	public Transform DishTarget;

	private PowerTransmitter _linkedPowerTransmitter;

	private float _powerProvided;

	public WirelessNetwork WirelessInputNetwork => InputNetwork as WirelessNetwork;

	public PowerTransmitter LinkedPowerTransmitter
	{
		get
		{
			return _linkedPowerTransmitter;
		}
		set
		{
			if (value != null)
			{
				InputNetwork = value.OutputNetwork;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractMode, (value != null) ? 1 : 0);
			}
			if (value == null)
			{
				base.VisualizerIntensity = 0f;
			}
			_linkedPowerTransmitter = value;
			CheckError();
		}
	}

	public override void Awake()
	{
		base.Awake();
		RayPosition = RayTransform.position;
		PowerTransmitterVisualiser.SetDirection(PowerTransmitterVisualiser.Direction.In);
		PowerTransmitterVisualiser.Deactivate();
	}

	public override void OnDestroy()
	{
		if ((bool)LinkedPowerTransmitter)
		{
			LinkedPowerTransmitter.LinkedReceiver = null;
		}
		base.OnDestroy();
	}

	public void OnTransmitterCreated()
	{
		if (!IsCursor)
		{
			Transmitters.AllTransmitters.Add(this);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new PowerReceiverSaveData();
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

	public void RequestRetarget()
	{
		if (!(LinkedPowerTransmitter == null))
		{
			LinkedPowerTransmitter.TryContactReceiver();
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.OnOff)
		{
			if (OnOff)
			{
				RequestRetarget();
			}
			else
			{
				base.VisualizerIntensity = 0f;
			}
		}
		if (interactable.Action == InteractableType.Powered && !Powered)
		{
			base.VisualizerIntensity = 0f;
		}
	}

	public override bool AllowSetPower(CableNetwork cableNetwork)
	{
		if (WirelessInputNetwork == cableNetwork)
		{
			return true;
		}
		return false;
	}

	public override void RunAfterAnimation()
	{
		RequestRetarget();
	}

	public override void UsePower(CableNetwork cableNetwork, float powerUsed)
	{
		if (Error != 1 && OnOff && cableNetwork == OutputNetwork)
		{
			_powerProvided += powerUsed;
		}
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		if (WirelessInputNetwork == null || cableNetwork == WirelessInputNetwork)
		{
			if (!OnOff || WirelessInputNetwork == null)
			{
				base.VisualizerIntensity = 0f;
				return;
			}
			base.VisualizerIntensity = RocketMath.MapToScale(0f, PowerTransmitter.MaxPowerTransmission, 0f, 1f, powerAdded);
			_powerProvided -= powerAdded;
		}
	}

	public override float GetUsedPower([NotNull] CableNetwork cableNetwork)
	{
		if (InputNetwork == null)
		{
			base.VisualizerIntensity = 0f;
		}
		if (InputNetwork == null || cableNetwork != InputNetwork)
		{
			return 0f;
		}
		if (Error == 1)
		{
			base.VisualizerIntensity = 0f;
			if (!OnOff)
			{
				return 0f;
			}
			return UsedPower;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return Mathf.Min(PowerTransmitter.MaxPowerTransmission + UsedPower, _powerProvided);
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (OutputNetwork == null || Error == 1 || cableNetwork != OutputNetwork)
		{
			return 0f;
		}
		if (!OnOff || WirelessInputNetwork == null)
		{
			return 0f;
		}
		return WirelessInputNetwork.PotentialLoad;
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (!IsCursor && GameManager.GameState == GameState.Running)
		{
			base.Horizontal = 0.0;
			base.Vertical = 1.0;
			base.RotatableBehaviour.TargetHorizontal = base.Horizontal;
			base.RotatableBehaviour.TargetVertical = base.Vertical;
		}
	}

	protected override void CheckConnections()
	{
		Cable cable = OutputConnection.GetCable();
		OutputNetwork = (cable ? cable.CableNetwork : null);
	}
}
