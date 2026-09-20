using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using CharacterCustomisation;

namespace DLC;

public static class DLCManager
{
	private static DLCType _ownedDLC;

	public static readonly DLCType AllDLC = DLCType.Zrilian | DLCType.HemDroid | DLCType.HumanCharacter | DLCType.CountryOveralls | DLCType.BobbleHeadEva | DLCType.IcarusSuit | DLCType.BobbleHeadHard | DLCType.BobbleHeadMarine | DLCType.MetallicPaints;

	public static DLCType GetOwnedDLC()
	{
		return _ownedDLC;
	}

	public static void Initialize()
	{
		FetchDlcOwnership();
	}

	private static void GrantFullOwnership()
	{
		_ownedDLC = AllDLC;
	}

	private static void FetchOwnershipFromSteam()
	{
		if (NetworkManager.CurrentTransport.IsDlcInstalled(1038500u))
		{
			_ownedDLC |= DLCType.HemDroid;
		}
		if (NetworkManager.CurrentTransport.IsDlcInstalled(1038400u))
		{
			_ownedDLC |= DLCType.Zrilian;
		}
		if (NetworkManager.CurrentTransport.IsDlcInstalled(2089290u))
		{
			_ownedDLC |= DLCType.HumanCharacter;
		}
		if (NetworkManager.CurrentTransport.IsDlcInstalled(2542990u))
		{
			_ownedDLC |= DLCType.CountryOveralls;
		}
		if (NetworkManager.CurrentTransport.IsDlcInstalled(3196220u))
		{
			_ownedDLC |= DLCType.BobbleHeadMarine;
		}
		if (NetworkManager.CurrentTransport.IsDlcInstalled(3166330u))
		{
			_ownedDLC |= DLCType.BobbleHeadEva;
		}
		if (NetworkManager.CurrentTransport.IsDlcInstalled(3196210u))
		{
			_ownedDLC |= DLCType.BobbleHeadHard;
		}
		if (NetworkManager.CurrentTransport.IsDlcInstalled(1149460u))
		{
			_ownedDLC |= DLCType.IcarusSuit;
		}
		if (NetworkManager.CurrentTransport.IsDlcInstalled(4842920u))
		{
			_ownedDLC |= DLCType.MetallicPaints;
		}
	}

	private static void FetchDlcOwnership()
	{
		FetchOwnershipFromSteam();
	}

	private static bool CheckAccess(DLCType dlcType)
	{
		if (dlcType == DLCType.None)
		{
			return true;
		}
		return (dlcType & _ownedDLC) != 0;
	}

	public static string GetStorePageLink(DLCType dlcType)
	{
		return dlcType switch
		{
			DLCType.Zrilian => "https://store.steampowered.com/app/1038400", 
			DLCType.HemDroid => "https://store.steampowered.com/app/1038500", 
			DLCType.HumanCharacter => "https://store.steampowered.com/app/2089290", 
			DLCType.CountryOveralls => "https://store.steampowered.com/app/2542990", 
			DLCType.BobbleHeadMarine => "https://store.steampowered.com/app/3196220", 
			DLCType.BobbleHeadHard => "https://store.steampowered.com/app/3196210", 
			DLCType.BobbleHeadEva => "https://store.steampowered.com/app/3166330", 
			DLCType.IcarusSuit => "https://store.steampowered.com/app/1149460", 
			DLCType.MetallicPaints => "https://store.steampowered.com/app/4842920", 
			_ => "https://store.steampowered.com/dlc/544550/Stationeers/", 
		};
	}

	public static bool CheckAccess(KitItem kitItem)
	{
		if ((bool)kitItem)
		{
			return CheckAccess(kitItem.DlcType);
		}
		return true;
	}

	public static bool CheckAccess(Thing thing)
	{
		if (!thing)
		{
			return true;
		}
		return CheckAccess(thing.DLCType);
	}
}
