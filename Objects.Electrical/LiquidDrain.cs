using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Objects.Electrical;

public class LiquidDrain : SmallDeviceOutput
{
	[Header("Liquid Drain")]
	[SerializeField]
	[FormerlySerializedAs("MolesDrainedPerTick")]
	private float molesDrainedPerTick = 50f;

	public MoleQuantity MolesDrainedPerTick => new MoleQuantity(molesDrainedPerTick);

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (!OnOff || cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			return 0f;
		}
		return UsedPower;
	}

	public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
	{
		base.ReceivePower(cableNetwork, powerAdded);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (base.HasOpenGrid)
		{
			return passiveTooltip;
		}
		passiveTooltip.Title = DisplayName;
		passiveTooltip.Extended = GetExtendedText().ToString();
		return passiveTooltip;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (!base.GridController.CanContainAtmos(base.WorldGrid))
		{
			extendedText.AppendLine(GameStrings.DeviceWorldGridBlocked.AsString(ToTooltip()));
			return extendedText;
		}
		if (Cell.IsInCrewModule(base.WorldGrid, out var crewModule))
		{
			extendedText.AppendLine(GameStrings.DeviceOutputCrewModule.AsString(crewModule.ToTooltip()));
		}
		return extendedText;
	}

	public override void OnPreAtmosphere()
	{
		base.OnPreAtmosphere();
		if (OnOff && Powered && IsOperable && ConnectedPipeNetwork.Atmosphere != null && ConnectedPipeNetwork.Atmosphere.GasMixture.GetTotalMolesLiquids > MoleQuantity.Zero)
		{
			Atmosphere atmosphere = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			GasMixture gasMixture = ConnectedPipeNetwork.Atmosphere.GasMixture.Remove(MolesDrainedPerTick, AtmosphereHelper.MatterState.All);
			if (gasMixture.IsValid)
			{
				atmosphere.Add(gasMixture);
			}
		}
	}
}
