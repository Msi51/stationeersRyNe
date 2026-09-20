using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Objects.Items;
using UnityEngine;

namespace Objects;

public class DirtCanister : Item
{
	[Tooltip("The maximum amount which can be collected")]
	public float MaxCollectedDirt = 64f;

	[Tooltip("The renderers for the display")]
	public MeshRenderer[] DisplayAmount;

	public DirtCanisterState DirtCanisterState;

	private float _currentCollectedDirt;

	private float _canisterFullRatio = 0.999f;

	private float _canisterHighRatio = 0.75f;

	private float _canisterMediumRatio = 0.5f;

	private float _canisterLowRatio = 0.25f;

	private float _canisterEmptyRatio;

	public float CurrentCollectedDirt
	{
		get
		{
			return _currentCollectedDirt;
		}
		set
		{
			_currentCollectedDirt = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
			SetDirtCanisterState();
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(CurrentCollectedDirt);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			CurrentCollectedDirt = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(CurrentCollectedDirt);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		CurrentCollectedDirt = reader.ReadSingle();
	}

	public float GetCurrentCollectedDirtAmount()
	{
		return CurrentCollectedDirt;
	}

	public bool AddDirt(float amount)
	{
		if (CurrentCollectedDirt < MaxCollectedDirt)
		{
			CurrentCollectedDirt += amount;
			return true;
		}
		return false;
	}

	public bool RemoveDirt(float amount)
	{
		if (CurrentCollectedDirt > 0f)
		{
			CurrentCollectedDirt -= amount;
			if (CurrentCollectedDirt < 0f)
			{
				CurrentCollectedDirt = 0f;
			}
			return true;
		}
		return false;
	}

	public void SetDirtCanisterState()
	{
		float ratio = CurrentCollectedDirt / MaxCollectedDirt;
		UpdateInternalStateDisplay(ratio);
		UpdateInternalState(ratio);
		UpdateSlotValue();
	}

	private void UpdateInternalStateDisplay(float ratio)
	{
		DisableAllDisplayAmounts();
		if ((base.ParentSlot == null || !base.ParentSlot.HidesOccupant) && ratio != 0f)
		{
			if (ratio >= _canisterFullRatio)
			{
				DisplayAmount[4].enabled = true;
			}
			else if (ratio >= _canisterHighRatio)
			{
				DisplayAmount[3].enabled = true;
			}
			else if (ratio >= _canisterMediumRatio)
			{
				DisplayAmount[2].enabled = true;
			}
			else if (ratio >= _canisterLowRatio)
			{
				DisplayAmount[1].enabled = true;
			}
			else if (ratio > _canisterEmptyRatio)
			{
				DisplayAmount[0].enabled = true;
			}
		}
	}

	private void UpdateInternalState(float ratio)
	{
		if (ratio == 0f)
		{
			DirtCanisterState = DirtCanisterState.Empty;
		}
		else if (ratio >= _canisterFullRatio)
		{
			DirtCanisterState = DirtCanisterState.Full;
		}
		else if (ratio >= _canisterHighRatio)
		{
			DirtCanisterState = DirtCanisterState.High;
		}
		else if (ratio >= _canisterMediumRatio)
		{
			DirtCanisterState = DirtCanisterState.Medium;
		}
		else if (ratio >= _canisterLowRatio)
		{
			DirtCanisterState = DirtCanisterState.Low;
		}
		else if (ratio > _canisterEmptyRatio)
		{
			DirtCanisterState = DirtCanisterState.Empty;
		}
	}

	public void DisableAllDisplayAmounts()
	{
		MeshRenderer[] displayAmount = DisplayAmount;
		for (int i = 0; i < displayAmount.Length; i++)
		{
			displayAmount[i].enabled = false;
		}
	}

	public override bool CanCacheRenderer(Renderer selectedRenderer)
	{
		MeshRenderer[] displayAmount = DisplayAmount;
		for (int i = 0; i < displayAmount.Length; i++)
		{
			if (displayAmount[i] == selectedRenderer)
			{
				return false;
			}
		}
		return true;
	}

	public override void OnEnterInventory(Thing parent)
	{
		base.OnEnterInventory(parent);
		if (base.ParentSlot != null && base.ParentSlot.HidesOccupant)
		{
			DisableAllDisplayAmounts();
		}
		if (parent is VoxelTool)
		{
			SetDirtCanisterState();
		}
	}

	public override void OnExitInventory(Thing parent)
	{
		base.OnExitInventory(parent);
		SetDirtCanisterState();
	}

	public void UpdateSlotValue()
	{
		if (base.ParentSlot != null)
		{
			base.ParentSlot.RefreshQuantity();
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		result.Extended = GetExtendedText().ToString();
		return result;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		extendedText.AppendLine(GameStrings.DirtCanisterDirtIsAt.AsString(CurrentCollectedDirt.ToStringPrefix("g", "yellow")));
		return extendedText;
	}

	public override string GetQuantityText()
	{
		return StringGenerator.GetString((int)CurrentCollectedDirt, Unit.Stack);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DirtCanisterSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (!IsCursor && savedData is DirtCanisterSaveData dirtCanisterSaveData)
		{
			CurrentCollectedDirt = dirtCanisterSaveData.CurrentCollectedDirt;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is DirtCanisterSaveData dirtCanisterSaveData && !IsCursor)
		{
			dirtCanisterSaveData.CurrentCollectedDirt = CurrentCollectedDirt;
		}
	}
}
