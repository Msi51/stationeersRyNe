using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Objects.Electrical;

namespace Networks;

public class LandingPadNetwork : AtmosphericsNetwork
{
	private readonly List<LandingPadCenter> _centerPieces = new List<LandingPadCenter>();

	private readonly List<LandingPadTaxiThreshold> _thresholdPieces = new List<LandingPadTaxiThreshold>();

	public static List<LandingPadNetwork> AllLandingPadNetworks = new List<LandingPadNetwork>();

	public PressurekPa MaxPressureKpa => Chemistry.Limits.MAXPressureGasPipe;

	public override bool PreventStateChange => true;

	public override StructureNetworkType NetworkType => StructureNetworkType.LandingPad;

	public static long[] AllNetworkIds => ReferencableNetworkHelper.GetValidNetworkIds(AllLandingPadNetworks);

	public LandingPadCenter LandingPadCenter
	{
		get
		{
			int count = _centerPieces.Count;
			if (count != 0 && count <= 1)
			{
				return _centerPieces[0];
			}
			return null;
		}
	}

	public bool IsCompleted()
	{
		if (LandingPadCenter == null)
		{
			return false;
		}
		foreach (INetworkedStructure structure in base.StructureList)
		{
			if (structure != null && !structure.IsStructureCompleted)
			{
				return false;
			}
		}
		return true;
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		AllLandingPadNetworks.Add(this);
	}

	protected override void OnDeregister()
	{
		base.OnDeregister();
		AllLandingPadNetworks.Remove(this);
	}

	public bool ValidApproachState(out LandingPadTaxiThreshold validThreshold)
	{
		validThreshold = null;
		if (_thresholdPieces.Count == 0)
		{
			return true;
		}
		foreach (LandingPadTaxiThreshold thresholdPiece in _thresholdPieces)
		{
			if (thresholdPiece != null && thresholdPiece.OnOff)
			{
				if (!(validThreshold == null))
				{
					validThreshold = null;
					return false;
				}
				validThreshold = thresholdPiece;
			}
		}
		return true;
	}

	public LandingPadNetwork(long referenceId = 0L)
		: base(referenceId)
	{
	}

	protected override ReferencableNetwork<INetworkedStructure> CreateNewNetwork()
	{
		return new LandingPadNetwork(0L);
	}

	public override bool Add(INetworkedStructure iNetworkedStructure)
	{
		if (base.Add(iNetworkedStructure))
		{
			if (iNetworkedStructure is LandingPadCenter item && !_centerPieces.Contains(item))
			{
				_centerPieces.Add(item);
				OnNetworkChanged();
			}
			else if (iNetworkedStructure is LandingPadTaxiThreshold item2 && !_thresholdPieces.Contains(item2))
			{
				_thresholdPieces.Add(item2);
				OnNetworkChanged();
			}
			return true;
		}
		return false;
	}

	public override bool Remove(INetworkedStructure iNetworkedStructure)
	{
		if (base.Remove(iNetworkedStructure))
		{
			if (iNetworkedStructure is LandingPadCenter item)
			{
				_centerPieces.Remove(item);
			}
			else if (iNetworkedStructure is LandingPadTaxiThreshold item2)
			{
				_thresholdPieces.Remove(item2);
			}
			return true;
		}
		return false;
	}

	protected override void OnNetworkChanged()
	{
		base.OnNetworkChanged();
		foreach (LandingPadCenter centerPiece in _centerPieces)
		{
			centerPiece.RefreshPadPower();
		}
		foreach (INetworkedStructure structure in base.StructureList)
		{
			if (structure is LandingPadDataPowerConnection { DataCableNetwork: not null } landingPadDataPowerConnection)
			{
				landingPadDataPowerConnection.DataCableNetwork.RefreshNetworkDevice(landingPadDataPowerConnection);
			}
			structure.OnStructureNetworkUpdated();
		}
		LinkTaxiWaypoints();
	}

	public void LinkTaxiWaypoints()
	{
		if (GameManager.IsThread)
		{
			return;
		}
		ClearWaypointLinksOnNetwork();
		if (LandingPadCenter == null)
		{
			return;
		}
		if (ValidApproachState(out var validThreshold))
		{
			if (validThreshold == null)
			{
				return;
			}
			validThreshold.LinkTaxiWaypoints(LandingPadCenter);
		}
		LandingPadCenter.CheckError();
	}

	public List<DynamicThing> GetNetworkInventory()
	{
		List<DynamicThing> list = new List<DynamicThing>();
		List<Device> list2 = new List<Device>();
		foreach (INetworkedStructure structure in base.StructureList)
		{
			if (!(structure is Device device) || device.DataCable == null)
			{
				continue;
			}
			foreach (Device dataDevice in device.DataCable.CableNetwork.DataDeviceList)
			{
				if (!list2.Contains(dataDevice) && dataDevice is ITradableInventory tradableInventory && device.Powered && dataDevice.OnOff)
				{
					list.AddRange(tradableInventory.GetContents());
					list2.Add(dataDevice);
				}
			}
		}
		return list;
	}

	public List<ITradableInventory> GetVendorsOnNetwork()
	{
		List<ITradableInventory> list = new List<ITradableInventory>();
		foreach (INetworkedStructure structure in base.StructureList)
		{
			if (!(structure is Device device) || device.DataCable == null)
			{
				continue;
			}
			foreach (Device dataDevice in device.DataCable.CableNetwork.DataDeviceList)
			{
				if (dataDevice is ITradableInventory item && dataDevice.Powered && dataDevice.OnOff)
				{
					list.Add(item);
				}
			}
		}
		return list;
	}
}
