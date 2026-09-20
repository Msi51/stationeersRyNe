using UnityEngine;

public abstract class Positioner : MonoBehaviour
{
	public Projection projection;

	public LayerMask layers;

	public bool alwaysVisible;

	private Projection proj;

	private void OnDisable()
	{
		if (proj != null)
		{
			proj.gameObject.SetActive(value: false);
		}
	}

	protected virtual void Start()
	{
		proj = Object.Instantiate(projection.gameObject, ProjectionPool.Parent).GetComponent<Projection>();
		proj.name = "Projection";
	}

	protected void Reproject(Ray Ray, float CastLength, Vector3 ReferenceUp)
	{
		if (Physics.Raycast(Ray, out var hitInfo, float.PositiveInfinity, layers.value))
		{
			proj.gameObject.SetActive(value: true);
			proj.transform.rotation = Quaternion.LookRotation(-hitInfo.normal, ReferenceUp);
			proj.transform.position = hitInfo.point;
		}
		else if (!alwaysVisible)
		{
			proj.gameObject.SetActive(value: false);
		}
	}

	private Vector3 Divide(Vector3 A, Vector3 B)
	{
		return new Vector3(A.x / B.x, A.y / B.y, A.z / B.z);
	}
}
