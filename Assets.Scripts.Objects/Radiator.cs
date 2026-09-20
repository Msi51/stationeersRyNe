using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects;

public class Radiator : DeviceInputOutput, IThermal
{
	[SerializeField]
	[FormerlySerializedAs("Volume")]
	private float volume = 200f;

	public override bool HasReadableAtmosphere => true;

	public VolumeLitres Volume => new VolumeLitres(volume);

	public override WreckageSize WreckageSize => WreckageSize.Medium;

	public override int WreckageQuantity => 2;

	private Vector3 FrameMountDirection
	{
		get
		{
			if (PlacementType != PlacementSnap.Grid)
			{
				return ThingTransform.forward;
			}
			return ThingTransform.up;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType - 354 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.EnergyConvected => EnergyConvected, 
			LogicType.EnergyRadiated => EnergyRadiated, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(20f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(6f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			Atmosphere obj = new Atmosphere(this, Volume, 0L)
			{
				ForceNonCachable = true
			};
			Atmosphere atmosphere = obj;
			base.InternalAtmosphere = obj;
		}
	}

	public override void OnPreAtmosphere()
	{
		BuildState currentBuildState = base.CurrentBuildState;
		List<BuildState> buildStates = BuildStates;
		if (currentBuildState == buildStates[buildStates.Count - 1] && base.IsInputValid)
		{
			AtmosphereHelper.Mix(InputNetwork.Atmosphere, base.InternalAtmosphere, AtmosphereHelper.MatterState.All);
		}
	}

	public override void OnAtmosphericTick()
	{
		BuildState currentBuildState = base.CurrentBuildState;
		List<BuildState> buildStates = BuildStates;
		if (currentBuildState == buildStates[buildStates.Count - 1] && base.IsOutputValid)
		{
			AtmosphereHelper.Mix(OutputNetwork.Atmosphere, base.InternalAtmosphere, AtmosphereHelper.MatterState.All);
		}
	}

	public override CanConstructInfo CanConstruct()
	{
		if (PlacementType == PlacementSnap.FaceMount)
		{
			return base.CanConstruct();
		}
		Vector3 worldPosition = base.ThingTransformPosition - FrameMountDirection * GridSize / 2f;
		Structure structure = base.GridController.Get<Structure>(worldPosition, StructureElement.Center);
		if ((bool)structure && structure.AllowMounting)
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}
}
