using System;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;

namespace Objects.Rockets.Scanning;

public struct RocketActionResult : IEquatable<RocketActionResult>
{
	public static readonly RocketActionResult Success;

	private readonly IReferencable _referencable;

	private readonly string _referencableName;

	public bool IsSuccess { get; private set; }

	public Assets.Scripts.Localization2.GameString FailureInfo { get; }

	private int Hash { get; }

	public long ReferencableId => _referencable?.ReferenceId ?? 0;

	public static implicit operator bool(RocketActionResult result)
	{
		return result.IsSuccess;
	}

	public static string GetText(RocketActionResult result)
	{
		if (string.IsNullOrEmpty(result._referencableName))
		{
			return result.FailureInfo?.DisplayString ?? string.Empty;
		}
		return result.FailureInfo?.AsString(result._referencableName) ?? string.Empty;
	}

	private RocketActionResult(bool isSuccess, Assets.Scripts.Localization2.GameString failureInfo, IReferencable referencable)
	{
		IsSuccess = isSuccess;
		FailureInfo = failureInfo;
		Hash = failureInfo.Key;
		_referencable = referencable;
		_referencableName = referencable?.DisplayName;
	}

	private RocketActionResult(bool isSuccess, Assets.Scripts.Localization2.GameString failureInfo, string referencableName)
	{
		IsSuccess = isSuccess;
		FailureInfo = failureInfo;
		Hash = failureInfo.Key;
		_referencable = null;
		_referencableName = referencableName;
	}

	public static RocketActionResult Failure(Assets.Scripts.Localization2.GameString failInfo, IReferencable referencable = null)
	{
		return new RocketActionResult(isSuccess: false, failInfo, referencable);
	}

	public bool Equals(RocketActionResult other)
	{
		if (IsSuccess && other.IsSuccess)
		{
			return true;
		}
		if (!IsSuccess && !other.IsSuccess && Hash == other.Hash)
		{
			return true;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj != null)
		{
			return Equals((RocketActionResult)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Hash;
	}

	public static void Write(RocketBinaryWriter writer, RocketActionResult result)
	{
		writer.WriteBoolean(result.IsSuccess);
		writer.WriteInt32(result.Hash);
		IReferencable referencable = result._referencable;
		bool flag = referencable != null && !referencable.BeingDestroyed;
		writer.WriteBoolean(flag);
		if (flag)
		{
			Network.WritePackedId(writer, result._referencable);
		}
		else
		{
			writer.WriteString(result._referencableName);
		}
	}

	public static RocketActionResult Create(RocketBinaryReader reader)
	{
		bool isSuccess = reader.ReadBoolean();
		Assets.Scripts.Localization2.GameString.TryGet(reader.ReadInt32(), out var gameString);
		if (reader.ReadBoolean())
		{
			Network.ReadPackedId(reader, out var referenceId);
			IReferencable referencable = Referencable.Find<IReferencable>(referenceId);
			return new RocketActionResult(isSuccess, gameString, referencable);
		}
		string referencableName = reader.ReadString();
		return new RocketActionResult(isSuccess, gameString, referencableName);
	}

	static RocketActionResult()
	{
		Success = new RocketActionResult
		{
			IsSuccess = true
		};
	}
}
