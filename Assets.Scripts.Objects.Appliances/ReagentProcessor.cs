using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Reagents;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Appliances;

public class ReagentProcessor : Appliance, IResourceConsumer, IReferencable, IEvaluable
{
	public static ReagentRecipeComparable RecipeComparable = new ReagentRecipeComparable("ReagentProcessor");

	public Light ExportLight;

	public static float ContentsScale = 0.6f;

	private Item _currentOutput;

	private UniTask _processingTask;

	private IProcessable _plantToGrind;

	private WorldManager.ProcessingData _recipeData;

	public virtual Dictionary<int, Item> Recipes => RecipeComparable.Recipes;

	public Slot InputSlot => Slots[0];

	public Slot OutputSlot => Slots[1];

	private bool IsError
	{
		get
		{
			if (!OnOff || !Powered || IsOpen)
			{
				return true;
			}
			if (!InputSlot.Occupant)
			{
				return true;
			}
			if (!(InputSlot.Occupant is IProcessable))
			{
				return true;
			}
			if (!OutputSlot.Occupant || !_currentOutput)
			{
				return false;
			}
			if (_currentOutput.PrefabHash != OutputSlot.Occupant.PrefabHash || !OutputSlot.Occupant.HasRoom)
			{
				return true;
			}
			return false;
		}
	}

	public bool CanProcess(Recipe recipe)
	{
		foreach (KeyValuePair<int, Item> recipe2 in Recipes)
		{
			Item item = Prefab.Find<Item>(recipe2.Key);
			if ((object)item != null && item.CreatedReagentMixture.ContainsSome(recipe))
			{
				return true;
			}
		}
		return false;
	}

	public bool CanProcess(Reagent reagentType)
	{
		foreach (KeyValuePair<int, Item> recipe in Recipes)
		{
			Item item = Prefab.Find<Item>(recipe.Key);
			if ((object)item != null && item.CreatedReagentMixture.Contains(reagentType))
			{
				return true;
			}
		}
		return false;
	}

	public List<Item> GetResourcesUsed()
	{
		List<Item> list = new List<Item>(Recipes.Count);
		foreach (KeyValuePair<int, Item> recipe in Recipes)
		{
			Item item = Prefab.Find<Item>(recipe.Key);
			if ((object)item != null && !list.Contains(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public override void Awake()
	{
		base.Awake();
		OutputSlot.IsInteractable = false;
		OutputSlot.Interactable.Collider.enabled = false;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild.ParentSlot == OutputSlot)
		{
			OutputSlot.Interactable.Collider.enabled = true;
			OutputSlot.IsInteractable = true;
			OutputSlot.Interactable.SetBounds(newChild);
			OutputSlot.Location.localScale = Vector3.one * ContentsScale;
		}
		IsOperable();
		ExportLight.enabled = !IsOpen && (bool)OutputSlot.Occupant;
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (OutputSlot.Occupant == null)
		{
			OutputSlot.IsInteractable = false;
			OutputSlot.Interactable.Collider.enabled = false;
		}
		IsOperable();
		ExportLight.enabled = !IsOpen && (bool)OutputSlot.Occupant;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (GameManager.RunSimulation && _processingTask.Status != UniTaskStatus.Pending && IsOperable())
		{
			_processingTask = Processing();
		}
		ExportLight.enabled = !IsOpen && OutputSlot.IsInteractable;
	}

	private static Item FindRecipeOutput(IProcessable source)
	{
		ReagentRecipeComparable.AllRecipes.TryGetValue(source.GetPrefabHash(), out var value);
		return value;
	}

	private static WorldManager.ProcessingData FindRecipeData(IProcessable source)
	{
		ReagentRecipeComparable.RecipeData.TryGetValue(source.GetPrefabHash(), out var value);
		return value;
	}

	private async UniTask Processing()
	{
		CancellationToken cancelToken = base.gameObject.GetCancellationTokenOnDestroy();
		while ((bool)InputSlot.Occupant)
		{
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
			if (!OnOff || !Powered || IsOpen)
			{
				break;
			}
			_plantToGrind = InputSlot.Occupant as IProcessable;
			_currentOutput = FindRecipeOutput(_plantToGrind);
			_recipeData = FindRecipeData(_plantToGrind);
			if (_recipeData == null || _currentOutput == null)
			{
				return;
			}
			float complete = 0f;
			float length = _recipeData.Time;
			while (complete < length)
			{
				if (cancelToken.IsCancellationRequested)
				{
					return;
				}
				if (!OnOff || !Powered)
				{
					break;
				}
				complete += Time.deltaTime;
				await UniTask.NextFrame(cancelToken);
			}
			if (complete >= length && _plantToGrind != null && IsOperable())
			{
				if ((bool)_currentOutput)
				{
					ProcessOutput();
				}
				if (_plantToGrind != null)
				{
					if (_plantToGrind != null && _plantToGrind.GetQuantity <= (float)_recipeData.In)
					{
						_plantToGrind.SetQuantity(_plantToGrind.GetQuantity - (float)_recipeData.In);
						OnServer.Interact(base.InteractOnOff, 0);
						_currentOutput = null;
						return;
					}
					_plantToGrind.SetQuantity(_plantToGrind.GetQuantity - (float)_recipeData.In);
				}
			}
			if (OutputSlot.Occupant is IQuantity quantity && quantity.GetQuantity >= quantity.GetMaxQuantity)
			{
				_currentOutput = null;
				return;
			}
		}
		_currentOutput = null;
	}

	private void ProcessOutput()
	{
		if ((bool)OutputSlot.Occupant)
		{
			if (OutputSlot.Occupant.PrefabHash == _currentOutput.PrefabHash && OutputSlot.Occupant.HasRoom && OutputSlot.Occupant is IQuantity quantity)
			{
				quantity.SetQuantity(Mathf.Min(quantity.GetMaxQuantity, quantity.GetQuantity + (float)_recipeData.Out));
			}
			return;
		}
		Item item = Thing.Create<Item>(_currentOutput, OutputSlot.Location.position, OutputSlot.Location.rotation, 0L);
		if ((bool)item)
		{
			item.name = _currentOutput.name;
			OnServer.MoveToSlot(item, OutputSlot);
			if (item is IQuantity quantity2)
			{
				quantity2.SetQuantity(Mathf.Min(quantity2.GetMaxQuantity, _recipeData.Out));
			}
		}
	}

	protected override bool IsOperable()
	{
		if (!GameManager.RunSimulation)
		{
			return true;
		}
		int num = ((IsError && Powered) ? 1 : 0);
		if (Error != num)
		{
			OnServer.Interact(base.InteractError, num);
		}
		return !IsError;
	}
}
