using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Objects.Items;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Recycler : DeviceImportExport
{
	public static Dictionary<int, ReagentMixture> RecycleRecipes = new Dictionary<int, ReagentMixture>();

	private static float _recycleRatio = 0.5f;

	private ReagentMixture _currentMix;

	private ReagentMixture _ratioMix;

	private object _occupantLock = new object();

	public Ore SlagPrefab;

	[ReadOnly]
	private DynamicThing _occupant;

	private int _idleTicks;

	private bool _exportMaxOre;

	private bool _exportOre;

	[Tooltip("The collider for interior display")]
	public Collider WindowPanel;

	public static readonly int ActivateRecyclerHash = Animator.StringToHash("ActivateRecycler");

	public override bool HasReadableReagentMixture => true;

	public static bool AddRecycleRecipe(int hash, ReagentMixture contents, bool authored = false, float recycleRatioOverride = 0f)
	{
		if (RecycleRecipes.ContainsKey(hash))
		{
			if (authored)
			{
				RecycleRecipes[hash] = contents;
				return true;
			}
			return false;
		}
		RecycleRecipes.Add(hash, contents * ((recycleRatioOverride > 0f) ? recycleRatioOverride : _recycleRatio));
		return true;
	}

	public static void RemoveRecipe(int hash)
	{
		RecycleRecipes.Remove(hash);
	}

	public Ore CreateOutput(Connection outputConnection)
	{
		_ratioMix = ReagentMixture.GetRatioMixture();
		ReagentMixture reagentMixture = new ReagentMixture(_ratioMix) * Math.Min(ReagentMixture.TotalReagents, SlagPrefab.MaxQuantity);
		int num = (int)reagentMixture.TotalReagents;
		if (num < 1)
		{
			ReagentMixture.Clear();
			return null;
		}
		ReagentMixture.Subtract(reagentMixture);
		Ore ore = Thing.Create<Ore>(SlagPrefab, outputConnection.Transform);
		ore.ParentSlot = null;
		ore.SetQuantity(num);
		ore.QuantitySmelted = num;
		ore.CreatedReagentMixture = _ratioMix;
		OnServer.MoveToSlot(ore, ExportSlot);
		return ore;
	}

	public override void Awake()
	{
		base.Awake();
		ReagentMixture = new ReagentMixture(this);
	}

	private bool Recycle()
	{
		DynamicThing occupant;
		lock (_occupantLock)
		{
			occupant = _occupant;
			if (occupant == null)
			{
				return false;
			}
		}
		return RecycleThing(occupant);
	}

	private bool RecycleThing(DynamicThing thing)
	{
		if (thing.HasSlots)
		{
			foreach (Slot slot in thing.Slots)
			{
				if (!(slot.Occupant == null))
				{
					return RecycleThing(slot.Occupant);
				}
			}
		}
		_currentMix = GetSafeMixture(thing);
		if (_currentMix != null && _currentMix.TotalReagents > 0.0)
		{
			ReagentMixture?.Add(_currentMix);
		}
		lock (_occupantLock)
		{
			thing.Recycle();
		}
		return true;
	}

	private static ReagentMixture GetSafeMixture(DynamicThing occupant)
	{
		ReagentMixture value = null;
		RecycleRecipes.TryGetValue(occupant.PrefabHash, out value);
		if (occupant is SpaceOre spaceOre)
		{
			value = new ReagentMixture(spaceOre.CreatedReagentMixture);
			value /= 2.0;
		}
		else if (occupant is Ore ore)
		{
			value = ore.CreatedReagentMixture;
		}
		else if (occupant is Wreckage wreckage)
		{
			value = wreckage.CreatedReagentMixture;
		}
		else if (occupant is Plant plant)
		{
			value = new ReagentMixture(new Biomass(plant.BiomassValue));
		}
		else if (occupant is DecayedFood decayedFood)
		{
			value = new ReagentMixture(new Biomass(decayedFood.BiomassValue));
		}
		else if (occupant is EggShell eggShell)
		{
			value = new ReagentMixture(new Biomass(eggShell.BiomassValue));
		}
		else if (occupant is Hay hay)
		{
			value = new ReagentMixture(new Biomass(hay.BiomassValue));
		}
		else if (occupant is INutrition)
		{
			value = null;
		}
		return value;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		lock (_occupantLock)
		{
			_occupant = ImportSlot.Occupant;
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		lock (_occupantLock)
		{
			_occupant = ImportSlot.Occupant;
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!GameManager.RunSimulation || !OnOff || !Powered || _occupant != null)
		{
			return;
		}
		if (ReagentMixture.TotalReagents >= 50.0)
		{
			_exportMaxOre = true;
		}
		else if (_idleTicks > 5)
		{
			if (ReagentMixture.TotalReagents >= 1.0)
			{
				_exportOre = true;
				return;
			}
			if (Importing == 1)
			{
				OnServer.Interact(base.InteractImport, 0);
			}
			_idleTicks = 0;
		}
		_idleTicks++;
		if (Activate == 1)
		{
			OnServer.Interact(base.InteractActivate, 0);
		}
	}

	protected override void OnServerImportTick()
	{
		if (!base.IsStructureCompleted)
		{
			return;
		}
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
		if (!OnOff || !Powered)
		{
			return;
		}
		if (_occupant != null && base.IsImportClosed)
		{
			if (Activate != 1)
			{
				OnServer.Interact(base.InteractActivate, 1);
			}
			OnServer.Interact(base.InteractImport, 1);
			Recycle();
			_idleTicks = 0;
		}
		if (CanCompleteImport && ImportingThing == null)
		{
			OnServer.Interact(base.InteractImport, 0);
		}
	}

	protected override void OnServerExportTick()
	{
		if (!GameManager.RunSimulation || !OnOff || !Powered)
		{
			return;
		}
		if ((_exportOre || _exportMaxOre) && IsNextExportReady)
		{
			CreateOutput(base.ExportConnection);
			if (ReagentMixture.TotalReagents < 1.0)
			{
				_exportOre = false;
			}
			if (ReagentMixture.TotalReagents < 50.0)
			{
				_exportMaxOre = false;
			}
		}
		if (CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
			_idleTicks = 0;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		if (hitCollider == WindowPanel)
		{
			string text = string.Empty;
			if (ImportSlot.Occupant != null)
			{
				text += $"Recycling {ImportSlot.Occupant.ToTooltip()}";
			}
			string text2 = ReagentMixture.ToString();
			text += text2;
			result.Title = DisplayName;
			result.Action = Localization.GetInterface("Contents");
			result.State = text;
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public void StartRecycleSound()
	{
		PlaySound(ActivateRecyclerHash);
	}

	public void StopRecycleSound()
	{
		StopSound(ActivateRecyclerHash);
	}
}
