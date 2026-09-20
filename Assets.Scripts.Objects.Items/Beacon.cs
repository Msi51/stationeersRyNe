using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using Objects.Electrical;

namespace Assets.Scripts.Objects.Items;

public class Beacon : PowerTool, ISpatial, IPhysical, IProfile, IDensePoolable, ITrackable
{
	public static List<Beacon> AllBeacons = new List<Beacon>();

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		StartCoroutine(WaitThenSetColor());
	}

	private IEnumerator WaitThenSetColor()
	{
		while (GameManager.GameState != GameState.Running)
		{
			yield return Yielders.EndOfFrame;
		}
		yield return Yielders.EndOfFrame;
		SetCustomColor(OnOff && Powered);
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		foreach (ThingLight light in Lights)
		{
			light.Light.color = CustomColor.Light;
		}
		SetCustomColor(OnOff && Powered);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		SetCustomColor(OnOff && Powered);
	}

	public override void Awake()
	{
		base.Awake();
		ITrackable.Trackables.Add(this);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		ITrackable.Trackables.Remove(this);
	}
}
