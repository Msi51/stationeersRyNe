using UnityEngine;

public interface IPlayableArea
{
	PlayableAreaRule PlayableAreaState { get; }

	Vector2 LastValidPlayablePosition { get; set; }

	PlayableAreaRule CheckPlayableArea();
}
