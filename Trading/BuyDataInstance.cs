using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;

namespace Trading;

public class BuyDataInstance : TransactionDataInstance
{
	public readonly BuyData BuyData;

	public int Required;

	public readonly BuyItem BuyingItem;

	public override string DisplayName
	{
		get
		{
			if (!string.IsNullOrEmpty(BuyData.Name))
			{
				return BuyData.Name;
			}
			if (BuyingItem?.Prefab != null && !string.IsNullOrEmpty(BuyingItem.Prefab.DisplayName))
			{
				return BuyingItem.Prefab.DisplayName;
			}
			return string.Empty;
		}
	}

	public override bool IsGasTransaction()
	{
		foreach (ConditionData condition in BuyData.Conditions)
		{
			if (condition is GasCondition)
			{
				return true;
			}
		}
		return false;
	}

	public BuyDataInstance(BuyData buyData, Random random)
	{
		BuyData = buyData;
		Required = buyData.Required.GenerateValue(random);
		BuyingItem = buyData.BuyingItem;
		if (buyData.SelectData?.SelectItem(random) is BuyItem buyingItem)
		{
			BuyingItem = buyingItem;
		}
		SetThumbnail(buyData).Forget();
	}

	public override Thing GetItemPrefab()
	{
		return BuyingItem?.Prefab;
	}

	public bool BuyConditionsMet<T>(T t) where T : IEvaluable
	{
		foreach (ConditionDataCollection conditionCollection in BuyData.ConditionCollections)
		{
			if (!conditionCollection.Evaluate(t))
			{
				return false;
			}
		}
		foreach (ConditionData condition in BuyData.Conditions)
		{
			if (!condition.Evaluate(t))
			{
				return false;
			}
		}
		if (BuyingItem != null && t is Thing thing)
		{
			if (thing.GetPrefabHash() == BuyingItem.Prefab.PrefabHash && BuyingItem.BuyConditionsMet(t))
			{
				return true;
			}
			return false;
		}
		return true;
	}

	public string ToolTip()
	{
		if (HasCustomToolTip(BuyData.CustomToolTipGameStringKey, out var toolTip))
		{
			return toolTip;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ConditionData condition in BuyData.Conditions)
		{
			condition.ToolTip(stringBuilder, 0);
		}
		foreach (ConditionDataCollection conditionCollection in BuyData.ConditionCollections)
		{
			conditionCollection.ToolTip(stringBuilder, 0);
		}
		BuyingItem?.ToolTip(stringBuilder);
		string text = stringBuilder.ToString();
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return GameStrings.TradingToolTipNoRequirements.DisplayString;
	}

	public override void Write(RocketBinaryWriter writer)
	{
		base.Write(writer);
		writer.WriteInt32(Required);
	}

	public override void Read(RocketBinaryReader reader)
	{
		base.Read(reader);
		Required = reader.ReadInt32();
	}

	public void ToTreeString(TreeString contactString)
	{
		TreeString myString = TreeString.Node($"{DisplayName} €{BuyData.Value} x {Required}", contactString);
		foreach (ConditionDataCollection conditionCollection in BuyData.ConditionCollections)
		{
			conditionCollection.ToTreeString(myString);
		}
		foreach (ConditionData condition in BuyData.Conditions)
		{
			condition.ToTreeString(myString);
		}
	}

	private bool Evaluate(List<DynamicThing> inventory)
	{
		foreach (DynamicThing item in inventory)
		{
			if (item is ITradable t && BuyConditionsMet(t))
			{
				return true;
			}
		}
		return false;
	}

	public void EvaluateToTree(TreeString contactString, List<DynamicThing> inventory)
	{
		bool flag = Evaluate(inventory);
		TreeString treeString = TreeString.Node($"{DisplayName} €{BuyData.Value} x {Required} is {flag}", contactString);
		foreach (ConditionDataCollection conditionCollection in BuyData.ConditionCollections)
		{
			conditionCollection.EvaluateToTree(treeString, inventory);
		}
		foreach (ConditionData condition in BuyData.Conditions)
		{
			condition.EvaluateToTree(treeString, inventory);
		}
		BuyingItem?.EvaluateToTree(treeString, inventory);
	}
}
