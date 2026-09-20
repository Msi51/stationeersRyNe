namespace UI.ElementCollection;

public interface IUIElement<in TData>
{
	long Id { get; set; }

	bool ShowingThisFrame { get; set; }

	void DoUpdate(TData data);
}
