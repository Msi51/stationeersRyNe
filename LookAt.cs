using UnityEngine;

public class LookAt : MonoBehaviour
{
	private enum DIRECTION
	{
		FORWARD,
		RIGHT,
		UP
	}

	[SerializeField]
	private Transform target;

	[SerializeField]
	private DIRECTION MapTo;

	[SerializeField]
	private bool invert;

	private void Update()
	{
		Vector3 vector = target.position - base.transform.position;
		if (invert)
		{
			vector = -vector;
		}
		if (MapTo == DIRECTION.FORWARD)
		{
			base.transform.forward = vector.normalized;
		}
		else if (MapTo == DIRECTION.RIGHT)
		{
			base.transform.right = vector.normalized;
		}
		else if (MapTo == DIRECTION.UP)
		{
			base.transform.up = vector.normalized;
		}
	}
}
