using CharacterCustomisation;
using UnityEngine;

namespace ThingImport.Thumbnails;

public sealed class CharacterConfig
{
	public int KitIndex;

	public int Head;

	public int Eyes;

	public int Hair;

	public int HairColour;

	public int FacialHair;

	public int FacialHairColour;

	public int EyeColour;

	public int Skin;

	public string BodyClothing;

	public string ArmorClothing;

	public int BodyColor = -1;

	public int ArmorColor = -1;

	public BlendShapeType Expression;

	public HairMode HairMode = HairMode.Normal;

	public Vector3 Euler;

	public float Zoom = 1f;

	public CharacterConfig Clone()
	{
		return (CharacterConfig)MemberwiseClone();
	}
}
