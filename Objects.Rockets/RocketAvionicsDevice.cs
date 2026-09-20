using System;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets.Mining;
using Objects.Rockets.UI;
using Reagents;
using UnityEngine;

namespace Objects.Rockets;

public class RocketAvionicsDevice : Device, IRocketInternals, IRocketComponent, ISmartRotatable, IMemoryReadable, IMemory, IMemoryWritable, IInstructable, ILogicTick, ILogicStack
{
	private const int STACK_SIZE = 64;

	private const int STACK_TRANSACTION_BEGIN = 0;

	private const int STACK_TRANSACTION_END = 53;

	private const int STACK_DATA_BEGIN = 54;

	private const int STACK_DATA_END = 62;

	private const int STACK_POINTER_ADDRESS = 63;

	private const int STACK_TRANSACTION_COUNT = 53;

	private readonly LogicStack _stack = new LogicStack(64);

	private readonly StackPointerStackAddress _stackPointer = new StackPointerStackAddress(63, 0.0);

	private RocketActionStackAddress _actionInstruction;

	private const int INVALID_SKIP = int.MinValue;

	private const int INVALID_WAIT = -1;

	public BoxCollider InfoBox;

	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public Rocket Rocket => RocketNetwork?.Rocket;

	protected override bool IsOperable
	{
		get
		{
			if (Rocket != null && base.IsStructureCompleted && OnOff)
			{
				return Powered;
			}
			return false;
		}
	}

	public override string[] ModeStrings => EnumCollections.RocketMode.Names;

	public override bool HasReadableReagentMixture => true;

	public override ReagentMixture ReadableReagentMixture => GetTargetReagentComposition();

	public float GetImpactVelocity
	{
		get
		{
			if (!IsOperable)
			{
				return 0f;
			}
			return Rocket?.CalculateImpactVelocity() ?? 0f;
		}
	}

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public IEnumCollection GetInstructions()
	{
		return EnumCollections.RocketAvionicsInstructions;
	}

	public string GetInstructionDescription(int i)
	{
		return EnumCollections.RocketAvionicsInstructions[i] switch
		{
			RocketAvionicsInstruction.StackPointer => LogicStack.FormatInstruction(63, LogicStack.OpCode, new LogicStack.InstructionFormat("Index", typeof(ushort))), 
			RocketAvionicsInstruction.SurveySite => LogicStack.FormatInstruction(54, 62, LogicStack.OpCode, new LogicStack.InstructionFormat("Resource_Type", typeof(byte)), new LogicStack.InstructionFormat("Survey_Progress", typeof(ushort))), 
			RocketAvionicsInstruction.ResourceSite => LogicStack.FormatInstruction(54, 62, LogicStack.OpCode, new LogicStack.InstructionFormat("Resource_Type", typeof(byte)), new LogicStack.InstructionFormat("Density_Ratio_10", typeof(byte)), new LogicStack.InstructionFormat("Richness_Ratio_10", typeof(byte)), new LogicStack.InstructionFormat("Size_Ratio_10", typeof(byte))), 
			RocketAvionicsInstruction.ChildSurveySite => LogicStack.FormatInstruction(54, 62, LogicStack.OpCode, new LogicStack.InstructionFormat("Resource_Type", typeof(byte)), new LogicStack.InstructionFormat("Survey_Progress", typeof(ushort))), 
			RocketAvionicsInstruction.ChildResourceSite => LogicStack.FormatInstruction(54, 62, LogicStack.OpCode, new LogicStack.InstructionFormat("Resource_Type", typeof(byte)), new LogicStack.InstructionFormat("Density_Ratio_10", typeof(byte)), new LogicStack.InstructionFormat("Richness_Ratio_10", typeof(byte)), new LogicStack.InstructionFormat("Size_Ratio_10", typeof(byte))), 
			RocketAvionicsInstruction.JumpToAddress => LogicStack.FormatInstruction(0, 53, LogicStack.OpCode, new LogicStack.InstructionFormat("Stack_Address", typeof(ushort))), 
			_ => throw new NotImplementedException(), 
		};
	}

	public int GetStackSize()
	{
		return 64;
	}

	private void WriteAsSurveySite(RocketAvionicsInstruction opcode, MineableDeposit deposit, ref int stackCursor)
	{
		byte @byte = (byte)deposit.DepositType;
		long l = LogicStack.PackByteUshort((byte)opcode, @byte, (ushort)deposit.Parent.SurveyPoints);
		_stack[stackCursor] = ProgrammableChip.LongToDouble(l);
		stackCursor++;
	}

	private void WriteAsResourceSite(RocketAvionicsInstruction opcode, MineableDeposit deposit, ref int stackCursor)
	{
		byte @byte = (byte)deposit.DepositType;
		byte byte2 = (byte)(Mathf.Clamp01(deposit.Density / 10f) * 255f);
		byte byte3 = (byte)(Mathf.Clamp01(deposit.Richness / 10f) * 255f);
		byte byte4 = (byte)(Mathf.Clamp01(deposit.Size / 10f) * 255f);
		long l = LogicStack.PackByteX4((byte)opcode, @byte, byte2, byte3, byte4);
		_stack[stackCursor] = ProgrammableChip.LongToDouble(l);
		stackCursor++;
	}

	public LogicStack GetLogicStack()
	{
		return _stack;
	}

	public void OnLogicTick()
	{
		if (!GameManager.RunSimulation || !OnOff || !Powered || !base.IsStructureCompleted)
		{
			return;
		}
		_stack.Clear(54, 62);
		SpaceMapNode spaceMapNode = Rocket?.CurrentNode;
		if (spaceMapNode != null)
		{
			int stackCursor = 54;
			for (int i = 0; i < spaceMapNode.ChildConnections.Count; i++)
			{
				if (stackCursor >= 62)
				{
					break;
				}
				MineableDeposit mineableDeposit = spaceMapNode.ChildConnections[i].Child?.Deposit;
				if (mineableDeposit != null)
				{
					if (mineableDeposit.Parent.SurveyPoints < 100)
					{
						WriteAsSurveySite(RocketAvionicsInstruction.ChildSurveySite, mineableDeposit, ref stackCursor);
					}
					else
					{
						WriteAsResourceSite(RocketAvionicsInstruction.ChildResourceSite, mineableDeposit, ref stackCursor);
					}
				}
			}
			if (stackCursor == 54)
			{
				MineableDeposit deposit = spaceMapNode.Deposit;
				if (deposit != null)
				{
					if (deposit.Parent.SurveyPoints < 100)
					{
						WriteAsSurveySite(RocketAvionicsInstruction.SurveySite, deposit, ref stackCursor);
					}
					else
					{
						WriteAsResourceSite(RocketAvionicsInstruction.ResourceSite, deposit, ref stackCursor);
					}
				}
			}
		}
		_ = _actionInstruction;
		int num = 0;
		while (num < 53)
		{
			num++;
			StackAddress stackAddress = new StackAddress(_stackPointer, ReadMemory(_stackPointer));
			if (stackAddress.Opcode == 0)
			{
				AdvanceStack();
				continue;
			}
			if (stackAddress.Opcode == 2)
			{
				(byte, ushort) tuple = LogicStack.UnpackUInt16(stackAddress.IntegerValue);
				_stackPointer.Set(ClampAddress(tuple.Item2));
				_stackPointer.Write(_stack);
				break;
			}
			AdvanceStack();
		}
	}

	public double ReadMemory(int address)
	{
		return _stack[address];
	}

	public void WriteMemory(int address, double value)
	{
		_stack[address] = value;
	}

	public void ClearMemory()
	{
		_stack.Clear();
		_stackPointer.Set(0);
		_stackPointer.Write(_stack);
	}

	private void AdvanceStack()
	{
		ushort index = _stackPointer.Index;
		index++;
		if (index > 53)
		{
			index = 0;
		}
		_stackPointer.Set(index);
		_stackPointer.Write(_stack);
	}

	private void ClearAndAdvance()
	{
		_stack.Clear(54, 62);
		OnServer.Interact(base.InteractActivate, 0);
		WriteMemory(_actionInstruction.StackIndex, 0.0);
		_actionInstruction = null;
		AdvanceStack();
	}

	private void SkipAndAdvance()
	{
		_stack.Clear(54, 62);
		OnServer.Interact(base.InteractActivate, 0);
		_actionInstruction = null;
		AdvanceStack();
	}

	private void SkipAndJump(ushort address)
	{
		_stack.Clear(54, 62);
		OnServer.Interact(base.InteractActivate, 0);
		_actionInstruction = null;
		_stackPointer.Set(address);
		_stackPointer.Write(_stack);
	}

	private ushort ClampAddress(ushort address)
	{
		return (ushort)Mathf.Clamp(address, 0, 53);
	}

	public void SetTargetDestination(SpaceMapNode destination)
	{
		if (IsOperable)
		{
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(new SetRocketTargetDestinationMessage
				{
					AvionicsId = base.ReferenceId,
					SpaceMapNodeId = (destination?.ReferenceId ?? 0)
				});
			}
			else if ((!Rocket.IsManned || Rocket.IsValidMannedTarget(destination)) && !Rocket.CurrentNode.IsDestroyedLaunchPadNode)
			{
				Rocket.ChangeTarget(destination);
			}
		}
	}

	public bool GetIsOperable()
	{
		return IsOperable;
	}

	public void SetAutoShutOff(bool value)
	{
		if (IsOperable)
		{
			Rocket.AutomatedShutOff = value;
		}
	}

	public void SetAutoLand(bool value)
	{
		if (IsOperable)
		{
			Rocket.AutomatedLanding = value;
		}
	}

	public string GetRocketName()
	{
		return Rocket?.DisplayName ?? string.Empty;
	}

	public float GetAcceleration()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.Acceleration ?? 0f;
	}

	public float GetEngineAcceleration()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.EngineAcceleration ?? 0f;
	}

	public float GetGravity()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.GetGravity().value ?? 0f;
	}

	public float GetFuelTime()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.EstimatedRemainingBurnTimeSeconds ?? 0f;
	}

	public float GetTotalMolesVolatiles()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.TotalMolesVolatiles().ToFloat() ?? 0f;
	}

	public float GetTotalMolesOxidizer()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.TotalMolesOxidizer().ToFloat() ?? 0f;
	}

	public int GetBatteryPercentage()
	{
		if (!IsOperable)
		{
			return 0;
		}
		return Rocket?.BatteryPercentage() ?? 0;
	}

	public float GetDistanceToTarget()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.DistanceToTarget ?? 0f;
	}

	public float GetTargetVelocity()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.TargetVelocity() ?? 0f;
	}

	public SpaceMapNode GetTarget()
	{
		if (!IsOperable)
		{
			return null;
		}
		return Rocket?.TargetNode;
	}

	public SpaceMapNode GetNextNode()
	{
		if (!IsOperable)
		{
			return null;
		}
		return Rocket?.CurrentTransit?.Destination;
	}

	public SpaceMapNode GetCurrentNode()
	{
		if (!IsOperable)
		{
			return null;
		}
		return Rocket?.CurrentNode;
	}

	public float GetProgress()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.GetMapProgress() ?? 0f;
	}

	public ulong GetTargetCode()
	{
		if (!IsOperable)
		{
			return 0uL;
		}
		return (Rocket?.TargetNode?.Code.Value).GetValueOrDefault();
	}

	public ulong GetCurrentCode()
	{
		if (!IsOperable)
		{
			return 0uL;
		}
		return (Rocket?.CurrentNode?.Code.Value).GetValueOrDefault();
	}

	public bool GetOrbitalPosition(out float position)
	{
		if (IsOperable)
		{
			return Rocket.GetOrbitalPosition(out position);
		}
		position = -1f;
		return false;
	}

	public float GetTargetSurveyedRatioUnClamped()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode == null)
		{
			return 0f;
		}
		if (spaceMapNode.SurveyData == null)
		{
			return -1f;
		}
		return (float)spaceMapNode.SurveyPoints / (float)spaceMapNode.SurveyData.Difficulty;
	}

	public float GetTargetSize()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode == null)
		{
			return 0f;
		}
		if (spaceMapNode.Deposit == null || !spaceMapNode.Deposit.TargetReached(SurveyTarget.Size))
		{
			return -1f;
		}
		return spaceMapNode.Deposit.Size;
	}

	public float GetTargetRichness()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode == null)
		{
			return 0f;
		}
		if (spaceMapNode.Deposit == null || !spaceMapNode.Deposit.TargetReached(SurveyTarget.Richness))
		{
			return -1f;
		}
		return spaceMapNode.Deposit.Richness;
	}

	public int GetTargetMinedQuantity()
	{
		if (!IsOperable)
		{
			return 0;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode == null)
		{
			return 0;
		}
		if (spaceMapNode.Deposit == null)
		{
			return -1;
		}
		return (int)spaceMapNode.Deposit.MinedQuantityTotal;
	}

	public int GetTargetEstimatedTotalQuantity()
	{
		if (!IsOperable)
		{
			return 0;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode == null)
		{
			return 0;
		}
		if (spaceMapNode.Deposit == null)
		{
			return -1;
		}
		return (int)spaceMapNode.Deposit.TotalOreAtLocation;
	}

	public int GetTargetResourceQuantity()
	{
		if (!IsOperable)
		{
			return 0;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode == null)
		{
			return 0;
		}
		if (spaceMapNode.Deposit == null)
		{
			return -1;
		}
		return spaceMapNode.Deposit.OreQuantity();
	}

	public float GetTargetDensity()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode == null)
		{
			return 0f;
		}
		if (spaceMapNode.Deposit == null || !spaceMapNode.Deposit.TargetReached(SurveyTarget.Density))
		{
			return -1f;
		}
		return spaceMapNode.Deposit.Density;
	}

	public float GetTargetChartRatio()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode == null)
		{
			return 0f;
		}
		if (spaceMapNode.ChartData == null)
		{
			return -1f;
		}
		return Mathf.Clamp01((float)spaceMapNode.ChartPoints / (float)spaceMapNode.GetHighestChartDifficulty());
	}

	public float GetTargetDiscoverRatio()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode == null)
		{
			return 0f;
		}
		if (spaceMapNode.DiscoverData == null)
		{
			return -1f;
		}
		return Mathf.Clamp01((float)spaceMapNode.DiscoverPoints / (float)spaceMapNode.DiscoverData.Difficulty);
	}

	public int GetTargetNavPoints()
	{
		if (!IsOperable)
		{
			return 0;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode == null)
		{
			return 0;
		}
		int num = 0;
		for (int num2 = spaceMapNode.ChildConnections.Count - 1; num2 >= 0; num2--)
		{
			NodeType? nodeType = spaceMapNode.ChildConnections[num2]?.Child.NodeType;
			if (nodeType.HasValue && nodeType == NodeType.Static)
			{
				num++;
			}
		}
		return num;
	}

	public int GetTargetChartedNavPoints()
	{
		if (!IsOperable)
		{
			return 0;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode == null)
		{
			return 0;
		}
		int num = 0;
		for (int num2 = spaceMapNode.ChildConnections.Count - 1; num2 >= 0; num2--)
		{
			NodeConnection nodeConnection = spaceMapNode.ChildConnections[num2];
			NodeType? nodeType = nodeConnection?.Child.NodeType;
			if (nodeType.HasValue && nodeType == NodeType.Static && nodeConnection.Child.IsCharted)
			{
				num++;
			}
		}
		return num;
	}

	public int GetTargetDiscoveredSites()
	{
		if (!IsOperable)
		{
			return 0;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode == null)
		{
			return 0;
		}
		int num = 0;
		for (int num2 = spaceMapNode.ChildConnections.Count - 1; num2 >= 0; num2--)
		{
			NodeType? nodeType = spaceMapNode.ChildConnections[num2]?.Child.NodeType;
			if (nodeType.HasValue && nodeType == NodeType.Generated)
			{
				num++;
			}
		}
		return num;
	}

	public ReagentMixture GetTargetReagentComposition()
	{
		if (!IsOperable)
		{
			return ReagentMixture.Empty;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode?.Deposit == null || !spaceMapNode.Deposit.TargetReached(SurveyTarget.Composition))
		{
			return ReagentMixture.Empty;
		}
		return spaceMapNode.Deposit.GetDepositReagentMixture();
	}

	public float GetTotalReagents()
	{
		ReagentMixture targetReagentComposition = GetTargetReagentComposition();
		if (targetReagentComposition == null)
		{
			return 0f;
		}
		return (float)targetReagentComposition.TotalReagents;
	}

	public double GetTargetGasTypeRatio(LogicType logicType)
	{
		if (!IsOperable)
		{
			return 0.0;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode?.Deposit == null || !spaceMapNode.Deposit.TargetReached(SurveyTarget.Composition))
		{
			return 0.0;
		}
		return GasMixtureHelper.GasRatio(logicType, spaceMapNode.Deposit.GetDepositGasMixture());
	}

	public TemperatureKelvin GetTargetTemperatureKelvin()
	{
		if (!IsOperable)
		{
			return TemperatureKelvin.Zero;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode?.Deposit == null || !spaceMapNode.Deposit.TargetReached(SurveyTarget.Composition))
		{
			return TemperatureKelvin.Zero;
		}
		GasMixture depositGasMixture = spaceMapNode.Deposit.GetDepositGasMixture();
		if (depositGasMixture.GetTotalMolesGassesAndLiquids <= MoleQuantity.Zero)
		{
			return TemperatureKelvin.Zero;
		}
		return depositGasMixture.Temperature;
	}

	public MoleQuantity GetTargetTotalMoles()
	{
		if (!IsOperable)
		{
			return MoleQuantity.Zero;
		}
		SpaceMapNode spaceMapNode = Rocket.TargetNode ?? Rocket.CurrentNode;
		if (spaceMapNode?.Deposit == null || !spaceMapNode.Deposit.TargetReached(SurveyTarget.Composition))
		{
			return MoleQuantity.Zero;
		}
		return spaceMapNode.Deposit.GetDepositGasMixture().GetTotalMolesGassesAndLiquids;
	}

	public float GetEta()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.EstimatedTimeToTargetSeconds() ?? 0f;
	}

	public float GetNextEta()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.EstimatedTimeToNextTargetSeconds() ?? 0f;
	}

	public float GetTimeToApex()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.TimeToApex() ?? 0f;
	}

	public float GetApexAltitude()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket.ApexAltitude();
	}

	public float GetVelocity()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.Velocity ?? 0f;
	}

	public float GetMass()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.TotalMass() ?? 0f;
	}

	public float GetDryMass()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.GetDryMass() ?? 0f;
	}

	public float GetThrust()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.GetThrust() ?? 0f;
	}

	public float GetWeight()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.Weight() ?? 0f;
	}

	public float GetThrustToWeight()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.ThrustToWeightRatio() ?? 0f;
	}

	public bool GetIsAutoShutOff()
	{
		return Rocket?.AutomatedShutOff ?? false;
	}

	public bool GetIsAutoLand()
	{
		return Rocket?.AutomatedLanding ?? false;
	}

	public float GetAltitude()
	{
		if (!IsOperable)
		{
			return 0f;
		}
		return Rocket?.GetAltitude() ?? 0f;
	}

	public RocketMode GetRocketMode()
	{
		return Rocket?.RocketMode ?? RocketMode.Invalid;
	}

	public ReEntryProfile GetRocketReEntryProfile()
	{
		return Rocket?.ReEntryProfile ?? ReEntryProfile.None;
	}

	private FlightControlRule GetFlightControlRule()
	{
		return Rocket?.FlightControlRule ?? FlightControlRule.None;
	}

	public float GetLastCalculatedThrust()
	{
		return Rocket?.GetLastCalculatedThrust() ?? 0f;
	}

	public float GetLastCalculatedThrustToWeight()
	{
		return Rocket?.GetLastCalculatedThrustToWeight() ?? 0f;
	}

	public float GetLastCalculatedFuelTime()
	{
		return Rocket?.GetLastCalculatedFuelTime() ?? 0f;
	}

	public float GetLastCalculatedAcceleration()
	{
		return Rocket?.GetLastCalculatedAcceleration() ?? 0f;
	}

	public void SetReEntryProfile(ReEntryProfile reEntryProfile)
	{
		if (IsOperable)
		{
			Rocket.ReEntryProfile = reEntryProfile;
		}
	}

	public void SetRocketMode(RocketMode rocketMode)
	{
		if (IsOperable)
		{
			if (NetworkManager.IsClient)
			{
				NetworkClient.SendToServer(new SetRocketModeMessage
				{
					AvionicsId = base.ReferenceId,
					Mode = rocketMode
				});
			}
			else
			{
				Rocket.RocketMode = rocketMode;
			}
		}
	}

	public void RunAutoShutOff()
	{
		if (base.DataCableNetwork == null)
		{
			return;
		}
		foreach (Device dataDevice in base.DataCableNetwork.DataDeviceList)
		{
			if (dataDevice.HasOnOffState && !(dataDevice is RocketAvionicsDevice) && !(dataDevice is Battery) && !(dataDevice is RocketCircuitHousing))
			{
				dataDevice.SetLogicValue(LogicType.On, 0.0);
			}
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == InfoBox)
		{
			PassiveTooltip result = new PassiveTooltip(true);
			result.Title = DisplayName;
			result.Extended = GetInfoBoxString().ToString();
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override PassiveUITooltip GetPassiveUITooltip()
	{
		return PassiveUITooltip.Make(DisplayName, GetInfoBoxString().ToString());
	}

	private StringBuilder GetInfoBoxString()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (IsOperable)
		{
			extendedText.AppendLine(GameStrings.AvionicsDeviceParentRocket.AsString(Rocket?.ToTooltip()));
			extendedText.AppendLine(GameStrings.AvionicsDeviceRocketMass.AsString(GetMass().ToStringPrefix("N", "yellow")));
			extendedText.AppendLine(GameStrings.AvionicsDeviceRocketWeight.AsString(GetWeight().ToStringPrefix("kg", "yellow")));
			extendedText.AppendLine(GameStrings.AvionicsDeviceRocketThrustToWeight.AsString($"<color=yellow>{GetThrustToWeight():F2}</color>"));
			if (base.DataCableNetwork != null)
			{
				foreach (Device dataDevice in base.DataCableNetwork.DataDeviceList)
				{
					if (!(dataDevice is RocketDataDownLink rocketDataDownLink))
					{
						continue;
					}
					if (!rocketDataDownLink.AtLeastOneReceiver)
					{
						extendedText.Append(GameStrings.DownlinkConnectedToNothing);
					}
					else
					{
						extendedText.Append(GameStrings.DownlinkConnectedTo);
						for (int i = 0; i < rocketDataDownLink.ConnectedDataNetReceivers.Count; i++)
						{
							IReceiveDataNetworkDevices receiveDataNetworkDevices = rocketDataDownLink.ConnectedDataNetReceivers[i];
							extendedText.Append(receiveDataNetworkDevices.ToTooltip());
							if (i < rocketDataDownLink.ConnectedDataNetReceivers.Count - 1)
							{
								extendedText.Append(", ");
							}
						}
					}
					extendedText.AppendLine();
				}
			}
		}
		else if (OnOff && !Powered)
		{
			extendedText.AppendLine(GameStrings.TooltipDeviceUnpowered.AsColor("red"));
		}
		return extendedText;
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Open:
		case LogicType.Mode:
		case LogicType.Temperature:
		case LogicType.Reagents:
		case LogicType.RatioOxygen:
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioNitrogen:
		case LogicType.RatioPollutant:
		case LogicType.RatioMethane:
		case LogicType.RatioWater:
		case LogicType.Quantity:
		case LogicType.TotalMoles:
		case LogicType.VelocityRelativeY:
		case LogicType.RatioNitrousOxide:
		case LogicType.RatioLiquidNitrogen:
		case LogicType.RatioLiquidOxygen:
		case LogicType.RatioLiquidMethane:
		case LogicType.RatioSteam:
		case LogicType.RatioLiquidCarbonDioxide:
		case LogicType.RatioLiquidPollutant:
		case LogicType.RatioLiquidNitrousOxide:
		case LogicType.Progress:
		case LogicType.DestinationCode:
		case LogicType.Acceleration:
		case LogicType.AutoShutOff:
		case LogicType.Mass:
		case LogicType.DryMass:
		case LogicType.Thrust:
		case LogicType.Weight:
		case LogicType.ThrustToWeight:
		case LogicType.TimeToDestination:
		case LogicType.BurnTimeRemaining:
		case LogicType.FlightControlRule:
		case LogicType.ReEntryAltitude:
		case LogicType.Apex:
		case LogicType.RatioHydrogen:
		case LogicType.RatioLiquidHydrogen:
		case LogicType.RatioPollutedWater:
		case LogicType.Discover:
		case LogicType.Chart:
		case LogicType.Survey:
		case LogicType.NavPoints:
		case LogicType.ChartedNavPoints:
		case LogicType.Sites:
		case LogicType.CurrentCode:
		case LogicType.Density:
		case LogicType.Richness:
		case LogicType.Size:
		case LogicType.TotalQuantity:
		case LogicType.MinedQuantity:
		case LogicType.Altitude:
		case LogicType.RatioHydrazine:
		case LogicType.RatioLiquidHydrazine:
		case LogicType.RatioLiquidAlcohol:
		case LogicType.RatioHelium:
		case LogicType.RatioLiquidSodiumChloride:
		case LogicType.RatioSilanol:
		case LogicType.RatioLiquidSilanol:
		case LogicType.RatioHydrochloricAcid:
		case LogicType.RatioLiquidHydrochloricAcid:
		case LogicType.RatioOzone:
		case LogicType.RatioLiquidOzone:
		case LogicType.CurrentNodeType:
		case LogicType.TargetNodeType:
		case LogicType.Gravity:
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Open:
		case LogicType.Mode:
		case LogicType.DestinationCode:
		case LogicType.AutoShutOff:
		case LogicType.AutoLand:
			return true;
		default:
			return base.CanLogicWrite(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Open:
		{
			CrewModule crewModule = RocketNetwork?.CrewModule;
			return ((object)crewModule == null) ? (-1) : (crewModule.IsOpen ? 1 : 0);
		}
		case LogicType.Quantity:
			return GetTargetResourceQuantity();
		case LogicType.MinedQuantity:
			return GetTargetMinedQuantity();
		case LogicType.TotalQuantity:
			return GetTargetEstimatedTotalQuantity();
		case LogicType.Density:
			return GetTargetDensity();
		case LogicType.Richness:
			return GetTargetRichness();
		case LogicType.Size:
			return GetTargetSize();
		case LogicType.Progress:
			return GetProgress();
		case LogicType.DestinationCode:
			return GetTargetCode();
		case LogicType.CurrentCode:
			return GetCurrentCode();
		case LogicType.Acceleration:
			return GetAcceleration();
		case LogicType.AutoShutOff:
			return GetIsAutoShutOff() ? 1 : 0;
		case LogicType.AutoLand:
			return GetIsAutoLand() ? 1 : 0;
		case LogicType.Mass:
			return GetMass();
		case LogicType.DryMass:
			return GetDryMass();
		case LogicType.Thrust:
			return GetThrust();
		case LogicType.Weight:
			return GetWeight();
		case LogicType.ThrustToWeight:
			return GetThrustToWeight();
		case LogicType.VelocityRelativeY:
			return GetVelocity();
		case LogicType.TimeToDestination:
			return GetEta();
		case LogicType.BurnTimeRemaining:
			return GetFuelTime();
		case LogicType.Mode:
			return (int)GetRocketMode();
		case LogicType.FlightControlRule:
			return (double)GetFlightControlRule();
		case LogicType.ReEntryAltitude:
			return Rocket.ReEntryProfiles[GetRocketReEntryProfile()];
		case LogicType.Apex:
			return GetApexAltitude();
		case LogicType.Altitude:
			return GetAltitude();
		case LogicType.Gravity:
			return GetGravity();
		case LogicType.Discover:
			return GetTargetDiscoverRatio();
		case LogicType.Chart:
			return GetTargetChartRatio();
		case LogicType.Survey:
			return GetTargetSurveyedRatioUnClamped();
		case LogicType.NavPoints:
			return GetTargetNavPoints();
		case LogicType.ChartedNavPoints:
			return GetTargetChartedNavPoints();
		case LogicType.Sites:
			return GetTargetDiscoveredSites();
		case LogicType.Reagents:
			return GetTotalReagents();
		case LogicType.Temperature:
			return GetTargetTemperatureKelvin().ToDouble();
		case LogicType.TotalMoles:
			return GetTargetTotalMoles().ToDouble();
		case LogicType.RatioOxygen:
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioNitrogen:
		case LogicType.RatioPollutant:
		case LogicType.RatioMethane:
		case LogicType.RatioWater:
		case LogicType.RatioNitrousOxide:
		case LogicType.RatioLiquidNitrogen:
		case LogicType.RatioLiquidOxygen:
		case LogicType.RatioLiquidMethane:
		case LogicType.RatioSteam:
		case LogicType.RatioLiquidCarbonDioxide:
		case LogicType.RatioLiquidPollutant:
		case LogicType.RatioLiquidNitrousOxide:
		case LogicType.RatioHydrogen:
		case LogicType.RatioLiquidHydrogen:
		case LogicType.RatioPollutedWater:
		case LogicType.RatioHydrazine:
		case LogicType.RatioLiquidHydrazine:
		case LogicType.RatioLiquidAlcohol:
		case LogicType.RatioHelium:
		case LogicType.RatioLiquidSodiumChloride:
		case LogicType.RatioSilanol:
		case LogicType.RatioLiquidSilanol:
		case LogicType.RatioHydrochloricAcid:
		case LogicType.RatioLiquidHydrochloricAcid:
		case LogicType.RatioOzone:
		case LogicType.RatioLiquidOzone:
			return GetTargetGasTypeRatio(logicType);
		case LogicType.CurrentNodeType:
			return (double)(GetCurrentNode()?.NodeType ?? NodeType.None);
		case LogicType.TargetNodeType:
			return (double)(GetTarget()?.NodeType ?? NodeType.None);
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		switch (logicType)
		{
		case LogicType.Open:
		{
			CrewModule crewModule = RocketNetwork?.CrewModule;
			if ((object)crewModule != null)
			{
				OnServer.Interact(crewModule.InteractOpen, (value > 0.0) ? 1 : 0);
			}
			break;
		}
		case LogicType.DestinationCode:
		{
			SpaceMapNode spaceMapNode = SpaceMapCode.Get((ulong)value);
			if (spaceMapNode != null && spaceMapNode.IsAccessible)
			{
				SetTargetDestination(spaceMapNode);
			}
			break;
		}
		case LogicType.AutoShutOff:
			SetAutoShutOff(value > 0.0);
			break;
		case LogicType.AutoLand:
			SetAutoLand(value > 0.0);
			break;
		case LogicType.Mode:
			SetRocketMode((RocketMode)(int)value);
			break;
		}
		base.SetLogicValue(logicType, value);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketAvionicsSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void ValidateOnLoad(int currentSaveVersion)
	{
		base.ValidateOnLoad(currentSaveVersion);
		if (currentSaveVersion <= 25553)
		{
			base.CurrentBuildStateIndex = BuildStates.Count - 1;
			UpdateStateVisualizer();
		}
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

	public float GetAutoLandConfidenceRatio(out float minRequiredThrust, out float expectedThrust)
	{
		minRequiredThrust = float.PositiveInfinity;
		expectedThrust = Rocket?.GetMaxExpectedThrust() ?? 0f;
		float altitude = ((Rocket != null) ? Rocket.ReEntryProfiles[Rocket.ReEntryProfile] : 0f);
		return Rocket?.GetAutoLandConfidenceRatio(SpaceMap.Current.DistanceToOrbit * 2f, WorldSetting.Current.Gravity, altitude, out minRequiredThrust) ?? 0f;
	}
}
