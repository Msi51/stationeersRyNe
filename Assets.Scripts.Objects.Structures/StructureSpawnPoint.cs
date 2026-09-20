using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Structures;

public class StructureSpawnPoint : SmallGrid, ISpawnPoint, IReferencable, IEvaluable
{
	public Transform PointToSpawn;

	private static string setSpawnMessageKey = "SetSpawnMessage";

	private static string setSpawnTitleKey = "SetSpawnTitle";

	public Transform GetSpawnPointTransform()
	{
		return PointToSpawn;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance result = base.InteractWith(interactable, interaction, doAction);
		if (interactable.Action == InteractableType.Activate && !(interaction.SourceThing is SprayCan))
		{
			Human human = interaction.SourceThing as Human;
			SerializedClientInfo clientInfo = GameManager.GetClientInfo((human?.OrganBrain?.ClientId).GetValueOrDefault());
			result = new DelayedActionInstance
			{
				color = Color.green,
				ActionMessage = ((clientInfo != null && clientInfo.SpawnPointReference == base.ReferenceId) ? GameStrings.RespawnHere.AsColor("green") : Localization.GetInterface(setSpawnMessageKey)),
				Duration = 0.5f,
				OverrideTitle = Localization.GetInterface(setSpawnTitleKey)
			};
			if ((bool)human && doAction && GameManager.RunSimulation)
			{
				SpawnPoint.AssignSpawn(human.OrganBrain.ClientId, this);
			}
		}
		return result;
	}
}
