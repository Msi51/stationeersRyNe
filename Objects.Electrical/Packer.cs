using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Objects.Components;

namespace Objects.Electrical;

public class Packer : ImportExport
{
	private string[] _modeStrings;

	private bool _dispense;

	private ImportInfo Import1 => Imports[0];

	private ImportInfo Import2 => Imports[1];

	private ExportInfo Export1 => Exports[0];

	public override string[] ModeStrings => EnumCollections.DeviceModes;

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (!OnOff || !Powered)
		{
			return;
		}
		DynamicThing occupant;
		bool flag = Import2.Slot.Contains<DynamicThing>(out occupant);
		DeviceMode deviceMode = (DeviceMode)Mode;
		if (IsImportClosed(Import1) && Import1.ImportingThing != null)
		{
			if (flag && GetAvailableSlot(Import1.ImportingThing, occupant, out var availableSlot))
			{
				OnServer.MoveToSlot(Import1.ImportingThing, availableSlot);
			}
			else if (deviceMode == DeviceMode.Auto && IsNextExportReady(Export1))
			{
				OnServer.MoveToSlot(Import1.ImportingThing, Export1.Slot);
			}
		}
		switch (deviceMode)
		{
		case DeviceMode.Auto:
		{
			if (IsNextExportReady(Export1) && IsImportClosed(Import2) && flag && (IsOpen || !GetAvailableSlot(null, occupant, out var _)))
			{
				OnServer.MoveToSlot(Import2.ImportingThing, Export1.Slot);
			}
			if (CanBeginExport(Export1))
			{
				OnServer.Interact(base.InteractExport, 1);
			}
			break;
		}
		case DeviceMode.Logic:
			if (_dispense)
			{
				if (IsNextExportReady(Export1) && IsImportClosed(Import2) && flag)
				{
					OnServer.MoveToSlot(Import2.ImportingThing, Export1.Slot);
				}
				if (CanBeginExport(Export1))
				{
					OnServer.Interact(base.InteractExport, 1);
					_dispense = false;
				}
			}
			break;
		}
	}

	public override bool CanBeginImport(ImportInfo import)
	{
		if (import == Import1)
		{
			if (base.CanBeginImport(import) && OnOff)
			{
				return Powered;
			}
			return false;
		}
		if (import == Import2)
		{
			if (base.CanBeginImport(import) && OnOff)
			{
				return Powered;
			}
			return false;
		}
		return base.CanBeginImport(import);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Dispense)
		{
			return base.IsStructureCompleted;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Dispense)
		{
			return base.IsStructureCompleted;
		}
		return base.CanLogicWrite(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Dispense)
		{
			if (!_dispense)
			{
				return 0.0;
			}
			return 1.0;
		}
		return base.GetLogicValue(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (logicType == LogicType.Dispense)
		{
			_dispense = value >= 1.0;
		}
		else
		{
			base.SetLogicValue(logicType, value);
		}
	}

	private bool GetAvailableSlot(DynamicThing importingThing, DynamicThing container, out Slot availableSlot)
	{
		Slot.Class obj = ((importingThing != null) ? importingThing.SlotType : Slot.Class.None);
		foreach (Slot slot in container.Slots)
		{
			if (slot.IsEmpty() && (importingThing == null || slot.Type == Slot.Class.None || slot.Type == obj))
			{
				availableSlot = slot;
				return true;
			}
		}
		availableSlot = null;
		return false;
	}
}
