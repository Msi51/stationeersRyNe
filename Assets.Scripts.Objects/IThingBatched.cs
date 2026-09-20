using System.Collections.Generic;

namespace Assets.Scripts.Objects;

public interface IThingBatched : IBatchRendered
{
	long ReferenceId { get; }

	List<ThingRenderer> GetThingRenderers();
}
