using UnityEngine;

namespace CharacterCustomisation.Clothing;

[CreateAssetMenu(fileName = "Clothing", menuName = "Stationeers/Character Customisation/New Clothing", order = 0)]
public class ClothingItem : KitItem
{
	[Tooltip("Skin Material will be provided by the face renderer, we just need to stipulate which index it should be used. Use -1 for not applicable.")]
	public int SkinMaterialIndex = -1;
}
