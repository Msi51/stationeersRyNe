using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class DirectHeatExchanger : HeatExchangerBase, IRocketInternals, IRocketComponent
{
	[SerializeField]
	protected Collider infoPanel;

	[SerializeField]
	protected RocketInternalCellType rocketInternalCellType;

	[Header("HeatExchanger")]
	[SerializeField]
	private float HeatExchangeArea = 5f;

	private MoleEnergy _energyTransfer;

	private MoleEnergy EnergyTransfer
	{
		get
		{
			return _energyTransfer;
		}
		set
		{
			if (!RocketMath.Approximately(value, _energyTransfer))
			{
				_energyTransfer = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
			}
		}
	}

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.IsInputValid && base.IsInput2Valid;
			if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (GameManager.RunSimulation && HasErrorState && Error == 1 && flag)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return flag;
		}
	}

	public RocketInternalCellType InternalCellType => rocketInternalCellType;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == null)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		PassiveTooltip result = new PassiveTooltip(true);
		if (hitCollider != infoPanel)
		{
			return base.GetPassiveTooltip(hitCollider);
		}
		result.Title = DisplayName;
		result.Extended = GetExtendedText().ToString();
		return result;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		float num = EnergyTransfer.ToFloat();
		float value = Mathf.Abs(num);
		StringManager.AddKeyValueLine(extendedText, GameStrings.EnergyTransfer, value.ToStringPrefix("J", "yellow"));
		StringManager.AddKeyValueLine(extendedText, GameStrings.HeatExchangeArea, HeatExchangeArea.ToStringPrefix("m²", "yellow"));
		string text = EnumCollections.NetworkType.GetName(InputConnection.ConnectionType).AsColor("green");
		string text2 = EnumCollections.NetworkType.GetName(InputConnection2.ConnectionType).AsColor("green");
		if (!(num > float.Epsilon))
		{
			if (num < float.Epsilon)
			{
				extendedText.AppendLine(GameStrings.HeatExchangeFromTo.AsString(text2, text));
			}
		}
		else
		{
			extendedText.AppendLine(GameStrings.HeatExchangeFromTo.AsString(text, text2));
		}
		return extendedText;
	}

	public override void OnAtmosphericTick()
	{
		if (!IsOperable)
		{
			EnergyTransfer = MoleEnergy.Zero;
			return;
		}
		if (InputNetwork.Atmosphere == null || InputNetwork2.Atmosphere == null)
		{
			EnergyTransfer = MoleEnergy.Zero;
			return;
		}
		MoleEnergy moleEnergy = AtmosphereHelper.GetConvectionHeat(InputNetwork.Atmosphere, InputNetwork2.Atmosphere, HeatExchangeArea * HeatExchangerBase.HeatExchangeRatio(InputNetwork.Atmosphere, InputNetwork2.Atmosphere)) * AtmosphericsManager.Instance.TickSpeedSeconds;
		InputNetwork.Atmosphere.GasMixture.TransferEnergyTo(ref InputNetwork2.Atmosphere.GasMixture, moleEnergy);
		EnergyTransfer = moleEnergy;
	}

	protected override void CheckConnections()
	{
		INetworkedPipe iNetworkedPipe = InputConnection.GetINetworkedPipe();
		INetworkedPipe iNetworkedPipe2 = InputConnection2.GetINetworkedPipe();
		InputNetwork = iNetworkedPipe?.PipeNetwork;
		InputNetwork2 = iNetworkedPipe2?.PipeNetwork;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteFloatHalf(EnergyTransfer.ToFloat());
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			EnergyTransfer = new MoleEnergy(reader.ReadFloatHalf());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(EnergyTransfer.ToFloat());
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		EnergyTransfer = new MoleEnergy(reader.ReadSingle());
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}
}
