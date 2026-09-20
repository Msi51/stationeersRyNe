using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Serialization;

[Serializable]
public class MainMenu
{
	public GameObject MenuPanel;

	public List<Animator> Animator = new List<Animator>();

	public List<ButtonSizeControl> SizeControl = new List<ButtonSizeControl>();

	public GameObject ButtonGrid;

	public GameObject SmallButtonGrid;
}
