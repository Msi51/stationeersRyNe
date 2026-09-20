public interface IKeyFrameCollection
{
	void Apply();

	void Lerp(IKeyFrameCollection other, float t);

	bool MoveTowards(float speed);
}
