using UnityEngine;

namespace AtmosphericScatteringONeils;

public class RotateLight : MonoBehaviour
{
	public float speed = 100f;

	private Vector3 lastMousePos;

	private bool rotate;

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			rotate = true;
		}
		if (Input.GetMouseButtonUp(0))
		{
			rotate = false;
		}
		Vector3 vector = lastMousePos - Input.mousePosition;
		if (rotate)
		{
			base.transform.Rotate(new Vector3(vector.y * Time.deltaTime * speed, vector.x * Time.deltaTime * speed, 0f));
		}
		lastMousePos = Input.mousePosition;
	}
}
