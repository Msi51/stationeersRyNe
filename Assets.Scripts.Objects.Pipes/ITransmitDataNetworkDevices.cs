using System.Collections.Generic;
using Assets.Scripts.Networks;
using Trading;

namespace Assets.Scripts.Objects.Pipes;

public interface ITransmitDataNetworkDevices : ILogicable, IReferencable, IEvaluable
{
	List<IReceiveDataNetworkDevices> ConnectedDataNetReceivers { get; set; }

	CableNetwork DataCableNetwork { get; }

	bool DataConnectionActive();

	void RemoveReceiver(IReceiveDataNetworkDevices receiver);

	void AddReceiver(IReceiveDataNetworkDevices receiver);
}
