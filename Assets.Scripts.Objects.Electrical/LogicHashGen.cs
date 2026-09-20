using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;

namespace Assets.Scripts.Objects.Electrical;

public class LogicHashGen : LogicUnitBase
{
	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.LogicProcessorsCategory);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return Setting;
		}
		return base.GetLogicValue(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Setting)
		{
			Setting = value;
		}
		else
		{
			base.SetLogicValue(logicType, value);
		}
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
			if (!interaction.SourceSlot.Contains<Screwdriver>())
			{
				return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (!GameManager.IsBatchMode)
			{
				ScrewSound();
				if (InventoryManager.ParentHuman.OrganBrain.ClientId == (interaction.SourceThing as Entity)?.OrganBrain?.ClientId && InputPrefabs.ShowInputPanelAllDynamicThings("Select Thing"))
				{
					InputPrefabs.OnSubmit += InputFinished;
				}
			}
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public void InputFinished(DynamicThing prefab)
	{
		if (!(prefab == null))
		{
			int prefabHash = prefab.PrefabHash;
			if (!GameManager.RunSimulation)
			{
				SetLogicFromClient setLogicFromClient = new SetLogicFromClient();
				setLogicFromClient.LogicId = base.NetworkId;
				setLogicFromClient.LogicType = LogicType.Setting;
				setLogicFromClient.Value = prefabHash;
				setLogicFromClient.SendToServer();
			}
			else
			{
				Setting = prefabHash;
			}
		}
	}
}
