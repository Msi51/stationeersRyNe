using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Objects.Electrical;
using Reagents;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Items;

public class Ice : Ore, IAtmospherical, ISpatial, IPhysical, IProfile, IDensePoolable, ILightActivated
{
	public static List<Ice> AllIcePrefabs = new List<Ice>();

	[SerializeField]
	[FormerlySerializedAs("MeltTemperature")]
	public float meltTemperature = 273.15f;

	private const int MELT_COOLDOWN_TICKS = 2;

	private int _meltCoolDown = 2;

	protected static ReagentMixture _meltingReagentMix;

	private bool _isMelting;

	public TemperatureKelvin MeltTemperature => new TemperatureKelvin(meltTemperature);

	public bool CanMelt
	{
		get
		{
			if (_meltCoolDown > 0)
			{
				_meltCoolDown--;
				return false;
			}
			if (base.ParentSlot != null)
			{
				if (!base.ParentSlot.Parent.CanIceMelt)
				{
					return false;
				}
				return !base.ParentSlot.HidesOccupant;
			}
			if (base.WorldAtmosphere == null)
			{
				return HasLight;
			}
			return true;
		}
	}

	public bool IsMelting
	{
		get
		{
			return _isMelting;
		}
		set
		{
			if (value != IsMelting && NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
			_isMelting = value;
		}
	}

	public void ResetMeltCoolDown()
	{
		_meltCoolDown = 2;
	}

	public override bool MoveToSlot(Slot destinationSlot, Thing originThing, bool forced = false)
	{
		if (!destinationSlot.Parent.CanIceMelt || destinationSlot.HidesOccupant)
		{
			ResetMeltCoolDown();
		}
		return base.MoveToSlot(destinationSlot, originThing, forced);
	}

	public override bool MoveToWorld(Vector3 worldPosition, Quaternion worldRotation, Vector3 velocity, Vector3 angularVelocity, float force = 0f)
	{
		ResetMeltCoolDown();
		return base.MoveToWorld(worldPosition, worldRotation, velocity, angularVelocity, force);
	}

	public override void Awake()
	{
		base.Awake();
		AtmosphericsManager.Instance.Register(this);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (!(attack.SourceItem is IWelder welder) || base.Indestructable)
		{
			return base.AttackWith(attack, doAction);
		}
		Tool tool = attack.SourceItem as Tool;
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0.5f,
			ActionMessage = "Melt Ice"
		};
		if ((bool)tool && !tool.IsOperable)
		{
			delayedActionInstance.IsDisabled = true;
			if (!tool.OnOff)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceNotOn);
			}
			if (welder.IsEmpty)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceNoFuel);
			}
			if (welder is WeldingTorch { Inflamed: false })
			{
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceNotHotEnough);
			}
			return delayedActionInstance;
		}
		if (!doAction)
		{
			return delayedActionInstance;
		}
		Smelt(base.WorldAtmosphere, _meltingReagentMix);
		return delayedActionInstance;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Deregister(this);
		}
	}

	public override void Recycle()
	{
		base.Recycle();
		Atmosphere localAtmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(base.WorldGrid);
		Smelt(localAtmosphere, _meltingReagentMix);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		HandleIceMelting();
	}

	protected virtual void HandleIceMelting()
	{
		if (!CanMelt)
		{
			return;
		}
		Atmosphere atmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(base.WorldGrid);
		if (base.ParentSlot?.Parent is Furnace)
		{
			atmosphere = base.ParentSlot.Parent.InternalAtmosphere;
		}
		if (base.ParentSlot?.Parent is FridgePowered && base.ParentSlot.Parent.Powered && base.ParentSlot.Parent.OnOff)
		{
			IsMelting = false;
			return;
		}
		bool num = atmosphere != null;
		bool flag = num && atmosphere.IsActive() && atmosphere.WillMeltIce() && atmosphere.Temperature > MeltTemperature;
		bool flag2 = !num || (atmosphere.IsActive() && atmosphere.Temperature <= MeltTemperature && atmosphere.WillMeltIce());
		IsMelting = flag || (!flag2 && HasLight);
		if (IsMelting)
		{
			Smelt(atmosphere, _meltingReagentMix);
		}
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (IsMelting)
		{
			extendedText.AppendLine(GameStrings.IsCurrentlyMelting.DisplayString);
		}
		return extendedText;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteBoolean(IsMelting);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			IsMelting = reader.ReadBoolean();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteBoolean(IsMelting);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		IsMelting = reader.ReadBoolean();
	}
}
