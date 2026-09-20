using UnityEngine;

public class CameraZoomS : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
		if (Input.GetAxis("Mouse ScrollWheel") > 0f)
		{
			base.transform.Translate(0f, 0f, 100f);
		}
		if (Input.GetAxis("Mouse ScrollWheel") < 0f)
		{
			base.transform.Translate(0f, 0f, -100f);
		}
		if (Input.GetKey(KeyCode.Space))
		{
			base.transform.Translate(0f, 0f, 2f);
		}
		if (Input.GetKey(KeyCode.C))
		{
			base.transform.Translate(0f, 0f, -2f);
		}
	}
}
