using UnityEngine;

public abstract class Printer : MonoBehaviour
{
	public Projection[] prints;

	public PrintSelection printMethod;

	public PrintParent parent;

	public FadeMethod fadeMethod;

	public float inDuration;

	public float fadeDelay;

	public float outDuration;

	public CullMethod cullMethod;

	public float cullDuration;

	public bool destroyOnPrint;

	public float frequencyTime;

	public float frequencyDistance;

	private float timeSincePrint = float.PositiveInfinity;

	private Vector3 lastPrintPos = Vector3.zero;

	private void Update()
	{
		timeSincePrint += Time.deltaTime;
	}

	protected void Print(Vector3 Position, Quaternion Rotation, Transform Surface)
	{
		if (prints == null || prints.Length < 1)
		{
			Debug.LogError("No Projections to print. Please set at least one projection to print.");
		}
		else
		{
			if (!(timeSincePrint >= frequencyTime) || !(Vector3.Distance(Position, lastPrintPos) >= frequencyDistance))
			{
				return;
			}
			switch (printMethod)
			{
			case PrintSelection.All:
			{
				Projection[] array = prints;
				foreach (Projection projection in array)
				{
					PrintProjection(projection, Position, Rotation, Surface);
				}
				break;
			}
			case PrintSelection.Random:
			{
				int num = Random.Range(0, prints.Length - 1);
				PrintProjection(prints[num], Position, Rotation, Surface);
				break;
			}
			}
			if (destroyOnPrint)
			{
				Object.Destroy(base.gameObject);
			}
			timeSincePrint = 0f;
			lastPrintPos = Position;
		}
	}

	private void PrintProjection(Projection Projection, Vector3 Position, Quaternion Rotation, Transform Surface)
	{
		Projection projection = ProjectionPool.RequestCopy(Projection);
		projection.Fade(fadeMethod, inDuration, fadeDelay, outDuration);
		projection.Culled(cullMethod, cullDuration);
		projection.transform.position = Position;
		projection.transform.rotation = Rotation;
		if (parent != PrintParent.Surface)
		{
			return;
		}
		Transform transform = null;
		foreach (Transform item in Surface)
		{
			if (item.name.Equals("Projections"))
			{
				transform = item;
			}
		}
		if (transform == null)
		{
			transform = new GameObject("Projections").transform;
			transform.SetParent(Surface);
		}
		projection.transform.SetParent(transform);
	}
}
