using System.IO;
using Assets.Scripts.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class WorldSave : UserInterfaceBase
{
	[SerializeField]
	private Button _button;

	public TextMeshProUGUI Filename;

	public TextMeshProUGUI Date;

	private SavePanel _savePanel;

	public void Initialize(SavePanel savePanel)
	{
		_savePanel = savePanel;
		_button.onClick.AddListener(WorldSaveClicked);
	}

	public void Show(SaveFileInfo saveFileInfo)
	{
		SetActive(active: true);
		Filename.text = Path.GetFileNameWithoutExtension(saveFileInfo.FileInfo.Name);
	}

	public void Hide()
	{
		SetActive(active: false);
	}

	private void WorldSaveClicked()
	{
		_savePanel.WorldSaveClicked(this);
	}
}
