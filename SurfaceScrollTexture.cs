using UnityEngine;

public class SurfaceScrollTexture : MonoBehaviour
{
	public float ScrollX;

	public float ScrollY;

	public Renderer JupiterMaterial;

	private float OffsetX;

	private float OffsetY;

	private Vector3 NewRot;

	public void Update()
	{
		OffsetX = Time.time * ScrollX;
		OffsetY = Time.time * ScrollY;
		JupiterMaterial.material.mainTextureOffset = new Vector3(OffsetX, OffsetY, 0f);
	}
}
