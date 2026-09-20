using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Trading;
using UnityEngine;

namespace TraderUI;

public class TradeDataHelper
{
	public readonly TraderContact Contact;

	public TradeDataHelper(TraderContact contact)
	{
		Contact = contact;
	}

	public TradeData Convert()
	{
		TraderDataInstance traderDataInstance = Contact?.DataInstance;
		if (traderDataInstance == null)
		{
			return null;
		}
		GetPlayerInfo(out var credits, out var playerName);
		return new TradeData
		{
			PlayerCredits = credits,
			PlayerName = playerName,
			TraderName = traderDataInstance.DisplayName,
			Buying = GetBuyData(traderDataInstance),
			Selling = GetSellData(traderDataInstance)
		};
	}

	public bool Depart()
	{
		if (GameManager.RunSimulation)
		{
			Contact.ConnectedPad.ServerCallTrader(isLanding: false, Contact);
		}
		else
		{
			NetworkClient.CallTrader(isLanding: false, Contact, Contact.ConnectedPad);
		}
		return true;
	}

	public bool AwaitingAtmosEvent()
	{
		return AwaitingAtmosEvent(Contact);
	}

	public static bool AwaitingAtmosEvent(TraderContact contact)
	{
		return (contact.ConnectedPad.LandingPadNetwork?.Atmosphere)?.IsAwaitingEvent ?? false;
	}

	public bool BuyItem(SellDataInstance traderIsSelling, int amount, float cost, out Assets.Scripts.Localization2.GameString errorMessage)
	{
		CreditCard creditCard = InventoryManager.ParentHuman?.GetCreditCard();
		return BuyItem(traderIsSelling, creditCard, Contact, amount, cost, out errorMessage);
	}

	public static bool BuyItem(SellDataInstance traderIsSelling, CreditCard creditCard, TraderContact contact, int amount, float cost, out Assets.Scripts.Localization2.GameString errorMessage)
	{
		errorMessage = null;
		if (!GameManager.RunSimulation)
		{
			ConsoleWindow.PrintError(GameStrings.ItemTransactionClientError.DisplayString);
			return false;
		}
		if (amount > traderIsSelling.Stock)
		{
			errorMessage = GameStrings.TransactionFailInsufficientStock;
			return false;
		}
		if ((object)creditCard == null)
		{
			errorMessage = GameStrings.TransactionFailCreditCard;
			return false;
		}
		if (cost > creditCard.Currency)
		{
			errorMessage = GameStrings.TransactionFailInsufficientFunds;
			return false;
		}
		ITradableInventory tradingParent = creditCard.RootParent as ITradableInventory;
		if (traderIsSelling.SellingItem == null)
		{
			if (!HandleBuyGasMix(traderIsSelling, contact, amount))
			{
				return false;
			}
			creditCard.Currency -= cost;
			traderIsSelling.Stock -= amount;
		}
		else if (!HandleBuyItem(creditCard, cost, tradingParent, traderIsSelling, contact, amount, ref errorMessage))
		{
			return false;
		}
		if (NetworkManager.IsServer)
		{
			contact.NetworkUpdateFlags |= 4;
		}
		return true;
	}

	public bool SellItem(BuyDataInstance traderIsBuying, int amount, float cost, out Assets.Scripts.Localization2.GameString errorMessage)
	{
		CreditCard creditCard = InventoryManager.ParentHuman?.GetCreditCard();
		return SellItem(traderIsBuying, creditCard, Contact, amount, cost, out errorMessage);
	}

	public static bool SellItem(BuyDataInstance traderIsBuying, CreditCard creditCard, TraderContact contact, int amount, float cost, out Assets.Scripts.Localization2.GameString errorMessage)
	{
		errorMessage = null;
		if (!GameManager.RunSimulation)
		{
			ConsoleWindow.PrintError(GameStrings.ItemTransactionClientError.DisplayString);
			return false;
		}
		if (amount > traderIsBuying.Required)
		{
			errorMessage = GameStrings.TransactionFailRequired;
			return false;
		}
		if (creditCard == null)
		{
			errorMessage = GameStrings.TransactionFailCreditCard;
			return false;
		}
		ITradableInventory tradeParent = creditCard.RootParent as ITradableInventory;
		if (traderIsBuying.BuyingItem == null)
		{
			if (!HandleSellGasMix(traderIsBuying, contact, amount, ref errorMessage))
			{
				return false;
			}
		}
		else if (!HandleSellItem(tradeParent, traderIsBuying, contact, amount, ref errorMessage))
		{
			return false;
		}
		creditCard.Currency += cost;
		traderIsBuying.Required -= amount;
		if (NetworkManager.IsServer)
		{
			contact.NetworkUpdateFlags |= 4;
		}
		return true;
	}

	private static bool HandleBuyGasMix(SellDataInstance traderIsSelling, TraderContact contact, int amount)
	{
		Atmosphere atmosphere = contact.ConnectedPad?.LandingPadNetwork?.Atmosphere;
		if (atmosphere == null)
		{
			ConsoleWindow.PrintError(GameStrings.TransactionAtmosError.DisplayString);
			return false;
		}
		GasMixture gasMixture = GasMixtureHelper.Create();
		traderIsSelling.ExecuteActions(ref gasMixture, amount);
		AtmosphericEventInstance.CreateAdd(atmosphere, gasMixture);
		return true;
	}

	private static Slot GetNextSlot(List<ITradableInventory> tradingInventories)
	{
		foreach (ITradableInventory tradingInventory in tradingInventories)
		{
			foreach (Slot slot in tradingInventory.GetSlots())
			{
				if (tradingInventory.IsSlotTradable(slot) && slot.IsEmpty())
				{
					return slot;
				}
			}
		}
		return null;
	}

	private static bool HandleBuyItem(CreditCard creditCard, float cost, ITradableInventory tradingParent, SellDataInstance traderIsSelling, TraderContact contact, int tradeInstanceQuantity, ref Assets.Scripts.Localization2.GameString errorMessage)
	{
		if ((object)traderIsSelling.SellingItem.Prefab == null)
		{
			errorMessage = GameStrings.TransactionFailItem;
			return false;
		}
		List<ITradableInventory> vendorsOnNetwork = contact.ConnectedPad.LandingPadNetwork.GetVendorsOnNetwork();
		if (tradingParent != null)
		{
			vendorsOnNetwork.Add(tradingParent);
		}
		if (vendorsOnNetwork.Count <= 0)
		{
			errorMessage = GameStrings.TransactionFailInventory;
			return false;
		}
		Slot nextSlot = GetNextSlot(vendorsOnNetwork);
		if (nextSlot == null)
		{
			errorMessage = GameStrings.TransactionFailInventory;
			return false;
		}
		int num = 0;
		int num2 = 0;
		IQuantity quantity = default(IQuantity);
		for (int i = 0; i < tradeInstanceQuantity; i++)
		{
			nextSlot = GetNextSlot(vendorsOnNetwork);
			if (nextSlot == null)
			{
				break;
			}
			DynamicThing dynamicThing = Make(traderIsSelling, nextSlot);
			bool flag;
			if (dynamicThing is Stackable || dynamicThing is Ingot)
			{
				quantity = (IQuantity)dynamicThing;
				flag = true;
			}
			else
			{
				flag = false;
			}
			if (flag)
			{
				int num3 = Mathf.FloorToInt(quantity.GetQuantity);
				if (num > 0)
				{
					quantity.SetQuantity((float)num + quantity.GetQuantity);
					num = 0;
				}
				int num4 = Mathf.FloorToInt(quantity.GetQuantity);
				while ((float)num4 < quantity.GetMaxQuantity && i < tradeInstanceQuantity - 1)
				{
					num4 += num3;
					i++;
				}
				if ((float)num4 > quantity.GetMaxQuantity)
				{
					if (GetNextSlot(vendorsOnNetwork) == null)
					{
						num4 -= num3;
						i--;
					}
					else
					{
						num = num4 - Mathf.FloorToInt(quantity.GetMaxQuantity);
						num4 = Mathf.FloorToInt(quantity.GetMaxQuantity);
					}
				}
				quantity.SetQuantity(num4);
			}
			num2 = i + 1;
		}
		if (num > 0 && Make(traderIsSelling, nextSlot) is IQuantity quantity2)
		{
			quantity2.SetQuantity(num);
		}
		float num5 = (float)num2 / (float)tradeInstanceQuantity;
		creditCard.Currency -= cost * num5;
		traderIsSelling.Stock -= num2;
		if (num2 < tradeInstanceQuantity)
		{
			if (NetworkManager.IsServer)
			{
				contact.NetworkUpdateFlags |= 4;
			}
			errorMessage = GameStrings.TransactionIncompleteTransaction;
			return false;
		}
		return true;
	}

	public static DynamicThing Make(SellDataInstance sellData, Slot slot)
	{
		DynamicThing dynamicThing = Thing.Create<DynamicThing>(sellData.SellingItem.Prefab, slot.Location.position, slot.Location.rotation, 0L);
		sellData.SellingItem.AssignToPrefab(dynamicThing);
		sellData.ExecuteActions((ITradable)dynamicThing);
		OnServer.MoveToSlot(dynamicThing, slot);
		return dynamicThing;
	}

	private static bool HandleSellGasMix(BuyDataInstance traderIsBuying, TraderContact contact, int amount, ref Assets.Scripts.Localization2.GameString errorMessage)
	{
		Atmosphere atmosphere = contact.ConnectedPad?.LandingPadNetwork?.Atmosphere;
		if (atmosphere == null)
		{
			ConsoleWindow.PrintError(GameStrings.TransactionAtmosError.DisplayString);
			return false;
		}
		int sellItemQuantity = GetSellItemQuantity(traderIsBuying, contact);
		if (amount > sellItemQuantity)
		{
			errorMessage = GameStrings.TransactionFailInsufficientAvailable;
			return false;
		}
		float quantityConditionValue = GetQuantityConditionValue(traderIsBuying);
		MoleQuantity moleQuantity = new MoleQuantity((float)amount * quantityConditionValue);
		GasMixture gasMixture = new GasMixture(atmosphere.GasMixture);
		gasMixture.Scale((moleQuantity / atmosphere.TotalMoles).ToFloat());
		AtmosphericEventInstance.CreateRemove(atmosphere, gasMixture);
		return true;
	}

	private static bool HandleSellItem(ITradableInventory tradeParent, BuyDataInstance traderIsBuying, TraderContact contact, int amount, ref Assets.Scripts.Localization2.GameString errorMessage)
	{
		List<(ITradable, int)> list = new List<(ITradable, int)>();
		List<ITradable> list2 = new List<ITradable>();
		int num = amount;
		List<DynamicThing> networkInventory = contact.ConnectedPad.LandingPadNetwork.GetNetworkInventory();
		networkInventory.AddRange(tradeParent.GetContents());
		foreach (DynamicThing item in networkInventory)
		{
			if (num == 0)
			{
				break;
			}
			if (item is ITradable tradable && traderIsBuying.BuyConditionsMet(tradable))
			{
				int num2 = Mathf.FloorToInt(tradable.GetTradableQuantity);
				if (num >= num2)
				{
					list2.Add(tradable);
					num -= num2;
				}
				else
				{
					list.Add((tradable, num));
					num = 0;
				}
			}
		}
		if (num > 0)
		{
			errorMessage = GameStrings.TransactionFailInsufficientAvailable;
			return false;
		}
		foreach (ITradable item2 in list2)
		{
			if (!(item2 is Thing thing))
			{
				ConsoleWindow.PrintError(GameStrings.TransactionErrorTradableCast.DisplayString);
			}
			else
			{
				OnServer.Destroy(thing);
			}
		}
		foreach (var item3 in list)
		{
			item3.Item1.SetQuantity(item3.Item1.GetQuantity - (float)item3.Item2);
		}
		return true;
	}

	private static float GetQuantityConditionValue(BuyDataInstance buyDataInstance)
	{
		foreach (ConditionData condition in buyDataInstance.BuyData.Conditions)
		{
			if (condition is MoleCondition { Value: var value })
			{
				return value;
			}
			if (condition is QuantityCondition { Quantity: var quantity })
			{
				return quantity;
			}
		}
		return 1f;
	}

	private static float GetQuantityActionValue(SellDataInstance sellDataInstance)
	{
		foreach (ActionData action in sellDataInstance.SellingItem.Actions)
		{
			if (action is QuantityAction quantityAction)
			{
				return quantityAction.Value;
			}
		}
		return 1f;
	}

	private void GetPlayerInfo(out float credits, out string playerName)
	{
		credits = 0f;
		playerName = "Player";
		Human parentHuman = InventoryManager.ParentHuman;
		if ((bool)parentHuman)
		{
			playerName = parentHuman.DisplayName;
			CreditCard creditCard = parentHuman.GetCreditCard();
			if ((bool)creditCard)
			{
				credits = creditCard.Currency;
			}
		}
	}

	private List<TradeItemData> GetBuyData(TraderDataInstance dataInstance)
	{
		List<TradeItemData> list = new List<TradeItemData>();
		foreach (SellDataInstance sellDataInstance in dataInstance.SellDataInstances)
		{
			TradeItemData item = new TradeItemData
			{
				ItemImage = GetBuyItemImage(sellDataInstance),
				ItemName = GetBuyItemName(sellDataInstance),
				Cost = sellDataInstance.SellData.GetCost(),
				NumberAvailable = sellDataInstance.Stock,
				NumberWanted = 9999,
				TooltipText = sellDataInstance.ToolTip(),
				DataInstance = sellDataInstance
			};
			list.Add(item);
		}
		return list;
	}

	private List<TradeItemData> GetSellData(TraderDataInstance dataInstance)
	{
		List<TradeItemData> list = new List<TradeItemData>();
		foreach (BuyDataInstance buyDataInstance in dataInstance.BuyDataInstances)
		{
			TradeItemData item = new TradeItemData
			{
				ItemImage = GetSellItemImage(buyDataInstance),
				ItemName = GetSellItemName(buyDataInstance),
				Cost = buyDataInstance.BuyData.GetCost(),
				NumberAvailable = GetSellItemQuantity(buyDataInstance, Contact),
				NumberWanted = buyDataInstance.Required,
				TooltipText = buyDataInstance.ToolTip(),
				DataInstance = buyDataInstance
			};
			list.Add(item);
		}
		return list;
	}

	private Sprite GetBuyItemImage(SellDataInstance traderIsSelling)
	{
		Texture2D thumbnail = traderIsSelling.Thumbnail;
		if (thumbnail != null)
		{
			return Sprite.Create(thumbnail, new Rect(0f, 0f, thumbnail.width, thumbnail.height), Vector2.one * 0.5f);
		}
		Thing thing = traderIsSelling.SellingItem?.Prefab;
		if ((object)thing == null)
		{
			return null;
		}
		if (traderIsSelling.SellingItem.colorSwatch != null)
		{
			return Thing.GetThumbnail(thing, traderIsSelling.SellingItem.colorSwatch.GetIndex());
		}
		return thing.GetThumbnail();
	}

	private string GetBuyItemName(SellDataInstance traderIsSelling)
	{
		if (!string.IsNullOrEmpty(traderIsSelling.SellData.Name))
		{
			return traderIsSelling.SellData.Name;
		}
		if (traderIsSelling?.SellData?.SellingItem != null && !string.IsNullOrEmpty(traderIsSelling.SellData.SellingItem.Name))
		{
			return traderIsSelling.SellData.SellingItem.Name;
		}
		if (traderIsSelling.SellingItem == null)
		{
			return "???";
		}
		return traderIsSelling.SellingItem.Prefab.DisplayName;
	}

	private Sprite GetSellItemImage(BuyDataInstance traderIsBuying)
	{
		Texture2D thumbnail = traderIsBuying.Thumbnail;
		if (thumbnail != null)
		{
			return Sprite.Create(thumbnail, new Rect(0f, 0f, thumbnail.width, thumbnail.height), Vector2.one * 0.5f);
		}
		Thing thing = traderIsBuying.BuyingItem?.Prefab;
		if ((object)thing == null)
		{
			return null;
		}
		if (traderIsBuying.BuyingItem.colorSwatch != null)
		{
			return Thing.GetThumbnail(thing, traderIsBuying.BuyingItem.colorSwatch.GetIndex());
		}
		return thing.GetThumbnail();
	}

	private string GetSellItemName(BuyDataInstance traderIsBuying)
	{
		if (!string.IsNullOrEmpty(traderIsBuying.BuyData.Name))
		{
			return traderIsBuying.BuyData.Name;
		}
		if (traderIsBuying?.BuyData?.BuyingItem != null && !string.IsNullOrEmpty(traderIsBuying.BuyData.BuyingItem.Name))
		{
			return traderIsBuying.BuyData.BuyingItem.Name;
		}
		if (traderIsBuying.BuyingItem == null)
		{
			return "???";
		}
		return traderIsBuying.BuyingItem.Prefab.DisplayName;
	}

	private static int GetSellItemQuantity(BuyDataInstance traderIsBuying, TraderContact contact)
	{
		if (traderIsBuying.BuyingItem == null)
		{
			if (contact?.ConnectedPad?.LandingPadNetwork?.Atmosphere == null)
			{
				return 0;
			}
			GasMixture gasMixture = contact.ConnectedPad.LandingPadNetwork.Atmosphere.GasMixture;
			if (traderIsBuying.BuyConditionsMet(gasMixture))
			{
				float quantityConditionValue = GetQuantityConditionValue(traderIsBuying);
				return Mathf.FloorToInt(Mathf.FloorToInt(gasMixture.GetTotalMoles().ToFloat() / quantityConditionValue));
			}
			return 0;
		}
		int num = 0;
		List<DynamicThing> networkInventory = contact.ConnectedPad.LandingPadNetwork.GetNetworkInventory();
		ITradableInventory parentHuman = InventoryManager.ParentHuman;
		if (parentHuman != null)
		{
			networkInventory.AddRange(parentHuman.GetContents());
		}
		for (int num2 = networkInventory.Count - 1; num2 >= 0; num2--)
		{
			DynamicThing dynamicThing = networkInventory[num2];
			if (!(dynamicThing is ITradable tradable) || !dynamicThing.enabled || dynamicThing.IsBeingDestroyed)
			{
				networkInventory.RemoveAt(num2);
			}
			else if (traderIsBuying.BuyConditionsMet(tradable))
			{
				num += Mathf.FloorToInt(tradable.GetTradableQuantity);
			}
		}
		return num;
	}
}
