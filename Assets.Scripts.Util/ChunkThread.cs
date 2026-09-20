using System.Threading;

namespace Assets.Scripts.Util;

public class ChunkThread
{
	public Thread TargetThread;

	public object Parameter;

	public ChunkThread(Thread t, object o)
	{
		TargetThread = t;
		Parameter = o;
	}
}
