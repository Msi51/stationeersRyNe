using Assets.Scripts.Util;

namespace Objects.Electrical;

public interface IPhysical : IProfile, IDensePoolable
{
	bool RunPhysicsUpdate { get; }

	void PhysicsUpdate();
}
