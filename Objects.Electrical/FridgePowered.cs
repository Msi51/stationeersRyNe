using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Electrical;

public class FridgePowered : DeviceInternal
{
	public MoleEnergy HeatTransferJoulesPerTick = new MoleEnergy(1000.0);

	public static TemperatureKelvin GoalTemperature = new TemperatureKelvin(142.0);

	private float _powerUsedDuringTick;

	public static float DoorOpenThermodynamicsChange = 0.01f;

	private Atmosphere _worldAtmosphere;

	private Vector3 _childRotation = new Vector3(45f, 90f, 90f);

	public bool DontUseOpen;

	public override float ConvectionFactor => ThermodynamicsScale + (IsOpen ? DoorOpenThermodynamicsChange : 0f);

	protected override bool IsOperable => true;

	public override void InitInternalAtmosphere()
	{
		if (base.Volume <= VolumeLitres.Zero || base.InternalAtmosphere != null)
		{
			return;
		}
		base.InternalAtmosphere = new Atmosphere(this, base.Volume, 0L);
		if (GameManager.GameState != GameState.Loading)
		{
			base.InternalAtmosphere.GasMixture.Add(new Mole(Chemistry.GasType.Nitrogen, new MoleQuantity(1.7200000286102295), MoleEnergy.Zero));
			Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
			if (atmosphere.IsAboveArmstrong())
			{
				base.InternalAtmosphere.GasMixture.TotalEnergy = IdealGas.Energy(base.InternalAtmosphere.GasMixture.HeatCapacity, atmosphere.Temperature);
				return;
			}
			TemperatureKelvin twentyDegrees = Chemistry.Temperature.TwentyDegrees;
			base.InternalAtmosphere.GasMixture.TotalEnergy = IdealGas.Energy(base.InternalAtmosphere.GasMixture.HeatCapacity, twentyDegrees);
		}
	}

	public override void OnAtmosphericTick()
	{
		_worldAtmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		if (OnOff && Powered && base.InternalAtmosphere != null)
		{
			MoleEnergy moleEnergy = IdealGas.Energy(base.InternalAtmosphere.GasMixture.HeatCapacity, GoalTemperature) - base.InternalAtmosphere.GasMixture.TotalEnergy;
			MoleEnergy energy2;
			if (moleEnergy < MoleEnergy.Zero)
			{
				MoleEnergy energy = RocketMath.Min(HeatTransferJoulesPerTick, -moleEnergy);
				energy2 = base.InternalAtmosphere.GasMixture.RemoveEnergy(energy);
				_worldAtmosphere.GasMixture.AddEnergy(energy2);
			}
			else
			{
				energy2 = RocketMath.Min(HeatTransferJoulesPerTick, moleEnergy);
				base.InternalAtmosphere.GasMixture.AddEnergy(energy2);
			}
			_powerUsedDuringTick = energy2.ToFloat();
		}
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (!OnOff || cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			return 0f;
		}
		if (!IsOperable)
		{
			return UsedPower;
		}
		return UsedPower + _powerUsedDuringTick;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
		_powerUsedDuringTick = 0f;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Temperature)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Temperature)
		{
			return base.InternalAtmosphere.Temperature.ToDouble();
		}
		return base.GetLogicValue(logicType);
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if ((object)newChild != null)
		{
			newChild.ScaleToSlot();
			newChild.ThingTransformLocalRotation = Quaternion.Euler(_childRotation + newChild.ChildSlotOffset);
			newChild.ThingTransformLocalPosition = newChild.ChildSlotOffsetPosition;
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		previousChild.SetVisibility(isVisible: true);
	}

	private void SetContentsVisibility(bool isVisible = true)
	{
		if (DontUseOpen)
		{
			return;
		}
		foreach (Interactable interactable in Interactables)
		{
			if (interactable.Action != InteractableType.Open && interactable.Action != InteractableType.OnOff && (bool)interactable.Collider)
			{
				interactable.Collider.enabled = isVisible;
			}
		}
		foreach (Slot slot in Slots)
		{
			if ((bool)slot.Occupant)
			{
				slot.Occupant.SetVisibility(isVisible);
			}
		}
	}

	public override void OnAnimationStart()
	{
		base.OnAnimationStart();
		if (IsOpen)
		{
			SetContentsVisibility();
		}
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
		if (!IsOpen)
		{
			SetContentsVisibility(isVisible: false);
		}
	}
}
