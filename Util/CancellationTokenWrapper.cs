using System.Threading;

namespace Util;

public class CancellationTokenWrapper
{
	private CancellationTokenSource _tokenSource;

	public bool Initialized => _tokenSource != null;

	public CancellationToken Token => _tokenSource.Token;

	public void Initialize()
	{
		_tokenSource = new CancellationTokenSource();
	}

	public void CancelAndInitialize()
	{
		Cancel();
		Initialize();
	}

	public void Cancel()
	{
		if (Initialized)
		{
			_tokenSource.Cancel();
			_tokenSource.Dispose();
			_tokenSource = null;
		}
	}
}
