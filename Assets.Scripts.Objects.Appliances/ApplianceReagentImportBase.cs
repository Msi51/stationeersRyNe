using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Appliances;

public class ApplianceReagentImportBase : Appliance
{
	public delegate void OnCreateRecipeApplianceEvent(Item item);

	public Item ResultPrefab;

	private Recipe _currentRecipe;

	private static readonly int BowlState = Animator.StringToHash("Bowl");

	private float _completed;

	public float NoRecipeTime = 5f;

	private UniTask _processing;

	private float _timeSinceIgnite;

	protected virtual Dictionary<Recipe, Item> Recipes => null;

	protected Slot OutputSlot => Slots[0];

	public override bool HasReadableReagentMixture => true;

	protected virtual bool IsError
	{
		get
		{
			if (!OnOff || !Powered || IsOpen || ReagentMixture.TotalReagents <= 0.0)
			{
				return true;
			}
			return false;
		}
	}

	[ByteArraySync]
	public float Completed
	{
		get
		{
			return _completed;
		}
		set
		{
			if (!RocketMath.Approximately(value, Completed))
			{
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 512;
				}
				_completed = value;
			}
		}
	}

	public event OnCreateRecipeApplianceEvent OnCreateRecipe;

	protected void GetRecipe()
	{
		Recipe recipe = new Recipe(ReagentMixture, null);
		Recipes.TryGetValue(recipe, out ResultPrefab);
		if (ResultPrefab != null)
		{
			foreach (Recipe key in Recipes.Keys)
			{
				if (key.Equals(recipe))
				{
					_currentRecipe = key;
					break;
				}
			}
			return;
		}
		_currentRecipe = default(Recipe);
	}

	public override void Awake()
	{
		base.Awake();
		ReagentMixture = new ReagentMixture(this);
	}

	public override void OnReagentUpdate()
	{
		base.OnReagentUpdate();
		if (BaseAnimator.HasParameter(BowlState))
		{
			BaseAnimator.SetBool(BowlState, ReagentMixture.TotalReagents > 0.0);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(Completed);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			Completed = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(Completed);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Completed = reader.ReadSingle();
	}

	private async UniTask Processing()
	{
		if (!OnOff || !Powered || IsOpen)
		{
			if (Activate != 0)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			return;
		}
		OnServer.Interact(base.InteractActivate, 1);
		float complete = 0f;
		GetRecipe();
		float waitTime = ((ResultPrefab != null) ? _currentRecipe.Time : NoRecipeTime);
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		while (complete < waitTime && OnOff && Powered && !IsOpen)
		{
			complete += Time.deltaTime;
			Completed = complete / waitTime;
			_timeSinceIgnite += Time.deltaTime;
			if (_timeSinceIgnite >= 1f)
			{
				AtmosphericsController.World.IgniteAtmosphere(base.WorldGrid, new MoleEnergy(100.0));
				_timeSinceIgnite = 0f;
			}
			await UniTask.NextFrame(cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
		}
		if (complete >= waitTime && IsOperable())
		{
			if (ResultPrefab != null)
			{
				Item item = Thing.Create<Item>(ResultPrefab, OutputSlot.Location.position, OutputSlot.Location.rotation, 0L);
				OnServer.MoveToSlot(item, OutputSlot);
				OnServer.Interact(base.InteractActivate, 2);
				if (this.OnCreateRecipe != null)
				{
					this.OnCreateRecipe(item);
				}
			}
			ReagentMixture.Clear();
		}
		if (IsOperable() || ResultPrefab != null)
		{
			OnServer.Interact(base.InteractOnOff, 0);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable == base.InteractOnOff || interactable == base.InteractPowered || interactable == base.InteractError || interactable == base.InteractOpen)
		{
			if (GameManager.RunSimulation && _processing.Status != UniTaskStatus.Pending && IsOperable())
			{
				_processing = Processing();
			}
			else if (GameManager.RunSimulation && interactable == base.InteractOpen && Activate != 0)
			{
				OnServer.Interact(base.InteractActivate, 0);
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

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		IsOperable();
	}
}
