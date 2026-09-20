namespace Audio;

public interface IGamePoolable<T> : IPoolable<T> where T : GameBase, IGamePoolable<T>, new()
{
	GameObjectPool<T> GamePool { get; set; }
}
