using System;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class MultiMergeConstructor : MultiConstructor, IShowBuildStateTooltip
{
	public Item ToolExit;

	public override int ConstructingSoundHash
	{
		get
		{
			if (!string.IsNullOrEmpty(UsingSound))
			{
				return Animator.StringToHash(UsingSound);
			}
			return 0;
		}
	}

	public override int FinishedConstructingSoundHash
	{
		get
		{
			if (!string.IsNullOrEmpty(UseCompleteSound))
			{
				return Animator.StringToHash(UseCompleteSound);
			}
			return 0;
		}
	}

	public override void Construct(Grid3 localPosition, Quaternion targetRotation, int optionIndex, Item offhandItem, bool authoringMode, ulong steamId)
	{
		if (!authoringMode && (offhandItem == null || (offhandItem.PrefabHash != ToolExit.PrefabHash && offhandItem.ReplacementOf != null && ToolExit.PrefabHash != offhandItem.ReplacementOf.PrefabHash)))
		{
			base.Construct(localPosition, targetRotation, optionIndex, offhandItem, authoringMode, steamId);
			return;
		}
		IGridMergeable gridMergeable = Constructables[optionIndex] as IGridMergeable;
		StructureFuselage structureFuselage = Constructables[optionIndex] as StructureFuselage;
		if (gridMergeable == null && structureFuselage == null)
		{
			base.Construct(localPosition, targetRotation, optionIndex, offhandItem, authoringMode, steamId);
			return;
		}
		IGridMergeable gridMergeable2 = null;
		Structure thing = null;
		if (gridMergeable is Piping)
		{
			Piping piping = base.GridController.GetPipe(localPosition) as Piping;
			if ((bool)piping)
			{
				piping.PipeNetwork?.Remove(piping);
			}
			gridMergeable2 = piping;
			thing = piping;
		}
		else if (gridMergeable is Cable)
		{
			Cable cable = base.GridController.GetCable(localPosition);
			if ((bool)cable)
			{
				cable.CableNetwork?.Remove(cable);
			}
			gridMergeable2 = cable;
			thing = cable;
		}
		else if ((bool)structureFuselage)
		{
			StructureFuselage structureFuselage2 = base.GridController.Get<StructureFuselage>(localPosition, StructureElement.Center);
			if ((bool)structureFuselage2)
			{
				structureFuselage2.StructureNetwork?.Remove(structureFuselage2);
			}
			if ((bool)structureFuselage2)
			{
				OnServer.Destroy(structureFuselage2);
			}
			base.Construct(localPosition, targetRotation, optionIndex, null, authoringMode, steamId, (!structureFuselage2) ? 1 : 0);
			return;
		}
		if (gridMergeable2 == null)
		{
			base.Construct(localPosition, targetRotation, optionIndex, offhandItem, authoringMode, steamId);
			return;
		}
		int[] openEndLocationPermutation = gridMergeable.GetOpenEndLocationPermutation(Quaternion.identity, targetRotation);
		int[] openEndLocationPermutation2 = gridMergeable2.GetOpenEndLocationPermutation(Quaternion.identity);
		int[] array = new int[6];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Math.Max(openEndLocationPermutation[i], openEndLocationPermutation2[i]);
		}
		SmartRotate.ConnectionType[] obj = new SmartRotate.ConnectionType[8]
		{
			SmartRotate.ConnectionType.Elbow,
			SmartRotate.ConnectionType.SideOutletElbow,
			SmartRotate.ConnectionType.Straight,
			SmartRotate.ConnectionType.Tee,
			SmartRotate.ConnectionType.SideOutletTee,
			SmartRotate.ConnectionType.Cross,
			SmartRotate.ConnectionType.SideOutletCross,
			SmartRotate.ConnectionType.SixWayCross
		};
		SmartRotate.ConnectionType connectionType = SmartRotate.ConnectionType.Elbow;
		int num = 0;
		SmartRotate.ConnectionType[] array2 = obj;
		foreach (SmartRotate.ConnectionType connectionType2 in array2)
		{
			if (SmartRotate.OrientationLookup[connectionType2].ContainsKey(array))
			{
				connectionType = connectionType2;
				num = SmartRotate.OrientationLookup[connectionType2][array];
				break;
			}
		}
		int num2 = 0;
		IGridMergeable gridMergeable3 = null;
		for (int k = 0; k < Constructables.Count; k++)
		{
			gridMergeable3 = Constructables[k] as IGridMergeable;
			if (gridMergeable3 != null && gridMergeable3.GetConnectionType() == connectionType)
			{
				num2 = k;
				break;
			}
		}
		int num3 = SmartRotate.OrientationLookup[connectionType][gridMergeable3.GetOpenEndsPermutation()];
		Quaternion quaternion = Quaternion.identity;
		while (num3 != num)
		{
			quaternion = SmartRotate.RotationsList[connectionType][num3].Rotation * quaternion;
			num3++;
			if (num3 >= SmartRotate.NumberOfUniqueOrientationsOf[connectionType])
			{
				num3 = 0;
			}
		}
		int index = 0;
		for (int l = 0; l < Constructables.Count; l++)
		{
			if (Constructables[l] is IGridMergeable gridMergeable4 && gridMergeable4.GetConnectionType() == gridMergeable2.GetConnectionType())
			{
				index = l;
				break;
			}
		}
		OnServer.Destroy(thing);
		int entryQuantity = Constructables[index].BuildStates[0].Tool.EntryQuantity;
		int num4 = Constructables[num2].BuildStates[0].Tool.EntryQuantity - entryQuantity;
		base.Construct(localPosition, quaternion, num2, null, authoringMode, steamId, num4);
	}
}
