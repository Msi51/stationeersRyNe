using Assets.Scripts.Objects;
using UnityEngine;

namespace ThingImport.Thumbnails;

public class ThumbnailGenerator : MonoBehaviour
{
	[SerializeField]
	private ThumbnailGeneratorRig _thumbnailGeneratorRig;

	public static ThumbnailGenerator Instance;

	private void Awake()
	{
		Instance = this;
	}

	public ThumbnailGeneratorRig CreateRig()
	{
		return Object.Instantiate(_thumbnailGeneratorRig, Vector3.zero, Quaternion.identity);
	}

	public string Generate(Thing thing, float zoom)
	{
		ThumbnailGeneratorRig thumbnailGeneratorRig = Object.Instantiate(_thumbnailGeneratorRig, Vector3.zero, Quaternion.identity);
		if (thing is DynamicThing thing2)
		{
			return thumbnailGeneratorRig.Generate(thing2, zoom);
		}
		Object.Destroy(thumbnailGeneratorRig.gameObject);
		return null;
	}
}
