using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using UI.ImGuiUi;
using UnityEngine;

namespace Networks;

public abstract class AtmosphericsNetwork : StructureNetwork
{
	private static class PipeNetworkStrings
	{
		public const string PipeNetwork = "PipeNetwork ->";

		public const string NetworkId = "NetworkId:";

		public const string Volume = "Volume:";

		public const string Temperature = "Temperature:";

		public const string Pressure = "Pressure:";

		public const string TotalMoles = "Total Moles:";

		public const string Energy = "Energy:";

		public const string StateEnergy = "StateEnergy:";

		public const string TotalEnergy = "Total Energy:";

		public const string LatentEnergyDelta = "Latent Energy Delta:";
	}

	private Atmosphere _atmosphere;

	private INetworkedAtmospherics _weakestMember;

	private bool _weakestMemberScannedThisTick;

	private bool _stressed;

	private float _lastSentEnergyRadiated = float.NaN;

	private float _lastSentEnergyConvected = float.NaN;

	private const float MIN_DAMAGE_PER_TICK = 0.2f;

	private const float MIN_FROZEN_DAMAGE_PER_TICK = 0.5f;

	private const float MAX_DAMAGE_PER_TICK = 10f;

	private const float DAMAGE_OFFSET = -0.8f;

	public const float MIN_FROZEN_MOLES_TO_DAMAGE_PER_VOLUME = 0.05f;

	public Atmosphere Atmosphere => _atmosphere;

	public Pipe.ContentType NetworkContentType { get; set; }

	public float EnergyConvected { get; set; }

	public float EnergyRadiated { get; set; }

	public override bool IsAwaitingEvent
	{
		get
		{
			if (Atmosphere != null)
			{
				return Atmosphere.IsAwaitingEvent;
			}
			return false;
		}
	}

	protected bool Stressed
	{
		get
		{
			return _stressed;
		}
		set
		{
			if (value == _stressed)
			{
				return;
			}
			for (int num = base.StructureList.Count - 1; num >= 0; num--)
			{
				if (num < base.StructureList.Count && base.StructureList[num] is INetworkedAtmospherics networkedAtmospherics)
				{
					networkedAtmospherics.GetAsThing.Stressed = value;
				}
			}
			_stressed = value;
		}
	}

	public virtual bool PreventStateChange => false;

	public bool HasNetworkFault { get; private set; }

	public INetworkedAtmospherics GetWeakestMember(Span<ThingRef<IReferencable>> weakestMemberBuffer)
	{
		int length = 0;
		if (_weakestMemberScannedThisTick)
		{
			return _weakestMember ?? PickRandomWeakestFallback();
		}
		if (_weakestMember != null && _weakestMember.StructureNetwork == this && _weakestMember.IsBurst == PipeBurst.None && HasOpenGridOrIsPipeWithOpenSurroundings(_weakestMember))
		{
			return _weakestMember;
		}
		lock (base.StructureList)
		{
			foreach (INetworkedStructure structure in base.StructureList)
			{
				weakestMemberBuffer[length++] = new ThingRef<IReferencable>(structure);
			}
		}
		float num = 0f;
		INetworkedAtmospherics networkedAtmospherics = null;
		Span<ThingRef<IReferencable>> span = weakestMemberBuffer;
		Span<ThingRef<IReferencable>> span2 = span.Slice(0, length);
		span = span2;
		for (int i = 0; i < span.Length; i++)
		{
			ThingRef<IReferencable> thingRef = span[i];
			if (thingRef.TryGet<INetworkedAtmospherics>(out var found) && HasOpenGridOrIsPipeWithOpenSurroundings(found) && (int)found.IsBurst <= 0)
			{
				float totalRatioClamped = found.GetAsThing.DamageState.TotalRatioClamped;
				if (totalRatioClamped > num)
				{
					networkedAtmospherics = found;
					num = totalRatioClamped;
				}
			}
		}
		if (networkedAtmospherics == null)
		{
			networkedAtmospherics = span2.Pick().Get<INetworkedAtmospherics>();
		}
		_weakestMember = networkedAtmospherics;
		return _weakestMember;
	}

	private bool HasOpenGridOrIsPipeWithOpenSurroundings(INetworkedAtmospherics atmos)
	{
		if (!atmos.HasOpenGrid)
		{
			if (atmos.GetAsThing is Pipe)
			{
				return HasOpenSurroundings(atmos);
			}
			return false;
		}
		return true;
	}

	private bool HasOpenSurroundings(INetworkedAtmospherics atmos)
	{
		lock (atmos.CurrentGrids)
		{
			for (int i = 0; i < atmos.CurrentGrids.Count; i++)
			{
				if (GridController.World.CanContainAtmos(atmos.CurrentGrids[i]))
				{
					return true;
				}
			}
		}
		return false;
	}

	private INetworkedAtmospherics PickRandomWeakestFallback()
	{
		lock (base.StructureList)
		{
			return (base.StructureList.Count == 0) ? null : (base.StructureList.Pick() as INetworkedAtmospherics);
		}
	}

	public AtmosphericsNetwork(long referenceId = 0L)
		: base(referenceId)
	{
		if (GameManager.GameState != GameState.Loading && GameManager.RunSimulation)
		{
			_atmosphere = new Atmosphere(this, 0L);
		}
	}

	public void RefreshNetworkVolume()
	{
		if (Atmosphere != null)
		{
			Atmosphere.Volume = GetNetworkVolume();
		}
	}

	public override void ValidateOnLoad(int saveVersion)
	{
		RefreshNetworkVolume();
		base.ValidateOnLoad(saveVersion);
		if (Atmosphere == null)
		{
			ConsoleWindow.PrintAction("AtmosphericsNetwork " + StringManager.Get((int)base.ReferenceId) + " does not have a serialized atmosphere due to a corrupted save. Creating a new one.");
			AssignAtmosphere(new Atmosphere(this, 0L)
			{
				Volume = GetNetworkVolume()
			});
		}
	}

	protected virtual PressurekPa MaxPressureSafe()
	{
		return NetworkContentType switch
		{
			Pipe.ContentType.Liquid => Chemistry.Limits.MAXPressureLiquidPipe * Thing.StressedRatio, 
			_ => Chemistry.Limits.MAXPressureGasPipe * Thing.StressedRatio, 
		};
	}

	public virtual void AssignAtmosphere(Atmosphere atmosphere)
	{
		_atmosphere = atmosphere;
	}

	protected override void OnDeregister()
	{
		base.OnDeregister();
		AtmosphericsManager.DeregisterFromMainThead(Atmosphere);
	}

	public override bool Merge(StructureNetwork oldNetwork)
	{
		if (!(oldNetwork is AtmosphericsNetwork atmosphericsNetwork) || atmosphericsNetwork == this)
		{
			return false;
		}
		if (GameManager.RunSimulation)
		{
			AtmosphericEventInstance.CreateAdd(Atmosphere, atmosphericsNetwork.Atmosphere.GasMixture);
		}
		return base.Merge(atmosphericsNetwork);
	}

	public override bool Add(INetworkedStructure iNetworkedStructure)
	{
		if (!(iNetworkedStructure is INetworkedAtmospherics networkedAtmospherics))
		{
			return false;
		}
		if (NetworkContentType == Pipe.ContentType.Unknown)
		{
			NetworkContentType = networkedAtmospherics.PipeContentType;
		}
		if (GameManager.GameState != GameState.Loading && GameManager.RunSimulation && Atmosphere == null)
		{
			AssignAtmosphere(new Atmosphere(this, 0L)
			{
				Volume = GetNetworkVolume()
			});
		}
		if (!CanContainPipe(networkedAtmospherics))
		{
			Debug.LogError("PipeNetwork.Add - Failed to add pipe. " + $"{networkedAtmospherics.PipeContentType} is not {NetworkContentType}");
			return false;
		}
		if (!base.Add(iNetworkedStructure))
		{
			return false;
		}
		if (GameManager.GameState == GameState.Running && GameManager.RunSimulation)
		{
			Atmosphere.Volume += networkedAtmospherics.Volume;
		}
		return true;
	}

	private VolumeLitres GetNetworkVolume()
	{
		VolumeLitres zero = VolumeLitres.Zero;
		foreach (INetworkedStructure structure in base.StructureList)
		{
			if (structure is INetworkedAtmospherics networkedAtmospherics)
			{
				zero += networkedAtmospherics.Volume;
			}
		}
		return zero;
	}

	public bool CanContainPipe(INetworkedAtmospherics pipe)
	{
		if (pipe != null)
		{
			return pipe.PipeContentType == NetworkContentType;
		}
		return false;
	}

	public override bool Remove(INetworkedStructure iNetworkedStructure)
	{
		if (base.Remove(iNetworkedStructure))
		{
			if (GameManager.GameState == GameState.Running && GameManager.RunSimulation && iNetworkedStructure is INetworkedAtmospherics networkedAtmospherics)
			{
				Atmosphere.Volume -= networkedAtmospherics.Volume;
			}
			return true;
		}
		return false;
	}

	protected override bool IsUpdateDirty()
	{
		if (EnergyRadiated == _lastSentEnergyRadiated)
		{
			return EnergyConvected != _lastSentEnergyConvected;
		}
		return true;
	}

	protected override void BuildUpdate(RocketBinaryWriter writer)
	{
		writer.WriteSingle(EnergyRadiated);
		writer.WriteSingle(EnergyConvected);
		_lastSentEnergyRadiated = EnergyRadiated;
		_lastSentEnergyConvected = EnergyConvected;
	}

	protected override void ProcessUpdate(RocketBinaryReader reader)
	{
		EnergyRadiated = reader.ReadSingle();
		EnergyConvected = reader.ReadSingle();
	}

	public void BeforeAtmosphericTick()
	{
		if (Atmosphere != null)
		{
			HasNetworkFault = false;
		}
	}

	public void OnAtmosphericTick(Span<ThingRef<IReferencable>> buffer)
	{
		if (Atmosphere == null)
		{
			return;
		}
		_weakestMemberScannedThisTick = false;
		if (GameManager.RunSimulation)
		{
			if (ScanStructuresAndEvaluate())
			{
				ApplyPressureDamageToWeakest();
			}
			EvaluateIncorrectMatterState(buffer);
		}
		else
		{
			EvaluateStressClient();
		}
	}

	public void SetNetworkFault(bool faultState)
	{
		HasNetworkFault = faultState;
	}

	private bool ScanStructuresAndEvaluate()
	{
		PressurekPa pressureGassesAndLiquids = Atmosphere.PressureGassesAndLiquids;
		float stressedRatio = Thing.StressedRatio;
		AtmosphericsController world = AtmosphericsController.World;
		bool result = false;
		INetworkedAtmospherics networkedAtmospherics = null;
		INetworkedAtmospherics networkedAtmospherics2 = null;
		float num = 0f;
		INetworkedAtmospherics weakestMember = _weakestMember;
		bool flag = false;
		lock (base.StructureList)
		{
			for (int num2 = base.StructureList.Count - 1; num2 >= 0; num2--)
			{
				if (base.StructureList[num2] is INetworkedAtmospherics { GetAsThing: var getAsThing } networkedAtmospherics3)
				{
					GridController gridController = getAsThing.GridController;
					PressurekPa maxPressure = networkedAtmospherics3.MaxPressure;
					PressurekPa pressurekPa = maxPressure * stressedRatio;
					bool flag2 = false;
					bool flag3 = false;
					bool flag4 = false;
					lock (networkedAtmospherics3.CurrentGrids)
					{
						List<WorldGrid> currentGrids = networkedAtmospherics3.CurrentGrids;
						int count = currentGrids.Count;
						for (int i = 0; i < count; i++)
						{
							WorldGrid worldGrid = currentGrids[i];
							if (gridController.CanContainAtmos(worldGrid))
							{
								flag4 = true;
								Atmosphere atmosphere = world.SampleGlobalAtmosphere(worldGrid);
								PressurekPa pressurekPa2 = RocketMath.Abs(pressureGassesAndLiquids - atmosphere.PressureGassesAndLiquids);
								if (pressurekPa2 > pressurekPa)
								{
									flag2 = true;
								}
								if (pressurekPa2 > maxPressure)
								{
									flag3 = true;
								}
							}
						}
					}
					if (flag2 != networkedAtmospherics3.Stressed)
					{
						networkedAtmospherics3.Stressed = flag2;
					}
					if (flag3)
					{
						result = true;
						if (networkedAtmospherics == null)
						{
							networkedAtmospherics = networkedAtmospherics3;
						}
					}
					if ((int)networkedAtmospherics3.IsBurst <= 0 && (networkedAtmospherics3.HasOpenGrid || (getAsThing is Pipe && flag4)))
					{
						if (networkedAtmospherics3 == weakestMember)
						{
							flag = true;
						}
						float totalRatioClamped = getAsThing.DamageState.TotalRatioClamped;
						if (totalRatioClamped > num)
						{
							networkedAtmospherics2 = networkedAtmospherics3;
							num = totalRatioClamped;
						}
					}
				}
			}
		}
		_weakestMember = (flag ? weakestMember : (networkedAtmospherics2 ?? networkedAtmospherics));
		_weakestMemberScannedThisTick = true;
		return result;
	}

	private void EvaluateStressClient()
	{
		for (int num = base.StructureList.Count - 1; num >= 0; num--)
		{
			if (base.StructureList[num] is INetworkedAtmospherics { HasOpenGrid: not false } networkedAtmospherics)
			{
				Atmosphere atmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(networkedAtmospherics.WorldGrid);
				PressurekPa pressurekPa = RocketMath.Abs(Atmosphere.PressureGassesAndLiquids - atmosphere.PressureGassesAndLiquids);
				networkedAtmospherics.Stressed = pressurekPa > networkedAtmospherics.MaxPressure * Thing.StressedRatio;
			}
		}
	}

	private void ApplyPressureDamageToWeakest()
	{
		INetworkedAtmospherics weakestMember = _weakestMember;
		if (weakestMember == null)
		{
			return;
		}
		Atmosphere atmosphere = AtmosphericsController.World.SampleGlobalAtmosphere(weakestMember.WorldGrid);
		if (atmosphere != null)
		{
			PressurekPa pressurekPa = RocketMath.Abs(atmosphere.PressureGassesAndLiquids - Atmosphere.PressureGassesAndLiquids);
			if (!(weakestMember.MaxPressure >= pressurekPa))
			{
				float value = Mathf.Clamp(MathF.Log((pressurekPa / weakestMember.MaxPressure).ToFloat(), 2f), 0.2f, 10f);
				weakestMember.GetAsThing.DamageState.Damage(ChangeDamageType.Increment, value, DamageUpdateType.Brute);
				weakestMember.DamageRecord |= PipeBurst.Pressure;
			}
		}
	}

	private MoleQuantity MinFrozenMolesToDamage()
	{
		return new MoleQuantity(0.05000000074505806 * Atmosphere.Volume.ToDouble());
	}

	private void EvaluateIncorrectMatterState(Span<ThingRef<IReferencable>> buffer)
	{
		if (Atmosphere.IsAboveArmstrong() || Atmosphere.TotalMolesLiquids > MinFrozenMolesToDamage())
		{
			GasMixture gasMixture = Atmosphere.GasMixture.CheckForFreezing(Atmosphere.PressureGasses);
			if (gasMixture.GetTotalMolesGassesAndLiquids > MinFrozenMolesToDamage())
			{
				INetworkedAtmospherics weakestMember = GetWeakestMember(buffer);
				if (weakestMember != null)
				{
					double value = Math.Log((gasMixture.GetTotalMolesGassesAndLiquids / MinFrozenMolesToDamage()).ToFloat(), 2.0);
					value = Math.Clamp(value, 0.5, 10.0);
					weakestMember.GetAsThing.DamageState.Damage(ChangeDamageType.Increment, (float)value, DamageUpdateType.Brute);
					weakestMember.DamageRecord |= PipeBurst.Solid;
				}
			}
		}
		switch (NetworkContentType)
		{
		case Pipe.ContentType.Gas:
		{
			float num = (Atmosphere.TotalVolumeLiquids / Atmosphere.Volume).ToFloat();
			if (num > 0.02f)
			{
				INetworkedAtmospherics weakestMember2 = GetWeakestMember(buffer);
				if (weakestMember2 != null)
				{
					double value2 = Math.Log10(num * 100f) + -0.800000011920929;
					value2 = Math.Clamp(value2, 0.20000000298023224, 10.0);
					weakestMember2.GetAsThing.DamageState.Damage(ChangeDamageType.Increment, (float)value2, DamageUpdateType.Brute);
					weakestMember2.DamageRecord |= PipeBurst.Liquid;
				}
			}
			break;
		}
		case Pipe.ContentType.Liquid:
		case Pipe.ContentType.All:
			break;
		}
	}

	protected virtual void Draw()
	{
	}

	public void OnImGuiDraw()
	{
		if (base.StructureList.Count == 0 || InventoryManager.ParentHuman == null || Atmosphere == null)
		{
			return;
		}
		Vector3 position = Vector3.zero;
		float num = float.PositiveInfinity;
		for (int i = 0; i < base.StructureList.Count; i++)
		{
			INetworkedAtmospherics networkedAtmospherics = (INetworkedAtmospherics)base.StructureList[i];
			if (networkedAtmospherics != null)
			{
				float num2 = Vector3.Distance(InventoryManager.ParentHuman.Position, networkedAtmospherics.GetAsThing.Position);
				if (num2 < num)
				{
					position = networkedAtmospherics.GetAsThing.ThingTransform.position;
					num = num2;
				}
				ImGuiExtensions.Rendering.RenderingColor = ImGuiExtensions.GetThingColour(networkedAtmospherics.GetAsThing);
				networkedAtmospherics.OnImGuiDraw();
				ImGuiExtensions.Rendering.RenderingColor = ImGuiExtensions.ColorConvertFloat4ToU32(Color.Lerp(networkedAtmospherics.GetAsThing.CustomColor.Color, Color.white, 0.2f));
				if (num2 < 2f)
				{
					ImGuiExtensions.Rendering.DrawTextInWorld("Convected: " + StringManager.Get(networkedAtmospherics.EnergyConvected) + "J \nRadiated: " + StringManager.Get(networkedAtmospherics.EnergyRadiated) + "J", networkedAtmospherics.GetAsThing.transform.position, (int)networkedAtmospherics.ReferenceId);
				}
			}
		}
		ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.White;
		ImGuiExtensions.Rendering.DrawTextInWorld(delegate
		{
			ImguiHelper.Text("PipeNetwork ->");
			ImguiHelper.Text("NetworkId:");
			ImguiHelper.SameLine();
			ImguiHelper.Text(StringManager.Get((int)base.ReferenceId));
			Draw();
			ImguiHelper.Text("Volume:");
			ImguiHelper.SameLine();
			ImguiHelper.Text(StringManager.Get(Mathf.RoundToInt(Atmosphere.Volume.ToFloat())));
			ImguiHelper.Text("Temperature:");
			ImguiHelper.SameLine();
			ImguiHelper.Text(StringManager.Get(Mathf.RoundToInt(Atmosphere.Temperature.ToFloat())));
			ImguiHelper.Text("Pressure:");
			ImguiHelper.SameLine();
			ImguiHelper.Text(StringManager.Get(Mathf.RoundToInt(Atmosphere.PressureGassesAndLiquids.ToFloat())));
			ImguiHelper.Text("Latent Energy Delta:");
			ImguiHelper.SameLine();
			ImguiHelper.Text(StringManager.Get((int)Atmosphere.LastTickLatentEnergy.ToFloat()));
			ImguiHelper.Text("Total Moles:");
			ImguiHelper.SameLine();
			ImguiHelper.Text(StringManager.Get((int)Atmosphere.TotalMoles.ToFloat()));
			ImguiHelper.Text("Energy:");
			ImguiHelper.SameLine();
			ImguiHelper.Text(StringManager.Get((int)Atmosphere.GasMixture.TotalEnergy.ToFloat()));
			ImguiHelper.Text("StateEnergy:");
			ImguiHelper.SameLine();
			ImguiHelper.Text(StringManager.Get((int)Atmosphere.GasMixture.GasStateEnergy().ToFloat()));
			ImguiHelper.Text("Total Energy:");
			ImguiHelper.SameLine();
			ImguiHelper.Text(StringManager.Get((int)Atmosphere.GasMixture.GasStateEnergy().ToFloat() + (int)Atmosphere.GasMixture.TotalEnergy.ToFloat()));
		}, position, (int)base.ReferenceId);
	}
}
