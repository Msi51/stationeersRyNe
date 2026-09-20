using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Networks;

namespace Objects.Rockets.UI.Models;

public static class RocketUIModelBuilder
{
	public static bool Build(RocketMotherboard motherboard, out RocketUIModel model)
	{
		model = new RocketUIModel
		{
			LogicControlModel = new LogicControlModel
			{
				Devices = new List<DeviceModel>(),
				LogicValues = new List<LogicValueModel>()
			},
			RocketPanelModel = new RocketPanelModel
			{
				ConnectedRocketModels = new List<ConnectedRocketModel>()
			},
			MapPanelModel = default(MapPanelModel)
		};
		ConnectedRocketInfo connectedRocketInfo = motherboard.SelectedRocket();
		ILogicable logicable = motherboard.SelectedLogicable();
		if (logicable != null && connectedRocketInfo?.DownLink?.DataCableNetwork != null)
		{
			model.LogicControlModel.SelectedDeviceReferenceId = logicable.ReferenceId;
			List<Device> list = new List<Device>(connectedRocketInfo.DownLink.DataCableNetwork.DeviceList);
			list.Sort(CompareDevices);
			foreach (Device item in list)
			{
				bool pinned = motherboard.IsDevicePinned(item);
				model.LogicControlModel.Devices.Add(new DeviceModel
				{
					Device = item,
					ReferenceId = item.ReferenceId,
					DisplayName = item.DisplayName,
					CanWriteOnOff = item.CanLogicWrite(LogicType.On),
					OnOff = item.OnOff,
					Powered = item.Powered,
					HasPowerState = item.HasPowerState,
					Error = (item.Error != 0),
					Pinned = pinned
				});
			}
			for (int num = EnumCollections.LogicTypes.Length - 1; num >= 0; num--)
			{
				LogicType logicType = EnumCollections.LogicTypes.Values[num];
				if (logicable.CanLogicRead(logicType))
				{
					bool pinned2 = motherboard.IsLogicValuePinned(logicable, logicType);
					model.LogicControlModel.LogicValues.Add(new LogicValueModel
					{
						DeviceReferenceId = logicable.ReferenceId,
						DisplayName = logicType.GetName(),
						Pinned = pinned2,
						LogicType = logicType,
						CanLogicWrite = logicable.CanLogicWrite(logicType),
						Value = motherboard.SelectedDeviceLogicValues[(int)logicType]
					});
				}
			}
		}
		BuildRocketModel(ref model.RocketPanelModel.SelectedRocketModel, connectedRocketInfo);
		BuildMapModel(ref model.MapPanelModel, connectedRocketInfo);
		return true;
	}

	private static void BuildMapModel(ref MapPanelModel model, ConnectedRocketInfo selectedRocket)
	{
		model.CurrentRocketHasDestination = selectedRocket != null && selectedRocket.Avionics != null && selectedRocket.Avionics.GetTarget() != null;
	}

	private static string GetNumberText(float value, string unit = "", string color = "yellow")
	{
		string text = ((!string.IsNullOrWhiteSpace(unit)) ? (" " + unit) : string.Empty);
		return (StringManager.Get(value) + text).AsColor(color);
	}

	private static string GetNumberText(float value, float lastValue, string unit)
	{
		string text = ((!string.IsNullOrWhiteSpace(unit)) ? (" " + unit) : string.Empty);
		string text2 = (StringManager.Get(value) + text).AsColor("yellow");
		string text3 = ("(" + StringManager.Get(lastValue) + text + ")").AsColor("grey");
		return text2 + " " + text3;
	}

	private static void BuildRocketModel(ref RocketModel model, ConnectedRocketInfo connectedRocketInfo)
	{
		RocketAvionicsDevice rocketAvionicsDevice = connectedRocketInfo?.Avionics;
		if (!(rocketAvionicsDevice == null))
		{
			model.ReferenceId = rocketAvionicsDevice.Rocket.ReferenceId;
			model.DisplayName = rocketAvionicsDevice.Rocket.DisplayName;
			model.AutoLand = rocketAvionicsDevice.Rocket.AutomatedLanding;
			model.AutoLandConfidenceRatio = rocketAvionicsDevice.GetAutoLandConfidenceRatio(out var minRequiredThrust, out var expectedThrust);
			model.ExpectedMaxThrustDuringAutoland = expectedThrust;
			model.AutoLandConfidenceString = (rocketAvionicsDevice.Rocket.AutomatedLanding ? Rocket.AutoLandConfidenceString(model.AutoLandConfidenceRatio) : GameStrings.NotApplicableString.DisplayString);
			string text = Rocket.AutoLandConfidenceColor(model.AutoLandConfidenceRatio);
			model.AutoLandConfidenceToolTip = "<color=" + text + ">" + model.AutoLandConfidenceString + "</color>";
			model.RequiredThrustToAutoLand = minRequiredThrust;
			model.ReEntryAltitude = Rocket.ReEntryProfiles[rocketAvionicsDevice.Rocket.ReEntryProfile];
			SpaceMapNode currentNode = rocketAvionicsDevice.GetCurrentNode();
			model.CurrentLocationName = ((currentNode != null) ? currentNode.DisplayName : GameStrings.None.DisplayString);
			model.TargetLocationName = rocketAvionicsDevice.GetTarget()?.DisplayName ?? GameStrings.None.DisplayString;
			model.NextLocationName = rocketAvionicsDevice.GetNextNode()?.DisplayName ?? GameStrings.None.DisplayString;
			float nextEta = rocketAvionicsDevice.GetNextEta();
			model.NextLocationEta = GetNumberText(nextEta, "s");
			float num = rocketAvionicsDevice.GetEta();
			bool flag = rocketAvionicsDevice.Rocket.RocketState == RocketState.Landing && num < 0f;
			if (flag)
			{
				num = rocketAvionicsDevice.GetTimeToApex();
			}
			model.TargetLocationEta = GetNumberText(num, "s");
			string targetLocationEtaLabel = ((rocketAvionicsDevice.Rocket.RocketState != RocketState.Landing) ? GameStrings.TimeToArrival.DisplayString : (flag ? GameStrings.TimeToArrest.DisplayString : GameStrings.TimeToImpact.DisplayString));
			model.TargetLocationEtaLabel = targetLocationEtaLabel;
			float getImpactVelocity = rocketAvionicsDevice.GetImpactVelocity;
			string text2 = ((getImpactVelocity > -4f) ? ((!(getImpactVelocity > -2f)) ? "yellow" : "green") : ((!(getImpactVelocity > -25f)) ? "red" : "orange"));
			string color = text2;
			string impactVelocity = ((!float.IsNaN(getImpactVelocity) && getImpactVelocity < 0f) ? GetNumberText(getImpactVelocity, "m/s", color) : GameStrings.NotApplicableString.DisplayString);
			model.ImpactVelocity = impactVelocity;
			float velocity = rocketAvionicsDevice.GetVelocity();
			model.Velocity = GetNumberText(velocity, "m/s");
			RocketState? rocketState = rocketAvionicsDevice.Rocket?.RocketState;
			string velocityLabel = ((!rocketState.HasValue || rocketState != RocketState.InSpace) ? GameStrings.GroundVelocity.DisplayString : GameStrings.TransferVelocity.DisplayString);
			model.VelocityLabel = velocityLabel;
			model.Acceleration = GetNumberText(rocketAvionicsDevice.GetAcceleration(), rocketAvionicsDevice.GetLastCalculatedAcceleration(), "m/s²");
			float engineAcceleration = rocketAvionicsDevice.GetEngineAcceleration();
			model.EngineAcceleration = GetNumberText(engineAcceleration, "m/s²");
			(float value, string description) gravity = rocketAvionicsDevice.Rocket.GetGravity();
			float item = gravity.value;
			string item2 = gravity.description;
			string numberText = GetNumberText(item, "m/s²");
			model.Gravity = numberText + " (" + item2 + ")";
			float apexAltitude = rocketAvionicsDevice.GetApexAltitude();
			string apex = ((rocketAvionicsDevice.Rocket.RocketState == RocketState.Landing && !float.IsNaN(apexAltitude) && !float.IsNegativeInfinity(apexAltitude)) ? GetNumberText(apexAltitude, "m") : GameStrings.NotApplicableString.DisplayString);
			model.Apex = apex;
			float altitude = rocketAvionicsDevice.GetAltitude();
			model.Altitude = ((altitude < 0f) ? GameStrings.Orbit.DisplayString : GetNumberText(altitude, "m"));
			float targetVelocity = rocketAvionicsDevice.GetTargetVelocity();
			model.TargetVelocity = GetNumberText(targetVelocity, "m/s");
			float fuelTime = rocketAvionicsDevice.GetFuelTime();
			string text3 = ((fuelTime > 0f) ? GetNumberText(fuelTime, "s") : GameStrings.NotApplicableString.DisplayString);
			string text4 = ("(" + StringManager.Get(rocketAvionicsDevice.GetLastCalculatedFuelTime()) + " s)").AsColor("grey");
			model.FuelTime = text3 + " " + text4;
			float mass = rocketAvionicsDevice.GetMass();
			model.Mass = GetNumberText(mass, "kg");
			float dryMass = rocketAvionicsDevice.GetDryMass();
			model.DryMass = GetNumberText(dryMass, "kg");
			float value = rocketAvionicsDevice.GetThrust() / 1000f;
			float lastValue = rocketAvionicsDevice.GetLastCalculatedThrust() / 1000f;
			model.Thrust = GetNumberText(value, lastValue, "kN");
			string numberText2 = GetNumberText(rocketAvionicsDevice.GetWeight() / 1000f, "kN");
			model.Weight = numberText2 + " (" + item2 + ")";
			float thrustToWeight = rocketAvionicsDevice.GetThrustToWeight();
			float lastCalculatedThrustToWeight = rocketAvionicsDevice.GetLastCalculatedThrustToWeight();
			string text5 = ("(" + StringManager.Get(lastCalculatedThrustToWeight) + ")").AsColor("grey");
			model.ThrustToWeight = GetNumberText(thrustToWeight) + " (" + item2 + ") " + text5;
			float totalMolesVolatiles = rocketAvionicsDevice.GetTotalMolesVolatiles();
			model.Volatiles = GetNumberText(totalMolesVolatiles, "mol");
			float totalMolesOxidizer = rocketAvionicsDevice.GetTotalMolesOxidizer();
			model.Oxidizer = GetNumberText(totalMolesOxidizer, "mol");
			int batteryPercentage = rocketAvionicsDevice.GetBatteryPercentage();
			model.BatteryPercentage = GetNumberText(batteryPercentage, "%");
			StringBuilder sb = new StringBuilder();
			model.MassToolTip = BuildMassTooltip(sb, rocketAvionicsDevice.RocketNetwork);
			model.DryMassToolTip = BuildDryMassTooltip(sb, rocketAvionicsDevice.RocketNetwork);
		}
	}

	private static string BuildMassTooltip(StringBuilder sb, RocketNetwork rocketNetwork)
	{
		sb.Clear();
		sb.Append(GameStrings.TotalMass);
		sb.Append(StringManager.Get(rocketNetwork.CombinedMass())).Append("kg");
		sb.Append("\n");
		sb.Append(GameStrings.FuelMass);
		sb.Append(StringManager.Get(rocketNetwork.GasMass)).Append("kg");
		sb.Append("\n");
		sb.Append(GameStrings.RocketDryMass).Append(" ");
		sb.Append(StringManager.Get(rocketNetwork.DryMass)).Append("kg");
		return sb.ToString();
	}

	private static string BuildDryMassTooltip(StringBuilder sb, RocketNetwork rocketNetwork)
	{
		sb.Clear();
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		for (int num4 = rocketNetwork.StructureList.Count - 1; num4 >= 0; num4--)
		{
			if (rocketNetwork.StructureList[num4] is IRocketMassContributor rocketMassContributor)
			{
				num += rocketMassContributor.MassContribution;
			}
		}
		for (int num5 = rocketNetwork.Internals.Count - 1; num5 >= 0; num5--)
		{
			IRocketInternals rocketInternals = rocketNetwork.Internals[num5];
			if (rocketInternals != null)
			{
				if (rocketInternals is RocketEngineBase rocketEngineBase)
				{
					num2 += rocketEngineBase.MassContribution;
				}
				else if (rocketInternals is IRocketMassContributor { MassContribution: >0f } rocketMassContributor2)
				{
					num3 += rocketMassContributor2.MassContribution;
				}
			}
		}
		sb.Append(GameStrings.RocketDryMassTotal);
		sb.Append(StringManager.Get(rocketNetwork.DryMass)).Append("kg");
		sb.Append("\n");
		sb.Append(GameStrings.FuselageMass);
		sb.Append(StringManager.Get(num)).Append("kg");
		sb.Append("\n");
		sb.Append(GameStrings.EngineMass);
		sb.Append(StringManager.Get(num2)).Append("kg");
		sb.Append("\n");
		sb.Append(GameStrings.PayloadMass);
		sb.Append(StringManager.Get(num3)).Append("kg");
		return sb.ToString();
	}

	private static int CompareDevices(Device a, Device b)
	{
		if (a.PrefabHash != b.PrefabHash)
		{
			return a.PrefabHash.CompareTo(b.PrefabHash);
		}
		return a.DisplayName.CompareTo(b.DisplayName);
	}
}
