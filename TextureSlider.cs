using UnityEngine;

public class TextureSlider : MonoBehaviour
{
	public string targetTextureName = "";

	public Vector2 speed = Vector2.one;

	[HideInInspector]
	public bool stringOverride;

	[HideInInspector]
	public int selectedPopupTextureIdx;

	private Material mat;

	private void Start()
	{
		mat = GetComponent<Renderer>().material;
	}

	private void Update()
	{
		Vector2 textureOffset = mat.GetTextureOffset(targetTextureName);
		mat.SetTextureOffset(targetTextureName, textureOffset + speed * Time.deltaTime);
	}
}
