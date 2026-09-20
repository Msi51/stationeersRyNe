using System.Collections.Generic;

namespace Assets.Scripts.Objects;

public interface ITrackable
{
	static readonly List<ITrackable> Trackables;

	string TrackableName { get; }

	static ITrackable()
	{
		Trackables = new List<ITrackable>();
	}
}
