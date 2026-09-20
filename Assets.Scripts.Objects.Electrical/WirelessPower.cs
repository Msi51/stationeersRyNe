using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class WirelessPower : ElectricalInputOutput, IRotatable
{
	[SerializeField]
	protected PowerTransmitterVisualiser PowerTransmitterVisualiser;

	private float _visualizerIntensity;

	public static readonly string[] ConnectionModeStrings = new string[2] { "Unlinked", "Linked" };

	[Header("Transmitter Dish")]
	public Transform DishTransform;

	public Vector3 DishForward;

	[Header("Transmitter Axle")]
	public Transform AxleTransform;

	public Vector3 AxleForward;

	public Transform RayTransform;

	protected Vector3 RayPosition = Vector3.zero;

	public bool IsDirty;

	private double _vertical;

	private double _horizontal;

	public float VisualizerIntensity
	{
		get
		{
			return _visualizerIntensity;
		}
		set
		{
			_visualizerIntensity = value;
			SetVisualizerIntensity(_visualizerIntensity);
		}
	}

	public override string[] ModeStrings => ConnectionModeStrings;

	public RotatableBehaviour RotatableBehaviour { get; set; }

	public double Vertical
	{
		get
		{
			return _vertical;
		}
		set
		{
			if (_vertical != value)
			{
				_vertical = value;
				if ((bool)DishTransform)
				{
					DishTransform.localRotation = Quaternion.Euler(Mathf.Lerp(90f, -90f, (float)_vertical), 0f, 0f);
				}
				DishForward = DishTransform.up;
				RayPosition = RayTransform.position;
				IsDirty = true;
			}
		}
	}

	public double Horizontal
	{
		get
		{
			return _horizontal;
		}
		set
		{
			if (_horizontal != value)
			{
				_horizontal = value;
				if ((bool)AxleTransform)
				{
					AxleTransform.localRotation = Quaternion.Euler(0f, (float)(_horizontal * MaximumHorizontal), 0f);
				}
				AxleForward = AxleTransform.up;
				RayPosition = RayTransform.position;
				IsDirty = true;
			}
		}
	}

	public float RotationTolerance => 1E-07f;

	public double MaximumVertical => 180.0;

	public double MaximumHorizontal => 360.0;

	public virtual float MovementSpeedHorizontal => 0.05f;

	public virtual float MovementSpeedVertical => 0.05f;

	private double HorizontalIncrement => 10.0 / MaximumHorizontal;

	private double VerticalIncrement => 10.0 / MaximumVertical;

	public virtual void SetVisualizerIntensity(float intensity)
	{
		PowerTransmitterVisualiser.SetIntensity(intensity);
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 512;
		}
	}

	public bool CanRotate()
	{
		return true;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteFloatHalf((float)(RotatableBehaviour?.TargetVertical ?? 0.0));
			writer.WriteFloatHalf((float)(RotatableBehaviour?.TargetHorizontal ?? 0.0));
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteFloatHalf(VisualizerIntensity);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			float num = reader.ReadFloatHalf();
			float num2 = reader.ReadFloatHalf();
			if (RotatableBehaviour != null)
			{
				RotatableBehaviour.TargetVertical = num;
				RotatableBehaviour.TargetHorizontal = num2;
			}
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			VisualizerIntensity = reader.ReadFloatHalf();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteDouble(RotatableBehaviour?.TargetVertical ?? 0.0);
		writer.WriteDouble(RotatableBehaviour?.TargetHorizontal ?? 0.0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		double targetVertical = reader.ReadDouble();
		double targetHorizontal = reader.ReadDouble();
		if (RotatableBehaviour != null)
		{
			RotatableBehaviour.TargetVertical = targetVertical;
			RotatableBehaviour.TargetHorizontal = targetHorizontal;
		}
	}

	public virtual void RunAfterAnimation()
	{
	}

	public async UniTaskVoid UpdateAnimator()
	{
		if (ThreadedManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if ((bool)BaseAnimator)
		{
			BaseAnimator.SetFloat(Defines.Animator.TargetHorizontal, (float)RotatableBehaviour.TargetHorizontal);
			BaseAnimator.SetFloat(Defines.Animator.TargetVertical, (float)RotatableBehaviour.TargetVertical);
		}
	}

	public override void Awake()
	{
		base.Awake();
		RotatableBehaviour = new RotatableBehaviour(this)
		{
			MaxAudibleSquareDistance = 600f
		};
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		InteractableType action = interactable.Action;
		if (action == InteractableType.Button1 || action == InteractableType.Button2 || action == InteractableType.Button3 || action == InteractableType.Button4)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (!(interaction.SourceSlot.Occupant is Wrench))
			{
				return delayedActionInstance.Fail(GameStrings.YouNeedAWrenchForOrientation);
			}
			switch (interactable.Action)
			{
			case InteractableType.Button2:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.HorizontalDegrees, StringManager.Get((int)(RotatableBehaviour.TargetHorizontal * MaximumHorizontal)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = RotatableBehaviour.TargetHorizontal + (interaction.AltKey ? (HorizontalIncrement * 0.10000000149011612) : HorizontalIncrement);
				if (num > 1.0)
				{
					num -= 1.0;
				}
				num = RotatableBehaviour.RoundRatioToNearestDegree(num, MaximumHorizontal);
				RotatableBehaviour.TargetHorizontal = num;
				PlayPooledAudioSound(Defines.Sounds.WrenchOneShot, Vector3.zero);
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button1:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.HorizontalDegrees, StringManager.Get((int)(RotatableBehaviour.TargetHorizontal * MaximumHorizontal)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = RotatableBehaviour.TargetHorizontal - (interaction.AltKey ? (HorizontalIncrement * 0.10000000149011612) : HorizontalIncrement);
				if (num < 0.0)
				{
					num += 1.0;
				}
				num = RotatableBehaviour.RoundRatioToNearestDegree(num, MaximumHorizontal);
				RotatableBehaviour.TargetHorizontal = num;
				PlayPooledAudioSound(Defines.Sounds.WrenchOneShot, Vector3.zero);
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button4:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.VerticalDegrees, StringManager.Get((int)(RotatableBehaviour.TargetVertical * MaximumVertical)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = RotatableBehaviour.TargetVertical + (interaction.AltKey ? (VerticalIncrement * 0.10000000149011612) : VerticalIncrement);
				if (num > 1.0)
				{
					num = 1.0;
				}
				num = RotatableBehaviour.RoundRatioToNearestDegree(num, MaximumVertical);
				RotatableBehaviour.TargetVertical = num;
				PlayPooledAudioSound(Defines.Sounds.WrenchOneShot, Vector3.zero);
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button3:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.VerticalDegrees, StringManager.Get((int)(RotatableBehaviour.TargetVertical * MaximumVertical)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = RotatableBehaviour.TargetVertical - (interaction.AltKey ? (VerticalIncrement * 0.10000000149011612) : VerticalIncrement);
				if (num < 0.0)
				{
					num = 0.0;
				}
				num = RotatableBehaviour.RoundRatioToNearestDegree(num, MaximumVertical);
				RotatableBehaviour.TargetVertical = num;
				PlayPooledAudioSound(Defines.Sounds.WrenchOneShot, Vector3.zero);
				return delayedActionInstance.Succeed();
			}
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	protected void CheckError()
	{
		if (GameManager.RunSimulation)
		{
			if (!IsOperable && Error == 0)
			{
				OnServer.Interact(base.InteractError, 1, skipAnimation: true);
			}
			else if (IsOperable && Error == 1)
			{
				OnServer.Interact(base.InteractError, 0, skipAnimation: true);
			}
		}
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Mode:
			return false;
		case LogicType.Horizontal:
		case LogicType.Vertical:
			return true;
		default:
			return base.CanLogicWrite(logicType);
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Charge:
		case LogicType.Horizontal:
		case LogicType.Vertical:
		case LogicType.PowerPotential:
		case LogicType.PowerActual:
		case LogicType.PositionX:
		case LogicType.PositionY:
		case LogicType.PositionZ:
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Horizontal => Horizontal * MaximumHorizontal, 
			LogicType.Vertical => Vertical * MaximumVertical, 
			LogicType.HorizontalRatio => Horizontal, 
			LogicType.VerticalRatio => Vertical, 
			LogicType.Charge => AvailablePower, 
			LogicType.PowerPotential => base.PotentialLoad, 
			LogicType.PowerActual => base.CurrentLoad, 
			LogicType.PositionX => RayPosition.x, 
			LogicType.PositionY => RayPosition.y, 
			LogicType.PositionZ => RayPosition.z, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.Horizontal:
		{
			value = RocketMath.ModuloCorrect(value, MaximumHorizontal);
			double num = value / MaximumHorizontal;
			if (!RocketMath.Approximately(num, RotatableBehaviour.TargetHorizontal, RotationTolerance))
			{
				RotatableBehaviour.TargetHorizontal = num;
			}
			break;
		}
		case LogicType.Vertical:
		{
			if (value < 0.0)
			{
				value = 0.0;
			}
			if (value > MaximumVertical)
			{
				value = MaximumVertical;
			}
			double num = value / MaximumVertical;
			if (!RocketMath.Approximately(num, RotatableBehaviour.TargetVertical, RotationTolerance))
			{
				RotatableBehaviour.TargetVertical = num;
			}
			break;
		}
		case LogicType.HorizontalRatio:
			value = RocketMath.ModuloCorrect(value, 1.0);
			if (!RocketMath.Approximately(value, RotatableBehaviour.TargetHorizontal, RotationTolerance))
			{
				RotatableBehaviour.TargetHorizontal = value;
			}
			break;
		case LogicType.VerticalRatio:
			if (value < 0.0)
			{
				value = 0.0;
			}
			if (value > 1.0)
			{
				value = 1.0;
			}
			if (!RocketMath.Approximately(value, RotatableBehaviour.TargetVertical, RotationTolerance))
			{
				RotatableBehaviour.TargetVertical = value;
			}
			break;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new WirelessPowerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is WirelessPowerSaveData wirelessPowerSaveData)
		{
			Horizontal = wirelessPowerSaveData.Horizontal;
			Vertical = wirelessPowerSaveData.Vertical;
			RotatableBehaviour.TargetHorizontal = wirelessPowerSaveData.TargetHorizontal;
			RotatableBehaviour.TargetVertical = wirelessPowerSaveData.TargetVertical;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is WirelessPowerSaveData wirelessPowerSaveData)
		{
			InitializeRotatableBehaviour();
			wirelessPowerSaveData.Horizontal = Horizontal;
			wirelessPowerSaveData.Vertical = Vertical;
			wirelessPowerSaveData.TargetHorizontal = RotatableBehaviour.TargetHorizontal;
			wirelessPowerSaveData.TargetVertical = RotatableBehaviour.TargetVertical;
		}
	}

	private void InitializeRotatableBehaviour()
	{
		if (RotatableBehaviour == null)
		{
			RotatableBehaviour obj = new RotatableBehaviour(this)
			{
				MaxAudibleSquareDistance = 600f
			};
			RotatableBehaviour rotatableBehaviour = obj;
			RotatableBehaviour = obj;
		}
	}
}
