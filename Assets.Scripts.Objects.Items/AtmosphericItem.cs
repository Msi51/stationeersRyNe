using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Objects.Electrical;
using Trading;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Items;

public class AtmosphericItem : ItemRenamable, IAtmospherical, ISpatial, IPhysical, IProfile, IDensePoolable, IThermal, IVolume, IInternalAtmosphere, IReferencable, IEvaluable
{
	[FormerlySerializedAs("Volume")]
	[SerializeField]
	private float volume = 10f;

	[ReadOnly]
	public List<LeakReference> LeakReferences;

	private float _leakRatio;

	public VolumeLitres Volume => new VolumeLitres(volume);

	public override float RepairRatio => LeakRatio;

	public PressurekPa Pressure => base.InternalAtmosphere?.PressureGassesAndLiquids ?? PressurekPa.Zero;

	[ByteArraySync]
	public float LeakRatio
	{
		get
		{
			return _leakRatio;
		}
		set
		{
			float leakRatio = _leakRatio;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 4096;
			}
			_leakRatio = value;
			if (base.ParentSlot != null && (bool)base.ParentSlot.Parent && !(leakRatio >= LeakRatio))
			{
				if (!GameManager.IsBatchMode && InventoryManager.Parent == base.ParentSlot.Parent)
				{
					WorldManager.DamageEffect(DamageType.Blood, 0.6f, 1.5f, blurEffect: true);
				}
				EnableLeak(Random.Range(0, LeakReferences.Count));
			}
		}
	}

	public override bool IsLeaking => LeakRatio > 0f;

	public override bool HasReadableAtmosphere => true;

	VolumeLitres IVolume.GetVolume => Volume;

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (LeakRatio > 0f)
		{
			passiveTooltip.RepairString = IRobotRepairer.Tooltip;
		}
		return passiveTooltip;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new AtmosphericItemSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteSingle(LeakRatio);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			LeakRatio = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(LeakRatio);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		LeakRatio = reader.ReadSingle();
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.PersonalSuits);
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is AtmosphericItemSaveData atmosphericItemSaveData)
		{
			LeakRatio = atmosphericItemSaveData.LeakRatio;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		AtmosphericItemSaveData atmosphericItemSaveData = savedData as AtmosphericItemSaveData;
		if (GameManager.GameState != GameState.None && atmosphericItemSaveData != null)
		{
			atmosphericItemSaveData.LeakRatio = LeakRatio;
		}
	}

	public virtual void EnableLeak(int index)
	{
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (attack.SourceItem is ISuitReparier suitReparier)
		{
			float num = suitReparier.RepairQuantity(this);
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = num * suitReparier.GetRepairSpeed(),
				ActionMessage = "Patch"
			};
			if (LeakRatio <= 0f)
			{
				return delayedActionInstance.Fail(GameStrings.StructureIsNotDamaged, ToTooltip());
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			suitReparier.RepairLeak(base.netId, num * attack.CompletedRatio);
		}
		return base.AttackWith(attack, doAction);
	}

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			AtmosphericsManager.Instance.Register(this);
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AtmosphericsManager.Instance.Deregister(this);
	}

	public void LeakAir()
	{
		if (!(LeakRatio <= 0f))
		{
			if (base.WorldAtmosphere == null || base.WorldAtmosphere.IsGlobalAtmosphere)
			{
				base.WorldAtmosphere = base.GridController.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			}
			AtmosphereHelper.EqualizeBothWays(base.InternalAtmosphere, base.WorldAtmosphere, AtmosphereHelper.MatterState.Gas, Chemistry.OneAtmosphere, MoleQuantity.MaxValue, LeakRatio);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		LeakAir();
	}
}
