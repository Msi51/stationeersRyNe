using System;
using System.Threading;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Util;

namespace Assets.Scripts.Networks;

public class ElectricityManager : ThreadedManager
{
	public static ElectricityManager Instance;

	private static WaitForEndOfFrame _waitForFrameSolar;

	private static CancellationTokenWrapper SolarProcessingCancellation = new CancellationTokenWrapper();

	private static readonly Func<ISolarRadiator, bool> CalculateSolarEfficiencyAction = (ISolarRadiator radiator) => radiator != null && !radiator.IsBeingDestroyed && radiator.CalculateSolarEfficiency();

	private const int MAX_POWERED_ITEMS = 8192;

	public static readonly DensePool<IPowered> AllPoweredThings = new DensePool<IPowered>("AllPoweredThings", 8192);

	private static readonly Action<IPowered> IPoweredThingsAction = delegate(IPowered ipowered)
	{
		ipowered?.OnPowerTick();
	};

	private static readonly Action<CableNetwork> CableNetworkTickAction = delegate(CableNetwork cableNetwork)
	{
		cableNetwork?.OnPowerTick();
	};

	public static bool ForceNextDeltaFull = true;

	private static bool _forceThisTick;

	private static readonly Func<RocketBinaryWriter, CableNetwork, bool> CableNetworkWriteAction = delegate(RocketBinaryWriter writer, CableNetwork network)
	{
		if (network == null)
		{
			return false;
		}
		if (!_forceThisTick && !network.IsLoadDirty())
		{
			return false;
		}
		Network.WritePackedId(writer, network);
		writer.WriteSingle(network.CurrentLoad);
		writer.WriteSingle(network.PotentialLoad);
		writer.WriteSingle(network.RequiredLoad);
		network.MarkLoadSent();
		return true;
	};

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if (Instance == null)
		{
			Instance = this;
		}
	}

	private static async UniTaskVoid SolarProcessing(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			if (!WorldManager.IsGamePaused && GameManager.GameState == GameState.Running)
			{
				await SolarRadiators.AllSolarRadiators.ForEachAsync(PlayerLoopTiming.FixedUpdate, cancellationToken, CalculateSolarEfficiencyAction);
			}
			await UniTask.NextFrame(PlayerLoopTiming.FixedUpdate, cancellationToken);
		}
	}

	public override void StartManager()
	{
		base.StartManager();
		SolarProcessingCancellation.CancelAndInitialize();
		SolarProcessing(SolarProcessingCancellation.Token).Forget();
	}

	public override void StopManager()
	{
		base.StopManager();
		SolarProcessingCancellation.Cancel();
	}

	public static void Register(IPowered item)
	{
		if (GameManager.GameState != GameState.None)
		{
			AllPoweredThings.Add(item);
		}
	}

	public static void Deregister(IPowered item)
	{
		if (GameManager.GameState != GameState.None)
		{
			AllPoweredThings.Remove(item);
		}
	}

	public static void ElectricityTick()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		try
		{
			CableNetwork.AllCableNetworks.ForEach(CableNetworkTickAction);
			AllPoweredThings.ForEach(IPoweredThingsAction);
			CircuitHolders.Execute();
		}
		catch (Exception ex)
		{
			string text = "Exception: " + ex.Message + "\n";
			string stackTrace = ex.StackTrace;
			for (int i = 0; i < stackTrace.Length; i++)
			{
				text += stackTrace[i];
			}
			if (GameManager.GameState != GameState.None)
			{
				Debug.LogError(text);
				ConsoleWindow.PrintError("Electronics THread Exception.</b> " + ex.Message + "</color>");
			}
		}
	}

	public static void ClearAll()
	{
		AllPoweredThings.Clear();
	}

	public static void SerialiseDeltaState(RocketBinaryWriter writer)
	{
		_forceThisTick = ForceNextDeltaFull;
		ForceNextDeltaFull = false;
		CableNetwork.AllCableNetworks.Cleanup();
		Network.WriteIndex<ushort>(writer, out var count, out var bufferIndex);
		CableNetwork.AllCableNetworks.ForEach(writer, CableNetworkWriteAction, ref count);
		Network.WriteIndex(writer, count, bufferIndex);
	}

	public static void DeserializeDeltaState(RocketBinaryReader reader)
	{
		ushort num = reader.ReadUInt16();
		for (int i = 0; i < num; i++)
		{
			Network.ReadPackedId(reader, out var referenceId);
			float currentLoad = reader.ReadSingle();
			float potentialLoad = reader.ReadSingle();
			float requiredLoad = reader.ReadSingle();
			CableNetwork cableNetwork = Referencable.Find<CableNetwork>(referenceId);
			if (cableNetwork != null)
			{
				cableNetwork.CurrentLoad = currentLoad;
				cableNetwork.PotentialLoad = potentialLoad;
				cableNetwork.RequiredLoad = requiredLoad;
			}
		}
	}
}
