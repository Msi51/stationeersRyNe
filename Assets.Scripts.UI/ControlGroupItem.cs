using Assets.Scripts.Util;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI;

public class ControlGroupItem : UserInterfaceBase
{
	public TMP_Text Title;

	public bool Toggled;

	public bool IsChildVisible
	{
		get
		{
			for (int i = 1; i < RectTransform.childCount; i++)
			{
				Transform child = RectTransform.GetChild(i);
				if (child.gameObject.activeSelf || child.gameObject.activeInHierarchy)
				{
					return true;
				}
			}
			return false;
		}
	}

	public void Created(ControlsGroup controllerGroup)
	{
		controllerGroup.Transform = Transform;
		Title.text = controllerGroup.Name.ToProper();
		base.name = "ControlGroup" + controllerGroup.Name;
	}
}
