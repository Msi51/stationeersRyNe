using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.Util;
using Networks;

namespace Assets.Scripts.Networks;

public class PipeNetwork : AtmosphericsNetwork, IDensePoolable
{
	public const int MAX_PIPE_NETWORKS = 8192;

	public static ConcurrentDensePool<PipeNetwork> AllPipeNetworks = new ConcurrentDensePool<PipeNetwork>("AllPipeNetworks", 8192);

	public static bool InUseWithEvent = false;

	private readonly DensePoolReference<PipeNetwork> _densePoolReference = new DensePoolReference<PipeNetwork>(AllPipeNetworks);

	public readonly List<Device> DeviceList = new List<Device>();

	public readonly Dictionary<Device, HashSet<INetworkedStructure>> DeviceRegister = new Dictionary<Device, HashSet<INetworkedStructure>>();

	private const string PERCENT_SYMBOL = "%";

	public override StructureNetworkType NetworkType => StructureNetworkType.Pipe;

	public override string DisplayName => "PipeNetwork_" + StringManager.Get((int)base.ReferenceId);

	public static long[] AllNetworkIds => ReferencableNetworkHelper.GetValidNetworkIds(AllPipeNetworks);

	public bool OnAddToPool(object densePool, int slot)
	{
		if (_densePoolReference.CanAddToPool(densePool))
		{
			return _densePoolReference.AddToPool(densePool, slot);
		}
		return false;
	}

	public void OnRemoveFromPool(object densePool)
	{
		_densePoolReference.OnRemovedFrom(densePool);
	}

	public override void PrintDebugInfo(bool verbose = false)
	{
		ConsoleWindow.Print(DisplayName ?? "");
		ConsoleWindow.Print("Atmosphere Gas Mixture: " + base.Atmosphere.GasMixture);
	}

	public PipeNetwork(long referenceId = 0L)
		: base(referenceId)
	{
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		AllPipeNetworks.Add(this);
	}

	protected override void OnDeregister()
	{
		base.OnDeregister();
		AllPipeNetworks.Remove(this);
		for (int num = DeviceList.Count - 1; num >= 0; num--)
		{
			Device device = DeviceList[num];
			RemoveDevice(device);
		}
	}

	protected override PressurekPa MaxPressureSafe()
	{
		for (int num = DeviceList.Count - 1; num >= 0; num--)
		{
			Device device = DeviceList[num];
			if ((object)device != null && (device is GasTankStorage || device is SuitStorage))
			{
				return Chemistry.OneAtmosphere * 100.0 * Thing.StressedRatio;
			}
		}
		return base.MaxPressureSafe();
	}

	public HashSet<INetworkedStructure> GetDeviceRegistration(Device device)
	{
		DeviceRegister.TryGetValue(device, out var value);
		return value;
	}

	public void AddDevice(INetworkedPipe pipe, Device device)
	{
		HashSet<INetworkedStructure> deviceRegistration = GetDeviceRegistration(device);
		if (deviceRegistration == null)
		{
			deviceRegistration = new HashSet<INetworkedStructure> { pipe };
			DeviceList.Add(device);
			DeviceRegister.Add(device, deviceRegistration);
			device.ConnectedPipeNetworks.Add(this);
			RefreshNetworkDevice(device);
		}
		else if (!deviceRegistration.Contains(pipe))
		{
			deviceRegistration.Add(pipe);
			DeviceRegister[device] = deviceRegistration;
			RefreshNetworkDevice(device);
		}
	}

	private void RefreshNetworkDevice(Device device)
	{
		device.OnAddPipeNetwork(this);
	}

	public void RemoveDevice(Device device)
	{
		DeviceRegister.Remove(device);
		DeviceList.Remove(device);
		device.ConnectedPipeNetworks.Remove(this);
		device.OnRemovePipeNetwork(this);
	}

	public void RemoveDevice(INetworkedPipe pipe, Device device)
	{
		HashSet<INetworkedStructure> deviceRegistration = GetDeviceRegistration(device);
		if (deviceRegistration != null)
		{
			deviceRegistration.Remove(pipe);
			if (deviceRegistration.Count == 0)
			{
				DeviceRegister.Remove(device);
				DeviceList.Remove(device);
				device.ConnectedPipeNetworks.Remove(this);
				device.OnRemovePipeNetwork(this);
			}
		}
	}

	public override bool Add(INetworkedStructure iNetworkedStructure)
	{
		if (!(iNetworkedStructure is INetworkedPipe networkedPipe))
		{
			return false;
		}
		if (base.Add(iNetworkedStructure))
		{
			Span<SmallCellRef> span = stackalloc SmallCellRef[32];
			int count = 0;
			networkedPipe.FillConnected<Device>(span, ref count);
			Span<SmallCellRef> span2 = span;
			Span<SmallCellRef> span3 = span2.Slice(0, count);
			for (int i = 0; i < span3.Length; i++)
			{
				SmallCellRef smallCellRef = span3[i];
				AddDevice(networkedPipe, smallCellRef.Get<Device>());
			}
			return true;
		}
		return false;
	}

	protected override ReferencableNetwork<INetworkedStructure> CreateNewNetwork()
	{
		return new PipeNetwork(0L);
	}

	protected override void OnRebuildVisit(INetworkedStructure current, ReferencableNetwork oldNetwork)
	{
		if (oldNetwork is PipeNetwork pipeNetwork)
		{
			Span<SmallCellRef> span = stackalloc SmallCellRef[32];
			int count = 0;
			INetworkedPipe networkedPipe = (INetworkedPipe)current;
			networkedPipe.FillConnected<Device>(span, ref count);
			Span<SmallCellRef> span2 = span;
			Span<SmallCellRef> span3 = span2.Slice(0, count);
			for (int i = 0; i < span3.Length; i++)
			{
				SmallCellRef smallCellRef = span3[i];
				pipeNetwork.RemoveDevice(networkedPipe, smallCellRef.Get<Device>());
			}
		}
	}

	protected override void Draw()
	{
		base.Draw();
	}
}
