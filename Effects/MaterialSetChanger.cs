using System.Collections.Generic;
using UnityEngine;

namespace Effects;

public class MaterialSetChanger : StateMaterialChanger
{
	[SerializeField]
	private List<MaterialSet> _materialSets;

	private int _appliedSetIndex = -1;

	public int TargetSetIndex = -1;

	public override void RefreshState(bool skipAnimation)
	{
		if (_appliedSetIndex != TargetSetIndex)
		{
			ApplyCurrentMaterialSet(TargetSetIndex);
		}
	}

	private void ApplyCurrentMaterialSet(int index)
	{
		if (index >= 0 && index < _materialSets.Count)
		{
			_materialSets[index].ApplyTo(Renderer);
			_appliedSetIndex = index;
		}
	}
}
