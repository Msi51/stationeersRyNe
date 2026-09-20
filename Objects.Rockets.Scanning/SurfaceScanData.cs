using Assets.Scripts.Networking;

namespace Objects.Rockets.Scanning;

public class SurfaceScanData : SpaceMapNodeActionData
{
	public SurfaceScanData()
	{
	}

	public SurfaceScanData(RocketBinaryReader reader)
		: base(reader)
	{
	}

	public override RocketAction ToInstance(SpaceMapNode node)
	{
		return new SurfaceScan(this, node);
	}
}
