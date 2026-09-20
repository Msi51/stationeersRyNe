using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using UnityEngine;

public class FollowSun : GameBase
{
	public Vector3 Offset = new Vector3(8.7f, 18.9f, -437.9f);

	public Vector3 Rotation = new Vector3(0f, 0f, 0f);

	public bool ResetX;

	public bool ResetY;

	public bool ResetZ;

	public Light WorldSun => OrbitalSimulation.WorldSun;

	private void Update()
	{
		if (GameManager.GameState == GameState.Running && !(InventoryManager.Parent == null) && !(CursorManager.Instance == null) && !(OrbitalSimulation.WorldSun == null) && !WorldManager.IsGamePaused)
		{
			if (Transform.parent != WorldSun.transform)
			{
				Transform.parent = WorldSun.transform;
			}
			Transform.localPosition = Offset;
			Transform.LookAt(InventoryManager.Parent.ThingTransform);
			Vector3 eulerAngles = Transform.rotation.eulerAngles;
			if (ResetX)
			{
				eulerAngles.x = 0f;
			}
			if (ResetY)
			{
				eulerAngles.y = 0f;
			}
			if (ResetZ)
			{
				eulerAngles.z = 0f;
			}
			Transform.localRotation = Quaternion.Euler(eulerAngles);
			Transform.Rotate(Rotation, Space.Self);
		}
	}
}
