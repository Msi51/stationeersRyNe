using Assets.Scripts;
using UnityEngine;

public class SaturnShadows : GameBase
{
	public Transform SunTransform;

	[ReadOnly]
	public float sun_x;

	[ReadOnly]
	public float sun_y;

	[ReadOnly]
	public float sun_z;

	private void Update()
	{
		if ((bool)CursorManager.Instance)
		{
			if (!SunTransform)
			{
				SunTransform = OrbitalSimulation.WorldSunTransform;
			}
			if ((bool)SunTransform)
			{
				Vector3 eulerAngles = SunTransform.rotation.eulerAngles;
				sun_x = eulerAngles.x;
				sun_y = eulerAngles.y;
				sun_z = eulerAngles.z;
				Transform.rotation = Quaternion.Euler(sun_x, sun_y, sun_z);
			}
		}
	}
}
