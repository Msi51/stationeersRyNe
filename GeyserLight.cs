using UnityEngine;

public class GeyserLight : MonoBehaviour
{
	public GameObject geyser;

	private Vector3 sunRotation;

	private Vector3 geyserRotation;

	private float angularDifference;

	private float colorValue;

	private float geyserAngleDif;

	public GameObject sun;

	private Transform mCam;

	private bool disableBillboard;

	private void Start()
	{
		sun = GameObject.FindGameObjectWithTag("Sun");
		mCam = Camera.main.transform;
	}

	private void Update()
	{
		sunRotation = sun.transform.forward;
		geyserRotation = geyser.transform.forward;
		angularDifference = (90f - Vector3.Angle(sunRotation, geyserRotation)) * -0.011f;
		if (angularDifference > 0f)
		{
			angularDifference *= 4f;
		}
		if (angularDifference < 0f)
		{
			angularDifference *= -0.4f;
		}
		colorValue = 1f - angularDifference;
		geyser.GetComponent<Renderer>().material.SetColor("_EmisColor", new Color(colorValue, colorValue, colorValue));
		if (!disableBillboard)
		{
			geyser.transform.LookAt(mCam);
			geyser.transform.localRotation = Quaternion.Euler(90f, geyser.transform.localRotation.eulerAngles.y, 0f);
		}
	}

	public void setBillboard(bool value)
	{
		disableBillboard = value;
	}
}
