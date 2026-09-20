using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets.Scanning;
using Trading;
using UnityEngine;

namespace Objects.Rockets;

public abstract class RocketPowerUmbilical : ElectricalInputOutput, IUmbilical, IRocketComponent, IReferencable, IEvaluable, IRocketActionProgressableTarget
{
	[Header("Battery")]
	public float PowerMaximum = 10000f;

	private RocketPowerUmbilical _partnerUmbilical;

	private float _transferProgress;

	private bool _powerUpdate;

	private float _powerStored;

	private float _lastPowerStored;

	private int _lastPowerTickRemoved = -1;

	private float _lastPowerRemoved;

	private int _lastPowerTickAdded = -1;

	private float _lastPowerAdded;

	protected long _savedPartnerId;

	public RocketNetwork RocketNetwork { get; set; }

	public Thing AsThing => this;

	public bool CanTransfer => true;

	public int PartnerDistance { get; set; }

	public abstract Vector3 FirstPartnerSearchPosition { get; }

	public abstract UmbilicalType UmbilicalType { get; }

	protected RocketPowerUmbilical PartnerUmbilical
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

	public float PowerStored
	{
		get
		{
			return _powerStored;
		}
		set
		{
			if (!float.IsNaN(value))
			{
				_powerStored = Mathf.Clamp(value, 0f, PowerMaximum);
			}
		}
	}

	public override float AvailablePower => PowerStored;

	public float LastPowerRemoved
	{
		get
		{
			if (GameManager.RunSimulation && ElectricityManager.Instance.TotalTickCount > _lastPowerTickRemoved + 1)
			{
				_lastPowerRemoved = 0f;
			}
			return _lastPowerRemoved;
		}
		set
		{
			if (_lastPowerTickRemoved != ElectricityManager.Instance.TotalTickCount)
			{
				_lastPowerTickRemoved = ElectricityManager.Instance.TotalTickCount;
				_lastPowerRemoved = value / 1000f;
			}
			else
			{
				_lastPowerRemoved += value / 1000f;
			}
		}
	}

	protected float LastPowerAdded
	{
		get
		{
			if (GameManager.RunSimulation && ElectricityManager.Instance.TotalTickCount > _lastPowerTickAdded + 1)
			{
				_lastPowerAdded = 0f;
			}
			return _lastPowerAdded;
		}
		set
		{
			if (_lastPowerTickAdded != ElectricityManager.Instance.TotalTickCount)
			{
				_lastPowerTickAdded = ElectricityManager.Instance.TotalTickCount;
				_lastPowerAdded = value / 1000f;
			}
			else
			{
				_lastPowerAdded += value / 1000f;
			}
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public float GetActionProgress => TransferProgress;

	public abstract void PartnerRemoved();

	public abstract bool IsCompatibleWith(IUmbilical other);

	public abstract void SetPartner(IUmbilical partner);

	public virtual bool CanProgressAction(out RocketActionResult result)
	{
		result = RocketActionResult.Failure(GameStrings.NoPartnerUmbilical);
		return false;
	}

	public void ProgressTransferAction(float deltaTime, RocketTransfer rocketTransfer)
	{
		TransferProgress += deltaTime;
		TransferProgress = Mathf.Min(TransferProgress, 1f);
	}

	public void ClearAction()
	{
		TransferProgress = 0f;
		SetPartner(null);
	}

	public virtual void RetractUmbilical()
	{
	}

	public virtual void ExtendUmbilical()
	{
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(LastPowerAdded);
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(TransferProgress);
			Network.WritePackedId(writer, PartnerUmbilical);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			LastPowerAdded = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			TransferProgress = reader.ReadSingle();
			Network.ReadPackedId(reader, out var referenceId);
			SetPartner(Referencable.Find<RocketPowerUmbilical>(referenceId));
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(LastPowerAdded);
		writer.WriteSingle(TransferProgress);
		Network.WritePackedId(writer, PartnerUmbilical);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		LastPowerAdded = reader.ReadSingle();
		TransferProgress = reader.ReadSingle();
		Network.ReadPackedId(reader, out _savedPartnerId);
	}

	public string GetActionInfoText()
	{
		if (RocketNetwork == null || PartnerUmbilical?.RocketNetwork == null)
		{
			return string.Empty;
		}
		float num = 0f;
		float num2 = 0f;
		if (RocketNetwork.Batteries.Count > 0)
		{
			float num3 = 0f;
			float num4 = 0f;
			foreach (Battery battery in RocketNetwork.Batteries)
			{
				num3 += battery.PowerStored;
				num4 += battery.PowerMaximum;
			}
			num = num3 / num4;
		}
		if (PartnerUmbilical.RocketNetwork.Batteries.Count > 0)
		{
			float num5 = 0f;
			float num6 = 0f;
			foreach (Battery battery2 in PartnerUmbilical.RocketNetwork.Batteries)
			{
				num5 += battery2.PowerStored;
				num6 += battery2.PowerMaximum;
			}
			num2 = num5 / num6;
		}
		return GameStrings.PowerTransferActionInfo.AsString((num * 100f).ToStringPercent("yellow"), (num2 * 100f).ToStringPercent("yellow"));
	}
}
