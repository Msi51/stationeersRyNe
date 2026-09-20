using System.Collections.Generic;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Objects.Electrical;

public interface ICircuitHolder : IDensePoolable
{
	ulong LastEditedBy { get; set; }

	void ClearError();

	void RaiseError(int state);

	ILogicable GetLogicableFromIndex(int deviceIndex, int networkIndex = int.MinValue);

	ILogicable GetLogicableFromId(int deviceId, int networkIndex = int.MinValue);

	List<ILogicable> GetBatchOutput();

	bool IsValidIndex(int index);

	void SetDeviceLabel(int index, string label);

	UniTask HaltAndCatchFire();

	string GetSourceCode();

	void SetSourceCode(string sourceCode);

	void Execute();

	List<LogicBinding> GetLogicBindings();

	void HasPut();
}
