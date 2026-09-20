using Assets.Scripts.Util;
using Trading;

namespace Assets.Scripts.Objects.Items;

public interface IPowered : IDensePoolable, IReferencable, IEvaluable
{
	void OnPowerTick();
}
