using UnityEngine;

namespace CharacterCustomisation;

[CreateAssetMenu(fileName = "New Character Kit", menuName = "Stationeers/Character Customisation/New Kit", order = 0)]
public class CharacterKit : UniqueItem
{
	public KitItem[] Bodies;

	public KitItem[] Heads;

	public KitItem[] Eyes;

	public KitItem[] EyeColours;

	public KitItem[] SkinColours;

	public KitItem[] Hairs;

	public KitItem[] HairColours;

	public KitItem[] FacialHairs;
}
