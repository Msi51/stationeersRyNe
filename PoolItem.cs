using UnityEngine;

public class PoolItem
{
	private GameObject gameObject;

	private Projection projection;

	private FadeMethod fadeMethod;

	private float delay;

	private float inDuration;

	private float outDuration;

	private CullMethod cullMethod;

	private float cullDuration;

	private float timeElapsed;

	private float timeSinceSeen;

	public GameObject GameObject => gameObject;

	public Projection Projection => projection;

	public PoolItem(GameObject GameObject)
	{
		gameObject = GameObject;
	}

	public void Reset(ProjectionType Type)
	{
		gameObject.transform.SetParent(ProjectionPool.Parent);
		timeElapsed = 0f;
		fadeMethod = FadeMethod.None;
		inDuration = 0f;
		delay = 0f;
		outDuration = 0f;
		cullMethod = CullMethod.None;
		timeSinceSeen = 0f;
		cullDuration = 0f;
		switch (Type)
		{
		case ProjectionType.Decal:
			gameObject.GetComponent<Eraser>().enabled = false;
			gameObject.GetComponent<Pulse>().enabled = false;
			projection = gameObject.GetComponent<Decal>();
			break;
		case ProjectionType.Eraser:
			gameObject.GetComponent<Decal>().enabled = false;
			gameObject.GetComponent<Pulse>().enabled = false;
			projection = gameObject.GetComponent<Eraser>();
			break;
		case ProjectionType.Pulse:
			gameObject.GetComponent<Decal>().enabled = false;
			gameObject.GetComponent<Eraser>().enabled = false;
			projection = gameObject.GetComponent<Pulse>();
			break;
		}
		projection.enabled = true;
		projection.AlphaModifier = 1f;
		projection.ScaleModifier = 1f;
	}

	public void Update(float deltaTime)
	{
		if (fadeMethod != FadeMethod.None)
		{
			float num = 1f;
			timeElapsed += deltaTime;
			if (timeElapsed < inDuration)
			{
				num = 1f - (inDuration - timeElapsed) / inDuration;
			}
			if (timeElapsed > inDuration + delay)
			{
				num = (inDuration + delay + outDuration - timeElapsed) / outDuration;
			}
			if (fadeMethod == FadeMethod.Alpha || fadeMethod == FadeMethod.Both)
			{
				projection.AlphaModifier = num;
			}
			if (fadeMethod == FadeMethod.Scale || fadeMethod == FadeMethod.Both)
			{
				projection.ScaleModifier = num;
			}
			if (timeElapsed >= inDuration + delay + outDuration)
			{
				ProjectionPool.Return(this);
			}
		}
		else
		{
			projection.AlphaModifier = 1f;
			projection.ScaleModifier = 1f;
		}
		if (cullMethod != CullMethod.None)
		{
			if (projection.Visible)
			{
				timeSinceSeen = 0f;
			}
			else
			{
				timeSinceSeen += deltaTime;
			}
			if (timeSinceSeen > cullDuration)
			{
				ProjectionPool.Return(this);
			}
		}
	}

	public void Fade(FadeMethod Method, float InDuration, float Delay, float OutDuration)
	{
		fadeMethod = Method;
		delay = Delay;
		inDuration = InDuration;
		outDuration = OutDuration;
	}

	public void Culled(CullMethod Method, float Duration)
	{
		cullMethod = Method;
		cullDuration = Duration;
	}
}
