using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Serialization;
using Cysharp.Threading.Tasks;
using Steamworks.Ugc;
using UnityEngine;

namespace Assets.Scripts.UI;

[XmlRoot("InstructionData")]
public class InstructionData
{
	[XmlElement]
	public long DateTime = System.DateTime.Now.ToFileTime();

	[XmlElement]
	public string GameVersion = GameManager.GetGameVersion();

	[XmlElement]
	public string Title = string.Empty;

	[XmlElement]
	public string Description = string.Empty;

	[XmlElement]
	public string Author = string.Empty;

	[XmlElement]
	public ulong WorkshopFileHandle;

	[XmlElement]
	public string Instructions = string.Empty;

	[XmlIgnore]
	public SteamTransport.ItemWrapper ItemWrapper;

	private string _tempChangeNote;

	[XmlIgnore]
	public DirectoryInfo DirectoryPath => new DirectoryInfo(ItemWrapper.DirectoryPath);

	[XmlIgnore]
	public ulong AuthorId => ItemWrapper.AuthorId;

	public void OnSearchTextChanged()
	{
	}

	public string GetDescription(int maxCharacters)
	{
		if (Description.Length > maxCharacters)
		{
			return Description.Substring(0, maxCharacters) + "...";
		}
		return Description;
	}

	public static InstructionData GetFromFile(string filePath)
	{
		filePath = filePath.Replace("\\", "/");
		if (XmlSerialization.Deserialize(new XmlSerializer(typeof(InstructionData)), filePath) is InstructionData result)
		{
			return result;
		}
		Debug.LogError("Could not cast to InstructionData : " + filePath);
		return null;
	}

	public void SaveToFile(DirectoryInfo directory)
	{
		string text = directory.FullName + "/instruction.xml";
		text = text.Replace("\\", "/");
		XmlSerialization.Serialization(new XmlSerializer(typeof(InstructionData)), this, text);
	}

	public async UniTask<(bool success, ulong fileId)> PublishToWorkshop()
	{
		DirectoryInfo directoryPath = DirectoryPath;
		InstructionData fromFile = GetFromFile(directoryPath.FullName + "/instruction.xml");
		if (fromFile != null)
		{
			WorkshopFileHandle = fromFile.WorkshopFileHandle;
		}
		InputSourceCode.Instance.PCM.SaveCodeScreenshot(directoryPath.FullName, directoryPath.Name);
		SteamTransport.WorkShopItemDetail ItemDetail = new SteamTransport.WorkShopItemDetail
		{
			Title = directoryPath.Name,
			Path = directoryPath.FullName,
			PreviewPath = directoryPath.FullName + "/" + XmlSaveLoad.WorkShopPreviewFileName,
			PublishedFileId = WorkshopFileHandle,
			Type = SteamTransport.WorkshopType.ICCode,
			Description = Description
		};
		TaskCompletionSource<string> changeLogTask = new TaskCompletionSource<string>();
		if (WorkshopFileHandle == 0L || !ItemWrapper.IsOwner)
		{
			changeLogTask.TrySetResult("Initial Upload");
		}
		else
		{
			InputWindow.ShowInputPanel("Enter Change Note (Optional)");
			InputWindow.OnSubmit += delegate(string fileName, string changeNote)
			{
				changeLogTask.TrySetResult(changeNote);
			};
		}
		SteamTransport.WorkShopItemDetail workShopItemDetail = ItemDetail;
		workShopItemDetail.ChangeNote = await changeLogTask.Task;
		ProgressPanel.ShowProgressBar();
		(bool success, ulong fileId, PublishResult result) publishResult = default((bool, ulong, PublishResult));
		try
		{
			publishResult = await SteamTransport.Workshop_PublishItemAsync(ItemDetail);
		}
		catch
		{
			ConsoleWindow.PrintError($"Failed to publish: {publishResult.result.Result}", suppressStacktrace: true);
		}
		ProgressPanel.ShowProgressSuccessOrFailure(publishResult.success);
		return (publishResult.success, publishResult.fileId);
	}

	public void SaveFromComputer()
	{
		Instructions = InputSourceCode.Copy();
		SaveToFile(DirectoryPath);
	}

	public void LoadIntoComputer()
	{
		InputSourceCode.Paste(Instructions);
	}

	public void DeleteFile()
	{
		InputSourceCode.DeleteInstruction(DirectoryPath.Name);
	}

	public async UniTask<bool> DeleteWorkshopItem()
	{
		DeleteFile();
		return await SteamTransport.Workshop_DeleteItemAsync(WorkshopFileHandle);
	}
}
