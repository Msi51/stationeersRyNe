using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(ProgrammableChipSaveData))]
public class ProgrammableChipSaveData : DynamicThingSaveData
{
	[XmlElement]
	public double[] Registers;

	[XmlElement]
	public string SourceCode;

	[XmlElement]
	public int NextAddr;

	[XmlArray]
	public List<string> DeviceLables = new List<string>();

	[XmlElement]
	public string[] AliasesKeys;

	[XmlElement]
	public int[] AliasesValues;

	[XmlElement]
	public string[] NewAliasesKeys;

	[XmlElement]
	public int[] NewAliasesValuesTarget;

	[XmlElement]
	public int[] NewAliasesValuesIndex;

	[XmlElement]
	public string[] DefineKeys;

	[XmlElement]
	public double[] DefineValues;

	[XmlElement]
	public string[] JumpTagsKeys;

	[XmlElement]
	public int[] JumpTagsValues;

	[XmlElement]
	public double[] Stack;

	[XmlElement]
	public double SleepDurationRemaining = double.NaN;
}
