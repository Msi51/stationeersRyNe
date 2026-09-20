using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Motherboard;

public class ScreenDropdownBase : MonoBehaviour
{
	public static List<Dropdown.OptionData> OptionData = new List<Dropdown.OptionData>();

	public Image BackgroundImage;

	public Dropdown Device;

	public Dropdown Type;

	public Dropdown Value;

	public Button ButtonDelete;

	public static string[] LogicTypeNames = Enum.GetNames(typeof(LogicType));

	public static LogicType[] LogicTypes = new LogicType[LogicTypeNames.Length];

	private static bool _isInitialized;

	public static string EnterNewValue = "Enter New Value";

	protected virtual void Awake()
	{
		if (!_isInitialized)
		{
			Array values = Enum.GetValues(typeof(LogicType));
			for (int i = 0; i < values.Length; i++)
			{
				LogicTypes[i] = (LogicType)values.GetValue(i);
			}
		}
		Device.ReplaceRaycasters();
		Type.ReplaceRaycasters();
		Value.ReplaceRaycasters();
	}

	public virtual void RefreshAll()
	{
	}

	public virtual void OnTypeChanged()
	{
	}

	public virtual void OnDeviceChanged()
	{
	}
}
