using UnityEngine;

public class ConstantRotation : MonoBehaviour
{
	private Quaternion initialRotation;

	public Vector3 eulerSpeeds = Vector3.one.normalized;

	private void Start()
	{
	}

	private void Update()
	{
		base.transform.rotation *= Quaternion.Euler(eulerSpeeds * Time.deltaTime);
	}
}
