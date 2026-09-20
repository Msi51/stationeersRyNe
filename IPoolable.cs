public interface IPoolable<T> where T : IPoolable<T>, new()
{
	int PoolId { get; set; }

	string DebugName { get; set; }

	bool IsVisible { get; }

	bool IsActive { get; set; }

	ObjectPool<T> Pool { get; set; }

	void ReturnToPool();

	void SetVisible(bool isVisible);
}
