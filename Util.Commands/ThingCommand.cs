using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using ThingImport;
using ThingImport.Thumbnails;
using UnityEngine;

namespace Util.Commands;

public class ThingCommand : CommandBase
{
	private static int _total;

	private static Dictionary<string, int> _animatorParentCount = new Dictionary<string, int>();

	private const float DEFAULT_ANGLE_THRRESHOLD = 10f;

	private const float DEFAULT_MERGE_DISTANCE = 0.001f;

	private const float DEFAULT_SCALE = 1f;

	private static readonly Action<Thing> CountAnimatorAction = delegate(Thing thing)
	{
		if ((bool)thing && (bool)thing.BaseAnimator)
		{
			_total++;
			if (_animatorParentCount.TryGetValue(thing.PrefabName, out var _))
			{
				_animatorParentCount[thing.PrefabName]++;
			}
			else
			{
				_animatorParentCount.Add(thing.PrefabName, 1);
			}
		}
	};

	public override string HelpText
	{
		get
		{
			(string, string)[] array = new(string, string)[10]
			{
				("(no args)", "Returns total thing count"),
				("find <id>", "Finds thing by reference id"),
				("delete <id>", "Deletes thing by reference id"),
				("spawn <prefabName> [amount] [player]", "Spawns thing by prefab name (next to player name/clientId, or player 0)"),
				("info <id>", "Prints debug info"),
				("info-verbose <id>", "Prints verbose debug info"),
				("countanimator", "Prints count of all animator things"),
				("blueprint <meshPath> <angleThreshold> <mergeDistance> <scale>", "Generates edge blueprint from mesh"),
				("thumb <id> <zoom>", "Generates thumbnail for thing"),
				("checkslots", "Checks for invalid slots in all things")
			};
			int num = 0;
			(string, string)[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				string item = array2[i].Item1;
				if (item.Length > num)
				{
					num = item.Length;
				}
			}
			num += 2;
			StringBuilder stringBuilder = new StringBuilder();
			for (int j = 0; j < array.Length; j++)
			{
				if (j > 0)
				{
					stringBuilder.AppendLine();
				}
				stringBuilder.Append(array[j].Item1.PadRight(num));
				stringBuilder.Append(array[j].Item2);
			}
			return stringBuilder.ToString();
		}
	}

	public override string[] Arguments => new string[10] { "No args returns total thing count", "find", "delete", "spawn", "info", "countanimator", "blueprint", "thumbnail", "spawnvel", "checkslots" };

	public override bool IsLaunchCmd { get; }

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("thing"))
		{
			return null;
		}
		if (args.Length == 0)
		{
			return $"Thing count: {OcclusionManager.AllThings.ActiveCount}";
		}
		switch (args[0].ToLower())
		{
		case "countanimator":
		{
			_animatorParentCount.Clear();
			_total = 0;
			OcclusionManager.AllThings.ForEach(CountAnimatorAction);
			List<string> list4 = _animatorParentCount.Keys.ToList();
			list4.Sort((string a, string b) => (_animatorParentCount[a] >= _animatorParentCount[b]) ? 1 : (-1));
			foreach (string item in list4)
			{
				ConsoleWindow.Print($"{item}: {_animatorParentCount[item]}");
			}
			ConsoleWindow.Print($"AnimatorThings: {_total}");
			return null;
		}
		case "find":
		{
			if (ConsoleWindow.IsInvalidSyntax(args, 2))
			{
				return null;
			}
			if (!uint.TryParse(args[1], out var result2))
			{
				return "Thing: " + args[1] + " is not a valid ReferenceId";
			}
			if (!FindThing(result2, out var thing2))
			{
				return null;
			}
			return $"Thing {result2}: {thing2}";
		}
		case "delete":
		{
			if (ConsoleWindow.IsInvalidSyntax(args, 2))
			{
				return null;
			}
			if (CommandBase.CannotAsClient("delete"))
			{
				return null;
			}
			if (!CommandBase.Get(args, 1, "id", out uint result))
			{
				return null;
			}
			if (!FindThing(result, out var thing))
			{
				return null;
			}
			OnServer.Destroy(thing);
			ConsoleWindow.PrintAction("deleted '" + thing.DisplayName + "'");
			break;
		}
		case "spawn":
			try
			{
				if (!CommandBase.Get(args, 1, "Prefab Name", out string result3))
				{
					return null;
				}
				int result4 = 1;
				string target = null;
				if (args.Length >= 3)
				{
					if (!int.TryParse(args[2], out result4))
					{
						result4 = 1;
						target = args[2];
					}
					else if (args.Length >= 4)
					{
						target = args[3];
					}
				}
				string err;
				long humanRefId = GetHumanRefId(target, out err);
				if (humanRefId == -1 || err != null)
				{
					return err;
				}
				for (int i = 0; i < result4; i++)
				{
					OnServer.SpawnDynamicThingMaxStack(humanRefId, result3);
				}
			}
			catch (Exception ex)
			{
				ConsoleWindow.PrintError(ex.Message);
			}
			break;
		case "spawnvel":
			return HandleSpawnVel(args);
		case "info":
		{
			if (ConsoleWindow.IsInvalidSyntax(args, 2))
			{
				return null;
			}
			if (!CommandBase.Get(args, 1, "Reference ID", out long result11))
			{
				return null;
			}
			if (Referencable.Referencables.TryGetValue(result11, out var value))
			{
				value.PrintDebugInfo();
			}
			break;
		}
		case "info-verbose":
		{
			if (ConsoleWindow.IsInvalidSyntax(args, 2))
			{
				return null;
			}
			if (!CommandBase.Get(args, 1, "Reference ID", out long result12))
			{
				return null;
			}
			if (Referencable.Referencables.TryGetValue(result12, out var value2))
			{
				value2.PrintDebugInfo(verbose: true);
			}
			break;
		}
		case "blueprint":
		{
			CommandBase.Get(args, 1, "Mesh Path", out string result7);
			if (!CommandBase.Get(args, 2, "Angle Threshold", out float result8))
			{
				result8 = 10f;
			}
			if (!CommandBase.Get(args, 3, "Merge Distance", out float result9))
			{
				result9 = 0.001f;
			}
			if (!CommandBase.Get(args, 4, "Scale", out float result10))
			{
				result10 = 1f;
			}
			MeshReference meshReference = new MeshReference
			{
				Path = result7
			};
			StreamingAssetLoader.Clear();
			Mesh mesh = StreamingAssetLoader.LoadMesh(meshReference, result10);
			if (mesh == null)
			{
				return "Mesh not found at path: " + result7;
			}
			List<Edge> list2 = EdgeGenerator.GenerateSharpEdges(mesh, result8, result9);
			List<string> list3 = new List<string>();
			foreach (Edge item2 in list2)
			{
				list3.Add($"<Edge x1=\"{item2.A.x}\" y1=\"{item2.A.y}\" z1=\"{item2.A.z}\" x2=\"{item2.B.x}\" y2=\"{item2.B.y}\" z2=\"{item2.B.z}\"/>");
			}
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(result7);
			string text = Path.Combine(folderPath, fileNameWithoutExtension + "_edges.txt");
			using StreamWriter streamWriter = new StreamWriter(text);
			foreach (string item3 in list3)
			{
				streamWriter.WriteLine(item3);
			}
			return "Edges generated at: " + text;
		}
		case "thumbnail":
		{
			if (!CommandBase.Get(args, 1, "Reference Id", out long result5))
			{
				return null;
			}
			if (!CommandBase.Get(args, 2, "Zoom", out float result6))
			{
				return null;
			}
			if (!Thing.TryFind(result5, out var thing3))
			{
				return null;
			}
			if (thing3 is DynamicThing thing4)
			{
				return ThumbnailGenerator.Instance.Generate(thing4, result6);
			}
			return null;
		}
		case "checkslots":
		{
			List<string> list = new List<string>();
			foreach (Thing sourcePrefab in WorldManager.Instance.SourcePrefabs)
			{
				if (sourcePrefab == null || sourcePrefab.Slots == null || sourcePrefab.Slots.Count == 0)
				{
					continue;
				}
				foreach (Slot slot in sourcePrefab.Slots)
				{
					if (slot == null)
					{
						ConsoleWindow.PrintAction("Slot is null in prefab: " + sourcePrefab.PrefabName);
					}
					else if (slot.StringHash == 0)
					{
						ConsoleWindow.PrintAction($"Slot {slot.SlotIndex} in prefab {sourcePrefab.PrefabName} has no StringHash");
					}
				}
			}
			if (list.Count > 0)
			{
				return string.Join("\n", list);
			}
			return "No invalid slots found";
		}
		}
		return null;
	}

	private static string HandleSpawnVel(string[] args)
	{
		if (!CommandBase.Get(args, 1, "prefab name", out string result))
		{
			return null;
		}
		if (!CommandBase.Get(args, 2, "force", out float result2))
		{
			return null;
		}
		Vector3 position = CameraController.Instance.MainCameraTransform.position;
		Vector3 forward = CameraController.Instance.MainCameraTransform.forward;
		OnServer.Create<DynamicThing>(result, position + forward, Quaternion.identity).RigidBody.AddForce(result2 * forward);
		return null;
	}

	private static long GetHumanRefId(string target, out string err)
	{
		err = null;
		if (!string.IsNullOrEmpty(target))
		{
			ulong result;
			Human human = (ulong.TryParse(target, out result) ? Human.Find(result) : Human.Find(target));
			if ((bool)human)
			{
				return human.ReferenceId;
			}
			err = "No player matching '" + target + "'";
			return -1L;
		}
		if (GameManager.IsBatchMode)
		{
			if (Human.AllHumans.Count > 0)
			{
				return Human.AllHumans[0].ReferenceId;
			}
			err = "No humans to spawn things next to";
			return -1L;
		}
		if ((bool)InventoryManager.ParentHuman)
		{
			return InventoryManager.ParentHuman.ReferenceId;
		}
		err = "Human referenceId could not be found";
		return -1L;
	}

	private static bool FindThing(uint thingRefId, out Thing thing)
	{
		if (Thing.TryFind(thingRefId, out thing))
		{
			return true;
		}
		ConsoleWindow.PrintError(ConsoleStrings.Error.CommandArgumentUnknown.AsString("thing", thingRefId.ToString()), suppressStacktrace: true);
		return false;
	}
}
