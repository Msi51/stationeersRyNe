using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using Objects.Rockets.Scanning;
using Trading;
using UnityEngine;

public class RocketPayloadBay : DeviceInputOutput, IRocketInternals, IRocketComponent, IRocketActionProgressable, IReferencable, IEvaluable, IRocketMassContributor, IPayloadMount, IFastenedConnector
{
	private float _positionX;

	private float _positionZ;

	private float deployProgress;

	private const float BaseMass = 100f;

	public IRocketPayload Payload => Slots[0].Get<IRocketPayload>();

	public Slot PayloadSlot => Slots[0];

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public float DeployProgress
	{
		get
		{
			return deployProgress;
		}
		set
		{
			deployProgress = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	public float GetActionProgress => DeployProgress;

	protected override bool IsOperable => true;

	public float MassContribution => (100f + Payload?.MassContribution) ?? 100f;

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public string GetActionInfoText()
	{
		return GameStrings.RocketDeployActionInfo.AsString(OnOff ? ActionStrings.On : ActionStrings.Off, IsOpen ? ActionStrings.Opened : ActionStrings.Closed, StringManager.Get(DeployProgress));
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (!(DeployProgress < 1f))
		{
			Payload?.OnDeploy();
			DeployProgress = 0f;
		}
	}

	public override void AssessError()
	{
		base.AssessError();
		DynamicThing dynamicThing = PayloadSlot.Get();
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && !dynamicThing)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if ((GameManager.RunSimulation && HasErrorState && Error == 1 && (bool)dynamicThing) || !OnOff || !Powered)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public bool CanProgressAction(out RocketActionResult result)
	{
		if (!OnOff || !Powered)
		{
			return result = RocketActionResult.Failure(GameStrings.DeviceOffOrUnpowered, this);
		}
		if (!IsOpen)
		{
			return result = RocketActionResult.Failure(GameStrings.ThisDeviceNotOpen, this);
		}
		if (Payload == null)
		{
			return result = RocketActionResult.Failure(GameStrings.DeviceNoPayload, this);
		}
		result = Payload.CanDeploy();
		return result = RocketActionResult.Success;
	}

	public void ProgressDeployAction(float deltaTime, RocketDeploy action)
	{
		if (Payload != null)
		{
			DeployProgress += deltaTime / Payload.TimeToDeploy;
			DeployProgress = Mathf.Min(DeployProgress, 1f);
		}
	}

	public void ClearAction()
	{
		DeployProgress = 0f;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (IsOpen)
		{
			OnServer.Interact(base.InteractOpen, 0);
		}
		RocketNetwork?.RecalculateStructureMass();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		RocketNetwork?.RecalculateStructureMass();
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.PositionX:
			return true;
		case LogicType.PositionZ:
			return true;
		case LogicType.Setting:
		case LogicType.Maximum:
		case LogicType.Ratio:
			return false;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.PositionX => _positionX, 
			LogicType.PositionZ => _positionZ, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.PositionX => true, 
			LogicType.PositionZ => true, 
			LogicType.Setting => false, 
			_ => base.CanLogicWrite(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		switch (logicType)
		{
		case LogicType.PositionX:
			_positionX = (float)value;
			break;
		case LogicType.PositionZ:
			_positionZ = (float)value;
			break;
		default:
			base.SetLogicValue(logicType, value);
			break;
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (PayloadSlot.Contains<RocketPayload>(out var occupant))
		{
			occupant.MainCollider.enabled = true;
		}
		return base.AttackWith(attack, doAction);
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(DeployProgress);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		DeployProgress = reader.ReadSingle();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(DeployProgress);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			DeployProgress = reader.ReadSingle();
		}
	}
}
