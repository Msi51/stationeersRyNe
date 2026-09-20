using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Motherboard;

public class ScreenColorItem : MonoBehaviour
{
	public GameObject Parent;

	public Image Image;

	[ReadOnly]
	public int Bit;
}
