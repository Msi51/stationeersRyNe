using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Objects.Items;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class FabricatorBase : DeviceImportExport, IWreckage
{
	protected Connection _inputPowerConnection;

	public static List<Device> AllFabricatorPrefabs = new List<Device>();

	private bool _needsValidate = true;

	public Collider InfoPanel;

	public Collider InfoPanel2;

	public Ore SlagPrefab;

	public FabricatorJob CurrentJob;

	private Ingot _importingIngot;

	public static readonly int ImportErrorHash = Animator.StringToHash("ImportError");

	public bool NeedsValidate
	{
		get
		{
			return _needsValidate;
		}
		set
		{
			if (value)
			{
				RefreshDynamicThings();
			}
			else
			{
				_needsValidate = false;
			}
		}
	}

	public override WreckageSize WreckageSize => WreckageSize.Medium;

	public override int WreckageQuantity => 2;

	public override DropType DropType => DropType.Ingot;

	public override bool CanBeginImport
	{
		get
		{
			if (base.CanBeginImport)
			{
				return _importingIngot != null;
			}
			return false;
		}
	}

	public override bool HasReadableReagentMixture => true;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(40f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Fabricators);
	}

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		if ((InfoPanel != null && hitCollider == InfoPanel) || (InfoPanel2 != null && hitCollider == InfoPanel2))
		{
			if (!base.CurrentBuildState.CanManufacture)
			{
				return base.GetPassiveTooltip(hitCollider);
			}
			result.Title = Localization.GetInterface("Contents");
			result.State = ReagentMixture.ToString();
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public virtual void RefreshDynamicThings()
	{
	}

	public void CollectResource(Consumable consumable)
	{
		ReagentMixture.Add(consumable.CreatedReagentMixture * consumable.Quantity);
		OnServer.Destroy(consumable);
	}

	public override void Awake()
	{
		base.Awake();
		ReagentMixture = new ReagentMixture(this);
		if (GetConnection(NetworkType.Power, ConnectionRole.None, out var connection))
		{
			_inputPowerConnection = connection;
		}
	}

	protected override void OnImportClosingComplete()
	{
		if (_importingIngot != null)
		{
			CollectResource(_importingIngot);
		}
		OnServer.Interact(base.InteractImport, 0);
	}

	protected override void OnServerImportTick()
	{
		if (base.IsStructureCompleted)
		{
			if (IsNextImportReady)
			{
				TryChuteImport();
			}
			if (CanBeginImport)
			{
				OnServer.Interact(base.InteractImport, 1);
			}
			if (CanCompleteImport && ImportingThing == null)
			{
				OnServer.Interact(base.InteractImport, 0);
			}
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		PlayImportErrorSound(newChild);
		if (GameManager.RunSimulation && newChild.ParentSlot == ImportSlot)
		{
			_importingIngot = ImportingThing as Ingot;
		}
	}

	public virtual void PlayImportErrorSound(DynamicThing newChild)
	{
		if (!GameManager.IsBatchMode && newChild.ParentSlot == ImportSlot && !(newChild is Ingot))
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(this, ImportErrorHash, base.InteractOnOff.Collider.transform.localPosition);
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		if (GameManager.RunSimulation && ImportingThing == previousChild)
		{
			_importingIngot = null;
		}
		base.OnChildExitInventory(previousChild);
	}
}
