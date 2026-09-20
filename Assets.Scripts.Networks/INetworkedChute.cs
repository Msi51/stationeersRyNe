using Networks;
using Trading;

namespace Assets.Scripts.Networks;

public interface INetworkedChute : INetworkedStructure, INetworkMember, IReferencable, IEvaluable, ISmallGrid, ITooltip
{
	ChuteNetwork ChuteNetwork { get; }

	void OnImGuiDraw();
}
