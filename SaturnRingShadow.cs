using UnityEngine;

public class SaturnRingShadow : GameBase
{
	public SaturnShadows SaturnShadows;

	public Transform Planet;

	public Transform container;

	public Transform subContainer;

	public float sun_x;

	public float sun_y;

	public float sun_z;

	public float planet_x;

	public float planet_y;

	public float planet_z;

	public float angularDifference;

	public float pa1;

	public Projector Projector;

	public Transform SunTransform => SaturnShadows.SunTransform;

	private void Update()
	{
		if ((bool)SaturnShadows && (bool)SunTransform)
		{
			sun_x = SaturnShadows.sun_x;
			sun_y = SaturnShadows.sun_y;
			sun_z = SaturnShadows.sun_z;
			Vector3 eulerAngles = Planet.rotation.eulerAngles;
			planet_x = eulerAngles.x;
			planet_y = eulerAngles.y;
			planet_z = eulerAngles.z;
			angularDifference = Quaternion.Angle(Planet.rotation, SunTransform.rotation);
			container.transform.rotation = Quaternion.Euler(planet_x, planet_y, planet_z);
			subContainer.eulerAngles = new Vector3(0f, sun_y, 0f);
			subContainer.localRotation = Quaternion.Euler(0f, subContainer.localRotation.eulerAngles.y, 0f);
			Transform.rotation = Quaternion.Euler(sun_x, sun_y, sun_z);
			pa1 = Transform.localRotation.eulerAngles.x;
			if (pa1 >= 180f)
			{
				Transform.localRotation = Quaternion.Euler(pa1, 0f, 180f);
			}
			else
			{
				Transform.localRotation = Quaternion.Euler(pa1, 0f, 0f);
			}
			angularDifference = ((pa1 >= 180f) ? (pa1 - 360f) : pa1);
			angularDifference = Mathf.Abs(angularDifference);
			Projector.fieldOfView = angularDifference * 0.8f;
		}
	}
}
