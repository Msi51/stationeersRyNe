using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UI;
using UnityEngine;

public class MainMenuWindowManager : ManagerBase
{
	private readonly Stack<MainMenuPage> _pageStack = new Stack<MainMenuPage>();

	public string StartPage = "MainMenu";

	[SerializeField]
	[ReadOnly]
	private List<MainMenuPage> windows;

	[SerializeField]
	[ReadOnly]
	private List<string> windowNames;

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		GenerateWindowsArray();
		InitilizeAllWindows();
		if (!string.IsNullOrEmpty(StartPage))
		{
			EnableMainMenuPage(StartPage);
		}
	}

	private void InitilizeAllWindows()
	{
		foreach (MainMenuPage window in windows)
		{
			window.OnManagerStart();
		}
	}

	public void GenerateWindowsArray()
	{
		windows.Clear();
		windowNames.Clear();
		MainMenuPage[] componentsInChildren = GetComponentsInChildren<MainMenuPage>(includeInactive: true);
		foreach (MainMenuPage page in componentsInChildren)
		{
			RegisterPage(page);
		}
	}

	public void RegisterPage(MainMenuPage page)
	{
		if (!windows.Contains(page))
		{
			windows.Add(page);
			windowNames.Add(page.WindowName);
		}
	}

	public void DisableAllPages()
	{
		while (_pageStack.Count > 0)
		{
			MainMenuPage mainMenuPage = _pageStack.Pop();
			mainMenuPage.OnPagePopped();
			mainMenuPage.Hide();
		}
		SetWorldInfoPage(enable: false);
	}

	public void EnableMainMenuPage(string windowName)
	{
		if (_pageStack.Count > 0)
		{
			MainMenuPage mainMenuPage = _pageStack.Peek();
			if (windowName == mainMenuPage.WindowName)
			{
				return;
			}
		}
		MainMenuPage mainMenuPage2 = windows.SingleOrDefault((MainMenuPage x) => x.WindowName == windowName);
		if (!mainMenuPage2)
		{
			Debug.LogError("The menu (" + windowName + ") that was request from the main menu manager was not found", this);
			return;
		}
		if (_pageStack.Any())
		{
			_pageStack.Peek().Hide();
		}
		mainMenuPage2.Show();
		_pageStack.Push(mainMenuPage2);
		mainMenuPage2.OnPagePushed();
		MainMenu.Instance.CurrentPageChanged(mainMenuPage2);
		SetWorldInfoPage(windowName);
	}

	public void PageBackward()
	{
		if (_pageStack.Count > 1)
		{
			MainMenuPage mainMenuPage = _pageStack.Pop();
			mainMenuPage.OnPagePopped();
			mainMenuPage.Hide();
			MainMenuPage mainMenuPage2 = _pageStack.Peek();
			mainMenuPage2.Show();
			MainMenu.Instance.CurrentPageChanged(mainMenuPage2);
			SetWorldInfoPage(mainMenuPage2.WindowName);
		}
	}

	public void DisableMainMenuPage(string windowName)
	{
		string windowName2 = _pageStack.Peek().WindowName;
		if (!(windowName != windowName2))
		{
			SetWorldInfoPage(enable: false);
			PageBackward();
		}
	}

	private static void SetWorldInfoPage(string windowName)
	{
		int worldInfoPage;
		switch (windowName)
		{
		default:
			worldInfoPage = ((windowName == "TutorialScenarios") ? 1 : 0);
			break;
		case "NewGame":
		case "WorldConfiguration":
		case "StartingConditions":
			worldInfoPage = 1;
			break;
		}
		SetWorldInfoPage((byte)worldInfoPage != 0);
		MainMenu.Instance.WorldInfo.SummaryInfo.SetVisible(windowName == "NewGame" || windowName == "WorldConfiguration" || windowName == "StartingConditions");
		MainMenu.Instance.WorldInfo.DifficultyInfo.SetVisible(windowName == "WorldConfiguration" || windowName == "StartingConditions");
		MainMenu.Instance.StartConditionInfo.SetVisible(windowName == "StartingConditions");
	}

	private static void SetWorldInfoPage(bool enable)
	{
		MainMenu.Instance.WorldInfo.SetVisible(enable);
	}
}
