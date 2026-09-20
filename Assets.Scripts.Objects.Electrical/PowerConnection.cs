using Assets.Scripts.Networks;

namespace Assets.Scripts.Objects.Electrical;

public class PowerConnection : Electrical
{
	protected override bool IsOperable
	{
		get
		{
			if (ConnectedCableNetworks.Count == 2 && Error == 1)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			else if (ConnectedCableNetworks.Count != 2 && Error == 0)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return ConnectedCableNetworks.Count == 2;
		}
	}

	private int GetNetworkIndex(CableNetwork cableNetwork)
	{
		return ConnectedCableNetworks.FindIndex((CableNetwork c) => c == cableNetwork);
	}

	private CableNetwork GetOtherNetwork(CableNetwork cableNetwork)
	{
		if (!IsOperable)
		{
			return cableNetwork;
		}
		if (GetNetworkIndex(cableNetwork) != 0)
		{
			return ConnectedCableNetworks[0];
		}
		return ConnectedCableNetworks[1];
	}

	public override void InitializeDevice()
	{
		base.InitializeDevice();
		CheckConnections();
	}

	protected override void CheckConnections()
	{
		base.CheckConnections();
		_ = IsOperable;
	}

	public override void OnAddCableNetwork(CableNetwork newNetwork)
	{
		base.OnAddCableNetwork(newNetwork);
		CheckConnections();
	}

	public override void OnRemoveCableNetwork(CableNetwork oldNetwork)
	{
		base.OnRemoveCableNetwork(oldNetwork);
		CheckConnections();
	}
}
