using System;
using TerrainSystem.Lods;

namespace TerrainSystem;

public static class WorkerCollections
{
	public static readonly int DefaultWorkerThreads = Math.Max(Environment.ProcessorCount - 2, 4);

	public static readonly WorkerCollection<LodMeshWorker, LodObject> LodMeshWorkers = new WorkerCollection<LodMeshWorker, LodObject>();

	public static readonly WorkerCollection<LodRequestWorker, ILodRequester> LodRequestWorkers = new WorkerCollection<LodRequestWorker, ILodRequester>();

	public static readonly WorkerCollection<VeinSeedWorker, VeinSeedJob> VeinSeedWorker = new WorkerCollection<VeinSeedWorker, VeinSeedJob>();

	public static readonly WorkerCollection<VeinGenerationWorker, VeinTemplate> VeinGenerationWorkers = new WorkerCollection<VeinGenerationWorker, VeinTemplate>();

	public static readonly WorkerCollection<VeinDeduplicationWorker, VeinCluster> VeinDeduplicationWorkers = new WorkerCollection<VeinDeduplicationWorker, VeinCluster>();

	public static readonly WorkerCollection<FillOctreeWorker, FillOctreeJob> FillOctreeWorkers = new WorkerCollection<FillOctreeWorker, FillOctreeJob>();

	public static readonly WorkerCollection<LoadTerrainWorker, LoadTerrainJob> LoadTerrainWorkers = new WorkerCollection<LoadTerrainWorker, LoadTerrainJob>();

	public static readonly WorkerCollection<MinableRenderWorker, MinableRenderJob> MinableRenderWorkers = new WorkerCollection<MinableRenderWorker, MinableRenderJob>();

	public static readonly WorkerCollection<ColliderBakeWorker, ColliderBakeJob> ColliderBakeWorkers = new WorkerCollection<ColliderBakeWorker, ColliderBakeJob>();

	public static void Initialize()
	{
		LodMeshWorkers.Initialize(DefaultWorkerThreads);
		LodRequestWorkers.Initialize(4);
		VeinSeedWorker.Initialize(1);
		VeinGenerationWorkers.Initialize(DefaultWorkerThreads);
		VeinDeduplicationWorkers.Initialize(DefaultWorkerThreads);
		MinableRenderWorkers.Initialize(5);
		FillOctreeWorkers.Initialize(64);
		LoadTerrainWorkers.Initialize(Environment.ProcessorCount);
		ColliderBakeWorkers.Initialize(2);
	}

	public static void Destroy()
	{
		LodMeshWorkers.AbortAll();
		LodRequestWorkers.AbortAll();
		VeinSeedWorker.AbortAll();
		VeinGenerationWorkers.AbortAll();
		VeinDeduplicationWorkers.AbortAll();
		MinableRenderWorkers.AbortAll();
		FillOctreeWorkers.AbortAll();
		LoadTerrainWorkers.AbortAll();
		ColliderBakeWorkers.AbortAll();
	}
}
