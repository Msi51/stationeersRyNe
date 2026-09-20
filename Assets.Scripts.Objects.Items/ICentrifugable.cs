using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public interface ICentrifugable
{
	GameObject GameObject { get; }

	float ProcessTime { get; }

	bool IsCentrifugeSmelt { get; }

	ReagentMixture CentrifugeProcessUnit();
}
