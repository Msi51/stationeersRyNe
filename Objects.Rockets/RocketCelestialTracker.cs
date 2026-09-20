using System;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Networks;
using UnityEngine;

namespace Objects.Rockets;

public class RocketCelestialTracker : Device, IRocketInternals, IRocketComponent, ISmartRotatable, IMemoryReadable, IMemory, IInstructable, ILogicTick, ILogicStack
{
	private struct CelestialDirection
	{
		public float Azimuth;

		public float Elevation;

		public float Radius;

		public static CelestialDirection Calculate(Celestial celestial, Quaternion direction)
		{
			Vector3 v = RocketMath.InverseTransformDirecton(celestial.WorldVector, direction);
			v = v.yxz();
			RocketMath.CartesianToSphericalFixed(out var azimuth, out var elevation, out var radius, v);
			return new CelestialDirection
			{
				Azimuth = azimuth,
				Elevation = elevation,
				Radius = radius
			};
		}
	}

	public DialKnob Knob;

	public BoxCollider InfoBox;

	private byte _index;

	private float _horizontal;

	private float _vertical;

	private LogicStack _stack = new LogicStack(12);

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

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public Celestial CurrentCelestial => OrbitalSimulation.GetCelestial(Index);

	[ByteArraySync]
	public byte Index
	{
		get
		{
			return _index;
		}
		set
		{
			if (value != _index)
			{
				_index = (byte)GetSafeIndex(value);
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
			}
			Knob?.SetState((float)(int)_index / (float)OrbitalSimulation.GetCelestialCount());
		}
	}

	public bool CanTrack
	{
		get
		{
			if (CurrentCelestial is CelestialBody)
			{
				RocketState? rocketState = Rocket?.RocketState;
				if (rocketState.HasValue)
				{
					return rocketState == RocketState.InSpace;
				}
				return false;
			}
			return true;
		}
	}

	public bool GetIsOperable()
	{
		return IsOperable;
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (base.IsStructureCompleted)
		{
			_index = (byte)GetSafeIndex(Index);
			Knob?.SetState((float)(int)_index / (float)OrbitalSimulation.GetCelestialCount());
		}
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		if (base.IsStructureCompleted)
		{
			_index = (byte)GetSafeIndex(Index);
			Knob?.SetState((float)(int)_index / (float)OrbitalSimulation.GetCelestialCount());
		}
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override string GetContextualName(Interactable interactable)
	{
		InteractableType action = interactable.Action;
		if ((uint)(action - 31) <= 1u)
		{
			if (!IsOperable)
			{
				return GameStrings.DeviceIndexSetTo.AsString(StringManager.Get(Index).AsColor("green"));
			}
			if (CurrentCelestial == null)
			{
				return GameStrings.NoCelestialFound;
			}
			return CurrentCelestial.Name;
		}
		return base.GetContextualName(interactable);
	}

	private int GetSafeIndex(int nextIndex)
	{
		if (nextIndex < 0)
		{
			nextIndex = Index;
		}
		if (nextIndex >= OrbitalSimulation.GetCelestialCount())
		{
			nextIndex = Index;
		}
		return nextIndex;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		InteractableType action = interactable.Action;
		if (action == InteractableType.Button1 || action == InteractableType.Button2)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			int num = Index;
			switch (interactable.Action)
			{
			case InteractableType.Button1:
				num = GetSafeIndex(Index - 1);
				break;
			case InteractableType.Button2:
				num = GetSafeIndex(Index + 1);
				break;
			}
			Celestial celestial = OrbitalSimulation.GetCelestial(num);
			if (celestial == null)
			{
				return delayedActionInstance.Fail(GameStrings.LogicNoReadableDevices);
			}
			if (!IsOperable)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceIndexSetTo, StringManager.Get(num).AsColor("green"));
			}
			else
			{
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, celestial.ToTooltip());
			}
			delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				Index = (byte)num;
			}
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType - 20 <= LogicType.Power || logicType - 241 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.CelestialHash => CurrentCelestial?.Hash ?? 0, 
			LogicType.Horizontal => _horizontal, 
			LogicType.Vertical => _vertical, 
			LogicType.Index => (int)Index, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Index)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Index)
		{
			Index = (byte)Math.Round(value, 0);
		}
		base.SetLogicValue(logicType, value);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketCelestialTrackerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RocketCelestialTrackerSaveData rocketCelestialTrackerSaveData)
		{
			Index = rocketCelestialTrackerSaveData.Index;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RocketCelestialTrackerSaveData rocketCelestialTrackerSaveData)
		{
			rocketCelestialTrackerSaveData.Index = Index;
		}
	}

	public void OnLogicTick()
	{
		if (!IsOperable || !CanTrack)
		{
			_horizontal = float.NaN;
			_vertical = float.NaN;
			return;
		}
		int celestialCount = OrbitalSimulation.GetCelestialCount();
		for (int i = 0; i < celestialCount && i < _stack.Size; i++)
		{
			if (OrbitalSimulation.GetCelestial(i) != null)
			{
				CelestialDirection celestialDirection = CelestialDirection.Calculate(CurrentCelestial, Direction);
				byte opCode = 1;
				float num = 57.29578f * celestialDirection.Azimuth;
				float num2 = 57.29578f * celestialDirection.Elevation;
				_stack[i] = LogicStack.PackByteInt16(opCode, (byte)i, DoubleToShort((double)num * 10.0), DoubleToShort((double)num2 * 10.0));
				if (i == Index)
				{
					_horizontal = num;
					_vertical = num2;
				}
			}
		}
	}

	private short DoubleToShort(double degrees)
	{
		return (short)(degrees % 360.0);
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
		if (IsOperable && CurrentCelestial != null)
		{
			extendedText.AppendLine(GameStrings.CelestialTrackerSetTo.AsString(CurrentCelestial.ToTooltip()));
			if (CanTrack)
			{
				extendedText.Append("Horizontal ").Append("<color=yellow>");
				extendedText.Append(StringGenerator.GetString(Mathf.RoundToInt(_horizontal), Unit.Degrees));
				extendedText.AppendLine("</color>");
				extendedText.Append("Vertical ").Append("<color=yellow>");
				extendedText.Append(StringGenerator.GetString(Mathf.RoundToInt(_vertical), Unit.Degrees));
				extendedText.AppendLine("</color>");
			}
			else
			{
				extendedText.AppendLine(GameStrings.CelestialTrackerRequiresOrbit.AsColor("red"));
			}
		}
		else
		{
			if (OnOff && !Powered)
			{
				extendedText.AppendLine(GameStrings.TooltipDeviceUnpowered.AsColor("red"));
			}
			extendedText.AppendLine(GameStrings.DeviceIndexSetTo.AsString(StringManager.Get(Index).AsColor("green")));
		}
		return extendedText;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteByte(Index);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			Index = reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte(Index);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Index = reader.ReadByte();
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

	public int GetStackSize()
	{
		return _stack.Size;
	}

	public LogicStack GetLogicStack()
	{
		return _stack;
	}

	public double ReadMemory(int address)
	{
		return _stack[address];
	}

	public IEnumCollection GetInstructions()
	{
		return EnumCollections.CelestialTracking;
	}

	public string GetInstructionDescription(int i)
	{
		if (EnumCollections.CelestialTracking[i] != CelestialTracking.BodyOrientation)
		{
			throw new NotImplementedException();
		}
		return LogicStack.FormatInstruction(LogicStack.OpCode, new LogicStack.InstructionFormat("Celestial_Index", typeof(byte)), new LogicStack.InstructionFormat("Horizontal_Deci_Degrees", typeof(short)), new LogicStack.InstructionFormat("Vertical_Deci_Degrees", typeof(short)));
	}
}
