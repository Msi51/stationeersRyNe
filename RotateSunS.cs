using UnityEngine;

public class RotateSunS : MonoBehaviour
{
	private float mouseXAmount;

	private void Start()
	{
	}

	private void Update()
	{
		if (Input.GetMouseButton(0))
		{
			mouseXAmount = (0f - Input.GetAxis("Mouse X")) * 3f;
			base.transform.Rotate(0f, mouseXAmount, 0f);
		}
		if (Input.GetKey(KeyCode.Delete))
		{
			base.transform.Rotate(0f, 0.2f, 0f);
		}
		if (Input.GetKey(KeyCode.PageDown))
		{
			base.transform.Rotate(0f, -0.2f, 0f);
		}
		if (Input.GetKey(KeyCode.Home))
		{
			base.transform.Rotate(0.2f, 0f, 0f);
		}
		if (Input.GetKey(KeyCode.End))
		{
			base.transform.Rotate(-0.2f, 0f, 0f);
		}
	}
}
