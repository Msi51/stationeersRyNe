using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Structures;

public class Geyser : LargeStructure, ISmartRotatable
{
	[Header("Geyser")]
	[SerializeField]
	private Transform _particlePosition;

	[SerializeField]
	private float _maxPressureKpa;

	[SerializeField]
	private List<GasQuantity> _expelledGasses;

	public static List<Geyser> AllGeysers = new List<Geyser>();

	private bool _tapped;

	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.FlatExhaustive;

	public int[] OpenEndsPermutation = new int[4] { 0, 1, 2, 3 };

	private bool Tapped
	{
		get
		{
			return _tapped;
		}
		set
		{
			_tapped = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public new static void ClearAll()
	{
		AllGeysers.Clear();
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		AtmosphericsManager.Instance.Register(this);
		AllGeysers.Add(this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		AtmosphericsManager.Instance.Deregister(this);
		AllGeysers.Remove(this);
	}

	private void FixedUpdate()
	{
		if (!IsCursor && AtmosphericsManager.Instance?.GeyserParticles != null)
		{
			AtmosphericsManager.Instance.GeyserParticles.Emit(_particlePosition.position, _particlePosition.rotation, 1);
		}
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (Tapped)
		{
			return null;
		}
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return null;
		}
		if (sourceItem is Crowbar)
		{
			DelayedActionInstance result = new DelayedActionInstance
			{
				Duration = 5f,
				ActionMessage = "Untap Geyser"
			};
			if (!doAction)
			{
				return result;
			}
			Tapped = true;
			return result;
		}
		return base.AttackWith(attack, doAction);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!Tapped)
		{
			return;
		}
		WorldGrid worldGrid = new WorldGrid(LocalGrid + Grid3.Up);
		if (!base.GridController.CanContainAtmos(worldGrid))
		{
			return;
		}
		Atmosphere atmosphere = base.AtmosphericsController.CloneGlobalAtmosphere(worldGrid, 0L);
		if (!((atmosphere.PressureGassesAndLiquids - new PressurekPa(_maxPressureKpa)).ToDouble() < 0.0))
		{
			return;
		}
		GasMixture gasMixture = GasMixtureHelper.Create();
		foreach (GasQuantity expelledGass in _expelledGasses)
		{
			TemperatureKelvin temperature = new TemperatureKelvin(expelledGass.TemperatureC + 273.15f);
			MoleQuantity quantity = new MoleQuantity(expelledGass.Moles);
			SpecificHeat specificHeat = Mole.SpecificHeat(expelledGass.GasType);
			MoleEnergy energy = IdealGas.Energy(temperature, specificHeat, quantity);
			gasMixture.Add(new Mole(expelledGass.GasType, quantity, energy));
		}
		atmosphere.Add(gasMixture);
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteBoolean(Tapped);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Tapped = reader.ReadBoolean();
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			Tapped = reader.ReadBoolean();
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteBoolean(Tapped);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new GeyserSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData saveData)
	{
		base.InitialiseSaveData(ref saveData);
		if (saveData is GeyserSaveData geyserSaveData)
		{
			geyserSaveData.Tapped = Tapped;
		}
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is GeyserSaveData geyserSaveData)
		{
			_tapped = geyserSaveData.Tapped;
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

	public List<Connection> GetOpenEnds()
	{
		return null;
	}

	public int ConnectedCount()
	{
		return 0;
	}

	public int GetOpenEndsCount()
	{
		return 0;
	}

	public float GetGridSize()
	{
		return 2f;
	}
}
