using System;
using System.Collections.Generic;
using Assets.Scripts.Objects.Electrical;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Motherboard;

public class ScreenFabricator : MonoBehaviour
{
	public Text Title;

	public Image BackgroundImage;

	public VerticalLayoutGroup JobGrid;

	public Button ButtonRename;

	public Button ButtonAddJob;

	public Button ButtonPlay;

	public Image ButtonPlayImage;

	public Button ButtonIncrease;

	public Button ButtonDecrease;

	public Button ButtonDelete;

	public Sprite ImagePlay;

	public Sprite ImagePause;

	public GameObject ConstructingPanel;

	public Image ConstructingImage;

	public Text ConstructingTitle;

	public Slider ConstructingProgress;

	public Text ConstructingResources;

	[ReadOnly]
	public List<ScreenConstructionJob> ScreenJobs = new List<ScreenConstructionJob>();

	public List<FabricatorJob> JobReferences = new List<FabricatorJob>();

	[NonSerialized]
	[ReadOnly]
	public Fabricator AssignedFabricator;
}
