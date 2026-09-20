using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects;
using Objects.Electrical;
using Trading;
using UnityEngine;
using Weather;

namespace Assets.Scripts.Objects.Electrical;

public class SolarPanel : Electrical, IRepairable, IRotatable, ISolarRadiator, IDensePoolable, IPowerGenerator, IReferencable, IEvaluable
{
	[Header("Solar Panel")]
	[Tooltip("How many watts are generated")]
	public float MaxPowerGenerated = 500f;

	[Tooltip("The size of the panel")]
	public Vector2 PanelSize = new Vector2(1f, 1f);

	[Tooltip("What layers can cause a collision when calculating obscurance")]
	public LayerMask CollisionMask;

	[ReadOnly]
	[Tooltip("The efficiency of the solar panel")]
	public float GenerationEfficiency;

	[ReadOnly]
	[Tooltip("The solar control motherboard that the panel is currently using")]
	public SolarControl Motherboard;

	[SerializeField]
	private List<SolarPanelArm> _panelArms;

	private const float EFFICIENCY_SCALAR = 1.4f;

	private const float EFFICIENCY_SCALAR_STORM = 1.6f;

	private new const float SHADOW_DISTANCE = 20f;

	private double _vertical;

	private double _horizontal;

	private float _panelArea;

	public static float RepairSpeedScale = 0.4f;

	private float _generated;

	public static float MinimumToProvide = 0.1f;

	public Event OnPowerGenerateRate;

	public const float _horizontalIncrement = 1f / 36f;

	private readonly RaycastHit[] _raycastHits = new RaycastHit[1];

	public RotatableBehaviour RotatableBehaviour { get; set; }

	public double Vertical
	{
		get
		{
			return _vertical;
		}
		set
		{
			_vertical = value;
			SetArmPitch(_vertical);
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
			_horizontal = value;
			SetArmYaw(_horizontal * MaximumHorizontal);
		}
	}

	public float RotationTolerance => 0.001f;

	public double MaximumVertical => 165.0;

	public double MinimumVertical => 15.0;

	public double MaximumHorizontal => 360.0;

	public virtual float MovementSpeedHorizontal => 0.05f;

	public virtual float MovementSpeedVertical => 0.05f;

	private int Efficiency => Mathf.RoundToInt(GenerationEfficiency * (1f - DamageState.TotalRatio) * 100f);

	private int Health => Mathf.RoundToInt(100f - DamageState.TotalRatio * 100f);

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

	public float RepairRatio => DamageState.TotalRatio;

	protected override bool IsOperable
	{
		get
		{
			if (!IsBroken)
			{
				return base.CurrentBuildStateIndex == BuildStates.Count - 1;
			}
			return false;
		}
	}

	public float GenerationRate
	{
		get
		{
			if (!IsOperable)
			{
				return 0f;
			}
			_generated = PowerGenerated() * GenerationEfficiency * (1f - DamageState.TotalRatio);
			if (!(_generated > MinimumToProvide))
			{
				return 0f;
			}
			return _generated;
		}
	}

	public bool CanRotate()
	{
		return !IsBroken;
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(20f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public float GetMaxPowerGenerated()
	{
		return MaxPowerGenerated;
	}

	public float PowerGenerated()
	{
		float solarRatioAt = WeatherManager.GetSolarRatioAt(base.Position.y);
		float num = OrbitalSimulation.SolarIrradiance * _panelArea * solarRatioAt;
		float num2 = (WeatherManager.CurrentEventAffects(base.Position.y) ? 1.6f : 1.4f);
		float b = num / MaxPowerGenerated;
		b = Mathf.Max(num2, b);
		float num3 = Mathf.Log(b, num2);
		return num / num3;
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

	private void SetArmYaw(double value)
	{
		foreach (SolarPanelArm panelArm in _panelArms)
		{
			panelArm.SetYaw(value);
		}
	}

	private void SetArmPitch(double value)
	{
		foreach (SolarPanelArm panelArm in _panelArms)
		{
			panelArm.SetPitch(value);
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
		RotatableBehaviour = new RotatableBehaviour(this);
		_panelArea = PanelSize.x * PanelSize.y;
		if (GameManager.GameState == GameState.Running)
		{
			Vertical = 0.5;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new SolarPanelSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is SolarPanelSaveData solarPanelSaveData)
		{
			InitializeRotatableBehaviour();
			Horizontal = solarPanelSaveData.Horizontal;
			Vertical = solarPanelSaveData.Vertical;
			RotatableBehaviour.TargetHorizontal = solarPanelSaveData.TargetHorizontal;
			RotatableBehaviour.TargetVertical = solarPanelSaveData.TargetVertical;
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

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is SolarPanelSaveData solarPanelSaveData)
		{
			if (RotatableBehaviour == null)
			{
				InitializeRotatableBehaviour();
			}
			solarPanelSaveData.Horizontal = Horizontal;
			solarPanelSaveData.Vertical = Vertical;
			solarPanelSaveData.TargetHorizontal = RotatableBehaviour.TargetHorizontal;
			solarPanelSaveData.TargetVertical = RotatableBehaviour.TargetVertical;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (base.CurrentBuildStateIndex == BuildStates.Count - 1)
		{
			passiveTooltip.Title = DisplayName;
			passiveTooltip.State = SolarInfo();
		}
		if (DamageState.Total > 0f)
		{
			passiveTooltip.RepairString = ISolarRepairer.Tooltip;
		}
		return passiveTooltip;
	}

	public string SolarInfo()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(GameStrings.GeneratingPower);
		stringBuilder.Append(" ");
		stringBuilder.Append(GenerationRate.ToStringPrefix("W", "yellow"));
		stringBuilder.AppendLine();
		stringBuilder.Append(GameStrings.SolarPanelEfficiency);
		stringBuilder.Append(" ");
		stringBuilder.Append(Efficiency.ToStringPercent("yellow"));
		stringBuilder.AppendLine();
		stringBuilder.Append(GameStrings.ThingHealth.AsString(Health.ToStringPercent(DamageColor)));
		return stringBuilder.ToString();
	}

	public override void PrintDebugInfo(bool verbose = false)
	{
		base.PrintDebugInfo(verbose);
		ConsoleWindow.Print("Generating: " + GenerationRate.ToStringPrefix("W"));
		ConsoleWindow.Print("Efficiency: " + Efficiency.ToStringPercent());
		ConsoleWindow.Print("Health: " + Health.ToStringPercent());
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (base.IsStructureCompleted && attack.SourceItem is ISolarRepairer solarRepairer)
		{
			float num = solarRepairer.RepairQuantity(this);
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = num * solarRepairer.GetRepairSpeed() * RepairSpeedScale,
				ActionMessage = GameStrings.ActionPatchSolarPanels.DisplayString
			};
			if (DamageState.TotalRatio <= float.Epsilon)
			{
				return delayedActionInstance.Fail(GameStrings.StructureIsNotDamaged, ToTooltip());
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			solarRepairer.Repair(base.netId, num * attack.CompletedRatio);
		}
		return base.AttackWith(attack, doAction);
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			return 0f;
		}
		if (OnPowerGenerateRate != null)
		{
			OnPowerGenerateRate();
		}
		return GenerationRate;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Charge || logicType - 20 <= LogicType.Power || logicType - 23 <= LogicType.Power)
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
			LogicType.Vertical => Mathf.Lerp((float)MinimumVertical, (float)MaximumVertical, (float)Vertical), 
			LogicType.HorizontalRatio => Horizontal, 
			LogicType.VerticalRatio => Vertical, 
			LogicType.Charge => GenerationRate, 
			LogicType.Maximum => PowerGenerated(), 
			LogicType.Ratio => GenerationEfficiency, 
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
			if (value <= MinimumVertical)
			{
				value = MinimumVertical;
			}
			if (value >= MaximumVertical)
			{
				value = MaximumVertical;
			}
			double num = RocketMath.MapToScale((float)MinimumVertical, (float)MaximumVertical, 0f, 1f, (float)value);
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
			delayedActionInstance.ExtendedMessage = SolarInfo();
			switch (interactable.Action)
			{
			case InteractableType.Button4:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.HorizontalDegrees, StringManager.Get((int)Math.Round(RotatableBehaviour.TargetHorizontal * MaximumHorizontal)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num2 = RotatableBehaviour.TargetHorizontal + (double)(interaction.AltKey ? 0.0027777778f : (1f / 36f));
				if (num2 > 1.0)
				{
					num2 -= 1.0;
				}
				RotatableBehaviour.TargetHorizontal = num2;
				WrenchRotateSound(interaction);
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button2:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.HorizontalDegrees, StringManager.Get((int)Math.Round(RotatableBehaviour.TargetHorizontal * MaximumHorizontal)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num2 = RotatableBehaviour.TargetHorizontal - (double)(interaction.AltKey ? 0.0027777778f : (1f / 36f));
				if (num2 < 0.0)
				{
					num2 += 1.0;
				}
				RotatableBehaviour.TargetHorizontal = num2;
				WrenchRotateSound(interaction);
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button1:
			{
				if (!doAction)
				{
					float num3 = Mathf.Lerp((float)MinimumVertical, (float)MaximumVertical, (float)RotatableBehaviour.TargetVertical);
					delayedActionInstance.AppendStateMessage(GameStrings.VerticalDegrees, StringManager.Get((int)num3));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num2 = RotatableBehaviour.TargetVertical + (interaction.AltKey ? (1.0 / (MaximumVertical - MinimumVertical)) : (10.0 / (MaximumVertical - MinimumVertical)));
				if (num2 > 1.0)
				{
					num2 = 1.0;
				}
				RotatableBehaviour.TargetVertical = num2;
				WrenchRotateSound(interaction);
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button3:
			{
				if (!doAction)
				{
					float num = Mathf.Lerp((float)MinimumVertical, (float)MaximumVertical, (float)RotatableBehaviour.TargetVertical);
					delayedActionInstance.AppendStateMessage(GameStrings.VerticalDegrees, StringManager.Get((int)num));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num2 = RotatableBehaviour.TargetVertical - (interaction.AltKey ? (1.0 / (MaximumVertical - MinimumVertical)) : (10.0 / (MaximumVertical - MinimumVertical)));
				if (num2 < 0.0)
				{
					num2 = 0.0;
				}
				RotatableBehaviour.TargetVertical = num2;
				WrenchRotateSound(interaction);
				return delayedActionInstance.Succeed();
			}
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	private void WrenchRotateSound(Interaction interaction)
	{
		if (interaction.SourceSlot.Occupant is Wrench && interaction.SourceThing.RootParentHuman.IsLocalPlayer)
		{
			PlayNetworkSound(Defines.Sounds.WrenchOneShot);
		}
	}

	public void OrientatePanel(SolarControl motherboard)
	{
		if ((bool)motherboard)
		{
			Motherboard = motherboard;
			RotatableBehaviour.TargetHorizontal = Motherboard.TargetHorizontal;
			RotatableBehaviour.TargetVertical = Motherboard.TargetVertical;
		}
	}

	public bool CalculateSolarEfficiency()
	{
		float num = 0f;
		foreach (SolarPanelArm panelArm in _panelArms)
		{
			num += panelArm.CalculateSolarEfficiency(_raycastHits, CollisionMask);
		}
		num /= (float)_panelArms.Count;
		GenerationEfficiency = num;
		return true;
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (!IsCursor && GameManager.GameState == GameState.Running)
		{
			Horizontal = 0.0;
			Vertical = 0.5;
			RotatableBehaviour.TargetHorizontal = Horizontal;
			RotatableBehaviour.TargetVertical = Vertical;
		}
	}
}
