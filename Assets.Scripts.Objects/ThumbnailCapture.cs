using UnityEngine;

namespace Assets.Scripts.Objects;

public class ThumbnailCapture : MonoBehaviour
{
	public Camera ThumbnailCamera;

	public Light ThumbnailLight;

	public Shader ThumbnailShader;

	public float DistanceFromCamera;

	public Vector3 ThingRotationOffset;

	public bool IncludeBoundsInPositioning;

	public bool AllowCameraRepositioning = true;

	public bool StopOnEachThing;

	public string PrebfabName;

	private Material _material;

	private RenderTexture _textTexture;

	private static int textTextureWidth = 256;

	private static int textTextureHeight = 256;
}
