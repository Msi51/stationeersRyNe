using UnityEngine;

public class RotateCameraS : MonoBehaviour
{
	private float mouseXAmount;

	private float mouseYAmount;

	public Transform cam;

	private void Start()
	{
	}

	private void Update()
	{
		if (Input.GetKey(KeyCode.A))
		{
			base.transform.Rotate(0f, 0f, 0.1f);
		}
		if (Input.GetKey(KeyCode.E))
		{
			base.transform.Rotate(0f, 0f, -0.1f);
		}
		if (Input.GetKey(KeyCode.LeftArrow))
		{
			base.transform.Rotate(0f, 0.07f, 0f);
		}
		if (Input.GetKey(KeyCode.RightArrow))
		{
			base.transform.Rotate(0f, -0.07f, 0f);
		}
		if (Input.GetKey(KeyCode.UpArrow))
		{
			base.transform.Rotate(0.07f, 0f, 0f);
		}
		if (Input.GetKey(KeyCode.DownArrow))
		{
			base.transform.Rotate(-0.07f, 0f, 0f);
		}
		if (Input.GetKey(KeyCode.Z))
		{
			cam.Rotate(-0.07f, 0f, 0f);
		}
		if (Input.GetKey(KeyCode.S))
		{
			cam.Rotate(0.07f, 0f, 0f);
		}
		if (Input.GetKey(KeyCode.Q))
		{
			cam.Rotate(0f, -0.07f, 0f);
		}
		if (Input.GetKey(KeyCode.D))
		{
			cam.Rotate(0f, 0.07f, 0f);
		}
	}
}
