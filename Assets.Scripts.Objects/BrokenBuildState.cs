using System;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public struct BrokenBuildState
{
	public BuildState BuildState;

	public ReagentMixture TotalReagentMixture;

	[HideInInspector]
	public bool HasBroken;
}
