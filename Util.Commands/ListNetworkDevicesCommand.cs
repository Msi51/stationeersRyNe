using System.Text;
using Assets.Scripts;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;

namespace Util.Commands;

public class ListNetworkDevicesCommand : CommandBase
{
	public override string HelpText => "Lists every device on the given network. Works for PipeNetwork, CableNetwork, and ChuteNetwork referenced by id.";

	public override string[] Arguments => new string[1] { "id" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("listnetworkdevices"))
		{
			return null;
		}
		if (args.Length != 2)
		{
			ConsoleWindow.PrintError("Expected 2 arguments.", suppressStacktrace: true);
			return null;
		}
		if (!CommandBase.Get(args, 1, "id", out string result))
		{
			return null;
		}
		if (!int.TryParse(result, out var result2))
		{
			ConsoleWindow.PrintError("'" + result + "' is not a valid integer.", suppressStacktrace: true);
			return null;
		}
		PipeNetwork pipeNetwork = Referencable.Find<PipeNetwork>(result2);
		CableNetwork cableNetwork = Referencable.Find<CableNetwork>(result2);
		ChuteNetwork chuteNetwork = Referencable.Find<ChuteNetwork>(result2);
		StringBuilder stringBuilder = new StringBuilder();
		if (pipeNetwork != null)
		{
			stringBuilder.AppendLine($"Number of devices in network: {pipeNetwork.DeviceList.Count}");
			foreach (Device device in pipeNetwork.DeviceList)
			{
				stringBuilder.AppendLine(((object)device == null) ? "null entry" : $"refId:{device.ReferenceId}. {device.DisplayName}");
			}
		}
		else if (cableNetwork != null)
		{
			stringBuilder.AppendLine($"Number of devices in network: {cableNetwork.DeviceList.Count}");
			foreach (Device device2 in cableNetwork.DeviceList)
			{
				stringBuilder.AppendLine(((object)device2 == null) ? "null entry" : $"refId:{device2.ReferenceId}. {device2.DisplayName}");
			}
		}
		else if (chuteNetwork != null)
		{
			stringBuilder.AppendLine($"Number of devices in network: {chuteNetwork.DeviceList.Count}");
			foreach (Device device3 in chuteNetwork.DeviceList)
			{
				stringBuilder.AppendLine(((object)device3 == null) ? "null entry" : $"refId:{device3.ReferenceId}. {device3.DisplayName}");
			}
		}
		else
		{
			stringBuilder.AppendLine($"Could not find network: {result2}.");
		}
		return stringBuilder.ToString();
	}
}
