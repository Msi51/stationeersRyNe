using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class LogicReaderBase : LogicUnitBase
{
	private LogicUnitBase _input1;

	private LogicUnitBase _output1;

	public LogicUnitBase Input1
	{
		get
		{
			return _input1;
		}
		set
		{
			if (!(_input1 == value))
			{
				_input1 = value;
				LogicNetworkChange();
			}
		}
	}

	public LogicUnitBase Output1
	{
		get
		{
			return _output1;
		}
		set
		{
			if (!(_output1 == value))
			{
				_output1 = value;
				LogicNetworkChange();
			}
		}
	}

	public override void OnSettingChanged()
	{
		base.OnSettingChanged();
		PlayPooledAudioSound(Defines.Sounds.LogicRead, LogicUnitBase.SoundOffset);
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.LogicReadersCategory);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return Setting;
		}
		return base.GetLogicValue(logicType);
	}
}
