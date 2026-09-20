using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using Objects.Electrical;
using Trading;

namespace Assets.Scripts.Objects.Items;

public interface IInternalAtmosphere : ISpatial, IPhysical, IProfile, IDensePoolable, IReferencable, IEvaluable
{
	Atmosphere InternalAtmosphere { get; set; }
}
