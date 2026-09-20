using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Electrical;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Motherboard;

public class ScreenSorter : MonoBehaviour
{
	public Text Title;

	public Image BackgroundImage;

	public GridLayoutGroup WhitelistGrid;

	public Button ButtonRename;

	public Button ButtonAddFilter;

	[ReadOnly]
	public List<ScreenFilter> ScreenFilters = new List<ScreenFilter>();

	public List<FilterReference> DisplayedFilters = new List<FilterReference>();

	[NonSerialized]
	[ReadOnly]
	public Sorter AssignedSorter;
}
