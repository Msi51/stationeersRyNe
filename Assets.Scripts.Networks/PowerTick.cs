using System.Collections.Generic;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Networks;

public class PowerTick
{
	public CableNetwork CableNetwork;

	public List<Device> Devices = new List<Device>();

	public List<CableFuse> Fuses = new List<CableFuse>();

	public List<Cable> Cables = new List<Cable>();

	public List<Cable> BreakableCables = new List<Cable>();

	public List<CableFuse> BreakableFuses = new List<CableFuse>();

	public float Potential;

	public float Required;

	public float Consumed;

	private float _powerAvailable;

	private Device _currentDevice;

	private CableFuse _currentFuse;

	private readonly List<PowerProvider> _providers = new List<PowerProvider>();

	private readonly List<PowerProvider> _inputOutputDevices = new List<PowerProvider>();

	private List<long> _networkTraversalRecord = new List<long>(Device.MaxProviderRecursionIterations);

	private float _netPower;

	private bool _isPowerMet;

	private float _powerRatio;

	private float _actual;

	public PowerProvider[] Providers { get; private set; }

	public PowerProvider[] InputOutputDevices { get; private set; }

	public void Initialise(CableNetwork cableNetwork)
	{
		CableNetwork = cableNetwork;
		Devices.Clear();
		Fuses.Clear();
		Cables.Clear();
		lock (CableNetwork.PowerDeviceList)
		{
			Devices.AddRange(CableNetwork.PowerDeviceList);
		}
		lock (CableNetwork.FuseList)
		{
			Fuses.AddRange(CableNetwork.FuseList);
		}
		lock (CableNetwork.CableList)
		{
			Cables.AddRange(CableNetwork.CableList);
		}
		Potential = 0f;
		Required = 0f;
		Consumed = 0f;
	}

	public void CalculateState()
	{
		_providers.Clear();
		_inputOutputDevices.Clear();
		int count = Devices.Count;
		while (count-- > 0)
		{
			_currentDevice = Devices[count];
			if (_currentDevice == null)
			{
				continue;
			}
			float usedPower = _currentDevice.GetUsedPower(CableNetwork);
			if (usedPower > 0f)
			{
				Required += usedPower;
			}
			float generatedPower = _currentDevice.GetGeneratedPower(CableNetwork);
			if (generatedPower > 0f)
			{
				Potential += generatedPower;
				PowerProvider item = new PowerProvider(_currentDevice, CableNetwork);
				_providers.Add(item);
				if (_currentDevice.IsPowerInputOutput)
				{
					_inputOutputDevices.Add(item);
				}
			}
		}
		Providers = _providers.ToArray();
		InputOutputDevices = _inputOutputDevices.ToArray();
		CheckForRecursiveProviders();
	}

	private void CheckForRecursiveProviders()
	{
		_networkTraversalRecord.Clear();
		PowerProvider[] inputOutputDevices = InputOutputDevices;
		foreach (PowerProvider powerProvider in inputOutputDevices)
		{
			if (powerProvider.Device.IsProviderToDevice(powerProvider.Device, ref _networkTraversalRecord))
			{
				if (powerProvider.Device.PowerCableNetwork.FuseList.Count > 0)
				{
					BreakableFuses.Add(powerProvider.Device.PowerCableNetwork.FuseList[0]);
				}
				else
				{
					BreakableCables.Add(powerProvider.Device.PowerCable);
				}
				break;
			}
		}
	}

	private bool ConsumePower(Device device, CableNetwork cableNetwork, float powerRequired)
	{
		int num = Providers.Length;
		while (num-- > 0)
		{
			PowerProvider powerProvider = Providers[num];
			if (powerProvider != null && !(powerProvider.Energy <= 0f))
			{
				float num2 = Mathf.Min(powerRequired, powerProvider.Energy);
				powerProvider.Energy -= num2;
				powerRequired -= num2;
				Consumed += num2;
				device.ReceivePower(cableNetwork, num2);
				if (powerRequired <= 0f)
				{
					return true;
				}
			}
		}
		return powerRequired <= 0f;
	}

	private void CacheState()
	{
		_netPower = Potential - Required;
		_isPowerMet = _netPower > 0f;
		if (Potential > 0f && Required > 0f)
		{
			_powerRatio = Mathf.Clamp(_isPowerMet ? 1f : (Potential / Required), 0f, 1f);
		}
		else
		{
			_powerRatio = 1f;
		}
		_actual = Mathf.Min(Potential, Required);
	}

	private void GetBreakableFuses()
	{
		for (int i = 0; i < Fuses.Count; i++)
		{
			CableFuse cableFuse = Fuses[i];
			if (!(cableFuse == null) && cableFuse.PowerBreak < _actual)
			{
				BreakableFuses.Add(cableFuse);
			}
		}
	}

	private void GetBreakableCables()
	{
		for (int i = 0; i < Cables.Count; i++)
		{
			Cable cable = Cables[i];
			if (!(cable == null) && cable.MaxVoltage < _actual)
			{
				BreakableCables.Add(cable);
			}
		}
	}

	private void BreakSingleFuse()
	{
		CableFuse cableFuse = BreakableFuses.Pick();
		if (cableFuse != null)
		{
			Required = cableFuse.PowerBreak;
			CacheState();
			cableFuse.Break();
		}
	}

	private void BreakSingleCable()
	{
		Cable cable = BreakableCables.Pick();
		if (cable != null)
		{
			Required = cable.MaxVoltage;
			CacheState();
			cable.Break();
		}
	}

	public void ApplyState()
	{
		CacheState();
		GetBreakableFuses();
		if (BreakableFuses.Count > 0)
		{
			BreakSingleFuse();
		}
		GetBreakableCables();
		if (BreakableCables.Count > 0)
		{
			BreakSingleCable();
		}
		for (int i = 0; i < Devices.Count; i++)
		{
			Device device = Devices[i];
			if (device == null)
			{
				continue;
			}
			float usedPower = device.GetUsedPower(CableNetwork);
			if (usedPower < 0f)
			{
				continue;
			}
			usedPower *= _powerRatio;
			if (usedPower > 0f && ConsumePower(device, CableNetwork, usedPower) && (_isPowerMet || (device.IsPowerProvider && _powerRatio > 0f)))
			{
				if (!device.Powered)
				{
					device.SetPowerFromThread(CableNetwork, hasPower: true).Forget();
				}
			}
			else if (device.AllowSetPower(CableNetwork) && device.Powered)
			{
				device.SetPowerFromThread(CableNetwork, hasPower: false).Forget();
			}
		}
		int num = Providers.Length;
		while (num-- > 0)
		{
			Providers[num]?.ApplyPower();
		}
	}
}
