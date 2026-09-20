using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Motherboard;

public class ScreenState : MonoBehaviour
{
	public Text Title;

	public Image BackgroundImage;

	public Image TriggeredImage;

	public GridLayoutGroup ConditionGrid;

	public GridLayoutGroup ActionGrid;

	public Dropdown NextStateDropdown;

	public Dropdown FalseStateDropdown;

	public Button ButtonDelete;

	public Button ButtonNewAction;

	public Button ButtonNewCondition;

	public Button ButtonRename;

	public Button ButtonPlay;

	private void Awake()
	{
		TriggeredImage.enabled = false;
		NextStateDropdown.ReplaceRaycasters();
		FalseStateDropdown.ReplaceRaycasters();
	}

	public ScreenCondition[] GetConditions()
	{
		return ConditionGrid.GetComponentsInChildren<ScreenCondition>();
	}

	public ScreenAction[] GetActions()
	{
		return ActionGrid.GetComponentsInChildren<ScreenAction>();
	}
}
