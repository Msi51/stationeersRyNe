using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class Toolbar : MonoBehaviour
{
	public GameObject ButtonPrefab;

	public GridLayoutGroup DynamicGrid;

	public Scrollbar ScrollBar;

	protected Button CurrentButton;

	protected List<Button> Buttons = new List<Button>();

	public void PopulateList(List<Device> buttonNames)
	{
		foreach (Button button in Buttons)
		{
			Object.Destroy(button.gameObject);
		}
		Buttons.Clear();
		foreach (Device buttonName in buttonNames)
		{
			MakeButton(buttonName);
		}
		if (base.gameObject.activeInHierarchy)
		{
			StartCoroutine(UpdateList());
		}
	}

	private void MakeButton(Device buttonName)
	{
		GameObject obj = Object.Instantiate(ButtonPrefab);
		obj.transform.SetParent(DynamicGrid.transform, worldPositionStays: false);
		obj.GetComponentInChildren<Text>().text = buttonName.DisplayName;
		Button component = obj.GetComponent<Button>();
		Buttons.Add(component);
		if (buttonName.IsDoor)
		{
			component.interactable = false;
		}
	}

	public virtual IEnumerator UpdateList()
	{
		yield return 0;
		ScrollBar.numberOfSteps = Buttons.Count;
		ScrollBar.size = Buttons.Count;
		ScrollBar.value = 0f;
	}
}
