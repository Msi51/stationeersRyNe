using System.Text;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Reagents;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class PrefabReference : UserInterfaceAnimated, IScreenSpaceTooltip
{
	public Image Thumbnail;

	public DynamicThing Prefab;

	public TextMeshProUGUI Text;

	public int PrefabHash => Prefab.PrefabHash;

	public Recipe Recipe { get; private set; }

	public bool TooltipIsVisible => IsVisible;

	public void Select()
	{
		InputPrefabs.CurrentPrefab = this;
		InputPrefabs.Submit();
	}

	public void SetPrefab(DynamicThing prefab, Recipe recipe)
	{
		Prefab = prefab;
		Thumbnail.sprite = prefab.Thumbnail;
		Recipe = recipe;
		RefreshString();
	}

	public void RefreshString()
	{
		if ((bool)Prefab && Prefab.SlotType != Slot.Class.None)
		{
			Text.text = Prefab.ToTooltip() + "\n" + Localization.GetSlotTooltip(Prefab.SlotType);
		}
		else
		{
			Text.text = Prefab.ToTooltip();
		}
	}

	private string GetTooltipDescription()
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (Prefab.SlotType != Slot.Class.None)
		{
			stringBuilder.AppendLine(Localization.GetSlotTooltip(Prefab.SlotType));
		}
		Recipe recipe = InputPrefabs.Fabricator?.GetRecipeSafe(Prefab) ?? Recipe.INVALID;
		if (!recipe.IsNaN())
		{
			stringBuilder.Append(recipe.GetComparisonResult(InputPrefabs.Fabricator?.ReagentMixture));
		}
		if (Prefab is INutrition iNutrition)
		{
			Food.AppendFoodQualityText(stringBuilder, iNutrition);
		}
		return stringBuilder.ToString();
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
		PanelToolTip.Instance.SetUpTooltip(Prefab.ToTooltip(), GetTooltipDescription(), this);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);
		PanelToolTip.Instance.ClearToolTip();
	}

	public void DoUpdate()
	{
		PanelToolTip.Instance.SetInfoText(GetTooltipDescription());
	}
}
