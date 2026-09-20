using Assets.Scripts.Util;

namespace Assets.Scripts;

public interface ILightActivated : IDensePoolable
{
	bool IsBeingDestroyed { get; }
}
