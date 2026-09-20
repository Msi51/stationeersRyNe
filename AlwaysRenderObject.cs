using UnityEngine;

public class AlwaysRenderObject : MonoBehaviour
{
	public MeshFilter MeshFilter;

	public MeshRenderer MeshRenderer;

	private void Start()
	{
		if ((bool)MeshRenderer)
		{
			Bounds localBounds = new Bounds(Vector3.zero, new Vector3(10000f, 10000f, 10000f));
			MeshRenderer.localBounds = localBounds;
		}
	}
}
