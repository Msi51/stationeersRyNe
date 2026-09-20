using System.Collections.Generic;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Trading;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Electrical;

public interface IComputer : IReferencable, IEvaluable
{
	CableNetwork DataCableNetwork { get; }

	Cable DataCable { get; set; }

	GraphicRaycaster GraphicRaycaster { get; }

	GameObject Screen { get; set; }

	Motherboard CurrentMotherboard { get; set; }

	bool ShowComputerScreen { get; }

	bool IsBeingDestroyed { get; }

	Thing AsThing();

	Device AsDevice();

	void CheckStatus();

	List<ILogicable> DeviceList();

	float ZOffset();
}
