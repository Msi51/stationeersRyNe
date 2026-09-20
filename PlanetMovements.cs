using UnityEngine;

public class PlanetMovements : MonoBehaviour
{
	public float rotationSpeed;

	private void Update()
	{
		base.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
	}
}
