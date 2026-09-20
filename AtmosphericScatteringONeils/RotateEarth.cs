using UnityEngine;

namespace AtmosphericScatteringONeils;

public class RotateEarth : MonoBehaviour
{
	public float speed = 1f;

	private void Update()
	{
		base.transform.Rotate(new Vector3(0f, Time.deltaTime * speed, 0f));
	}
}
