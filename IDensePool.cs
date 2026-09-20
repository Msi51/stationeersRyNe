public interface IDensePool : IListable
{
	string Name { get; }

	void DrawNullCountInList(ref int index);

	void DrawSummary();
}
