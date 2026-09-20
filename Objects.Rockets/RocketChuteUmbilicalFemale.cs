using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Networks;
using Objects.Pipes;
using Objects.Rockets.Scanning;
using Trading;
using UnityEngine;

namespace Objects.Rockets;

public class RocketChuteUmbilicalFemale : ChuteDevice, IUmbilical, IRocketComponent, IReferencable, IEvaluable, IRocketInternals
{
	private RocketChuteUmbilicalMale _partnerUmbilical;

	public bool CanTransfer => true;

	private bool PartnerValid
	{
		get
		{
			if ((object)_partnerUmbilical != null)
			{
				return _partnerUmbilical.CanTransfer;
			}
			return false;
		}
	}

	public Vector3 FirstPartnerSearchPosition => base.Position + Forward * SmallGrid.SmallGridSize;

	public UmbilicalType UmbilicalType => UmbilicalType.Socket;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Umbilical;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public Thing AsThing => this;

	public Type PartnerType => typeof(RocketChuteUmbilicalMale);

	public int PartnerDistance { get; set; }

	public float GetActionProgress => 0f;

	public IRocketActionProgressableTarget CurrentTarget => null;

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.UmbilicalCategory);
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		RocketUmbilicalHelper.FindAndSetOtherUmbilical(this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		if ((object)_partnerUmbilical != null)
		{
			_partnerUmbilical.PartnerRemoved();
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (CanTransfer && PartnerValid && (bool)base.TransportSlot.Occupant)
		{
			Connection connection = _partnerUmbilical.OpenEnds[0];
			SmallGrid chuteOrDevice = connection.GetChuteOrDevice();
			if (chuteOrDevice == null)
			{
				OnServer.MoveToWorld(base.TransportSlot.Occupant, connection.Transform.position, Quaternion.identity, -connection.Transform.forward, UnityEngine.Random.insideUnitSphere);
			}
			else if (chuteOrDevice is IChute chute && !chute.TransportSlot.Occupant)
			{
				chute.SetNeighbor(_partnerUmbilical);
				OnServer.MoveToSlot(base.TransportSlot.Occupant, chute.TransportSlot);
			}
		}
	}

	public void OnLaunch(bool immediate = false)
	{
		if ((object)_partnerUmbilical != null)
		{
			if (_partnerUmbilical.IsOpen)
			{
				_partnerUmbilical.DamageState.Damage(ChangeDamageType.Set, _partnerUmbilical.DamageState.MaxDamage, DamageUpdateType.Brute);
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
		_partnerUmbilical = partner as RocketChuteUmbilicalMale;
	}

	public bool IsCompatibleWith(IUmbilical other)
	{
		return other is RocketChuteUmbilicalMale;
	}

	public void PartnerRemoved()
	{
		SetPartner(null);
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
		ThingSaveData savedData = new RocketChuteUmbilicalFemaleSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RocketChuteUmbilicalFemaleSaveData rocketChuteUmbilicalFemaleSaveData)
		{
			PartnerDistance = rocketChuteUmbilicalFemaleSaveData.PartnerDistance;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RocketChuteUmbilicalFemaleSaveData rocketChuteUmbilicalFemaleSaveData)
		{
			rocketChuteUmbilicalFemaleSaveData.PartnerDistance = PartnerDistance;
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action != InteractableType.Open)
		{
			return base.GetContextualName(interactable);
		}
		if (!IsOpen)
		{
			return ActionStrings.Extend;
		}
		return ActionStrings.Retract;
	}

	public bool CanProgressAction(out RocketActionResult result)
	{
		result = RocketActionResult.Failure(GameStrings.None);
		return result;
	}

	public void ProgressTransferAction(float deltaTime, RocketTransfer rocketTransfer)
	{
	}

	public void ClearAction()
	{
	}

	public string GetActionInfoText()
	{
		return "";
	}

	public List<IRocketActionProgressableTarget> GetValidTargets()
	{
		return null;
	}

	public void SetTarget(IRocketActionProgressableTarget selectedTarget)
	{
	}
}
