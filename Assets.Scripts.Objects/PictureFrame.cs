using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Networking;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class PictureFrame : SmallGrid
{
	public static List<PictureFrame> AllFrames = new List<PictureFrame>();

	public FrameOrientation Orientation;

	public static bool FinishLoad;

	public Dictionary<FrameOrientation, List<byte[]>> LoadingDictionary = new Dictionary<FrameOrientation, List<byte[]>>();

	public FileInfo[] CollectedFiles;

	private Texture2D _textureLoadTarget;

	private List<byte[]> PicturesToChooseFrom = new List<byte[]>();

	private Material PictureImage;

	[HideInInspector]
	public int CurrentPictureIndex;

	public void PopulateDictionary(FrameOrientation key, FileInfo fileN)
	{
		if (LoadingDictionary.ContainsKey(key))
		{
			LoadingDictionary[key].Add(File.ReadAllBytes(fileN.FullName));
			return;
		}
		LoadingDictionary.Add(key, new List<byte[]> { File.ReadAllBytes(fileN.FullName) });
	}

	public override void Awake()
	{
		base.Awake();
		if (!FinishLoad)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(Application.streamingAssetsPath + "/PictureFrameImages/Portrait");
			CollectedFiles = Array.FindAll(directoryInfo.GetFiles(), (FileInfo file) => file.Name.ToLower().EndsWith(".png"));
			FileInfo[] collectedFiles = CollectedFiles;
			foreach (FileInfo fileN in collectedFiles)
			{
				PopulateDictionary(FrameOrientation.Portrait, fileN);
			}
			directoryInfo = new DirectoryInfo(Application.streamingAssetsPath + "/PictureFrameImages/LandScape");
			CollectedFiles = Array.FindAll(directoryInfo.GetFiles(), (FileInfo file) => file.Name.ToLower().EndsWith(".png"));
			collectedFiles = CollectedFiles;
			foreach (FileInfo fileN2 in collectedFiles)
			{
				PopulateDictionary(FrameOrientation.Landscape, fileN2);
			}
			_textureLoadTarget = new Texture2D(2, 2);
			PictureImage = Renderers[0].Materials[1];
		}
	}

	public override void Start()
	{
		base.Start();
		PicturesToChooseFrom = LoadingDictionary[Orientation];
		ChangePicture();
		AllFrames.Add(this);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (GameManager.RunSimulation)
		{
			result = HandlePictureFrame(interactable.Action, interactable.ContextualName, -1, doAction).Succeed();
			if (NetworkManager.IsServer)
			{
				PictureFrameMessage pictureFrameMessage = new PictureFrameMessage();
				pictureFrameMessage.PictureFrame = base.netId;
				pictureFrameMessage.InteractableType = (int)interactable.Action;
				pictureFrameMessage.PictureIndex = CurrentPictureIndex;
				pictureFrameMessage.SendToClients();
			}
		}
		return result;
	}

	public DelayedActionInstance HandlePictureFrame(InteractableType interactable, string ContextualName, int OverrideIndex = -1, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = ContextualName
		};
		switch (interactable)
		{
		case InteractableType.Button1:
			if (doAction)
			{
				CurrentPictureIndex--;
				if (CurrentPictureIndex < 0)
				{
					CurrentPictureIndex = PicturesToChooseFrom.Count - 1;
				}
			}
			delayedActionInstance.ActionMessage = Localization.GetInterface("Picture") + (CurrentPictureIndex - 1);
			break;
		case InteractableType.Button2:
			if (doAction)
			{
				CurrentPictureIndex++;
				if (CurrentPictureIndex > PicturesToChooseFrom.Count - 1)
				{
					CurrentPictureIndex = 0;
				}
			}
			delayedActionInstance.ActionMessage = Localization.GetInterface("Picture") + (CurrentPictureIndex + 1);
			break;
		}
		if (OverrideIndex != -1)
		{
			CurrentPictureIndex = OverrideIndex;
		}
		ChangePicture();
		return delayedActionInstance;
	}

	public void ChangePicture()
	{
		if (CurrentPictureIndex <= PicturesToChooseFrom.Count - 1)
		{
			_textureLoadTarget.LoadImage(PicturesToChooseFrom[CurrentPictureIndex]);
		}
		else
		{
			_textureLoadTarget.LoadImage(PicturesToChooseFrom[0]);
		}
		PictureImage.mainTexture = _textureLoadTarget;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new PictureFrameSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		PictureFrameSaveData pictureFrameSaveData = savedData as PictureFrameSaveData;
		CurrentPictureIndex = pictureFrameSaveData.PictureIndex;
		PicturesToChooseFrom = LoadingDictionary[Orientation];
		ChangePicture();
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		(savedData as PictureFrameSaveData).PictureIndex = CurrentPictureIndex;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		for (int i = 0; i < AllFrames.Count; i++)
		{
			if (AllFrames[i].ReferenceId == base.ReferenceId)
			{
				AllFrames.RemoveAt(i);
				break;
			}
		}
	}
}
