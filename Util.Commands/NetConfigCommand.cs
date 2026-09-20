using Assets.Scripts.Networking;
using Assets.Scripts.Networking.Transports;

namespace Util.Commands;

internal class NetConfigCommand : ClassManipulator<NetConfig>
{
	public override string HelpText => "Reads or modifies values in NetConfig.xml (e.g. 'netconfig ip 127.0.0.1'); changes are persisted to disk.";

	protected override NetConfig ObjectInstance => NetworkManager.Config;

	protected override void OnValueChanged()
	{
		ObjectInstance.Save();
	}
}
