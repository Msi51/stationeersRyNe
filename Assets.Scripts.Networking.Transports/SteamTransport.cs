using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Steamworks;
using Steamworks.Data;
using Steamworks.ServerList;
using Steamworks.Ugc;
using UnityEngine;
using Util.Commands;

namespace Assets.Scripts.Networking.Transports;

public class SteamTransport
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct DLC_ID
	{
		public const uint ZrilianSpeciesPack = 1038400u;

		public const uint RobotSpeciesPack = 1038500u;

		public const uint NameinGame = 760470u;

		public const uint HumanCosmeticsPack = 2089290u;

		public const uint CountryOverallsPack = 2542990u;

		public const uint BobbleHeadEva = 3166330u;

		public const uint BobbleHeadHard = 3196210u;

		public const uint BobbleHeadMarine = 3196220u;

		public const uint IcarusSuitPack = 1149460u;

		public const uint MetallicSprayPaintsPack = 4842920u;
	}

	public class WorkshopProgress : IProgress<float>
	{
		public static Action<bool, float> ProgressEvent;

		private readonly bool _isDeleting;

		private float _lastValue;

		public WorkshopProgress(bool isDeleting)
		{
			_isDeleting = isDeleting;
		}

		public void Report(float value)
		{
			if (!(_lastValue >= value))
			{
				_lastValue = value;
				ProgressEvent?.Invoke(_isDeleting, value);
				Debug.Log($"[WORKSHOP] progress: {value}");
			}
		}
	}

	public readonly struct ItemWrapper
	{
		public readonly ulong Id;

		public readonly ulong AuthorId;

		public readonly string DirectoryPath;

		public readonly string DirectoryName;

		public readonly string FilePathFullName;

		public readonly string Title;

		public readonly DateTime LastWriteTime;

		public readonly bool IsOwner;

		private ItemWrapper(Item item, string localFileName)
		{
			try
			{
				Id = item.Id;
				AuthorId = item.Owner.Id;
				DirectoryPath = item.Directory;
				DirectoryName = item.Directory?.Split('\\').Last();
				LastWriteTime = item.Updated;
				Title = item.Title;
				FilePathFullName = item.Directory + "\\" + localFileName;
				IsOwner = (ulong)item.Owner.Id == (ulong)SteamClient.SteamId;
			}
			catch (Exception ex)
			{
				ConsoleWindow.PrintError($"Error wrapping item with id {item.Id}");
				ConsoleWindow.PrintError(ex.Message);
				throw;
			}
		}

		private ItemWrapper(FileInfo info, WorkshopType workType)
		{
			bool flag = workType == WorkshopType.Mod;
			Id = 0uL;
			AuthorId = (SteamClient.IsValid ? SteamClient.SteamId : ((SteamId)0uL));
			DirectoryPath = (flag ? info.Directory.Parent.FullName : info.DirectoryName);
			DirectoryName = (flag ? info.Directory.Parent.Name : info.Directory.Name);
			LastWriteTime = info.LastWriteTime;
			FilePathFullName = info.FullName;
			Title = info.Name;
			IsOwner = true;
		}

		public static ItemWrapper WrapWorkshopItem(Item item, string localFileName)
		{
			return new ItemWrapper(item, localFileName);
		}

		public static ItemWrapper WrapLocalItem(FileInfo info, WorkshopType workshopType)
		{
			return new ItemWrapper(info, workshopType);
		}

		public bool IsLocal()
		{
			return Id == 0;
		}

		public override string ToString()
		{
			return JObject.FromObject(this).ToString();
		}
	}

	public class WorkShopItemDetail
	{
		public ulong PublishedFileId { get; set; }

		public string Title { get; set; }

		public string Path { get; set; }

		public string PreviewPath { get; set; }

		public WorkshopType Type { get; set; }

		public string Description { get; set; }

		public string ChangeNote { get; set; }

		public List<string> CustomTags { get; set; }
	}

	public enum WorkshopType
	{
		World,
		Mod,
		ICCode
	}

	public const uint APP_ID = 544550u;

	private Internet _request;

	public bool IsInitialised => SteamClient.IsValid;

	public string Username => SteamClient.Name;

	public string PublicIp { get; set; }

	public static bool InitialiseSteamInEditor { get; set; }

	public void InitClient()
	{
		if (GameManager.IsBatchMode || (Application.isEditor && !InitialiseSteamInEditor))
		{
			return;
		}
		try
		{
			SteamClient.Init(544550u);
			SteamFriends.OnGameRichPresenceJoinRequested += RichPresenceJoinRequested;
			SteamServer.OnSteamServersConnected += delegate
			{
				Debug.Log($"Steam server connected: {SteamClient.IsValid}, IP: {SteamServer.PublicIp}");
			};
			SteamServer.OnSteamServersDisconnected += delegate(Result res)
			{
				Debug.Log($"Steam server disconnected. Reason {res}");
			};
			SteamServer.OnSteamServerConnectFailure += delegate(Result res, bool stillTrying)
			{
				Debug.Log($"Steam server failed to connect. Reason {res}, stillTrying: {stillTrying}");
			};
			SteamUserStats.OnUserStatsReceived += OnUserStatsReceived;
		}
		catch (Exception ex)
		{
			ConsoleWindow.Print("Error Initialising Steam: " + ex.Message, ConsoleColor.Red);
		}
	}

	private void OnUserStatsReceived(SteamId steamId, Result result)
	{
		if (result == Result.OK)
		{
			Achievements.Steam.InitializeAsAchievementStore();
		}
	}

	private static void RichPresenceJoinRequested(Friend friend, string args)
	{
		ConsoleWindow.Print("RichPresenceJoinRequested. friend: " + friend.Name + ", args: " + args);
		CommandLine.Process(args);
	}

	public UniTask<int> RegisterGameSession(GameSession gameSession)
	{
		throw new NotImplementedException();
	}

	public UniTask<bool> Ping(GameSession gameSession)
	{
		throw new NotImplementedException();
	}

	public void UnRegisterGameSession(GameSession gameSession)
	{
		SteamServer.Shutdown();
	}

	public void Shutdown()
	{
		SteamServer.Shutdown();
		SteamClient.Shutdown();
	}

	public string GetAppInstallDir()
	{
		if (!IsInitialised)
		{
			return null;
		}
		return SteamApps.AppInstallDir(544550u);
	}

	public bool IsDlcInstalled(uint dlcId)
	{
		if (IsInitialised)
		{
			return SteamApps.IsDlcInstalled(dlcId);
		}
		return false;
	}

	private static void SetRichPresence(string key, string value)
	{
		if (!GameManager.IsBatchMode && SteamClient.IsValid && !SteamFriends.SetRichPresence(key, value))
		{
			ConsoleWindow.PrintError("Could not set steam rich presence. " + key + ": " + value, suppressStacktrace: true);
		}
	}

	public static void SetSteamRichPresenceStatus()
	{
		GameManager.UpdateRichPresenceState();
		if (NetworkManager.NetworkRole == NetworkRole.Client)
		{
			SetSteamRichPresenceConnect(NetworkClient.Address, NetworkClient.Port);
		}
	}

	public static void SetSteamRichPresenceConnect(string address, string port)
	{
		SetRichPresence("connect", "-join " + address + ":" + port);
	}

	public void OpenWebPageOverlay(string url)
	{
		if (IsInitialised)
		{
			SteamFriends.OpenWebOverlay(url);
		}
	}

	public string GetFriendName(ulong clientId)
	{
		if (!IsInitialised)
		{
			return null;
		}
		return SteamFriends.GetFriends().First((Friend x) => (ulong)x.Id == clientId).Name;
	}

	public async UniTask<Texture2D> GetAvatarAsync(ulong clientId, AvatarSize size)
	{
		if (!IsInitialised)
		{
			return null;
		}
		Image? image = await (size switch
		{
			AvatarSize.Small => SteamFriends.GetSmallAvatarAsync(clientId), 
			AvatarSize.Medium => SteamFriends.GetMediumAvatarAsync(clientId), 
			AvatarSize.Large => SteamFriends.GetLargeAvatarAsync(clientId), 
			_ => throw new ArgumentOutOfRangeException("size", size, null), 
		});
		return image.HasValue ? ConvertImage(image.Value) : null;
	}

	private static Texture2D ConvertImage(Image image)
	{
		Texture2D texture2D = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.ARGB32, mipChain: false);
		texture2D.filterMode = FilterMode.Trilinear;
		for (int i = 0; i < image.Width; i++)
		{
			for (int j = 0; j < image.Height; j++)
			{
				Steamworks.Data.Color pixel = image.GetPixel(i, j);
				texture2D.SetPixel(i, (int)image.Height - j, new UnityEngine.Color((float)(int)pixel.r / 255f, (float)(int)pixel.g / 255f, (float)(int)pixel.b / 255f, (float)(int)pixel.a / 255f));
			}
		}
		texture2D.Apply();
		return texture2D;
	}

	public bool Authenticate(ulong clientId)
	{
		BeginAuthResult num = SteamUser.BeginAuthSession(SteamUser.GetAuthSessionTicket().Data, clientId);
		UserHasLicenseForAppResult userHasLicenseForAppResult = SteamServer.UserHasLicenseForApp(clientId, 544550u);
		if (num == BeginAuthResult.OK)
		{
			return userHasLicenseForAppResult == UserHasLicenseForAppResult.HasLicense;
		}
		return false;
	}

	private async UniTaskVoid FetchPublicIp()
	{
		await UniTask.WaitUntil(() => SteamServer.IsValid);
		IPAddress ip = null;
		while (ip == null || ip.ToString() == "0.0.0.0")
		{
			ip = SteamServer.PublicIp;
			Debug.Log($"[Steam] PublicIp: {ip}");
			await UniTask.Delay(1000);
		}
	}

	public void CancelServerListRequest()
	{
		_request?.Dispose();
		_request = null;
	}

	public static async UniTask<(bool success, ulong fileId, PublishResult result)> Workshop_PublishItemAsync(WorkShopItemDetail detail)
	{
		Editor seed = ((detail.PublishedFileId == 0L) ? Editor.NewCommunityFile : new Editor(detail.PublishedFileId)).WithTitle(detail.Title).WithDescription(detail.Description).WithPreviewFile(detail.PreviewPath)
			.WithContent(detail.Path)
			.WithChangeLog(detail.ChangeNote)
			.WithTag(GetTagFromType(detail.Type))
			.WithPublicVisibility();
		if (detail.CustomTags != null)
		{
			seed = detail.CustomTags.Aggregate(seed, (Editor current, string tag) => current.WithTag(tag));
		}
		PublishResult result = await seed.SubmitAsync(new WorkshopProgress(isDeleting: false));
		if (!result.Success)
		{
			Debug.LogError($"[Workshop_UploadItem] {result.Result}");
			return (result.Success, result.FileId, result);
		}
		bool smashedTheSubscribeButton = await Workshop_SubscribeToItemAsync(result.FileId);
		await Workshop_PollForItem(result.FileId, (Item? item) => item.HasValue);
		return (result.Success && smashedTheSubscribeButton, result.FileId, result);
	}

	private static string GetTagFromType(WorkshopType type)
	{
		return type switch
		{
			WorkshopType.World => "World Save", 
			WorkshopType.Mod => "Mod", 
			WorkshopType.ICCode => "IC Code", 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private static async UniTask<Item?> Workshop_PollForItem(ulong fileId, Func<Item?, bool> predicate)
	{
		Item? item;
		do
		{
			item = await Workshop_GetSingleItem(fileId);
			Debug.Log($"Workshop_PollForItem. Satisfied: {predicate(item)}");
			if (!predicate(item))
			{
				await UniTask.Delay(500);
			}
		}
		while (!predicate(item));
		return item.GetValueOrDefault();
	}

	public static async UniTask<bool> Workshop_ItemExists(ulong fileId)
	{
		Item? item = await Item.GetAsync(fileId, 0);
		return item.HasValue && item.Value.Result == Result.OK;
	}

	private static async UniTask<Item?> Workshop_GetSingleItem(ulong fileId)
	{
		Item? item = await Item.GetAsync(fileId);
		if (item.HasValue && item.GetValueOrDefault().NeedsUpdate)
		{
			await SteamUGC.DownloadAsync(item.Value.Id);
		}
		return item;
	}

	private static async UniTask<bool> Workshop_SubscribeToItemAsync(ulong fileId)
	{
		Item? item = await Workshop_GetSingleItem(fileId);
		if (item.HasValue)
		{
			return await item.Value.Subscribe();
		}
		return false;
	}

	public static async UniTask<bool> Workshop_UnsubscribeAsync(ulong fileId)
	{
		Item? item = await Workshop_GetSingleItem(fileId);
		if (!item.HasValue)
		{
			return false;
		}
		return await item.Value.Unsubscribe();
	}

	public static async UniTask<bool> Workshop_DeleteItemAsync(ulong fileId)
	{
		Item? item = await Workshop_GetSingleItem(fileId);
		if (!item.HasValue)
		{
			return false;
		}
		WorkshopProgress progress = new WorkshopProgress(isDeleting: true);
		bool unSubbed = await item.Value.Unsubscribe();
		progress.Report(0.3f);
		if (!string.IsNullOrEmpty(item.Value.Directory))
		{
			Directory.Delete(item.Value.Directory, recursive: true);
		}
		bool isDeleted = (ulong)item.Value.Owner.Id != (ulong)SteamClient.SteamId || await SteamUGC.DeleteFileAsync(fileId);
		progress.Report(0.5f);
		await Workshop_PollForItem(fileId, (Item? i) => !i.HasValue || !i.Value.IsSubscribed).TimeoutWithoutException(TimeSpan.FromSeconds(5.0));
		progress.Report(1f);
		return unSubbed && isDeleted;
	}

	public static async UniTask<IEnumerable<ItemWrapper>> Workshop_QueryItemsAsync(WorkshopType itemType, uint page = 1u)
	{
		if (!SteamClient.IsValid)
		{
			return Enumerable.Empty<ItemWrapper>();
		}
		List<Item> entries = (await Query.Items.WithTag(GetTagFromType(itemType)).WhereUserSubscribed().GetPageAsync((int)page))?.Entries.ToList() ?? new List<Item>();
		await UniTask.WhenAll(from x in entries
			where x.NeedsUpdate || !Directory.Exists(x.Directory)
			select SteamUGC.DownloadAsync(x.Id).AsUniTask());
		string fileName = itemType.GetLocalFileName();
		if (itemType == WorkshopType.Mod)
		{
			fileName = "About\\" + fileName;
		}
		return entries.Select((Item x) => ItemWrapper.WrapWorkshopItem(x, fileName));
	}
}
