using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Networks;
using UnityEngine;

namespace Objects.Rockets;

public interface IRocketInternals : IRocketComponent
{
	RocketInternalCellType InternalCellType { get; }

	bool StrictlyInternal { get; }

	RocketNetwork RocketNetwork { get; set; }

	Vector3 Position { get; set; }

	Vector3 ThingTransformPosition { get; }

	Transform Transform { get; }

	List<Connection> AccessOpenEnds { get; }

	WorldGrid WorldGrid { get; set; }

	void OnLaunch(bool immediate = false);

	void OnLanded(bool immediate = false);
}
