using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using Networks;

namespace Assets.Scripts.Networks;

public class ChuteNetwork : StructureNetwork
{
	public class ChuteNetworkStrings
	{
		public const string ChuteNetwork = "Network Id:";

		public const string ReferenceId = "Chute Id:";

		public const string Occupant = "Occupant:";
	}

	public static readonly List<ChuteNetwork> AllChuteNetworks = new List<ChuteNetwork>();

	public List<Device> DeviceList = new List<Device>();

	public Dictionary<Device, HashSet<INetworkedStructure>> DeviceRegister = new Dictionary<Device, HashSet<INetworkedStructure>>();

	public override StructureNetworkType NetworkType => StructureNetworkType.Chute;

	public override string DisplayName => "Chute Network: " + StringManager.Get(base.ReferenceId);

	public static long[] AllNetworkIds => ReferencableNetworkHelper.GetValidNetworkIds(AllChuteNetworks);

	public ChuteNetwork(long referenceId = 0L)
		: base(referenceId)
	{
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		AllChuteNetworks.Add(this);
	}

	protected override void OnDeregister()
	{
		base.OnDeregister();
		AllChuteNetworks.Remove(this);
		for (int num = DeviceList.Count - 1; num >= 0; num--)
		{
			Device device = DeviceList[num];
			RemoveDevice(device);
		}
	}

	public HashSet<INetworkedStructure> GetDeviceRegistration(Device device)
	{
		DeviceRegister.TryGetValue(device, out var value);
		return value;
	}

	public void AddDevice(INetworkedChute chute, Device device)
	{
		HashSet<INetworkedStructure> deviceRegistration = GetDeviceRegistration(device);
		if (deviceRegistration == null)
		{
			deviceRegistration = new HashSet<INetworkedStructure> { chute };
			DeviceList.Add(device);
			DeviceRegister.Add(device, deviceRegistration);
			device.ConnectedChuteNetworks.Add(this);
			RefreshNetworkDevice(device);
		}
		else if (!deviceRegistration.Contains(chute))
		{
			deviceRegistration.Add(chute);
			DeviceRegister[device] = deviceRegistration;
			RefreshNetworkDevice(device);
		}
	}

	private void RefreshNetworkDevice(Device device)
	{
		device.OnAddChuteNetwork(this);
	}

	public void RemoveDevice(Device device)
	{
		DeviceRegister.Remove(device);
		DeviceList.Remove(device);
		device.ConnectedChuteNetworks.Remove(this);
		device.OnRemoveChuteNetwork(this);
	}

	public void RemoveDevice(INetworkedChute chute, Device device)
	{
		HashSet<INetworkedStructure> deviceRegistration = GetDeviceRegistration(device);
		if (deviceRegistration != null)
		{
			deviceRegistration.Remove(chute);
			if (deviceRegistration.Count == 0)
			{
				DeviceRegister.Remove(device);
				DeviceList.Remove(device);
				device.ConnectedChuteNetworks.Remove(this);
				device.OnRemoveChuteNetwork(this);
			}
		}
	}

	public override bool Add(INetworkedStructure iNetworkedStructure)
	{
		if (!(iNetworkedStructure is INetworkedChute networkedChute))
		{
			return false;
		}
		if (base.Add(iNetworkedStructure))
		{
			Span<SmallCellRef> span = stackalloc SmallCellRef[32];
			int count = 0;
			networkedChute.FillConnected<Device>(span, ref count);
			Span<SmallCellRef> span2 = span;
			Span<SmallCellRef> span3 = span2.Slice(0, count);
			for (int i = 0; i < span3.Length; i++)
			{
				SmallCellRef smallCellRef = span3[i];
				AddDevice(networkedChute, smallCellRef.Get<Device>());
			}
			return true;
		}
		return false;
	}

	protected override ReferencableNetwork<INetworkedStructure> CreateNewNetwork()
	{
		return new ChuteNetwork(0L);
	}

	protected override void OnRebuildVisit(INetworkedStructure current, ReferencableNetwork oldNetwork)
	{
		if (oldNetwork is ChuteNetwork chuteNetwork)
		{
			Span<SmallCellRef> span = stackalloc SmallCellRef[32];
			int count = 0;
			INetworkedChute networkedChute = (INetworkedChute)current;
			networkedChute.FillConnected<Device>(span, ref count);
			Span<SmallCellRef> span2 = span;
			Span<SmallCellRef> span3 = span2.Slice(0, count);
			for (int i = 0; i < span3.Length; i++)
			{
				SmallCellRef smallCellRef = span3[i];
				chuteNetwork.RemoveDevice(networkedChute, smallCellRef.Get<Device>());
			}
		}
	}

	public override void OnImguiDraw()
	{
		if (base.StructureList.Count == 0 || InventoryManager.ParentHuman == null)
		{
			return;
		}
		foreach (INetworkedChute structure in base.StructureList)
		{
			if (structure != null)
			{
				ImGuiExtensions.Rendering.RenderingColor = ImGuiExtensions.GetThingColour(structure.GetAsThing);
				structure.OnImGuiDraw();
			}
		}
		ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.White;
	}
}
