using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Genetics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Util;
using Reagents;
using Trading;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Items;

public class Stackable : Item, IQuantity, ITradable, IEvaluable, IReferencable, IMergeable
{
	public delegate void SplitEvent(bool state);

	public List<GeneCollection> StackedGeneCollections;

	[Header("Stackable")]
	[SerializeField]
	[FormerlySerializedAs("Quantity")]
	private int quantity = 1;

	public int MaxQuantity = 10;

	public float GetMaxQuantity => MaxQuantity;

	public override float GetQuantity => Quantity;

	public new float GetTradableQuantity => GetQuantity;

	public float GetRatioQuantity => (float)Quantity / (float)MaxQuantity;

	public float GetProcessedQuantity => 1f / (float)MaxQuantity;

	public override bool HasRoom => !IsStackFull;

	public bool IsStackFull => Quantity >= MaxQuantity;

	public int Quantity
	{
		get
		{
			return quantity;
		}
		set
		{
			if (value == quantity)
			{
				return;
			}
			quantity = value;
			if (base.ParentSlot != null)
			{
				if (!GameManager.IsBatchMode && base.ParentSlot.Display != null)
				{
					base.ParentSlot.RefreshQuantity();
				}
				ItemMerged();
			}
			if (GameManager.RunSimulation && Quantity <= 0)
			{
				DestroyItemAtZero();
			}
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
		}
	}

	public static event Event MergedEvent;

	public static event SplitEvent OnStackSplit;

	public override bool SetGene(Gene gene, float value)
	{
		List<GeneCollection> stackedGeneCollections = StackedGeneCollections;
		if (stackedGeneCollections == null || stackedGeneCollections.Count != 1)
		{
			return false;
		}
		StackedGeneCollections[0].SetGeneValue(gene, Mathf.Clamp(value, -1f, 1f));
		return true;
	}

	public override ReagentMixture GetTotalReagentMixture()
	{
		if (CreatedReagentMixture.TotalReagents > 0.0 && CreatedReagentMixture.TotalReagents > 1.0)
		{
			CreatedReagentMixture = CreatedReagentMixture.GetRatioMixture();
		}
		return CreatedReagentMixture * Quantity;
	}

	public override void Smelt(Atmosphere localAtmosphere, ReagentMixture reagentMixture)
	{
		if (CreatedReagentMixture.TotalReagents > 0.0)
		{
			if (CreatedReagentMixture.TotalReagents > 1.0)
			{
				CreatedReagentMixture = CreatedReagentMixture.GetRatioMixture();
			}
			reagentMixture.Add(CreatedReagentMixture);
		}
		Quantity--;
	}

	public override void Recycle()
	{
		if (GameManager.RunSimulation && (float)Quantity <= 0f)
		{
			DestroyItem();
		}
		else
		{
			Quantity--;
		}
	}

	public bool CanUseStack(int quantity, ref DelayedActionInstance actionInstance)
	{
		if (Quantity - quantity >= 0)
		{
			return true;
		}
		actionInstance.IsDisabled = true;
		actionInstance.AppendStateMessage(GameStrings.StackableNotEnoughInStackOf, ToTooltip(), StringManager.Get(Quantity), StringManager.Get(quantity));
		return false;
	}

	public virtual bool CanStack(IMergeable targetStack)
	{
		if (targetStack == null)
		{
			return false;
		}
		return targetStack.GetPrefabHash() == GetPrefabHash();
	}

	public virtual int AddQuantity(int quantityToAdd)
	{
		if (Quantity == MaxQuantity || quantityToAdd == 0)
		{
			return 0;
		}
		int num = Mathf.Min(quantityToAdd, MaxQuantity - Quantity);
		Quantity += num;
		return num;
	}

	public virtual int RemoveQuantity(int quantityToRemove)
	{
		if (quantityToRemove == 0)
		{
			return 0;
		}
		int num = Mathf.Min(quantityToRemove, Quantity);
		Quantity -= num;
		return Mathf.Max(num, 0);
	}

	public virtual bool DecrementQuantity()
	{
		Quantity--;
		return Quantity > 0;
	}

	public virtual void SetQuantity(int newQuantity)
	{
		Quantity = Mathf.Min(newQuantity, MaxQuantity);
	}

	public override void SetQuantity(float value)
	{
		Quantity = (int)value;
	}

	public override string GetQuantityText()
	{
		if (MaxQuantity == 1)
		{
			return null;
		}
		return StringGenerator.GetString(Quantity, Unit.Stack);
	}

	public override object GetModXmlType()
	{
		return new StackableModData();
	}

	public override void DeserializModData(ThingModData modData)
	{
		base.DeserializModData(modData);
		if (modData is StackableModData stackableModData && !float.IsNaN(stackableModData.MaxQuantity))
		{
			MaxQuantity = (int)stackableModData.MaxQuantity;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is StackableSaveData stackableSaveData)
		{
			stackableSaveData.Quantity = Quantity;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new StackableSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is StackableSaveData stackableSaveData)
		{
			Quantity = stackableSaveData.Quantity;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteInt32(Quantity);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Quantity = reader.ReadInt32();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(Quantity);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Quantity = reader.ReadInt32();
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (interactable.Action == InteractableType.Button1)
		{
			if (Quantity <= 1)
			{
				return delayedActionInstance.Fail(GameStrings.StackableNotEnoughInStack);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			SplitStack(interaction, 1);
			Stackable.OnStackSplit?.Invoke(state: false);
			return delayedActionInstance.Succeed();
		}
		if (interactable.Action == InteractableType.Button2)
		{
			if (Quantity <= 1)
			{
				return delayedActionInstance.Fail(GameStrings.StackableNotEnoughInStack);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			SplitStack(interaction, Mathf.FloorToInt((float)Quantity * 0.5f));
			Stackable.OnStackSplit?.Invoke(state: true);
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	protected virtual void SplitStack(Interaction interaction, int splitQuantity)
	{
		if (!GameManager.RunSimulation || splitQuantity > Quantity)
		{
			return;
		}
		Human rootParentHuman = interaction.SourceSlot.Parent.RootParentHuman;
		if (rootParentHuman == null)
		{
			return;
		}
		Quantity -= splitQuantity;
		Vector3 vector = (rootParentHuman.AimIk ? rootParentHuman.HelmetSlot.Location.position : RootParent.CenterPosition);
		Vector3 vector2 = (rootParentHuman.AimIk ? rootParentHuman.HelmetSlot.Location.forward : RootParent.ThingTransform.forward);
		Vector3 safeDropPosition = GetSafeDropPosition(vector + vector2 * 0.1f, vector2, 0.5f);
		Stackable stackable = OnServer.Create<Stackable>(base.SourcePrefab, safeDropPosition, interaction.SourceThing.ThingTransform.rotation);
		if (!(stackable == null))
		{
			if (CustomColor.IsSet)
			{
				OnServer.SetCustomColor(stackable, CustomColor.Index);
			}
			stackable.Quantity = Mathf.Min(splitQuantity, stackable.MaxQuantity);
			stackable.DamageState.Copy(DamageState);
			OnSplitStack(stackable);
			SplitIntoHand(interaction, rootParentHuman, stackable);
		}
	}

	public virtual Stackable SplitStack(int splitQuantity, Slot slot)
	{
		Quantity -= splitQuantity;
		Stackable stackable = OnServer.Create<Stackable>(base.SourcePrefab, slot);
		if (stackable == null)
		{
			return null;
		}
		if (CustomColor.IsSet)
		{
			OnServer.SetCustomColor(stackable, CustomColor.Index);
		}
		stackable.Quantity = Mathf.Min(splitQuantity, stackable.MaxQuantity);
		stackable.DamageState.Copy(DamageState);
		OnSplitStack(stackable);
		return stackable;
	}

	protected void SplitIntoHand(Interaction interaction, Human human, Stackable newStack)
	{
		Slot sourceSlot = interaction.SourceSlot;
		Slot parentSlot = (interaction.DestinationThing as DynamicThing).ParentSlot;
		if (parentSlot != human.LeftHandSlot && parentSlot != human.RightHandSlot)
		{
			Slot handSlot = ((sourceSlot == human.LeftHandSlot) ? human.RightHandSlot : human.LeftHandSlot);
			if (!TrySplitIntoHand(sourceSlot, newStack))
			{
				TrySplitIntoHand(handSlot, newStack);
			}
		}
		else
		{
			Slot handSlot2 = ((human.LeftHandSlot == parentSlot) ? human.RightHandSlot : human.LeftHandSlot);
			TrySplitIntoHand(handSlot2, newStack);
		}
	}

	private bool TrySplitIntoHand(Slot handSlot, Stackable newStack)
	{
		if (handSlot.Occupant == null)
		{
			OnServer.MoveToSlot(newStack, handSlot);
			return true;
		}
		Stackable stackable = handSlot.Occupant as Stackable;
		if (stackable != null && handSlot.Occupant.PrefabHash == newStack.PrefabHash && stackable.Quantity < stackable.MaxQuantity)
		{
			stackable.Merge(newStack);
			return true;
		}
		return false;
	}

	protected virtual void OnSplitStack(Stackable newStack)
	{
		if (ReagentMixture != null && ReagentMixture.TotalReagents > 0.0)
		{
			newStack.ReagentMixture.Add(ReagentMixture);
		}
	}

	public virtual void Merge(IMergeable mergeable)
	{
		if (mergeable is Stackable stackable)
		{
			int num = Mathf.Min(stackable.Quantity + Quantity, MaxQuantity) - Quantity;
			Stackable.MergedEvent?.Invoke();
			OnMergeStack(stackable, num);
			stackable.Quantity -= num;
			Quantity += num;
			if (DamageState.Total > stackable.DamageState.Total)
			{
				stackable.DamageState.Copy(DamageState);
			}
			else
			{
				DamageState.Copy(stackable.DamageState);
			}
			if (stackable.Quantity == 0 && GameManager.RunSimulation)
			{
				OnServer.Destroy(stackable);
			}
		}
	}

	protected virtual void OnMergeStack(Stackable oldStack, float delta)
	{
	}

	public override bool OnUseItem(float quantity, Thing onUseThing)
	{
		if (!base.IsInstantiated)
		{
			return true;
		}
		quantity = Mathf.Min(quantity, Quantity);
		Quantity -= (int)quantity;
		if (GameManager.RunSimulation && StackedGeneCollections != null && StackedGeneCollections.Count - (int)quantity >= 0)
		{
			StackedGeneCollections.RemoveRange(StackedGeneCollections.Count - (int)quantity, (int)quantity);
		}
		if (NetworkManager.IsClient)
		{
			UseStackableMessage useStackableMessage = new UseStackableMessage();
			useStackableMessage.QuantityUsed = (int)quantity;
			useStackableMessage.ReferenceId = base.ReferenceId;
			useStackableMessage.SendToServer();
		}
		return base.OnUseItem(quantity, onUseThing);
	}

	public override void OnDamageDestroyed()
	{
		if (GameManager.RunSimulation)
		{
			DamageState.HealAll();
			Quantity--;
		}
	}

	public bool CheckValidInteractableStringHashes()
	{
		foreach (Interactable interactable in Interactables)
		{
			if (interactable.StringHash == 0)
			{
				return false;
			}
		}
		return true;
	}

	public bool IsColorCompatible(Stackable other)
	{
		if (PaintableMaterial == null || other.PaintableMaterial == null)
		{
			return true;
		}
		return CustomColor?.Index == other.CustomColor?.Index;
	}
}
