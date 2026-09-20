namespace Assets.Scripts.UI;

public interface IWindow
{
	int ZLayer { get; }

	void SetZLayer(int layer);
}
