using System.Collections.Generic;

namespace Assets.Scripts.OpenNat;

public class UPnpReqest
{
	public SoapClient.DelegateEvent OnComplete;

	public Mapping Mapping;

	public string OperationName;

	public IDictionary<string, object> Args;

	public UPnpReqest(SoapClient.DelegateEvent onComplete, Mapping mapping, string operationName, IDictionary<string, object> args)
	{
		OnComplete = onComplete;
		Mapping = mapping;
		OperationName = operationName;
		Args = args;
	}
}
