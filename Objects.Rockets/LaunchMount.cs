using System.Text;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Objects.Electrical;
using Objects.Rockets.UI;
using Trading;
using UnityEngine;
using UnityEngine.Serialization;

namespace Objects.Rockets;

public class LaunchMount : LargeStructure, ISupportsRocketConstruction, ISpaceMapNodeOwner, IReferencable, IEvaluable
{
	[FormerlySerializedAs("IsOrbital")]
	[SerializeField]
	private bool isOrbital;

	private const int MAX_LAUNCH_MOUNTS = 32;

	public Sprite MapIcon;

	private bool _mapIconDirty = true;

	private SpaceMapNode _spaceMapNode;

	[SerializeField]
	private Transform rocketTransform;

	private static readonly string[] _modeStrings = new string[2]
	{
		GameStrings.MediumRocketSize.DisplayString,
		GameStrings.LargeRocketSize.DisplayString
	};

	public static readonly int IconIdHash = Animator.StringToHash("LaunchMount");

	public bool IsOrbital => isOrbital;

	public SpaceMapNode SpaceMapNode
	{
		get
		{
			return _spaceMapNode;
		}
		set
		{
			if (SpaceMapNode != null && (LaunchMount)SpaceMapNode.Owner == this)
			{
				SpaceMapNode.Owner = null;
			}
			_spaceMapNode = value;
			if (SpaceMapNode != null)
			{
				SpaceMapNode.Owner = this;
			}
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public Vector3 RocketTransformPosition { get; private set; }

	public override string[] ModeStrings => _modeStrings;

	public Sprite GetMapIcon()
	{
		if (_mapIconDirty)
		{
			Sprite icon = RocketMapIconRenderer.Instance.GetIcon(this);
			RocketMapIconRenderer.DestroySprite(MapIcon);
			MapIcon = icon;
			_mapIconDirty = false;
		}
		return MapIcon;
	}

	protected override void OnBuildStateUpdated(int newState, int previousState)
	{
		base.OnBuildStateUpdated(newState, previousState);
		_mapIconDirty = true;
	}

	public override void SetCustomColor(int index, bool emissive = false)
	{
		base.SetCustomColor(index, emissive);
		_mapIconDirty = true;
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		RocketTransformPosition = rocketTransform.position;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = base.GetPassiveTooltip(hitCollider);
		if (string.IsNullOrEmpty(result.Title))
		{
			PassiveTooltip passiveTooltip = new PassiveTooltip(true);
			passiveTooltip.Title = DisplayName;
			passiveTooltip.Extended = GetExtendedText().ToString();
			result = passiveTooltip;
		}
		return result;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (SpaceMapNode != null)
		{
			extendedText.AppendLine(GameStrings.DestinationCode.AsString(SpaceMapNode.Code));
		}
		return extendedText;
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		DynamicThing sourceItem = attack.SourceItem;
		if (!sourceItem)
		{
			return null;
		}
		Labeller labeller = sourceItem as Labeller;
		if ((bool)labeller)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = ActionStrings.Rename
			};
			if (!labeller.OnOff)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
			}
			if (!labeller.IsOperable)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
			}
			if (!doAction)
			{
				return delayedActionInstance;
			}
			labeller.Rename(this);
			return delayedActionInstance;
		}
		return base.AttackWith(attack, doAction);
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteInt64(SpaceMapNode?.ReferenceId ?? 0);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			SpaceMapNode = Referencable.Find<SpaceMapNode>(reader.ReadInt64());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(SpaceMapNode?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		SpaceMapNode = Referencable.Find<SpaceMapNode>(reader.ReadInt64());
	}

	public override void OnAssignedReference()
	{
		base.OnAssignedReference();
		if (GameManager.RunSimulation && GameManager.GameState == GameState.Running)
		{
			SpaceMapNode = SpaceMapNode.Create(this);
		}
	}

	public override CanConstructInfo CanConstruct()
	{
		int num = CountSupportFrames();
		if (!isOrbital && num < 4)
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresMultipleSupportPillars.AsString(StringManager.Get(num), "4"));
		}
		CanConstructInfo result = base.CanConstruct();
		if (!result.CanConstruct)
		{
			return result;
		}
		if (SpaceMap.Current.EntryNode.DynamicNodeCount() >= 32)
		{
			return CanConstructInfo.InvalidPlacement("Maximum LaunchPads reached");
		}
		return CanConstructInfo.ValidPlacement;
	}

	protected override CanConstructInfo CanDeconstruct()
	{
		foreach (Rocket allRocket in Rocket.AllRockets)
		{
			if (allRocket.CurrentNode == SpaceMapNode || allRocket.TargetNode == SpaceMapNode)
			{
				return CanConstructInfo.InvalidPlacement(GameStrings.CannotDeconstructLaunchMount.AsString(DisplayName, allRocket.DisplayName));
			}
		}
		return base.CanDeconstruct();
	}

	private int CountSupportFrames()
	{
		Vector3 right = ThingTransform.right;
		Vector3 up = ThingTransform.up;
		Vector3 forward = ThingTransform.forward;
		Vector3 vector = base.ThingTransformPosition + forward * 0.99f + up * 0.01f;
		return (CheckMountable(vector + right * (0f - GridSize)) ? 1 : 0) + (CheckMountable(vector + right * GridSize) ? 1 : 0) + (CheckMountable(vector + forward * (0f - GridSize)) ? 1 : 0) + (CheckMountable(vector + forward * GridSize) ? 1 : 0);
		bool CheckMountable(Vector3 mountPos)
		{
			Structure structure = base.GridController.Get<Structure>(mountPos, StructureElement.Center);
			if (structure != null)
			{
				return structure.AllowMounting;
			}
			return false;
		}
	}

	public override void OnStructureBroken()
	{
		base.OnStructureBroken();
		HandleInteractingRocketsOnBroken();
		RemoveOwnedNode();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		HandleInteractingRocketsOnBroken();
		RemoveOwnedNode();
	}

	private void RemoveOwnedNode()
	{
		if (GameManager.RunSimulation && !Singleton<GameManager>.IsQuitting && GameManager.GameState != GameState.None && SpaceMapNode != null && Referencable.CompareBaseObjects(this, SpaceMapNode.Owner))
		{
			SpaceMapNode.DeRegister();
		}
	}

	public void HandleInteractingRocketsOnBroken()
	{
		if (!GameManager.RunSimulation || Singleton<GameManager>.IsQuitting)
		{
			return;
		}
		for (int i = 0; i < Rocket.AllRockets.Count; i++)
		{
			Rocket rocket = Rocket.AllRockets[i];
			if (rocket.TargetNode == SpaceMapNode || rocket.CurrentNode == SpaceMapNode)
			{
				rocket.HandleLaunchMountNodeBroken(SpaceMapNode, RocketTransformPosition);
			}
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LaunchMountSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LaunchMountSaveData launchMountSaveData)
		{
			SpaceMapNode = Referencable.Find<SpaceMapNode>(launchMountSaveData.LinkedNodeId);
			if (SpaceMapNode == null)
			{
				ConsoleWindow.PrintAction(DisplayName + " " + StringManager.Get(base.ReferenceId) + " has no linked map node. Generating a new map node and proceeding with loading.");
			}
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (GameManager.RunSimulation && SpaceMapNode == null)
		{
			SpaceMapNode = SpaceMapNode.Create(this);
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LaunchMountSaveData launchMountSaveData)
		{
			launchMountSaveData.LinkedNodeId = SpaceMapNode?.ReferenceId ?? 0;
		}
	}
}
