using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets.Scanning;
using Trading;
using UnityEngine;

namespace Objects.Rockets;

public class RocketGasUmbilicalFemale : DeviceInput, IUmbilical, IRocketComponent, IReferencable, IEvaluable, IRocketInternals, IRocketActionProgressableTarget, IRocketTransferActionProgressable, IRocketActionProgressable
{
	private IUmbilical _partnerUmbilical;

	private float _transferProgress;

	protected long _savedPartnerId;

	protected IUmbilical PartnerUmbilical
	{
		get
		{
			return _partnerUmbilical;
		}
		set
		{
			_partnerUmbilical = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	public float TransferProgress
	{
		get
		{
			return _transferProgress;
		}
		set
		{
			_transferProgress = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 512;
			}
		}
	}

	public Vector3 FirstPartnerSearchPosition => base.Position + Forward * SmallGrid.SmallGridSize;

	public UmbilicalType UmbilicalType => UmbilicalType.Socket;

	public bool InputValidWithAtmosphere
	{
		get
		{
			if (IsInputValid)
			{
				return ConnectedPipeNetwork.Atmosphere != null;
			}
			return false;
		}
	}

	public Atmosphere ConnectedAtmosphere => ConnectedPipeNetwork?.Atmosphere;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Umbilical;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public Thing AsThing => this;

	public int PartnerDistance { get; set; }

	public float GetActionProgress => TransferProgress;

	public IRocketActionProgressableTarget CurrentTarget => PartnerUmbilical as IRocketActionProgressableTarget;

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(TransferProgress);
			Network.WritePackedId(writer, PartnerUmbilical);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			TransferProgress = reader.ReadSingle();
			Network.ReadPackedId(reader, out var referenceId);
			SetPartner(Referencable.Find<IUmbilical>(referenceId));
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(TransferProgress);
		Network.WritePackedId(writer, PartnerUmbilical);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		TransferProgress = reader.ReadSingle();
		Network.ReadPackedId(reader, out _savedPartnerId);
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.UmbilicalCategory);
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		SetPartner(Thing.Find<IUmbilical>(_savedPartnerId));
	}

	public override void OnAtmosphericTick()
	{
		if (IsInputValid && ConnectedPipeNetwork.Atmosphere != null && _partnerUmbilical is RocketGasUmbilicalFemale rocketGasUmbilicalFemale)
		{
			AtmosphereHelper.Mix(ConnectedAtmosphere, rocketGasUmbilicalFemale.ConnectedAtmosphere, AtmosphereHelper.MatterState.All);
		}
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		_partnerUmbilical?.PartnerRemoved();
	}

	public void OnLaunch(bool immediate = false)
	{
		if (_partnerUmbilical != null)
		{
			if (_partnerUmbilical.IsOpen)
			{
				_partnerUmbilical.GetDamageState().Damage(ChangeDamageType.Set, _partnerUmbilical.GetDamageState().MaxDamage, DamageUpdateType.Brute);
			}
			else
			{
				_partnerUmbilical.PartnerRemoved();
			}
			_partnerUmbilical = null;
		}
	}

	public void OnLanded(bool immediate = false)
	{
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
	}

	public void SetPartner(IUmbilical partner)
	{
		_partnerUmbilical = partner;
	}

	public bool IsCompatibleWith(IUmbilical other)
	{
		if (other is RocketGasUmbilicalMale rocketGasUmbilicalMale)
		{
			foreach (Connection accessOpenEnd in base.AccessOpenEnds)
			{
				foreach (Connection accessOpenEnd2 in rocketGasUmbilicalMale.AccessOpenEnds)
				{
					if (accessOpenEnd.ConnectionType == accessOpenEnd2.ConnectionType)
					{
						return true;
					}
				}
			}
		}
		if (other is RocketGasUmbilicalFemale rocketGasUmbilicalFemale)
		{
			foreach (Connection accessOpenEnd3 in base.AccessOpenEnds)
			{
				foreach (Connection accessOpenEnd4 in rocketGasUmbilicalFemale.AccessOpenEnds)
				{
					if (accessOpenEnd3.ConnectionType == accessOpenEnd4.ConnectionType)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public void PartnerRemoved()
	{
		_partnerUmbilical = null;
	}

	public void RetractUmbilical()
	{
		_partnerUmbilical?.RetractUmbilical();
	}

	public void ExtendUmbilical()
	{
		_partnerUmbilical?.ExtendUmbilical();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketGasUmbilicalFemaleSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RocketGasUmbilicalFemaleSaveData rocketGasUmbilicalFemaleSaveData)
		{
			PartnerDistance = rocketGasUmbilicalFemaleSaveData.PartnerDistance;
			_savedPartnerId = rocketGasUmbilicalFemaleSaveData.PartnerUmbilicalId;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RocketGasUmbilicalFemaleSaveData rocketGasUmbilicalFemaleSaveData)
		{
			rocketGasUmbilicalFemaleSaveData.PartnerDistance = PartnerDistance;
			rocketGasUmbilicalFemaleSaveData.PartnerUmbilicalId = PartnerUmbilical?.ReferenceId ?? 0;
		}
	}

	public bool CanProgressAction(out RocketActionResult result)
	{
		RocketGasUmbilicalFemale rocketGasUmbilicalFemale = PartnerUmbilical as RocketGasUmbilicalFemale;
		if (!rocketGasUmbilicalFemale || RocketNetwork?.Rocket?.CurrentNode == null || rocketGasUmbilicalFemale.RocketNetwork?.Rocket?.CurrentNode != RocketNetwork.Rocket.CurrentNode)
		{
			return result = RocketActionResult.Failure(GameStrings.NoPartnerUmbilical);
		}
		return result = RocketActionResult.Success;
	}

	public void ProgressTransferAction(float deltaTime, RocketTransfer rocketTransfer)
	{
		TransferProgress += deltaTime;
		TransferProgress %= 1f;
	}

	public void ClearAction()
	{
		TransferProgress = 0f;
		SetPartner(null);
	}

	public string GetActionInfoText()
	{
		RocketGasUmbilicalFemale rocketGasUmbilicalFemale = PartnerUmbilical as RocketGasUmbilicalFemale;
		if (ConnectedAtmosphere == null || rocketGasUmbilicalFemale?.ConnectedAtmosphere == null)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		string value = ConnectedAtmosphere.PressureGassesAndLiquidsInPa.ToStringPrefix("Pa", "yellow");
		string value2 = ConnectedAtmosphere.TotalVolumeLiquids.ToFloat().ToStringPrefix("L", "yellow");
		string value3 = rocketGasUmbilicalFemale.ConnectedAtmosphere.PressureGassesAndLiquidsInPa.ToStringPrefix("Pa", "yellow");
		string value4 = rocketGasUmbilicalFemale.ConnectedAtmosphere.TotalVolumeLiquids.ToFloat().ToStringPrefix("L", "yellow");
		stringBuilder.Append(GameStrings.GasTransferActionInfo);
		stringBuilder.Append(GameStrings.ActionSource);
		stringBuilder.Append(value);
		if (ConnectedAtmosphere.TotalVolumeLiquids > VolumeLitres.Zero)
		{
			stringBuilder.Append(' ');
			stringBuilder.Append(value2);
		}
		stringBuilder.Append('.');
		stringBuilder.Append(GameStrings.ActionTarget);
		stringBuilder.Append(value3);
		if (rocketGasUmbilicalFemale.ConnectedAtmosphere.TotalVolumeLiquids > VolumeLitres.Zero)
		{
			stringBuilder.Append(' ');
			stringBuilder.Append(value4);
		}
		stringBuilder.Append('.');
		return stringBuilder.ToString();
	}

	public List<IRocketActionProgressableTarget> GetValidTargets()
	{
		List<IRocketActionProgressableTarget> list = new List<IRocketActionProgressableTarget>(4);
		Rocket rocket = RocketNetwork?.Rocket;
		List<Rocket> list2 = (rocket?.CurrentNode)?.RocketsHere;
		if (list2 == null)
		{
			return list;
		}
		foreach (Rocket item in list2)
		{
			if (item == rocket || item == null)
			{
				continue;
			}
			foreach (IUmbilical umbilical in item.GetUmbilicals())
			{
				if (umbilical is RocketGasUmbilicalFemale rocketGasUmbilicalFemale && IsCompatibleWith(rocketGasUmbilicalFemale))
				{
					list.Add(rocketGasUmbilicalFemale);
				}
			}
		}
		return list;
	}

	public void SetTarget(IRocketActionProgressableTarget selectedTarget)
	{
		RocketGasUmbilicalFemale rocketGasUmbilicalFemale = selectedTarget as RocketGasUmbilicalFemale;
		if (!IsCompatibleWith(rocketGasUmbilicalFemale))
		{
			rocketGasUmbilicalFemale = null;
		}
		SetPartner(rocketGasUmbilicalFemale);
		TransferProgress = 0f;
		if ((bool)rocketGasUmbilicalFemale)
		{
			rocketGasUmbilicalFemale.SetPartner(null);
		}
	}
}
