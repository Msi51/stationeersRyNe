using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class GasItem : MonoBehaviour
{
	[Flags]
	public enum StateSymbolType
	{
		none = 0,
		evaporation = 1,
		condensation = 2,
		freezing = 4
	}

	public GameObject Parent;

	public Transform ParentTransform;

	public Image GasSymbol;

	public TextMeshProUGUI GasPercentage;

	public TextMeshProUGUI GasMoles;

	public TextMeshProUGUI GasVolume;

	public Image evaporationSymbol;

	public Image condensationSymbol;

	public Image freezingSymbol;

	public string Percent;

	public string Moles;

	public string Volume;

	public float MolesValue;

	public float VolumeValue;

	public bool IsActive;

	private void Awake()
	{
		ParentTransform = base.transform;
		Parent = base.gameObject;
	}

	public void SetSymbol(StateSymbolType state)
	{
		if (!(base.gameObject == null))
		{
			if (evaporationSymbol != null)
			{
				evaporationSymbol.enabled = (state & StateSymbolType.evaporation) != 0;
			}
			if (condensationSymbol != null)
			{
				condensationSymbol.enabled = (state & StateSymbolType.condensation) != 0;
			}
			if (freezingSymbol != null)
			{
				freezingSymbol.enabled = (state & StateSymbolType.freezing) != 0;
			}
		}
	}
}
