using UnityEngine;

public class RotateAround : GameBase
{
	public float Speed = 1f;

	private void Update()
	{
		if (!WorldManager.IsGamePaused)
		{
			Transform.Rotate(Vector3.forward, Time.deltaTime * Speed, Space.Self);
		}
	}
}
