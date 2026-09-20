using Assets.Scripts;
using UnityEngine;

public class AlwaysFaceCamera : GameBase
{
	public void LateUpdate()
	{
		Transform.LookAt(CameraController.CurrentCamera.transform, Vector3.up);
	}
}
