using UnityEngine;

namespace CharacterCustomisation;

public class HairBehaviour : MonoBehaviour
{
	public GameObject normalHair;

	public GameObject hatHair;

	public GameObject helmetHair;

	public void Set(HairMode mode)
	{
		if ((bool)normalHair)
		{
			normalHair.SetActive(mode == HairMode.Normal);
		}
		if ((bool)hatHair)
		{
			hatHair.SetActive(mode == HairMode.Hat);
		}
		else if (mode == HairMode.Hat)
		{
			normalHair.SetActive(value: true);
		}
		if ((bool)helmetHair)
		{
			helmetHair.SetActive(mode == HairMode.Helmet);
		}
		else if (mode == HairMode.Helmet)
		{
			normalHair.SetActive(value: true);
		}
	}
}
