using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColourWheelPicker : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	public ColourWheel Parent;

	public RawImage ColourWheelImage;

	[HideInInspector]
	public Texture2D ColourWheelTexture2D;

	public RectTransform Rect;

	private void Awake()
	{
		if ((bool)ColourWheelImage)
		{
			ColourWheelTexture2D = ColourWheelImage.texture as Texture2D;
		}
		if ((bool)Rect)
		{
			Rect = GetComponent<RectTransform>();
		}
	}

	void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, eventData.pressPosition, eventData.pressEventCamera, out var localPoint);
		Color pixel = ColourWheelTexture2D.GetPixel((int)localPoint.x, (int)localPoint.y);
		Parent.SetCustomTerrainColour(pixel, updateValues: true);
	}
}
