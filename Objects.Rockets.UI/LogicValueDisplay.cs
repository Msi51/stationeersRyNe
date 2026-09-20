using System;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Objects.Rockets.UI.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Objects.Rockets.UI;

public class LogicValueDisplay : UserInterfaceBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	private TextMeshProUGUI _nameTextMesh;

	[SerializeField]
	private TMP_InputField _valueInputField;

	private LogicType _logicType;

	private LogicValueListPanel _listPanel;

	private RocketMotherboard _motherboard;

	public LogicValueModel LogicValueModel;

	public LogicType LogicType => _logicType;

	public void Initialize(LogicValueModel model, LogicValueListPanel listPanel, RocketMotherboard motherboard)
	{
		LogicValueModel = model;
		_motherboard = motherboard;
		_logicType = model.LogicType;
		_listPanel = listPanel;
		_valueInputField.interactable = model.CanLogicWrite;
		_nameTextMesh.text = model.DisplayName;
		Refresh(model);
	}

	public void Refresh(LogicValueModel model)
	{
		if (!_valueInputField.isFocused)
		{
			double num = Math.Round(model.Value, 3, MidpointRounding.AwayFromZero);
			_valueInputField.SetTextWithoutNotify(num.ToString());
		}
	}

	private void OnValueChanged(string value)
	{
		if (float.TryParse(value, out var result))
		{
			_motherboard.LogicValueChanged(result, _logicType, LogicValueModel.DeviceReferenceId);
		}
	}

	private new void OnEnable()
	{
		_valueInputField.onValueChanged.AddListener(OnValueChanged);
	}

	private new void OnDisable()
	{
		_valueInputField.onValueChanged.RemoveListener(OnValueChanged);
	}

	public new void OnPointerEnter(PointerEventData eventData)
	{
		_listPanel.LogicValueTextBeginHover(this);
	}

	public new void OnPointerExit(PointerEventData eventData)
	{
		_listPanel.LogicValueTextEndHover();
	}
}
