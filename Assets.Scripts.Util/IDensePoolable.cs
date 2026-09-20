namespace Assets.Scripts.Util;

public interface IDensePoolable
{
	bool OnAddToPool(object densePool, int slot);

	void OnRemoveFromPool(object densePool);
}
