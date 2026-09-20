using Assets.Scripts.Networks;
using Trading;

namespace Assets.Scripts.Objects.Pipes;

public interface IReceiveDataNetworkDevices : ILogicable, IReferencable, IEvaluable
{
	ITransmitDataNetworkDevices ConnectedDataNetTransmitter { get; set; }

	CableNetwork DataCableNetwork { get; }

	bool DataConnectionActive();

	bool GetIsOperable();
}
