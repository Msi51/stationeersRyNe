using Assets.Scripts.Atmospherics;

namespace Objects.Rockets;

public interface IWorkingAtmosphere
{
	Atmosphere GetWorkingAtmosphere();

	void CacheWorkingAtmosphere();
}
