using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using CharacterCustomisation;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using Trading;
using UnityEngine;
using WorldLogSystem;

namespace Assets.Scripts;

public class TraderContact : IReferencable, IEvaluable
{
	public delegate void Event();

	public ContactSlot ContactSlot;

	public static System.Random Randomize;

	public SpeciesClass RequiredPadEnvironment;

	public float BulkMultiplier = 1f;

	public float WattsToResolve = 100f;

	public float MinimumWattsToResolve;

	public float MinimumWattsToContact = 180f;

	public float SecondsRequiredToContact = 60f;

	private SatelliteDish _interrogatingDish;

	private float _normalizedSecondsConnected;

	private bool _contacted;

	public Vector3 Angle;

	public string ContactName;

	public float Lifetime;

	public float EndLifetime;

	public float InitialLifeTime;

	public ShuttleType ShuttleType = ShuttleType.Small;

	public ulong HumanTradingSteamID;

	private bool _currentlyTrading;

	private ITraderDestination _connectedPad;

	public static readonly List<TraderContact> AllStationContacts = new List<TraderContact>();

	public static readonly List<TraderContact> NewContacts = new List<TraderContact>();

	public static readonly List<long> DeletedContacts = new List<long>();

	public TraderDataInstance DataInstance { get; set; }

	public bool RequiresThreshold
	{
		get
		{
			ShuttleType shuttleType = ShuttleType;
			return shuttleType == ShuttleType.MediumPlane || shuttleType == ShuttleType.LargePlane;
		}
	}

	public int RequiredRunwayLength => ShuttleType switch
	{
		ShuttleType.MediumPlane => 15, 
		ShuttleType.LargePlane => 20, 
		_ => 0, 
	};

	public Vector2 DeorbitDistance => ShuttleType switch
	{
		ShuttleType.Small => new Vector2(100f, 75f), 
		ShuttleType.SmallGas => new Vector2(100f, 75f), 
		ShuttleType.Medium => new Vector2(200f, 150f), 
		ShuttleType.MediumGas => new Vector2(200f, 150f), 
		ShuttleType.Large => new Vector2(300f, 260f), 
		ShuttleType.LargeGas => new Vector2(300f, 260f), 
		ShuttleType.MediumPlane => new Vector2(250f, 90f), 
		ShuttleType.LargePlane => new Vector2(300f, 100f), 
		_ => new Vector2(100f, 100f), 
	};

	public float MovementSpeedMultiplier => ShuttleType switch
	{
		ShuttleType.Small => 1f, 
		ShuttleType.SmallGas => 1f, 
		ShuttleType.Medium => 1f, 
		ShuttleType.MediumGas => 1f, 
		ShuttleType.Large => 0.9f, 
		ShuttleType.LargeGas => 0.9f, 
		ShuttleType.MediumPlane => 1.4f, 
		ShuttleType.LargePlane => 1.4f, 
		_ => 1f, 
	};

	public SatelliteDish InterrogatingDish
	{
		get
		{
			return _interrogatingDish;
		}
		set
		{
			_interrogatingDish = value;
			if (NetworkManager.IsServer)
			{
				NetworkUpdateFlags |= 16;
			}
		}
	}

	public float NormalizedSecondsConnected
	{
		get
		{
			return _normalizedSecondsConnected;
		}
		set
		{
			_normalizedSecondsConnected = value;
			if (NetworkManager.IsServer)
			{
				NetworkUpdateFlags |= 32;
			}
		}
	}

	public bool Contacted
	{
		get
		{
			return _contacted;
		}
		set
		{
			if (NetworkManager.IsServer)
			{
				NetworkUpdateFlags |= 1;
			}
			_contacted = value;
		}
	}

	public bool IsPlane
	{
		get
		{
			ShuttleType shuttleType = ShuttleType;
			return shuttleType == ShuttleType.LargePlane || shuttleType == ShuttleType.MediumPlane;
		}
	}

	public bool HasEnvironmentRequirement => RequiredPadEnvironment != SpeciesClass.None;

	public bool CurrentlyTrading
	{
		get
		{
			return _currentlyTrading;
		}
		set
		{
			_currentlyTrading = value;
			if (NetworkManager.IsServer)
			{
				NetworkUpdateFlags |= 8;
			}
		}
	}

	public ITraderDestination ConnectedPad
	{
		get
		{
			return _connectedPad;
		}
		set
		{
			_connectedPad = value;
			if (NetworkManager.IsServer)
			{
				NetworkUpdateFlags |= 8;
			}
		}
	}

	public string DisplayName => ContactName;

	public ushort NetworkUpdateFlags { get; set; }

	public long ReferenceId { get; set; }

	public bool BeingDestroyed { get; set; }

	public static event Event OnInitialized;

	public Vector2 RequiredPadSize()
	{
		switch (ShuttleType)
		{
		case ShuttleType.Small:
		case ShuttleType.SmallGas:
			return new Vector2(3f, 3f);
		case ShuttleType.Medium:
		case ShuttleType.MediumGas:
			return new Vector2(5f, 5f);
		case ShuttleType.Large:
		case ShuttleType.LargeGas:
			return new Vector2(6f, 6f);
		case ShuttleType.MediumPlane:
			return new Vector2(7f, 7f);
		case ShuttleType.LargePlane:
			return new Vector2(9f, 9f);
		default:
			return TraderShuttle.DefaultTraderSize;
		}
	}

	public bool EnvironmentRequirementMet(LandingPadCenter landingPadCenter)
	{
		if (!HasEnvironmentRequirement || landingPadCenter == null)
		{
			return true;
		}
		Atmosphere atmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(landingPadCenter.WorldGrid);
		bool flag = atmosphere.PressureGasses > Chemistry.Limits.PressureMinimumSafe && atmosphere.PressureGasses < Chemistry.Limits.PressureMaximumSafe;
		bool flag2 = atmosphere.Temperature > Chemistry.Temperature.ZeroDegrees && atmosphere.Temperature < Chemistry.Temperature.FiftyDegrees;
		bool result = true;
		switch (RequiredPadEnvironment)
		{
		case SpeciesClass.Human:
		{
			bool flag3 = atmosphere.PartialPressureHumanToxins < Entity.ToxicPartialPressureForWarning;
			bool flag5 = atmosphere.PartialPressureO2 > Chemistry.MinimumOxygenPartialPressure;
			result = flag && flag2 && flag3 && flag5;
			break;
		}
		case SpeciesClass.Zrilian:
		{
			bool flag3 = atmosphere.PartialPressureZrillianToxins < Entity.ToxicPartialPressureForWarning;
			bool flag4 = atmosphere.PartialPressureMethane > Chemistry.MinimumOxygenPartialPressure;
			result = flag && flag2 && flag3 && flag4;
			break;
		}
		}
		return result;
	}

	public void ApplyShuttleVariant()
	{
		if (DataInstance.TraderData.ShuttleVariant == ShuttleVariant.Gas)
		{
			switch (ShuttleType)
			{
			case ShuttleType.Small:
			case ShuttleType.SmallGas:
				ShuttleType = ShuttleType.SmallGas;
				break;
			case ShuttleType.Medium:
			case ShuttleType.MediumGas:
			case ShuttleType.MediumPlane:
				ShuttleType = ShuttleType.MediumGas;
				break;
			case ShuttleType.Large:
			case ShuttleType.LargeGas:
			case ShuttleType.LargePlane:
				ShuttleType = ShuttleType.LargeGas;
				break;
			}
		}
		if (ShuttleType == ShuttleType.None)
		{
			ConsoleWindow.PrintError("TraderData " + DataInstance.TraderData.Id + " Shuttle type not specified");
		}
	}

	public void SetShuttleType(ShuttleType type)
	{
		ShuttleType = type;
	}

	public float InterrogationWattageRatio(SatelliteDish dish)
	{
		float wattageOnContact = dish.GetWattageOnContact(this);
		if (wattageOnContact < 0f)
		{
			return 0f;
		}
		return wattageOnContact / MinimumWattsToContact;
	}

	public float InterrogationRatio()
	{
		return Mathf.Clamp01(NormalizedSecondsConnected / SecondsRequiredToContact);
	}

	public float InterrogationTimeRemainingAtWattage(float wattage)
	{
		if (wattage < MinimumWattsToContact)
		{
			return 0f;
		}
		return (SecondsRequiredToContact - NormalizedSecondsConnected) / (wattage / MinimumWattsToContact);
	}

	public float TotalInterrogationTimeAtWattage(float wattage)
	{
		if (wattage < MinimumWattsToContact)
		{
			return 0f;
		}
		return SecondsRequiredToContact / (wattage / MinimumWattsToContact);
	}

	public float ResolveWattageRatio(SatelliteDish dish)
	{
		float wattageOnContact = dish.GetWattageOnContact(this);
		if (wattageOnContact < 0f)
		{
			return 0f;
		}
		return wattageOnContact / WattsToResolve;
	}

	public float ResolveTimeFromDish(SatelliteDish dish)
	{
		float wattageOnContact = dish.GetWattageOnContact(this);
		if (wattageOnContact < MinimumWattsToResolve)
		{
			return -1f;
		}
		return dish.baseTimeToResolve / (wattageOnContact / WattsToResolve);
	}

	public void OnAssignedReference()
	{
		AddContact(this);
		OnInitializedCall();
		HelperHintsManager.Register(this);
	}

	public void PrintDebugInfo(bool verbose = false)
	{
		TreeString treeString = TreeString.Node(DisplayName);
		TreeString.Variable("Name: " + DisplayName, treeString);
		TreeString.Variable("Id: " + DataInstance.TraderData.Id);
		TreeString.Variable($"Type: {ShuttleType}", treeString);
		TreeString.Variable($"Angle: {Angle}", treeString);
		TreeString.Variable($"Lifetime: {Lifetime}", treeString);
		TreeString.Variable($"EndLifetime: {EndLifetime}", treeString);
		TreeString.Variable($"InitialLifeTime: {InitialLifeTime}", treeString);
		TreeString.Variable($"HumanTradingSteamID: {HumanTradingSteamID}", treeString);
		TreeString.Variable($"CurrentlyTrading: {CurrentlyTrading}", treeString);
		TreeString.Variable($"ConnectedPad ID: {ConnectedPad?.ReferenceId}", treeString);
		TreeString.Variable("CheckSum: " + DataInstance.TraderData.TraderChecksum(), treeString);
		treeString.ToConsole();
	}

	public static void AddContact(TraderContact contact)
	{
		AllStationContacts.Add(contact);
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			NewContacts.Add(contact);
		}
	}

	public static void RemoveContact(TraderContact contact)
	{
		contact.BeingDestroyed = true;
		contact.ContactSlot?.Clear();
		contact.ContactSlot = null;
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			DeletedContacts.Add(contact.ReferenceId);
		}
		Referencable.Deregister(contact);
		HelperHintsManager.Deregister(contact);
		AllStationContacts.Remove(contact);
		WorldLog.Append(new TraderLeftRangeEvent(GameStrings.TraderLeftRangeEvent.AsString(contact.DisplayName)));
	}

	public static bool IsNetworkUpdateRequired(ushort toCheck, ushort networkUpdateType)
	{
		return (toCheck & networkUpdateType) != 0;
	}

	public static void SerializeDirty(RocketBinaryWriter writer)
	{
		byte b = 0;
		int position = writer.Position;
		writer.WriteByte(0);
		foreach (TraderContact allStationContact in AllStationContacts)
		{
			if (allStationContact.NetworkUpdateFlags != 0 && !allStationContact.BeingDestroyed)
			{
				writer.WriteInt64(allStationContact.ReferenceId);
				writer.WriteUInt16(allStationContact.NetworkUpdateFlags);
				allStationContact.SerializeDeltaState(writer, allStationContact.NetworkUpdateFlags);
				b++;
				allStationContact.NetworkUpdateFlags = 0;
			}
		}
		writer.Seek(position, SeekOrigin.Begin);
		writer.WriteByte(b);
		writer.Seek(0, SeekOrigin.End);
	}

	public static void DeserializeDirty(RocketBinaryReader reader)
	{
		byte b = reader.ReadByte();
		for (int i = 0; i < b; i++)
		{
			TraderContact traderContact = Referencable.Find<TraderContact>(reader.ReadInt64());
			ushort updateFlags = reader.ReadUInt16();
			traderContact.DeserializeDeltaState(reader, updateFlags);
		}
	}

	public static void SerializeNew(RocketBinaryWriter writer)
	{
		int count = NewContacts.Count;
		writer.WriteByte((byte)count);
		foreach (TraderContact newContact in NewContacts)
		{
			writer.WriteInt64(newContact.ReferenceId);
			writer.WriteInt32(newContact.ContactSlot.IdHash);
			newContact.Write(writer);
		}
		NewContacts.Clear();
	}

	public static void DeserializeNew(RocketBinaryReader reader)
	{
		byte b = reader.ReadByte();
		for (int i = 0; i < b; i++)
		{
			long referenceId = reader.ReadInt64();
			new TraderContact(ContactSlot.Get(reader.ReadInt32()), referenceId).Read(reader);
		}
	}

	public static void SerializeDeleted(RocketBinaryWriter writer)
	{
		int count = DeletedContacts.Count;
		writer.WriteByte((byte)count);
		foreach (long deletedContact in DeletedContacts)
		{
			writer.WriteInt64(deletedContact);
		}
		DeletedContacts.Clear();
	}

	public static void DeserializeDeleted(RocketBinaryReader reader)
	{
		byte b = reader.ReadByte();
		for (int i = 0; i < b; i++)
		{
			TraderContact traderContact = Referencable.Find<TraderContact>(reader.ReadInt64());
			if (traderContact != null)
			{
				RemoveContact(traderContact);
			}
		}
	}

	public TraderContact(ContactSlot slot, long referenceId = 0L)
	{
		ContactSlot = slot;
		slot.CurrentContact = this;
		if (referenceId == 0L)
		{
			Referencable.RegisterNew(this);
		}
		else
		{
			Referencable.RegisterAs(this, referenceId);
		}
	}

	public TraderContact(long referenceId = 0L)
	{
		if (referenceId == 0L)
		{
			Referencable.RegisterNew(this);
		}
		else
		{
			Referencable.RegisterAs(this, referenceId);
		}
	}

	public static void OnInitializedCall()
	{
		TraderContact.OnInitialized?.Invoke();
	}

	public static void InitializeAll()
	{
		Randomize = new System.Random(WorldManager.Seed);
		foreach (ContactSlot contactSlot in ContactSlot.ContactSlots)
		{
			contactSlot.TryGenerateNewContact();
		}
		ContactManager.Instance.StartCheckContactLifeTime();
		if (TraderContact.OnInitialized != null)
		{
			TraderContact.OnInitialized();
		}
	}

	public static async UniTaskVoid HandleSaveRevision(TraderContact traderContact, StationContactData contactData)
	{
		await UniTask.WaitUntil(() => GameManager.GameState != GameState.Loading);
		if (GameManager.GameState == GameState.Running && traderContact != null && traderContact.DataInstance == null)
		{
			TraderData traderData = TraderData.Find(contactData.InstanceSaveData?.TraderDataId ?? 0);
			if (traderData != null && contactData.InstanceSaveData != null)
			{
				traderContact.DataInstance = traderData.Instantiate(contactData.InstanceSaveData.Seed, traderContact, 0L);
			}
			else
			{
				RemoveContact(traderContact);
			}
		}
	}

	public static void LoadStationContacts(IEnumerable<StationContactData> contacts)
	{
		foreach (StationContactData contact in contacts)
		{
			ContactSlot contactSlot = ContactSlot.Find(contact.ContactSlotId);
			if (contactSlot != null && contactSlot.SavedContactId == contact.ReferenceId)
			{
				TraderContact traderContact = new TraderContact(contactSlot, contact.ReferenceId)
				{
					ContactName = contact.ContactName,
					Angle = contact.Angle,
					Lifetime = contact.Lifetime,
					Contacted = contact.Contacted,
					WattsToResolve = contact.WattsToResolve,
					MinimumWattsToResolve = contact.MinimumWattsToResolve,
					MinimumWattsToContact = contact.MinimumWattsToContact,
					SecondsRequiredToContact = contact.SecondsRequiredToContact,
					InitialLifeTime = Time.time,
					RequiredPadEnvironment = contact.RequiredPadEnvironment
				};
				traderContact.EndLifetime = traderContact.InitialLifeTime + traderContact.Lifetime;
				traderContact.DataInstance = TraderDataInstance.DeserializeInstanceSaveData(contact.InstanceSaveData, traderContact);
				traderContact.SetShuttleType(contact.ShuttleType);
				HandleSaveRevision(traderContact, contact).Forget();
				TraderContact.OnInitialized?.Invoke();
			}
		}
	}

	public ContactState GetContactState(SatelliteDish dish)
	{
		if (dish == null)
		{
			return ContactState.None;
		}
		if (Contacted)
		{
			return ContactState.Contacted;
		}
		if (InterrogatingDish != null)
		{
			return ContactState.Interrogating;
		}
		if (!dish.DishScannedContacts.TryGetData(this, out var data))
		{
			return ContactState.Unknown;
		}
		if (data.CurrentTimeTillResolve > 0f)
		{
			return ContactState.Resolving;
		}
		return ContactState.Resolved;
	}

	public static void Clear()
	{
		AllStationContacts.Clear();
	}

	public List<string> GetMessageText(CommsMotherboard motherBoard)
	{
		List<string> list = new List<string>();
		if (Contacted)
		{
			list.Add(Localization.GetInterface("ContactTradeRequest"));
		}
		string errorMessage;
		if (motherBoard.SelectedLandingPad == null)
		{
			list.Add(GameStrings.ContactTradeNoLandingPad.DisplayString);
		}
		else if (!motherBoard.SelectedLandingPad.CanTraderLand(this, out errorMessage))
		{
			list.Add(errorMessage);
		}
		if (motherBoard.IsError)
		{
			list.Add(Localization.GetInterface("ContactLandingRequestFailSatellite"));
		}
		return list;
	}

	public void SerializeDeltaState(RocketBinaryWriter writer, ushort updateFlags)
	{
		if (IsNetworkUpdateRequired(updateFlags, 4))
		{
			DataInstance.SerializeDeltaState(writer);
		}
		if (IsNetworkUpdateRequired(updateFlags, 8))
		{
			writer.WriteInt64(ConnectedPad?.ReferenceId ?? 0);
			writer.WriteBoolean(CurrentlyTrading);
		}
		if (IsNetworkUpdateRequired(updateFlags, 2))
		{
			writer.WriteUInt64(HumanTradingSteamID);
		}
		if (IsNetworkUpdateRequired(updateFlags, 1))
		{
			writer.WriteBoolean(Contacted);
		}
		if (IsNetworkUpdateRequired(updateFlags, 16))
		{
			writer.WriteInt64(InterrogatingDish?.ReferenceId ?? 0);
		}
		if (IsNetworkUpdateRequired(updateFlags, 32))
		{
			writer.WriteSingle(NormalizedSecondsConnected);
		}
	}

	public void DeserializeDeltaState(RocketBinaryReader reader, ushort updateFlags)
	{
		if (IsNetworkUpdateRequired(updateFlags, 4))
		{
			DataInstance.DeserializeDeltaState(reader);
		}
		if (IsNetworkUpdateRequired(updateFlags, 8))
		{
			long referenceId = reader.ReadInt64();
			ConnectedPad = Referencable.Find<ITraderDestination>(referenceId);
			CurrentlyTrading = reader.ReadBoolean();
		}
		if (IsNetworkUpdateRequired(updateFlags, 2))
		{
			HumanTradingSteamID = reader.ReadUInt64();
		}
		if (IsNetworkUpdateRequired(updateFlags, 1))
		{
			Contacted = reader.ReadBoolean();
		}
		if (IsNetworkUpdateRequired(updateFlags, 16))
		{
			InterrogatingDish = Thing.Find<SatelliteDish>(reader.ReadInt64());
		}
		if (IsNetworkUpdateRequired(updateFlags, 32))
		{
			NormalizedSecondsConnected = reader.ReadSingle();
		}
	}

	public void Read(RocketBinaryReader reader)
	{
		Angle = reader.ReadVector3();
		Lifetime = reader.ReadSingle();
		long num = reader.ReadInt64();
		if (num != -1)
		{
			ConnectedPad = Thing.Find<ITraderDestination>(num);
		}
		ContactName = reader.ReadString();
		CurrentlyTrading = reader.ReadBoolean();
		EndLifetime = reader.ReadSingle();
		InitialLifeTime = reader.ReadSingle();
		WattsToResolve = reader.ReadSingle();
		MinimumWattsToResolve = reader.ReadSingle();
		MinimumWattsToContact = reader.ReadSingle();
		SecondsRequiredToContact = reader.ReadSingle();
		ShuttleType = (ShuttleType)reader.ReadInt32();
		RequiredPadEnvironment = (SpeciesClass)reader.ReadInt32();
		HumanTradingSteamID = reader.ReadUInt64();
		Contacted = reader.ReadBoolean();
		InterrogatingDish = Thing.Find<SatelliteDish>(reader.ReadInt64());
		NormalizedSecondsConnected = reader.ReadSingle();
		DataInstance = TraderDataInstance.Read(reader, this);
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteVector3(Angle);
		writer.WriteSingle(Lifetime);
		writer.WriteInt64(ConnectedPad?.ReferenceId ?? (-1));
		writer.WriteString(ContactName);
		writer.WriteBoolean(CurrentlyTrading);
		writer.WriteSingle(EndLifetime);
		writer.WriteSingle(InitialLifeTime);
		writer.WriteSingle(WattsToResolve);
		writer.WriteSingle(MinimumWattsToResolve);
		writer.WriteSingle(MinimumWattsToContact);
		writer.WriteSingle(SecondsRequiredToContact);
		writer.WriteInt32((int)ShuttleType);
		writer.WriteInt32((int)RequiredPadEnvironment);
		writer.WriteUInt64(HumanTradingSteamID);
		writer.WriteBoolean(Contacted);
		writer.WriteInt64(InterrogatingDish?.ReferenceId ?? 0);
		writer.WriteSingle(NormalizedSecondsConnected);
		TraderDataInstance.Write(writer, DataInstance);
	}

	public static void SerializeOnJoin(RocketBinaryWriter writer)
	{
		writer.WriteUInt16((ushort)AllStationContacts.Count);
		foreach (TraderContact allStationContact in AllStationContacts)
		{
			writer.WriteInt64(allStationContact.ReferenceId);
			writer.WriteInt32(allStationContact.ContactSlot.ContactSlotData.IdHash);
			allStationContact.Write(writer);
		}
	}

	public static async UniTask DeserializeOnJoin(RocketBinaryReader reader)
	{
		await ImGuiLoadingScreen.SetState(GameStrings.LoadingScreenDeserializingStationContacts.DisplayString);
		ushort num = reader.ReadUInt16();
		for (int i = 0; i < num; i++)
		{
			long referenceId = reader.ReadInt64();
			new TraderContact(ContactSlot.Get(reader.ReadInt32()), referenceId).Read(reader);
		}
	}

	public void PrintBuyTrade()
	{
		TreeString treeString = new TreeString(DisplayName);
		foreach (BuyDataInstance buyDataInstance in DataInstance.BuyDataInstances)
		{
			buyDataInstance.ToTreeString(treeString);
		}
		ConsoleWindow.Print(treeString.ToString());
	}

	public void PrintSellTrade()
	{
		TreeString treeString = new TreeString(DisplayName);
		foreach (SellDataInstance sellDataInstance in DataInstance.SellDataInstances)
		{
			sellDataInstance.ToTreeString(treeString);
		}
		ConsoleWindow.Print(treeString.ToString());
	}

	public void PrintEvaluateTrade()
	{
		TreeString treeString = new TreeString(DisplayName);
		List<DynamicThing> networkInventory = ConnectedPad.LandingPadNetwork.GetNetworkInventory();
		ConsoleWindow.PrintAction("evaluating trades for '" + DisplayName + "'");
		ConsoleWindow.PrintAction($"including {networkInventory.Count((DynamicThing c) => (object)c != null)} items from pad inventories");
		ITradableInventory parentHuman = InventoryManager.ParentHuman;
		if (parentHuman != null)
		{
			List<DynamicThing> contents = parentHuman.GetContents();
			ConsoleWindow.PrintAction($"including '{parentHuman.DisplayName}' with {contents.Count((DynamicThing c) => (object)c != null)} items");
			networkInventory.AddRange(parentHuman.GetContents());
		}
		foreach (BuyDataInstance buyDataInstance in DataInstance.BuyDataInstances)
		{
			buyDataInstance.EvaluateToTree(treeString, networkInventory);
		}
		ConsoleWindow.Print(treeString.ToString());
	}
}
