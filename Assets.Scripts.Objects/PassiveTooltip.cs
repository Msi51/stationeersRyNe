using UnityEngine;

namespace Assets.Scripts.Objects;

public struct PassiveTooltip
{
	public string Title;

	public string Action;

	public string State;

	public string Extended;

	public string RepairString;

	public string DeconstructString;

	public string ConstructString;

	public string PlacementString;

	public string BuildStateIndexMessage;

	public bool ShowRotate;

	public bool ShowConstructionRotate;

	public bool ShowScroll;

	public bool ShowAction;

	public float Slider;

	public Color color;

	public bool FollowMouseMovement;

	public string GetExtendedText()
	{
		return Extended;
	}

	public void SetExtendedText(string text)
	{
		Extended = text;
	}

	public PassiveTooltip(bool toDefault = true)
	{
		Title = string.Empty;
		Action = string.Empty;
		State = string.Empty;
		Extended = string.Empty;
		RepairString = string.Empty;
		DeconstructString = string.Empty;
		ConstructString = string.Empty;
		PlacementString = string.Empty;
		ShowRotate = false;
		ShowScroll = false;
		ShowConstructionRotate = false;
		ShowAction = true;
		BuildStateIndexMessage = string.Empty;
		color = Color.white;
		Slider = -1f;
		FollowMouseMovement = false;
	}

	public PassiveTooltip(Thing.DelayedActionInstance actionInstance, string actionOverride, Thing cursorThing)
	{
		string title = ((actionInstance.OverrideTitle != string.Empty) ? actionInstance.OverrideTitle : cursorThing.DisplayName);
		string action = (string.IsNullOrEmpty(actionOverride) ? actionInstance.ActionMessage : actionOverride);
		Title = title;
		Action = action;
		State = actionInstance.GetStateMessage();
		Extended = actionInstance.GetExtendedText();
		RepairString = string.Empty;
		DeconstructString = string.Empty;
		ConstructString = string.Empty;
		PlacementString = string.Empty;
		ShowRotate = false;
		ShowScroll = false;
		ShowConstructionRotate = false;
		ShowAction = true;
		FollowMouseMovement = false;
		color = actionInstance.color;
		Slider = actionInstance.Slider;
		BuildStateIndexMessage = string.Empty;
	}

	public PassiveTooltip(string title, string action, string state, string extended, string repair, string deconstruct, string constructString, string placementString, float slider, bool showRotate, bool showScroll, bool showAction, Color colorIn, bool showConstructionRotate)
	{
		Title = title;
		Action = action;
		State = state;
		Extended = extended;
		Slider = slider;
		RepairString = repair;
		DeconstructString = deconstruct;
		ConstructString = constructString;
		PlacementString = placementString;
		ShowRotate = showRotate;
		ShowScroll = showScroll;
		ShowAction = showAction;
		ShowConstructionRotate = showConstructionRotate;
		color = colorIn;
		FollowMouseMovement = false;
		BuildStateIndexMessage = string.Empty;
	}

	public PassiveTooltip Populate(Connection end)
	{
		return end.Populate(this);
	}
}
