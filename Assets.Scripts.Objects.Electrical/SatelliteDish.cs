using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class SatelliteDish : LargeElectrical, IRotatable, ITrading
{
	public Action<SatelliteDish> OnSignalsUpdated;

	[Header("Satellite Dish")]
	public Transform DishTransform;

	public Vector3 DishForward;

	public double strongestSignal;

	private TraderContact _strongestContact;

	private long _bestContactFilterReferenceID = -1L;

	private List<ITraderDestination> _foundPads = new List<ITraderDestination>(8);

	private int _targetPadIndex;

	[SerializeField]
	private Knob horizontalKnob;

	[SerializeField]
	private Knob verticalKnob;

	[SerializeField]
	private Knob settingKnob;

	protected double _vertical;

	protected double _horizontal;

	private int _setting;

	public bool _isDirty = true;

	public ScannedContactDataCollection DishScannedContacts = new ScannedContactDataCollection();

	[SerializeField]
	private float dishFov = 360f;

	[SerializeField]
	private int minWattage = 50;

	[SerializeField]
	private int maxWattage = 200;

	[SerializeField]
	private int stepSmall = 1;

	[SerializeField]
	private int stepNormal = 10;

	[SerializeField]
	public float baseTimeToResolve = 10f;

	private TraderContact _interrogatingContact;

	private const float _resolvingPowerScale = 0.1f;

	public RotatableBehaviour RotatableBehaviour { get; set; }

	public virtual double Vertical
	{
		get
		{
			return _vertical;
		}
		set
		{
			if (_vertical != value)
			{
				_vertical = value;
				BaseAnimator.SetFloat(Defines.Animator.Vertical, (float)_vertical);
				DishForward = DishTransform.up;
				_isDirty = true;
			}
		}
	}

	public virtual double Horizontal
	{
		get
		{
			return _horizontal;
		}
		set
		{
			if (_horizontal != value)
			{
				_horizontal = value;
				BaseAnimator.SetFloat(Defines.Animator.Horizontal, (float)_horizontal);
				DishForward = DishTransform.up;
				_isDirty = true;
			}
		}
	}

	public int Setting
	{
		get
		{
			return _setting;
		}
		set
		{
			if (_setting != value)
			{
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
				_setting = value;
				settingKnob.SetKnob(Setting, maxWattage, minWattage).Forget();
				_isDirty = true;
			}
		}
	}

	public virtual float RotationTolerance => 0.0001f;

	public double MaximumVertical => 90.0;

	public double MaximumHorizontal => 360.0;

	public virtual float MovementSpeedHorizontal => 0.01f;

	public virtual float MovementSpeedVertical => 0.005f;

	private double HorizontalIncrement => 10.0 / MaximumHorizontal;

	private double VerticalIncrement => 10.0 / MaximumVertical;

	public TraderContact InterrogatingContact
	{
		get
		{
			return _interrogatingContact;
		}
		set
		{
			_interrogatingContact = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public bool CanRotate()
	{
		if (OnOff && Powered)
		{
			return base.IsStructureCompleted;
		}
		return false;
	}

	private ITraderDestination GetTargetLandingPad()
	{
		_foundPads.Clear();
		if ((object)base.DataCable == null)
		{
			FindDataCable();
		}
		if (base.DataCable == null || base.DataCable.CableNetwork == null)
		{
			return null;
		}
		foreach (Device dataDevice in base.DataCable.CableNetwork.DataDeviceList)
		{
			if (dataDevice is ITraderDestination item)
			{
				_foundPads.Add(item);
			}
			if (dataDevice is LandingPadModularDevice landingPadModularDevice && landingPadModularDevice.LandingPadCenter != null && !_foundPads.Contains(landingPadModularDevice.LandingPadCenter))
			{
				_foundPads.Add(landingPadModularDevice.LandingPadCenter);
			}
		}
		if (_foundPads.Count == 0)
		{
			return null;
		}
		_foundPads.Sort((ITraderDestination a, ITraderDestination b) => (a.ReferenceId > b.ReferenceId) ? 1 : (-1));
		int index = Mathf.Clamp(_targetPadIndex, 0, _foundPads.Count - 1);
		return _foundPads[index];
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteFloatHalf((float)(RotatableBehaviour?.TargetVertical ?? 0.0));
			writer.WriteFloatHalf((float)(RotatableBehaviour?.TargetHorizontal ?? 0.0));
			writer.WriteInt32(Setting);
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			DishScannedContacts.Write(writer);
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteInt64(InterrogatingContact?.ReferenceId ?? 0);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			float num = reader.ReadFloatHalf();
			float num2 = reader.ReadFloatHalf();
			Setting = reader.ReadInt32();
			if (RotatableBehaviour != null)
			{
				RotatableBehaviour.TargetVertical = num;
				RotatableBehaviour.TargetHorizontal = num2;
			}
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			DishScannedContacts.Read(reader);
			OnSignalsUpdated?.Invoke(this);
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			InterrogatingContact = Referencable.Find<TraderContact>(reader.ReadInt64());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteDouble(RotatableBehaviour?.TargetVertical ?? 0.0);
		writer.WriteDouble(RotatableBehaviour?.TargetHorizontal ?? 0.0);
		writer.WriteInt32(Setting);
		DishScannedContacts.Write(writer);
		writer.WriteInt64(InterrogatingContact?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		double targetVertical = reader.ReadDouble();
		double targetHorizontal = reader.ReadDouble();
		Setting = reader.ReadInt32();
		if (RotatableBehaviour != null)
		{
			RotatableBehaviour.TargetVertical = targetVertical;
			RotatableBehaviour.TargetHorizontal = targetHorizontal;
		}
		DishScannedContacts.Read(reader);
		InterrogatingContact = Referencable.Find<TraderContact>(reader.ReadInt64());
		if (InterrogatingContact != null)
		{
			InterrogatingContact.InterrogatingDish = this;
		}
	}

	public void RunAfterAnimation()
	{
	}

	public async UniTaskVoid UpdateAnimator()
	{
		if (ThreadedManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
		}
		verticalKnob.SetKnob((int)(RotatableBehaviour.TargetVertical * MaximumVertical), (int)MaximumVertical).Forget();
		horizontalKnob.SetKnob((int)(RotatableBehaviour.TargetHorizontal * MaximumHorizontal), (int)MaximumHorizontal).Forget();
	}

	public override void Awake()
	{
		base.Awake();
		InitializeRotatableBehaviour();
		verticalKnob.Initialize(this);
		horizontalKnob.Initialize(this);
		settingKnob.Initialize(this);
		verticalKnob.SetKnob((int)(RotatableBehaviour.TargetVertical * MaximumVertical), (int)MaximumVertical).Forget();
		horizontalKnob.SetKnob((int)(RotatableBehaviour.TargetHorizontal * MaximumHorizontal), (int)MaximumHorizontal).Forget();
		Setting = Mathf.Clamp(Setting, minWattage, maxWattage);
		settingKnob.SetKnob(Setting, maxWattage, minWattage).Forget();
	}

	private void InitializeRotatableBehaviour()
	{
		if (RotatableBehaviour == null)
		{
			RotatableBehaviour obj = new RotatableBehaviour(this)
			{
				MaxAudibleSquareDistance = 600f
			};
			RotatableBehaviour rotatableBehaviour = obj;
			RotatableBehaviour = obj;
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		if (!IsCursor && GameManager.GameState == GameState.Running)
		{
			Horizontal = 0.0;
			Vertical = 0.0;
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		InteractableType action = interactable.Action;
		if (action == InteractableType.Button1 || action == InteractableType.Button2 || action == InteractableType.Button3 || action == InteractableType.Button4 || action == InteractableType.Button5 || action == InteractableType.Button6)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			switch (interactable.Action)
			{
			case InteractableType.Button2:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.HorizontalDegrees, StringManager.Get((int)Math.Round(RotatableBehaviour.TargetHorizontal * MaximumHorizontal)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = RotatableBehaviour.TargetHorizontal + (interaction.AltKey ? (HorizontalIncrement * 0.10000000149011612) : HorizontalIncrement);
				if (num > 1.0)
				{
					num -= 1.0;
				}
				RotatableBehaviour.TargetHorizontal = num;
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button1:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.HorizontalDegrees, StringManager.Get((int)Math.Round(RotatableBehaviour.TargetHorizontal * MaximumHorizontal)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = RotatableBehaviour.TargetHorizontal - (interaction.AltKey ? (HorizontalIncrement * 0.10000000149011612) : HorizontalIncrement);
				if (num < 0.0)
				{
					num += 1.0;
				}
				RotatableBehaviour.TargetHorizontal = num;
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button4:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.VerticalDegrees, StringManager.Get((int)Math.Round(RotatableBehaviour.TargetVertical * MaximumVertical)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = RotatableBehaviour.TargetVertical + (interaction.AltKey ? (VerticalIncrement * 0.10000000149011612) : VerticalIncrement);
				if (num > 1.0)
				{
					num = 1.0;
				}
				RotatableBehaviour.TargetVertical = num;
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button3:
			{
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.VerticalDegrees, StringManager.Get((int)Math.Round(RotatableBehaviour.TargetVertical * MaximumVertical)));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					return delayedActionInstance.Succeed();
				}
				double num = RotatableBehaviour.TargetVertical - (interaction.AltKey ? (VerticalIncrement * 0.10000000149011612) : VerticalIncrement);
				if (num < 0.0)
				{
					num = 0.0;
				}
				RotatableBehaviour.TargetVertical = num;
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button5:
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.OutputWatts, StringManager.Get(Setting));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					delayedActionInstance.AppendStateMessage(GameStrings.UseLabelerToSet);
					return delayedActionInstance.Succeed();
				}
				if (GameManager.RunSimulation)
				{
					if (Setting > 0)
					{
						Setting -= (interaction.AltKey ? stepSmall : stepNormal);
					}
					Setting = Mathf.Max(Setting, minWattage);
				}
				return delayedActionInstance.Succeed();
			case InteractableType.Button6:
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.OutputWatts, StringManager.Get(Setting));
					delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
					delayedActionInstance.AppendStateMessage(GameStrings.UseLabelerToSet);
					return delayedActionInstance.Succeed();
				}
				if (GameManager.RunSimulation)
				{
					if (Setting < maxWattage)
					{
						Setting += (interaction.AltKey ? stepSmall : stepNormal);
					}
					Setting = Mathf.Min(Setting, maxWattage);
				}
				return delayedActionInstance.Succeed();
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	private void GetMostAlignedContact()
	{
		float num = 180f;
		TraderContact strongestContact = null;
		for (int num2 = DishScannedContacts.ScannedContactData.Count - 1; num2 >= 0; num2--)
		{
			ScannedContactData scannedContactData = DishScannedContacts.ScannedContactData[num2];
			if (scannedContactData != null)
			{
				float lastScannedDegreeOffset = scannedContactData.LastScannedDegreeOffset;
				if (num > lastScannedDegreeOffset && ((scannedContactData.Contact.ReferenceId == _bestContactFilterReferenceID && _bestContactFilterReferenceID != -1) || _bestContactFilterReferenceID == -1))
				{
					num = lastScannedDegreeOffset;
					strongestContact = scannedContactData.Contact;
				}
			}
		}
		strongestSignal = num;
		_strongestContact = strongestContact;
	}

	public float DegreesToWattageMultiplierPlaceholder(float degrees)
	{
		if (degrees <= 2f)
		{
			return 1f;
		}
		if (degrees <= 5f)
		{
			return RocketMath.MapToScale(2f, 5f, 1f, 0.9f, degrees);
		}
		if ((double)degrees <= 22.5)
		{
			return RocketMath.MapToScale(5f, 22.5f, 0.9f, 0.1f, degrees);
		}
		if (degrees <= 45f)
		{
			return RocketMath.MapToScale(22.5f, 45f, 0.1f, 0.05f, degrees);
		}
		if (degrees <= 90f)
		{
			return RocketMath.MapToScale(45f, 90f, 0.05f, 0.01f, degrees);
		}
		return RocketMath.MapToScale(90f, 180f, 0.01f, 0.001f, degrees);
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (base.PowerCable == null || base.PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		if (!OnOff)
		{
			return 0f;
		}
		if (InterrogatingContact != null && !InterrogatingContact.Contacted)
		{
			return Setting;
		}
		for (int num = DishScannedContacts.ScannedContactData.Count - 1; num >= 0; num--)
		{
			ScannedContactData scannedContactData = DishScannedContacts.ScannedContactData[num];
			if (scannedContactData != null && scannedContactData.CurrentTimeTillResolve > 0f)
			{
				return (float)Setting * 0.1f;
			}
		}
		return base.GetUsedPower(cableNetwork);
	}

	private void ScanForDishContacts()
	{
		foreach (TraderContact allStationContact in TraderContact.AllStationContacts)
		{
			float num = DishContactAngleCheck(allStationContact);
			if (num > 0f && GetWattageOnContact(allStationContact) >= allStationContact.MinimumWattsToResolve)
			{
				if (DishScannedContacts.TryGetData(allStationContact, out var data))
				{
					data.LastScannedDegreeOffset = num;
				}
				else
				{
					DishScannedContacts.Add(allStationContact, num);
				}
			}
			else
			{
				DishScannedContacts.Remove(allStationContact);
			}
		}
		RemoveDeregisteredContacts();
		GetMostAlignedContact();
	}

	private void RemoveDeregisteredContacts()
	{
		for (int num = DishScannedContacts.ScannedContactData.Count - 1; num >= 0; num--)
		{
			ScannedContactData scannedContactData = DishScannedContacts.ScannedContactData[num];
			if (scannedContactData == null || scannedContactData.Contact.BeingDestroyed)
			{
				DishScannedContacts.ScannedContactData.RemoveAt(num);
			}
		}
	}

	private float DishContactAngleCheck(TraderContact contact)
	{
		float num = Mathf.Acos(Vector3.Dot(DishForward, contact.Angle)) * 57.29578f;
		if (num < dishFov / 2f)
		{
			return num;
		}
		return -1f;
	}

	private float DishContactNoiseGeneration(TraderContact contact)
	{
		return 0f;
	}

	private void InitContactResolveTimers()
	{
		for (int num = DishScannedContacts.ScannedContactData.Count - 1; num >= 0; num--)
		{
			ScannedContactData scannedContactData = DishScannedContacts.ScannedContactData[num];
			if (scannedContactData != null)
			{
				TraderContact contact = scannedContactData.Contact;
				scannedContactData.CurrentTimeTillResolve = contact.ResolveTimeFromDish(this);
				scannedContactData.StartTimeTillResolve = contact.ResolveTimeFromDish(this);
			}
		}
	}

	private void ResolveDishContacts(float deltaTime)
	{
		for (int num = DishScannedContacts.ScannedContactData.Count - 1; num >= 0; num--)
		{
			ScannedContactData scannedContactData = DishScannedContacts.ScannedContactData[num];
			if (scannedContactData != null)
			{
				if (scannedContactData.CurrentTimeTillResolve >= 99999f)
				{
					scannedContactData.CurrentTimeTillResolve = scannedContactData.Contact.ResolveTimeFromDish(this);
					scannedContactData.StartTimeTillResolve = scannedContactData.Contact.ResolveTimeFromDish(this);
				}
				float currentTimeTillResolve = Mathf.Clamp(scannedContactData.CurrentTimeTillResolve - scannedContactData.Contact.ResolveWattageRatio(this) * deltaTime, 0f, 9999f);
				scannedContactData.CurrentTimeTillResolve = currentTimeTillResolve;
			}
		}
	}

	private void ProcessInterrogatingContact(float deltaTime)
	{
		if (InterrogatingContact == null)
		{
			return;
		}
		if (InterrogatingContact.InterrogationWattageRatio(this) < 1f || !Powered || !OnOff)
		{
			InterrogatingContact.NormalizedSecondsConnected = 0f;
			InterrogatingContact.InterrogatingDish = null;
			InterrogatingContact = null;
			if (Activate != 0)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			return;
		}
		float num = GetWattageOnContact(InterrogatingContact) / InterrogatingContact.MinimumWattsToContact * deltaTime;
		InterrogatingContact.NormalizedSecondsConnected += num;
		if (InterrogatingContact.NormalizedSecondsConnected >= InterrogatingContact.SecondsRequiredToContact)
		{
			InterrogatingContact.Contacted = true;
			InterrogatingContact.InterrogatingDish = null;
			InterrogatingContact = null;
			if (Activate != 0)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
		}
	}

	public TraderContact GetStrongestContact()
	{
		return _strongestContact;
	}

	public float GetWattageOnContact(TraderContact contact)
	{
		if (!DishScannedContacts.TryGetData(contact, out var data))
		{
			return DegreesToWattageMultiplierPlaceholder(DishContactAngleCheck(contact)) * (float)Setting;
		}
		return DegreesToWattageMultiplierPlaceholder(data.LastScannedDegreeOffset) * (float)Setting;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.OnOff && OnSignalsUpdated != null)
		{
			OnSignalsUpdated(this);
		}
		if (!GameManager.RunSimulation || interactable.Action != InteractableType.Activate)
		{
			return;
		}
		if (Activate == 1 && _strongestContact != null)
		{
			ITraderDestination targetLandingPad = GetTargetLandingPad();
			if (_strongestContact.Contacted && targetLandingPad != null && targetLandingPad.CurrentTradingContact == null && targetLandingPad.CanTraderLand(_strongestContact, out var _))
			{
				targetLandingPad.ServerCallTrader(isLanding: true, _strongestContact);
			}
			else if (InterrogatingContact == null && !_strongestContact.Contacted)
			{
				_strongestContact.InterrogatingDish = this;
				InterrogatingContact = _strongestContact;
			}
		}
		if (Activate == 0 && InterrogatingContact != null)
		{
			InterrogatingContact.InterrogatingDish = null;
			InterrogatingContact = null;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Setting:
		case LogicType.Horizontal:
		case LogicType.Vertical:
		case LogicType.Idle:
		case LogicType.SignalStrength:
		case LogicType.SignalID:
		case LogicType.InterrogationProgress:
		case LogicType.TargetPadIndex:
		case LogicType.SizeX:
		case LogicType.SizeZ:
		case LogicType.MinimumWattsToContact:
		case LogicType.WattsReachingContact:
		case LogicType.ContactTypeId:
		case LogicType.BestContactFilter:
		case LogicType.ContactSlotIndex:
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Setting:
		case LogicType.Horizontal:
		case LogicType.Vertical:
		case LogicType.TargetPadIndex:
		case LogicType.BestContactFilter:
			return true;
		default:
			return base.CanLogicWrite(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		ScannedContactData data;
		switch (logicType)
		{
		case LogicType.Horizontal:
			return Horizontal * MaximumHorizontal;
		case LogicType.Vertical:
			return Vertical * MaximumVertical;
		case LogicType.Setting:
			return Setting;
		case LogicType.HorizontalRatio:
			return Horizontal;
		case LogicType.VerticalRatio:
			return Vertical;
		case LogicType.SignalStrength:
			if (!DishScannedContacts.TryGetData(_strongestContact, out data))
			{
				return -1.0;
			}
			return (data.CurrentTimeTillResolve > 0f) ? (-1f) : data.LastScannedDegreeOffset;
		case LogicType.SignalID:
			return _strongestContact?.ReferenceId ?? (-1);
		case LogicType.InterrogationProgress:
			if (_strongestContact == null)
			{
				return -1.0;
			}
			if (!_strongestContact.Contacted)
			{
				return (_strongestContact?.InterrogationRatio()).Value;
			}
			return 1.0;
		case LogicType.TargetPadIndex:
			return _targetPadIndex;
		case LogicType.SizeX:
			if (!DishScannedContacts.TryGetData(_strongestContact, out data))
			{
				return -1.0;
			}
			return (data.CurrentTimeTillResolve > 0f) ? (-1f) : _strongestContact.RequiredPadSize().x;
		case LogicType.SizeZ:
			if (!DishScannedContacts.TryGetData(_strongestContact, out data))
			{
				return -1.0;
			}
			return (data.CurrentTimeTillResolve > 0f) ? (-1f) : _strongestContact.RequiredPadSize().y;
		case LogicType.MinimumWattsToContact:
			if (!DishScannedContacts.TryGetData(_strongestContact, out data))
			{
				return -1.0;
			}
			return (data.CurrentTimeTillResolve > 0f) ? (-1f) : _strongestContact.MinimumWattsToContact;
		case LogicType.WattsReachingContact:
			if (!DishScannedContacts.TryGetData(_strongestContact, out data))
			{
				return -1.0;
			}
			return (data.CurrentTimeTillResolve > 0f) ? (-1f) : GetWattageOnContact(_strongestContact);
		case LogicType.ContactTypeId:
			return _strongestContact?.DataInstance?.TraderData?.IdHash ?? 0;
		case LogicType.BestContactFilter:
			return _bestContactFilterReferenceID;
		case LogicType.Idle:
			return RotatableBehaviour.IsMoving ? 0f : 1f;
		case LogicType.ContactSlotIndex:
			return (_strongestContact?.ContactSlot != null) ? ContactSlot.ContactSlots.IndexOf(_strongestContact.ContactSlot) : (-1);
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.Horizontal:
		{
			value = RocketMath.ModuloCorrect(value, MaximumHorizontal);
			double num = value / MaximumHorizontal;
			if (!RocketMath.Approximately(num, RotatableBehaviour.TargetHorizontal, RotationTolerance))
			{
				RotatableBehaviour.TargetHorizontal = num;
			}
			break;
		}
		case LogicType.Vertical:
		{
			if (value < 0.0)
			{
				value = 0.0;
			}
			if (value > MaximumVertical)
			{
				value = MaximumVertical;
			}
			double num = value / MaximumVertical;
			if (!RocketMath.Approximately(num, RotatableBehaviour.TargetVertical, RotationTolerance))
			{
				RotatableBehaviour.TargetVertical = num;
			}
			break;
		}
		case LogicType.Setting:
			Setting = Mathf.Clamp((int)value, minWattage, maxWattage);
			break;
		case LogicType.HorizontalRatio:
			value = RocketMath.ModuloCorrect(value, 1.0);
			if (!RocketMath.Approximately(value, RotatableBehaviour.TargetHorizontal, RotationTolerance))
			{
				RotatableBehaviour.TargetHorizontal = value;
			}
			break;
		case LogicType.VerticalRatio:
			if (value < 0.0)
			{
				value = 0.0;
			}
			if (value > 1.0)
			{
				value = 1.0;
			}
			if (!RocketMath.Approximately(value, RotatableBehaviour.TargetVertical, RotationTolerance))
			{
				RotatableBehaviour.TargetVertical = value;
			}
			break;
		case LogicType.TargetPadIndex:
			if (value < 0.0)
			{
				value = 0.0;
			}
			_targetPadIndex = (int)value;
			break;
		case LogicType.BestContactFilter:
			_bestContactFilterReferenceID = (long)value;
			break;
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		ProcessInterrogatingContact(GameManager.LastTickTimeSeconds);
		if (!Powered || !OnOff)
		{
			_isDirty = true;
			return;
		}
		ScanForDishContacts();
		if (RotatableBehaviour.IsMoving || _isDirty)
		{
			InitContactResolveTimers();
		}
		else
		{
			ResolveDishContacts(GameManager.LastTickTimeSeconds);
		}
		OnSignalsUpdated?.Invoke(this);
		_isDirty = false;
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 512;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new SatelliteDishSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is SatelliteDishSaveData satelliteDishSaveData)
		{
			Horizontal = satelliteDishSaveData.Horizontal;
			Vertical = satelliteDishSaveData.Vertical;
			Setting = satelliteDishSaveData.Setting;
			RotatableBehaviour.TargetHorizontal = satelliteDishSaveData.TargetHorizontal;
			RotatableBehaviour.TargetVertical = satelliteDishSaveData.TargetVertical;
			_targetPadIndex = satelliteDishSaveData.TargetPadIndex;
			_bestContactFilterReferenceID = satelliteDishSaveData.BestSignalIDFilter;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is SatelliteDishSaveData satelliteDishSaveData)
		{
			InitializeRotatableBehaviour();
			satelliteDishSaveData.Horizontal = Horizontal;
			satelliteDishSaveData.Vertical = Vertical;
			satelliteDishSaveData.Setting = Setting;
			satelliteDishSaveData.TargetHorizontal = RotatableBehaviour.TargetHorizontal;
			satelliteDishSaveData.TargetVertical = RotatableBehaviour.TargetVertical;
			satelliteDishSaveData.TargetPadIndex = _targetPadIndex;
			satelliteDishSaveData.BestSignalIDFilter = _bestContactFilterReferenceID;
		}
	}
}
