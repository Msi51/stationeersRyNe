namespace Assets.Scripts.Util;

public class DensePoolReference<T> where T : class, IDensePoolable
{
	private readonly DensePool<T> _pool;

	private int _slot;

	private const int INVALID = -1;

	public int Slot => _slot;

	public DensePoolReference(DensePool<T> masterPool)
	{
		_pool = masterPool;
		_slot = -1;
	}

	public bool CanAddToPool(object incomingPool)
	{
		if (_pool != incomingPool)
		{
			return false;
		}
		if (_slot >= 0)
		{
			return false;
		}
		return true;
	}

	public bool AddToPool(object incomingPool, int index)
	{
		if (_pool != incomingPool)
		{
			return false;
		}
		if (_slot >= 0)
		{
			return false;
		}
		_slot = index;
		return true;
	}

	public void OnRemovedFrom(object outgoingPool)
	{
		if (_pool == outgoingPool && _slot >= 0)
		{
			_pool.RemoveAt(_slot);
			_slot = -1;
		}
	}
}
