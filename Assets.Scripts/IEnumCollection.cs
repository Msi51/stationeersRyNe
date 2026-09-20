namespace Assets.Scripts;

public interface IEnumCollection
{
	int Length { get; }

	string GetEnumTypeName();

	string GetNameFromValue(int value, bool padded = false);

	string GetNameFromIndex(int index, bool padded = false);

	int GetIntFromIndex(int i);
}
