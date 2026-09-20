using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI;

public class SceneLoadingPanel : MonoBehaviour
{
	public TextMeshProUGUI TextFeild;

	private int _animationCount;

	public void LoadEvent()
	{
		switch (_animationCount)
		{
		case 0:
			TextFeild.text = "Initializing.";
			_animationCount = 1;
			break;
		case 1:
			TextFeild.text = "Initializing..";
			_animationCount = 2;
			break;
		case 2:
			TextFeild.text = "Initializing...";
			_animationCount = 3;
			break;
		case 3:
			TextFeild.text = "Initializing";
			_animationCount = 0;
			break;
		default:
			TextFeild.text = "Initializing";
			_animationCount = 0;
			break;
		}
	}
}
