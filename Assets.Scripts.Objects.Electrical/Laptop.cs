using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Trading;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Electrical;

public class Laptop : PowerTool, IComputer, IReferencable, IEvaluable, ICircuitHolder, IDensePoolable, ITransmitable, ILogicable
{
	[Header("Computer")]
	public GameObject ComputerScreen;

	[SerializeField]
	private GraphicRaycaster _graphicRaycaster;

	private Vector3 LeftHandPos = new Vector3(-0.07f, 0.05f, 0f);

	private Vector3 LeftHandRot = new Vector3(-35f, -60f, 35f);

	private Vector3 RightHandPos = new Vector3(-0.07f, 0.03f, -0.03f);

	private Vector3 RightHandRot = new Vector3(35f, 60f, 35f);

	private int _codeErrorState;

	private List<ILogicable> _logicList = new List<ILogicable>(20);

	public ulong LastEditedBy { get; set; }

	public CableNetwork DataCableNetwork => null;

	public Cable DataCable { get; set; }

	public GameObject Screen
	{
		get
		{
			return ComputerScreen;
		}
		set
		{
			ComputerScreen = value;
		}
	}

	public Motherboard CurrentMotherboard { get; set; }

	public virtual bool ShowComputerScreen
	{
		get
		{
			if (!IsOccluded && OnOff)
			{
				return Powered;
			}
			return false;
		}
	}

	public override Vector3 CenterPosition => Bounds.center + base.Position;

	private Slot ProgrammableChipSlot => Slots[0];

	private ProgrammableChip ProgrammableChip => ProgrammableChipSlot.Occupant as ProgrammableChip;

	public override bool IsOperable
	{
		get
		{
			if (!base.IsOperable)
			{
				return false;
			}
			if (CurrentMotherboard != null)
			{
				Motherboard currentMotherboard = CurrentMotherboard;
				return currentMotherboard is ProgrammableChipMotherboard || currentMotherboard is MapMotherboard;
			}
			return false;
		}
	}

	GraphicRaycaster IComputer.GraphicRaycaster
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public void HasPut()
	{
	}

	public Thing AsThing()
	{
		return this;
	}

	public Device AsDevice()
	{
		return null;
	}

	public void CheckStatus()
	{
		if (GameManager.RunSimulation)
		{
			if (!IsOperable && Error == 0)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (IsOperable && Error == 1)
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
	}

	public List<ILogicable> DeviceList()
	{
		if (!(DataCable != null))
		{
			return new List<ILogicable> { this };
		}
		return new List<ILogicable>(DataCable.CableNetwork.DataDeviceList);
	}

	public float ZOffset()
	{
		return -2f;
	}

	public List<LogicBinding> GetLogicBindings()
	{
		return new List<LogicBinding>
		{
			new LogicBinding("LAPTOP")
		};
	}

	public override void Update100MS(float deltaTime)
	{
		base.Update100MS(deltaTime);
		_graphicRaycaster.enabled = CurrentCameraDistanceSquared < 16f;
	}

	public override void OnStartRender()
	{
		base.OnStartRender();
		if ((bool)ComputerScreen)
		{
			ComputerScreen.SetActive(ShowComputerScreen);
		}
	}

	public override void OnStopRender()
	{
		base.OnStopRender();
		if ((bool)ComputerScreen)
		{
			ComputerScreen.SetActive(ShowComputerScreen);
		}
	}

	public override void OnRenamed()
	{
		base.OnRenamed();
		if ((bool)CurrentMotherboard)
		{
			CurrentMotherboard.OnDeviceListChanged();
		}
		if ((bool)CurrentMotherboard)
		{
			CurrentMotherboard.OnRenamed();
		}
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (!NetworkManager.IsActiveAsClient && base.InteractOnOff.State == 1 && base.ParentSlot != null && !base.ParentSlot.IsHandSlot)
		{
			OnServer.Interact(this, InteractableType.OnOff, 0);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.GameState == GameState.Running)
		{
			CheckStatus();
			if (interactable.Action != InteractableType.Error)
			{
				CheckStatus();
			}
			if ((bool)ComputerScreen)
			{
				ComputerScreen.SetActive(ShowComputerScreen);
			}
		}
	}

	public override void OnFinishedInteractionSync(Interactable interactable)
	{
		base.OnFinishedInteractionSync(interactable);
		CheckStatus();
		if ((bool)Screen)
		{
			Screen.SetActive(ShowComputerScreen);
			if ((bool)ProgrammableChip)
			{
				ClearError();
			}
			CheckStatus();
		}
	}

	public override void SetHandPosition(bool leftHand)
	{
		if (leftHand)
		{
			LocalOffSetInHand = LeftHandPos;
			LocalRotationInHand = LeftHandRot;
		}
		else
		{
			LocalOffSetInHand = RightHandPos;
			LocalRotationInHand = RightHandRot;
		}
		base.ThingTransformLocalPosition = LocalOffSetInHand;
		base.ThingTransformLocalRotationEuler = LocalRotationInHand;
	}

	public void ClearError()
	{
		RaiseError(0);
	}

	public void RaiseError(int state)
	{
		_codeErrorState = state;
		CheckStatus();
	}

	public ILogicable GetLogicableFromIndex(int deviceIndex, int networkIndex = int.MinValue)
	{
		if (deviceIndex != int.MaxValue)
		{
			return null;
		}
		return this;
	}

	public ILogicable GetLogicableFromId(int deviceId, int networkIndex = int.MinValue)
	{
		if (deviceId == 0L)
		{
			return null;
		}
		ILogicable logicable = Referencable.Find<ILogicable>(deviceId);
		if (logicable == null)
		{
			return null;
		}
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant == logicable)
			{
				return logicable;
			}
		}
		return null;
	}

	public List<ILogicable> GetBatchOutput()
	{
		for (int i = 0; i < Slots.Count; i++)
		{
			Slot slot = Slots[i];
			_logicList[i] = slot.Occupant as ILogicable;
		}
		return _logicList;
	}

	public bool IsValidIndex(int index)
	{
		return index == int.MaxValue;
	}

	public void SetDeviceLabel(int index, string label)
	{
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = base.InteractWith(interactable, interaction, doAction);
		ProgrammableChip?.AppendErrorsToActionInstance(delayedActionInstance);
		return delayedActionInstance;
	}

	public async UniTask HaltAndCatchFire()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
			if (ThingTransform.GetCancellationTokenOnDestroy().IsCancellationRequested)
			{
				return;
			}
		}
		base.InternalAtmosphere.Sparked = true;
		AtmosphericEventInstance.CloneGlobal(base.WorldGrid, MoleEnergy.Zero, spark: true);
		global::Explosion.Explode(200f, base.ThingTransformLocalPosition, 4f);
		OnServer.Destroy(this);
	}

	public void SetSourceCode(string sourceCode)
	{
		if ((bool)ProgrammableChip)
		{
			ProgrammableChip.SetSourceCode(sourceCode, this);
			ProgrammableChip.SendUpdate();
		}
	}

	public void Execute()
	{
	}

	public string GetSourceCode()
	{
		if (!ProgrammableChip)
		{
			return "";
		}
		return ProgrammableChip.GetSourceCode();
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		ProgrammableChip programmableChip = ProgrammableChip;
		if ((bool)programmableChip)
		{
			extendedText.Append(programmableChip.GetErrorCode());
		}
		return extendedText;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.TemperatureExternal => true, 
			LogicType.PressureExternal => true, 
			LogicType.PositionX => true, 
			LogicType.PositionY => true, 
			LogicType.PositionZ => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.TemperatureExternal => base.WorldAtmosphere?.Temperature.ToDouble() ?? 0.0, 
			LogicType.PressureExternal => base.WorldAtmosphere?.PressureGassesAndLiquids.ToDouble() ?? 0.0, 
			LogicType.PositionX => base.Position.x, 
			LogicType.PositionY => base.Position.y, 
			LogicType.PositionZ => base.Position.z, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public void OnTransmitterCreated()
	{
		Transmitters.AllTransmitters.Add(this);
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		Motherboard motherboard = newChild as Motherboard;
		if ((bool)motherboard)
		{
			CurrentMotherboard = motherboard;
			foreach (GameObject screen in CurrentMotherboard.Screens)
			{
				screen.transform.SetParent(ComputerScreen.transform, worldPositionStays: false);
				screen.transform.localRotation = Quaternion.identity;
				Vector3 zero = Vector3.zero;
				zero.z = ZOffset();
				screen.transform.localPosition = zero;
				screen.transform.localScale = Vector3.one;
				RectTransform component = screen.GetComponent<RectTransform>();
				component.offsetMin = Vector2.zero;
				component.offsetMax = Vector2.zero;
			}
			CurrentMotherboard.SetMode(isNormal: true);
			CurrentMotherboard.ParentComputer = this;
			CurrentMotherboard.OnInsertedToComputer(this);
			if (GameManager.RunSimulation && Error == 1 && !CurrentMotherboard.IsError)
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
		if (GameManager.GameState == GameState.Running)
		{
			if (ProgrammableChip != null)
			{
				ProgrammableChip.Reset();
			}
			ClearError();
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (CurrentMotherboard == previousChild && !previousChild.BeingDestroyed)
		{
			Motherboard currentMotherboard = CurrentMotherboard;
			foreach (GameObject screen in CurrentMotherboard.Screens)
			{
				screen.transform.SetParent(CurrentMotherboard.ThingTransform, worldPositionStays: false);
				screen.transform.localRotation = Quaternion.identity;
				screen.transform.localPosition = Vector3.zero;
				screen.transform.localScale = Vector3.one;
			}
			CurrentMotherboard.SetMode(isNormal: true);
			CurrentMotherboard.ParentComputer = null;
			CurrentMotherboard = null;
			currentMotherboard.OnRemovedFromComputer(this);
		}
		CheckStatus();
	}
}
