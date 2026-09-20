using System;
using System.Collections.Generic;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Networks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Objects.Items;

public class SPUMesonScanner : SensorProcessingUnit
{
	[SerializeField]
	private Material _material;

	private static Dictionary<int, ScannerMeshBatch> _batches = new Dictionary<int, ScannerMeshBatch>(100);

	private static List<ScannerMeshBatch> _batchList = new List<ScannerMeshBatch>(100);

	private MaterialPropertyBlock _propertyBlock;

	private static readonly int ColorProperty = Shader.PropertyToID("_Color");

	private static readonly int MaxBatchSize = 1023;

	private static readonly Action<CableNetwork> CacheCablesAction = delegate(CableNetwork network)
	{
		foreach (Cable cable in network.CableList)
		{
			if (!cable.IsOccluded && !cable.IsBroken)
			{
				AddToBatch(cable);
			}
		}
	};

	private new void Start()
	{
		_propertyBlock = new MaterialPropertyBlock();
	}

	public override void Render()
	{
		CachePipes();
		CacheCables();
		CacheBrokenCables();
		CacheChutes();
		RenderMeshes();
		CleanUp();
	}

	private static void AddToBatch(Structure structure)
	{
		if ((object)structure != null && !structure.IsBeingDestroyed)
		{
			int num = ((structure is Pipe { IsBurst: not PipeBurst.None }) ? 1 : 0);
			int key = structure.PrefabHash + num;
			if (_batches.TryGetValue(key, out var value))
			{
				value.Matrices.Add(structure.GetBatchMatrix());
				value.Colors.Add(structure.CustomColor.Color);
			}
			else
			{
				ScannerMeshBatch scannerMeshBatch = ScannerMeshBatch.Create(structure);
				_batches.Add(key, scannerMeshBatch);
				_batchList.Add(scannerMeshBatch);
			}
		}
	}

	private void CachePipes()
	{
		DensePool<PipeNetwork>.ActiveEnumerable.Enumerator enumerator = PipeNetwork.AllPipeNetworks.Active().GetEnumerator();
		while (enumerator.MoveNext())
		{
			foreach (INetworkedStructure structure in enumerator.Current.StructureList)
			{
				if (structure is Pipe { IsOccluded: false } pipe)
				{
					AddToBatch(pipe);
				}
			}
		}
	}

	private void CacheCables()
	{
		CableNetwork.AllCableNetworks.ForEach(CacheCablesAction);
	}

	private void CacheBrokenCables()
	{
		foreach (CableRuptured item in CableRuptured.AllCableRuptured)
		{
			if (!item.IsOccluded)
			{
				AddToBatch(item);
			}
		}
	}

	private void CacheChutes()
	{
		foreach (ChuteNetwork allChuteNetwork in ChuteNetwork.AllChuteNetworks)
		{
			foreach (INetworkedStructure structure in allChuteNetwork.StructureList)
			{
				if (structure is Chute { IsOccluded: false } chute)
				{
					AddToBatch(chute);
				}
			}
		}
	}

	private void RenderMeshes()
	{
		foreach (ScannerMeshBatch batch in _batchList)
		{
			if (!batch.IsValid)
			{
				continue;
			}
			Vector4[] array = batch.Colors.ToArray();
			Matrix4x4[] sourceArray = batch.Matrices.ToArray();
			int num = array.Length;
			int sourceIndex = 0;
			int num2 = 0;
			while (num > 0)
			{
				if (num > MaxBatchSize)
				{
					num2 = MaxBatchSize;
					num -= MaxBatchSize;
				}
				else
				{
					num2 = num;
					num = 0;
				}
				Vector4[] array2 = new Vector4[num2];
				Array.Copy(array, sourceIndex, array2, 0, num2);
				Matrix4x4[] array3 = new Matrix4x4[num2];
				Array.Copy(sourceArray, sourceIndex, array3, 0, num2);
				_propertyBlock = new MaterialPropertyBlock();
				_propertyBlock.SetVectorArray(ColorProperty, array2);
				Graphics.DrawMeshInstanced(batch.Mesh, 0, _material, array3, num2, _propertyBlock, ShadowCastingMode.Off, receiveShadows: false);
				sourceIndex = num2;
			}
		}
	}

	private void CleanUp()
	{
		_batches.Clear();
		_batchList.Clear();
		_propertyBlock.Clear();
	}
}
