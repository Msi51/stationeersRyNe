using System;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Assets.Scripts.Vehicles;
using Objects.Electrical;
using Trading;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects;

public class PortableAtmospherics : DraggableThing, IAtmospherical, ISpatial, IPhysical, IProfile, IDensePoolable, IThermal, IVolume, IUnfastenable, IReferencable, IEvaluable
{
	[Header("Portable Atmospherics")]
	[Tooltip("What Portable Atmospherics pipe can hold: Gas or Liquid")]
	public Pipe.ContentType ContentType = Pipe.ContentType.Gas;

	[Tooltip("Internal volume of the device, in litres")]
	[FormerlySerializedAs("Litres")]
	[SerializeField]
	private float litres = 790f;

	[NonSerialized]
	[ReadOnly]
	public IPortablesConnector PortablesConnector;

	[Tooltip("Affects how fast the needle will move towards the current pressure")]
	public static float LerpSpeed = 2f;

	[Tooltip("Minimum degrees rotation on local Y")]
	public float NeedleMinimum;

	[Tooltip("Maximum degrees rotation on local Y")]
	public float NeedleMaximum = -280f;

	[Tooltip("The needle (required)")]
	public GameObject Needle;

	public RotationAxis NeedleAxis = RotationAxis.Y;

	[Tooltip("Check if needle needs to rotate in the opposite direction")]
	public bool FlipNeedleDirection;

	[Tooltip("The collider for tank display")]
	public Collider InfoPanel;

	[Tooltip("What internal volume worth of pressure is processed per tick")]
	[SerializeField]
	[FormerlySerializedAs("PressurePerTick")]
	private float pressurePerTick = 101.325f;

	private Transform _needleTransform;

	private float _lastAngle;

	private Quaternion _needleBaseRotation;

	protected float _pressureRating;

	protected float _needleRotation;

	public VolumeLitres Litres => new VolumeLitres(litres);

	public PressurekPa PressurePerTick => new PressurekPa(pressurePerTick);

	public override bool HasReadableAtmosphere => true;

	public VolumeLitres GetVolume => Litres;

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			if ((bool)Needle)
			{
				_needleTransform = Needle.transform;
				_needleBaseRotation = _needleTransform.localRotation;
			}
			AtmosphericsManager.Instance.Register(this);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SetPhysics(base.ParentSlot == null);
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Litres, 0L);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (GameManager.GameState != GameState.None)
		{
			if (!(this is DynamicAirConditioner))
			{
				VentAllAtmosphereToWorld();
			}
			AtmosphericsManager.Instance.Deregister(this);
		}
	}

	private void VentAllAtmosphereToWorld()
	{
		if (base.WorldAtmosphere == null)
		{
			base.WorldAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
		}
		AtmosphereHelper.MoveVolume(base.InternalAtmosphere, base.WorldAtmosphere, base.InternalAtmosphere.Volume, AtmosphereHelper.MatterState.All, MoleQuantity.Zero);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (base.Joint != null || !(attack.SourceItem as Wrench))
		{
			return base.AttackWith(attack, doAction);
		}
		IPortablesConnector portablesConnector = SmallCell.Get<IPortablesConnector>(CenterPosition);
		Rover rover = Rover.IsNearby(this) as Rover;
		Lander lander = null;
		if (base.ParentSlot != null)
		{
			lander = base.ParentSlot.Parent as Lander;
		}
		if (portablesConnector == null && rover == null && lander == null && base.ParentSlot == null)
		{
			return base.AttackWith(attack, doAction);
		}
		if (portablesConnector != null && portablesConnector.TankSlot.IsEmpty() && portablesConnector.PipeContentType != ContentType && portablesConnector.PipeContentType != Pipe.ContentType.All)
		{
			return portablesConnector.PipeContentType switch
			{
				Pipe.ContentType.Unknown => DelayedActionInstance.Failure(ActionStrings.Connect, GameStrings.DevicePortableConnectionFailureUnknown, portablesConnector.ToTooltip()), 
				Pipe.ContentType.Gas => DelayedActionInstance.Failure(ActionStrings.Connect, GameStrings.DevicePortableConnectionFailureGas, portablesConnector.ToTooltip()), 
				Pipe.ContentType.Liquid => DelayedActionInstance.Failure(ActionStrings.Connect, GameStrings.DevicePortableConnectionFailureGasLiquid, portablesConnector.ToTooltip()), 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 1f,
			ActionMessage = ((base.ParentSlot != null) ? ActionStrings.Disconnect : ActionStrings.Connect)
		};
		if (!doAction)
		{
			return result;
		}
		if (!GameManager.RunSimulation)
		{
			return result;
		}
		if ((bool)(attack.SourceItem as Wrench))
		{
			if (base.ParentSlot != null)
			{
				OnServer.MoveToWorld(this);
			}
			else if (rover != null || portablesConnector != null)
			{
				if (rover != null)
				{
					rover.Attach(this);
				}
				else
				{
					OnServer.MoveToSlot(this, portablesConnector.TankSlot);
				}
			}
		}
		return result;
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		Connector connector = parent as Connector;
		if (connector != null)
		{
			PortablesConnector = connector;
			if ((bool)BaseAnimator && BaseAnimator.HasState(0, DraggableThing.AnchoredState))
			{
				BaseAnimator.SetBool(DraggableThing.AnchoredState, value: true);
			}
		}
	}

	public override void OnExitInventory(Thing parent)
	{
		base.OnExitInventory(parent);
		if (PortablesConnector == parent)
		{
			PortablesConnector = null;
			if ((bool)BaseAnimator && BaseAnimator.HasState(0, DraggableThing.AnchoredState))
			{
				BaseAnimator.SetBool(DraggableThing.AnchoredState, value: false);
			}
		}
	}

	public override bool MoveToWorld(float force = 0f)
	{
		bool result = base.MoveToWorld(force);
		if (GameManager.RunSimulation)
		{
			RigidBody.AddForce(Vector3.up * 3f);
			RigidBody.AddTorque(UnityEngine.Random.insideUnitCircle);
		}
		return result;
	}

	public override void Delete(Thing sourceItem)
	{
		if (!sourceItem)
		{
			AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, base.InternalAtmosphere.GasMixture);
		}
		AtmosphericEventInstance.Reset(base.InternalAtmosphere);
		base.Delete(sourceItem);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == InfoPanel)
		{
			PassiveTooltip result = new PassiveTooltip(true);
			result.Title = DisplayName;
			StringBuilder stringBuilder = new StringBuilder();
			AtmosphericsManager.DisplayBasicAtmosphere(base.InternalAtmosphere, stringBuilder, ContentType);
			result.Extended = stringBuilder.ToString();
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override void OnThreadUpdate()
	{
		if (!GameManager.IsBatchMode && base.InternalAtmosphere != null)
		{
			_pressureRating = ((ContentType == Pipe.ContentType.Liquid) ? base.InternalAtmosphere.LiquidVolumeRatio : (base.InternalAtmosphere.PressureGassesAndLiquids.ToFloat() / DynamicGasCanister.StandardPressure.ToFloat()));
			if (float.IsNaN(_pressureRating))
			{
				_pressureRating = PressurekPa.Zero.ToFloat();
			}
			_needleRotation = (float)((!FlipNeedleDirection) ? 1 : (-1)) * Mathf.Lerp(NeedleMaximum, NeedleMinimum, _pressureRating);
		}
	}

	public override void UpdateEachFrame()
	{
		if (GameManager.IsBatchMode || WorldManager.IsGamePaused)
		{
			return;
		}
		base.UpdateEachFrame();
		if (!IsOccluded && (bool)_needleTransform)
		{
			_lastAngle = Mathf.Lerp(_lastAngle, _needleRotation, Time.deltaTime * LerpSpeed);
			_needleTransform.localRotation = _needleBaseRotation;
			switch (NeedleAxis)
			{
			case RotationAxis.X:
				_needleTransform.Rotate(_lastAngle, 0f, 0f, Space.Self);
				break;
			case RotationAxis.Y:
				_needleTransform.Rotate(0f, 0f - _lastAngle, 0f, Space.Self);
				break;
			case RotationAxis.Z:
				_needleTransform.Rotate(0f, 0f, _lastAngle, Space.Self);
				break;
			case RotationAxis.XY:
				break;
			}
		}
	}
}
