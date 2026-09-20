using System;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Effects;
using Objects;
using UnityEngine;
using Weather;

namespace Assets.Scripts;

public class GroundTelescope : LargeRotatable
{
	public DialKnob HorizontalKnob;

	public DialKnob VerticalKnob;

	public Animator DoorAnimator;

	private static readonly int _isOpen = Animator.StringToHash("IsOpen");

	[SerializeField]
	protected MaterialChanger StateMaterialChanger;

	private const float PARTIAL_ALIGNMENT = 5f;

	private const float FULL_ALIGNMENT = 2f;

	private float _trackedTime;

	public BoxCollider InfoBox;

	protected override bool IsOperable
	{
		get
		{
			if (OnOff && Powered)
			{
				return base.IsStructureCompleted;
			}
			return false;
		}
	}

	public Celestial CurrentCelestial
	{
		get
		{
			if (!IsOperable || !IsOpen)
			{
				return null;
			}
			return _celestialHit.Celestial;
		}
	}

	private CelestialHit _celestialHit { get; set; } = CelestialHit.INVALID;

	private double HorizontalIncrement => 10.0 / base.MaximumHorizontal;

	private double VerticalIncrement => 10.0 / base.MaximumVertical;

	private StringBuilder GetInfoBoxString()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (IsOperable && CurrentCelestial != null && !WeatherManager.IsWeatherEventRunning)
		{
			extendedText.AppendLine("Observing " + CurrentCelestial.ToTooltip());
			extendedText.AppendLine("Alignment Error " + _celestialHit.Angle.ToStringPrefix("°", "yellow", adaptive: false));
			if (IsAligned(2f))
			{
				extendedText.AppendLine("Distance " + CurrentCelestial.GetNearestUnitDistance().AsColor("yellow"));
				if (CurrentCelestial == OrbitalSimulation.GetPrimaryBody())
				{
					extendedText.AppendLine("Solar Irradiance " + OrbitalSimulation.SolarIrradiance.ToStringPrefix("W/m²", "yellow"));
				}
				if (CurrentCelestial is CelestialBody celestialBody)
				{
					if (celestialBody.OrbitingBody != null)
					{
						extendedText.AppendLine("In Orbit around " + celestialBody.OrbitingBody.ToTooltip());
					}
					if (celestialBody != OrbitalSimulation.GetPlayerBody())
					{
						extendedText.AppendLine("Period " + celestialBody.Orbit.GetOrbitLength().ToNearestString().AsColor("yellow"));
						extendedText.AppendLine("Inclination " + celestialBody.Orbit.Inclination.ToStringPrefix("°", "yellow", adaptive: false));
						extendedText.AppendLine("Eccentricity " + celestialBody.Orbit.Eccentricity.ToString("F2").AsColor("yellow"));
						extendedText.AppendLine("SemiMajorAxis " + celestialBody.Orbit.GetSemiMajorAxis().ToNearestString().AsColor("yellow"));
						extendedText.AppendLine("True Anomaly " + celestialBody.GetWrappedTrueAnomaly().ToStringPrefix("°", "yellow", adaptive: false));
					}
				}
			}
			else
			{
				extendedText.AppendLine("Not aligned enough to get orbital data");
			}
		}
		else
		{
			extendedText.AppendLine("Nothing is being observed");
			if (WeatherManager.IsWeatherEventRunning)
			{
				extendedText.AppendLine("Weather is obscuring the telescope".AsColor("red"));
			}
			if (OnOff && !Powered)
			{
				extendedText.AppendLine(GameStrings.TooltipDeviceUnpowered.AsColor("red"));
			}
		}
		if (base.RotatableBehaviour.IsMoving)
		{
			extendedText.AppendLine("Moving to Orientation");
		}
		if (!IsOpen)
		{
			extendedText.AppendLine("Telescope cover is closed".AsColor("red"));
		}
		return extendedText;
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (Powered)
		{
			switch (Activate)
			{
			default:
				StateMaterialChanger?.ChangeState(Defines.Animator.Activate0);
				break;
			case 1:
				StateMaterialChanger?.ChangeState(Defines.Animator.Activate1);
				break;
			case 2:
				StateMaterialChanger?.ChangeState(Defines.Animator.Activate2);
				break;
			case 3:
				StateMaterialChanger?.ChangeState(Defines.Animator.Activate3);
				break;
			}
		}
		else
		{
			StateMaterialChanger?.ChangeState(Defines.Animator.Off);
		}
		DoorAnimator.SetBool(_isOpen, IsOpen);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType - 20 <= LogicType.Power || logicType - 34 <= LogicType.Power || logicType - 242 <= LogicType.Activate)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	private bool IsAligned(float angle)
	{
		if (WeatherManager.IsWeatherEventRunning)
		{
			return false;
		}
		return _celestialHit.Angle <= angle;
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.CelestialHash => CurrentCelestial?.Hash ?? 0, 
			LogicType.Horizontal => Horizontal * base.MaximumHorizontal, 
			LogicType.Vertical => Vertical * base.MaximumVertical, 
			LogicType.HorizontalRatio => Horizontal, 
			LogicType.VerticalRatio => Vertical, 
			LogicType.AlignmentError => _celestialHit.GetLogicAngle(), 
			LogicType.CelestialParentHash => IsAligned(2f) ? _celestialHit.GetParentHash() : 0, 
			LogicType.DistanceAu => IsAligned(2f) ? _celestialHit.GetDistanceAu() : double.NaN, 
			LogicType.DistanceKm => IsAligned(2f) ? _celestialHit.GetDistanceKm() : double.NaN, 
			LogicType.OrbitPeriod => IsAligned(2f) ? _celestialHit.GetPeriodDays() : double.NaN, 
			LogicType.Inclination => IsAligned(2f) ? ((double)_celestialHit.GetInclination()) : double.NaN, 
			LogicType.Eccentricity => IsAligned(2f) ? ((double)_celestialHit.GetEccentricity()) : double.NaN, 
			LogicType.SemiMajorAxis => IsAligned(2f) ? _celestialHit.GetSemiMajorAxis() : double.NaN, 
			LogicType.TrueAnomaly => IsAligned(2f) ? _celestialHit.GetTrueAnomaly() : double.NaN, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType - 20 <= LogicType.Power || logicType - 34 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.Horizontal:
		{
			value = RocketMath.ModuloCorrect(value, base.MaximumHorizontal);
			double num = value / base.MaximumHorizontal;
			if (!RocketMath.Approximately(num, base.RotatableBehaviour.TargetHorizontal, RotationTolerance))
			{
				base.RotatableBehaviour.TargetHorizontal = num;
			}
			break;
		}
		case LogicType.Vertical:
		{
			if (value < 0.0)
			{
				value = 0.0;
			}
			if (value > base.MaximumVertical)
			{
				value = base.MaximumVertical;
			}
			double num = value / base.MaximumVertical;
			if (!RocketMath.Approximately(num, base.RotatableBehaviour.TargetVertical, RotationTolerance))
			{
				base.RotatableBehaviour.TargetVertical = num;
			}
			break;
		}
		case LogicType.HorizontalRatio:
			value = RocketMath.ModuloCorrect(value, 1.0);
			if (!RocketMath.Approximately(value, base.RotatableBehaviour.TargetHorizontal, RotationTolerance))
			{
				base.RotatableBehaviour.TargetHorizontal = value;
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
			if (!RocketMath.Approximately(value, base.RotatableBehaviour.TargetVertical, RotationTolerance))
			{
				base.RotatableBehaviour.TargetVertical = value;
			}
			break;
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Open)
		{
			DoorAnimator.SetBool(_isOpen, IsOpen);
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		DoorAnimator.SetBool(_isOpen, IsOpen);
		_celestialHit = CelestialHit.INVALID;
		DishForward = DishTransform.forward;
	}

	public override void OnFinishJoin()
	{
		base.OnFinishJoin();
		DoorAnimator.SetBool(_isOpen, IsOpen);
		_celestialHit = CelestialHit.INVALID;
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		if (!GameManager.IsRunning)
		{
			return;
		}
		if (!IsOperable || !IsOpen)
		{
			_celestialHit = CelestialHit.INVALID;
			CheckActivate();
			return;
		}
		_celestialHit = OrbitalSimulation.RaycastSky(DishForward, 5f);
		CheckActivate();
		if ((byte)Activate == 3)
		{
			_trackedTime += OcclusionManager.DeltaTime;
		}
	}

	private void CheckActivate()
	{
		int activate = (int)GetActivate();
		if (Activate != activate)
		{
			Interactable.SetFromThread(base.InteractActivate, activate);
		}
	}

	private TelescopeAlignment GetActivate()
	{
		TelescopeAlignment result = TelescopeAlignment.None;
		if (base.RotatableBehaviour.IsMoving)
		{
			result = TelescopeAlignment.Moving;
		}
		if (_celestialHit.Celestial == null)
		{
			return result;
		}
		if (IsAligned(2f))
		{
			result = TelescopeAlignment.Full;
		}
		else if (IsAligned(5f))
		{
			result = TelescopeAlignment.Partial;
		}
		return result;
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

	public override async UniTaskVoid UpdateAnimator()
	{
		if (ThreadedManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		SetKnobs();
	}

	public override void Update1000MS(float deltaTime)
	{
		base.Update1000MS(deltaTime);
		if (_trackedTime > 60f)
		{
			Achievements.Increment(Achievements.Stat.CelestialTrackingTime, 1);
			_trackedTime = 0f;
		}
	}

	private void SetKnobs()
	{
		VerticalKnob.SetState((float)(base.RotatableBehaviour.TargetVertical * base.MaximumVertical / base.MaximumVertical));
		HorizontalKnob.SetState((float)(base.RotatableBehaviour.TargetHorizontal * base.MaximumHorizontal / base.MaximumHorizontal));
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteFloatHalf((float)(base.RotatableBehaviour?.TargetVertical ?? 0.0));
			writer.WriteFloatHalf((float)(base.RotatableBehaviour?.TargetHorizontal ?? 0.0));
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			float num = reader.ReadFloatHalf();
			float num2 = reader.ReadFloatHalf();
			if (base.RotatableBehaviour != null)
			{
				base.RotatableBehaviour.TargetVertical = num;
				base.RotatableBehaviour.TargetHorizontal = num2;
			}
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteDouble(base.RotatableBehaviour?.TargetVertical ?? 0.0);
		writer.WriteDouble(base.RotatableBehaviour?.TargetHorizontal ?? 0.0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		double targetVertical = reader.ReadDouble();
		double targetHorizontal = reader.ReadDouble();
		if (base.RotatableBehaviour != null)
		{
			base.RotatableBehaviour.TargetVertical = targetVertical;
			base.RotatableBehaviour.TargetHorizontal = targetHorizontal;
		}
		_celestialHit = OrbitalSimulation.RaycastSky(DishForward, 5f);
		SetDishRotation();
		DishForward = DishTransform.forward;
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (!IsCursor)
		{
			if (GameManager.GameState == GameState.Running)
			{
				Horizontal = 0.0;
				Vertical = 0.0;
			}
			_celestialHit = CelestialHit.INVALID;
			CheckActivate();
			SetKnobs();
			SetDishRotation();
			DishForward = DishTransform.forward;
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		InteractableType action = interactable.Action;
		if (action == InteractableType.Button1 || action == InteractableType.Button2 || action == InteractableType.Button3 || action == InteractableType.Button4 || action == InteractableType.Open)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (IsLocked)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceLocked);
				return delayedActionInstance.Fail();
			}
			switch (interactable.Action)
			{
			case InteractableType.Open:
				if (!CanRotate())
				{
					return delayedActionInstance.Fail(GameStrings.TooltipDeviceUnpowered.AsColor("red"));
				}
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				OnServer.Interact(interactable, (interactable.State != 1) ? 1 : 0);
				return delayedActionInstance.Succeed();
			case InteractableType.Button2:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.HorizontalDegrees, StringManager.Get((int)Math.Round(base.RotatableBehaviour.TargetHorizontal * base.MaximumHorizontal)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = base.RotatableBehaviour.TargetHorizontal + (interaction.AltKey ? (HorizontalIncrement * 0.10000000149011612) : HorizontalIncrement);
				if (num > 1.0)
				{
					num -= 1.0;
				}
				base.RotatableBehaviour.TargetHorizontal = num;
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button1:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.HorizontalDegrees, StringManager.Get((int)Math.Round(base.RotatableBehaviour.TargetHorizontal * base.MaximumHorizontal)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = base.RotatableBehaviour.TargetHorizontal - (interaction.AltKey ? (HorizontalIncrement * 0.10000000149011612) : HorizontalIncrement);
				if (num < 0.0)
				{
					num += 1.0;
				}
				base.RotatableBehaviour.TargetHorizontal = num;
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button4:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.VerticalDegrees, StringManager.Get((int)Math.Round(base.RotatableBehaviour.TargetVertical * base.MaximumVertical)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = base.RotatableBehaviour.TargetVertical + (interaction.AltKey ? (VerticalIncrement * 0.10000000149011612) : VerticalIncrement);
				if (num > 1.0)
				{
					num = 1.0;
				}
				base.RotatableBehaviour.TargetVertical = num;
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button3:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.VerticalDegrees, StringManager.Get((int)Math.Round(base.RotatableBehaviour.TargetVertical * base.MaximumVertical)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = base.RotatableBehaviour.TargetVertical - (interaction.AltKey ? (VerticalIncrement * 0.10000000149011612) : VerticalIncrement);
				if (num < 0.0)
				{
					num = 0.0;
				}
				base.RotatableBehaviour.TargetVertical = num;
				return delayedActionInstance.Succeed();
			}
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
