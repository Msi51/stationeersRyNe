using System;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class RadiatorRotatable : Radiator, IRotatable, ISolarRadiator, IDensePoolable
{
	[Tooltip("The axis that is used to rotate the panel horizontally (vertical is done in animation).")]
	[SerializeField]
	protected Transform _panelRotation;

	[Tooltip("The radiator panel whose forward direction is used to calculate heating efficiency to sun direction.")]
	[SerializeField]
	protected Transform _radiatorPanel;

	[Tooltip("What layers can cause a collision when calculating obscurance.")]
	[SerializeField]
	protected LayerMask _collisionMask;

	[SerializeField]
	private Vector3 _rayOffset = Vector3.zero;

	[SerializeField]
	private Vector3 _rayCenter = Vector3.zero;

	private Vector3 _rayLeftUp;

	private Vector3 _rayRightUp;

	private Vector3 _rayLeftDown;

	private Vector3 _rayRightDown;

	private double _vertical;

	private double _horizontal;

	public static readonly int FrameUpdateCooldown = 60;

	private int _framePanelUpdated;

	public AnimationCurve SunAngleHeatCurve;

	private static readonly Action<ISolarRadiator> DrawAllRotatingRadiators = delegate(ISolarRadiator radiator)
	{
		if (radiator is RadiatorRotatable radiatorRotatable)
		{
			ImGuiExtensions.Rendering.DrawArrow(radiatorRotatable._radiatorPanel.rotation * radiatorRotatable._rayCenter + radiatorRotatable._radiatorPanel.position, OrbitalSimulation.WorldSunVector);
			ImGuiExtensions.Rendering.DrawArrow(radiatorRotatable._radiatorPanel.rotation * radiatorRotatable._rayLeftDown + radiatorRotatable._radiatorPanel.position, OrbitalSimulation.WorldSunVector);
			ImGuiExtensions.Rendering.DrawArrow(radiatorRotatable._radiatorPanel.rotation * radiatorRotatable._rayLeftUp + radiatorRotatable._radiatorPanel.position, OrbitalSimulation.WorldSunVector);
			ImGuiExtensions.Rendering.DrawArrow(radiatorRotatable._radiatorPanel.rotation * radiatorRotatable._rayRightUp + radiatorRotatable._radiatorPanel.position, OrbitalSimulation.WorldSunVector);
			ImGuiExtensions.Rendering.DrawArrow(radiatorRotatable._radiatorPanel.rotation * radiatorRotatable._rayRightDown + radiatorRotatable._radiatorPanel.position, OrbitalSimulation.WorldSunVector);
		}
	};

	public float SolarVisibility { get; private set; }

	public float HeatingEfficiency { get; private set; }

	public RotatableBehaviour RotatableBehaviour { get; set; }

	public virtual double Vertical
	{
		get
		{
			return _vertical;
		}
		set
		{
			_vertical = value;
		}
	}

	public virtual double Horizontal
	{
		get
		{
			return _horizontal;
		}
		set
		{
			_horizontal = value;
		}
	}

	public float RotationTolerance => 0.001f;

	public double MaximumVertical => 180.0;

	public double MaximumHorizontal => 360.0;

	public float MovementSpeedHorizontal => 0.05f;

	public float MovementSpeedVertical => 0.05f;

	private double _horizontalIncrement => 10.0 / MaximumHorizontal;

	private double _verticalIncrement => 10.0 / MaximumVertical;

	public string DamageColor
	{
		get
		{
			if (DamageState.TotalRatio > 0.75f)
			{
				return "red";
			}
			if (DamageState.TotalRatio > 0.25f)
			{
				return "yellow";
			}
			return "green";
		}
	}

	public bool CanRotate()
	{
		return !IsBroken;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteFloatHalf((float)(RotatableBehaviour?.TargetVertical ?? 0.0));
			writer.WriteFloatHalf((float)(RotatableBehaviour?.TargetHorizontal ?? 0.0));
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

	public void RunAfterAnimation()
	{
	}

	public async UniTaskVoid UpdateAnimator()
	{
	}

	public override void Awake()
	{
		base.Awake();
		RotatableBehaviour = new RotatableBehaviour(this)
		{
			MaxAudibleSquareDistance = 100f
		};
		_rayLeftUp = _rayCenter + new Vector3(_rayOffset.x, _rayOffset.y, _rayOffset.z);
		_rayRightUp = new Vector3(0f - _rayOffset.x, _rayOffset.y, _rayOffset.z);
		_rayRightDown = new Vector3(0f - _rayOffset.x, 0f - _rayOffset.y, _rayOffset.z);
		_rayLeftDown = new Vector3(_rayOffset.x, 0f - _rayOffset.y, _rayOffset.z);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		if (interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2 || interactable.Action == InteractableType.Button3 || interactable.Action == InteractableType.Button4)
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
			delayedActionInstance.ExtendedMessage = PanelInfo();
			switch (interactable.Action)
			{
			case InteractableType.Button2:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.HorizontalDegrees, StringManager.Get(RotatableBehaviour.TargetHorizontal * MaximumHorizontal));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = RotatableBehaviour.TargetHorizontal + (interaction.AltKey ? (_horizontalIncrement * 0.10000000149011612) : _horizontalIncrement);
				if (num > 1.0)
				{
					num -= 1.0;
				}
				RotatableBehaviour.TargetHorizontal = num;
				PlayPooledAudioSound(Defines.Sounds.WrenchOneShot, Vector3.zero);
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button4:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.HorizontalDegrees, StringManager.Get((int)(RotatableBehaviour.TargetHorizontal * MaximumHorizontal)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = RotatableBehaviour.TargetHorizontal - (interaction.AltKey ? (_horizontalIncrement * 0.10000000149011612) : _horizontalIncrement);
				if (num < 0.0)
				{
					num += 1.0;
				}
				RotatableBehaviour.TargetHorizontal = num;
				PlayPooledAudioSound(Defines.Sounds.WrenchOneShot, Vector3.zero);
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button1:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.VerticalDegrees, StringManager.Get((int)(RotatableBehaviour.TargetVertical * MaximumVertical)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = RotatableBehaviour.TargetVertical + (interaction.AltKey ? (_verticalIncrement * 0.10000000149011612) : _verticalIncrement);
				if (num > 1.0)
				{
					num = 1.0;
				}
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
				double num = RotatableBehaviour.TargetVertical - (interaction.AltKey ? (_verticalIncrement * 0.10000000149011612) : _verticalIncrement);
				if (num < 0.0)
				{
					num = 0.0;
				}
				RotatableBehaviour.TargetVertical = num;
				PlayPooledAudioSound(Defines.Sounds.WrenchOneShot, Vector3.zero);
				return delayedActionInstance.Succeed();
			}
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (base.CurrentBuildStateIndex == BuildStates.Count - 1)
		{
			passiveTooltip.Title = DisplayName;
			passiveTooltip.State = PanelInfo();
		}
		return passiveTooltip;
	}

	public virtual string PanelInfo()
	{
		return string.Format("HeatingEfficiency {0}\nHealth {1}", Mathf.Round(HeatingEfficiency * 100f).ToStringPercent("yellow"), (100f - DamageState.TotalRatio * 100f).ToStringPercent(DamageColor));
	}

	public bool CalculateSolarEfficiency()
	{
		if (_framePanelUpdated > Time.frameCount - FrameUpdateCooldown)
		{
			return false;
		}
		_framePanelUpdated = Time.frameCount;
		SolarVisibility = 1f;
		float num = 0.2f;
		if (CastForObsurance(_rayCenter))
		{
			SolarVisibility -= num;
		}
		if (CastForObsurance(_rayLeftUp))
		{
			SolarVisibility -= num;
		}
		if (CastForObsurance(_rayRightUp))
		{
			SolarVisibility -= num;
		}
		if (CastForObsurance(_rayRightDown))
		{
			SolarVisibility -= num;
		}
		if (CastForObsurance(_rayLeftDown))
		{
			SolarVisibility -= num;
		}
		float a = SunAngleHeatCurve.Evaluate(Mathf.Clamp(1f - (_radiatorPanel.forward - OrbitalSimulation.WorldSunVector).magnitude, -1f, 1f));
		float b = SunAngleHeatCurve.Evaluate(Mathf.Clamp(1f - (_radiatorPanel.forward * -1f - OrbitalSimulation.WorldSunVector).magnitude, -1f, 1f));
		HeatingEfficiency = Mathf.Max(a, b) * SolarVisibility;
		return true;
	}

	public bool CastForObsurance(Vector3 offset)
	{
		offset = _radiatorPanel.rotation * offset;
		Physics.Raycast(new Ray(_radiatorPanel.position + offset, OrbitalSimulation.WorldSunVector), out var hitInfo, float.PositiveInfinity, _collisionMask);
		if ((bool)hitInfo.collider)
		{
			return !hitInfo.collider.isTrigger;
		}
		return false;
	}

	private void DebugForObsurance(Vector3 offset)
	{
		offset = _radiatorPanel.rotation * offset;
		Physics.Raycast(new Ray(_radiatorPanel.position + offset, OrbitalSimulation.WorldSunVector), out var hitInfo, float.PositiveInfinity, _collisionMask);
		DebugHelpers.DrawArrow(_radiatorPanel.position + offset, OrbitalSimulation.WorldSunVector, 0.5f, hitInfo.collider ? Color.red : Color.green);
	}

	public static void DrawDebug()
	{
		SolarRadiators.AllSolarRadiators.ForEach(DrawAllRotatingRadiators);
	}

	private void OnDrawGizmos()
	{
		DebugForObsurance(_rayCenter);
		DebugForObsurance(_rayLeftUp);
		DebugForObsurance(_rayRightUp);
		DebugForObsurance(_rayRightDown);
		DebugForObsurance(_rayLeftDown);
	}

	public override CanConstructInfo CanConstruct()
	{
		Vector3 worldPosition = base.ThingTransformPosition - ThingTransform.up * GridSize;
		Structure structure = base.GridController.Get<Structure>(worldPosition, StructureElement.Center);
		if (!structure || ((bool)structure && !structure.AllowMounting))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
		}
		return base.CanConstruct();
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType - 20 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType - 20 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Horizontal => Horizontal * MaximumHorizontal, 
			LogicType.Vertical => Vertical * MaximumVertical, 
			LogicType.HorizontalRatio => Horizontal, 
			LogicType.VerticalRatio => Vertical, 
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
		ThingSaveData savedData = new RadiatorRotatableSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RadiatorRotatableSaveData radiatorRotatableSaveData)
		{
			Horizontal = radiatorRotatableSaveData.Horizontal;
			Vertical = radiatorRotatableSaveData.Vertical;
			RotatableBehaviour.TargetHorizontal = radiatorRotatableSaveData.TargetHorizontal;
			RotatableBehaviour.TargetVertical = radiatorRotatableSaveData.TargetVertical;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RadiatorRotatableSaveData radiatorRotatableSaveData)
		{
			InitializeRotatableBehaviour();
			radiatorRotatableSaveData.Horizontal = Horizontal;
			radiatorRotatableSaveData.Vertical = Vertical;
			radiatorRotatableSaveData.TargetHorizontal = RotatableBehaviour.TargetHorizontal;
			radiatorRotatableSaveData.TargetVertical = RotatableBehaviour.TargetVertical;
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
