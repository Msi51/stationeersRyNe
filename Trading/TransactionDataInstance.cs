using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Trading;

public abstract class TransactionDataInstance : IReferencable, IEvaluable
{
	public Texture2D Thumbnail;

	public virtual string DisplayName { get; }

	public ushort NetworkUpdateFlags { get; set; }

	public long ReferenceId { get; set; }

	public bool BeingDestroyed { get; set; }

	public virtual Thing GetItemPrefab()
	{
		return null;
	}

	public abstract bool IsGasTransaction();

	public virtual void Write(RocketBinaryWriter writer)
	{
	}

	public virtual void Read(RocketBinaryReader reader)
	{
	}

	public void PrintDebugInfo(bool verbose = false)
	{
	}

	public void OnAssignedReference()
	{
	}

	protected async UniTaskVoid SetThumbnail(TransactionData data)
	{
		if (Stationpedia.GetGasThumbnail(data.Thumbnail, out var thumbnail))
		{
			Thumbnail = thumbnail.texture;
		}
		else
		{
			Thumbnail = await WorldManager.LoadImageFromStreamingAssets("Images\\TradingItemThumbnails", data.Thumbnail);
		}
	}

	protected bool HasCustomToolTip(string stringKey, out string toolTip)
	{
		toolTip = string.Empty;
		if (string.IsNullOrEmpty(stringKey))
		{
			return false;
		}
		if (GameString.TryGet(Animator.StringToHash(stringKey), out var gameString))
		{
			toolTip = gameString.DisplayString;
			return true;
		}
		return false;
	}
}
