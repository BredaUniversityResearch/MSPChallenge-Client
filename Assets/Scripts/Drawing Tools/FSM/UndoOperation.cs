using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public abstract class UndoOperation
{
    public enum EditMode { Create, Modify, SetOperation }

	public abstract void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false);
}

public class BatchUndoOperationMarker : UndoOperation
{
    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        redo = new BatchUndoOperationMarker();
    }
}

public class ConcatOperationMarker : UndoOperation
{
    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        redo = new ConcatOperationMarker();
    }
}

public interface IPlanLayerHolder
{
    PlanLayer GetPlanLayer();
}

public interface ISubEntityHolder
{
    SubEntity GetSubEntity();
}

public abstract class PolygonUndoOperation : UndoOperation, IPlanLayerHolder, ISubEntityHolder
{
    public PolygonSubEntity SubEntity { get; private set; }
    public PlanLayer PlanLayer { get; private set; }

    public PolygonUndoOperation(PolygonSubEntity subEntity, PlanLayer planLayer)
    {
        SubEntity = subEntity;
        PlanLayer = planLayer;
    }

    public PlanLayer GetPlanLayer()
    {
        return PlanLayer;
    }

    public SubEntity GetSubEntity()
    {
        return SubEntity;
    }
}

public abstract class LineStringUndoOperation : UndoOperation, IPlanLayerHolder, ISubEntityHolder
{
    public LineStringSubEntity SubEntity { get; private set; }
    public PlanLayer PlanLayer { get; private set; }

    public LineStringUndoOperation(LineStringSubEntity subEntity, PlanLayer planLayer)
    {
        SubEntity = subEntity;
        PlanLayer = planLayer;
    }
    public PlanLayer GetPlanLayer()
    {
        return PlanLayer;
    }

    public SubEntity GetSubEntity()
    {
        return SubEntity;
    }
}

public abstract class PointUndoOperation : UndoOperation, IPlanLayerHolder, ISubEntityHolder
{
    public PointSubEntity SubEntity { get; private set; }
    public PlanLayer PlanLayer { get; private set; }

    public PointUndoOperation(PointSubEntity subEntity, PlanLayer planLayer)
    {
        SubEntity = subEntity;
        PlanLayer = planLayer;
    }
    public PlanLayer GetPlanLayer()
    {
        return PlanLayer;
    }

    public SubEntity GetSubEntity()
    {
        return SubEntity;
    }
}

public class ModifyPolygonOperation : PolygonUndoOperation
{
	protected SubEntityDataCopy previousData;
	EditMode editMode;

    public ModifyPolygonOperation(PolygonSubEntity subEntity, PlanLayer planLayer, SubEntityDataCopy previousData, EditMode editMode) : base(subEntity, planLayer)
    {
		this.previousData = previousData;
        this.editMode = editMode;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
		SubEntityDataCopy newData = SubEntity.GetDataCopy();
		SubEntity.SetDataToCopy(previousData);
		SubEntity.PerformValidityCheck(editMode == EditMode.Create, editMode == EditMode.Create);
        redo = new ModifyPolygonOperation(SubEntity, PlanLayer, newData, editMode);

        if (editMode == EditMode.Create)
        {
            SubEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
			if(!totalUndo)
				fsm.SetCurrentState(new CreatingPolygonState(fsm, SubEntity, PlanLayer));
        }
        else if (editMode == EditMode.Modify)
        {
            SubEntity.restrictionNeedsUpdate = true;
            SubEntity.Entity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Default); // redraw entire entity in case of entity type change
			if (!totalUndo)
				fsm.SetCurrentState(new SelectPolygonsState(fsm, PlanLayer));
        }
        else if (editMode == EditMode.SetOperation)
        {
            SubEntity.RedrawGameObject(SubEntityDrawMode.Default);
			if (!totalUndo)
				fsm.SetCurrentState(new SetOperationsState(fsm));
        }
    }
}

public class ModifyLineStringOperation : LineStringUndoOperation
{
	protected SubEntityDataCopy previousData;
    protected EditMode editMode;

    public ModifyLineStringOperation(LineStringSubEntity subEntity, PlanLayer planLayer, SubEntityDataCopy previousData, EditMode editMode) : base(subEntity, planLayer)
    {
		this.previousData = previousData;
        this.editMode = editMode;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
		SubEntityDataCopy newData = SubEntity.GetDataCopy();
        SubEntity.SetDataToCopy(previousData);
        redo = new ModifyLineStringOperation(SubEntity, PlanLayer, newData, editMode);
        
        if (editMode == EditMode.Create)
        {
            SubEntity.Entity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.BeingCreated); // redraw entire entity in case of entity type change
			if (!totalUndo)
				fsm.SetCurrentState(new CreatingLineStringState(fsm, PlanLayer, SubEntity));
        }
        else if (editMode == EditMode.Modify)
        {
            SubEntity.restrictionNeedsUpdate = true;
            SubEntity.RedrawGameObject(SubEntityDrawMode.Default);
			if (!totalUndo)
				fsm.SetCurrentState(new SelectLineStringsState(fsm, PlanLayer));
        }
    }
}

public class ModifyEnergyLineStringOperation : ModifyLineStringOperation
{
    public ModifyEnergyLineStringOperation(LineStringSubEntity subEntity, PlanLayer planLayer, SubEntityDataCopy previousData, EditMode editMode) : base(subEntity, planLayer, previousData, editMode)
    {}

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
		SubEntityDataCopy newData = SubEntity.GetDataCopy();
		SubEntity.SetDataToCopy(previousData);
		redo = new ModifyEnergyLineStringOperation(SubEntity, PlanLayer, newData, editMode);

        if (editMode == EditMode.Create)
        {
            SubEntity.Entity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.BeingCreated); // redraw entire entity in case of entity type change
			if (!totalUndo)
				fsm.SetCurrentState(new CreatingEnergyLineStringState(fsm, PlanLayer, SubEntity));
        }
        else if (editMode == EditMode.Modify)
        {
            SubEntity.restrictionNeedsUpdate = true;
            //SubEntity.RedrawGameObject(SubEntityDrawMode.PlanReference);
            SubEntity.RedrawGameObject();
        }
    }

}

public class ModifyPointOperation : PointUndoOperation
{
	protected SubEntityDataCopy previousData;

	public ModifyPointOperation(PointSubEntity subEntity, PlanLayer planLayer, SubEntityDataCopy previousData) : base(subEntity, planLayer)
    {
		this.previousData = previousData;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
		SubEntityDataCopy newData = SubEntity.GetDataCopy();
		SubEntity.SetDataToCopy(previousData);
		redo = new ModifyPointOperation(SubEntity, PlanLayer, newData);

        // redraw entire entity in case of entity type change
        SubEntity.Entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
		if (!totalUndo)
			fsm.SetCurrentState(new EditPointsState(fsm, PlanLayer));
    }
}

public class ModifyEnergyPointOperation : ModifyPointOperation
{

    public ModifyEnergyPointOperation(PointSubEntity subEntity, PlanLayer planLayer, SubEntityDataCopy previousData) : base(subEntity, planLayer, previousData)
    { }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
		SubEntityDataCopy newData = SubEntity.GetDataCopy();
		SubEntity.SetDataToCopy(previousData);
		redo = new ModifyEnergyPointOperation(SubEntity, PlanLayer, newData);

        // redraw entire entity in case of entity type change
        SubEntity.Entity.RedrawGameObjects(CameraManager.Instance.gameCamera);

		//Changed target state to energy variant
		if (!totalUndo)
			fsm.SetCurrentState(new EditEnergyPointsState(fsm, PlanLayer));
    }
}

public class CreatePolygonOperation : PolygonUndoOperation
{
    EditMode editMode;
    protected bool uncreate;

    public CreatePolygonOperation(PolygonSubEntity subEntity, PlanLayer planLayer, EditMode editMode, bool uncreate = false) : base(subEntity, planLayer)
    {
        this.editMode = editMode;
        this.uncreate = uncreate;
    }

    public bool IsCreatedManually()
    {
        return editMode == EditMode.Create;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        (SubEntity.Entity.Layer as PolygonLayer).RemoveSubEntity(SubEntity, uncreate);

        redo = new RemovePolygonOperation(SubEntity, PlanLayer, editMode, uncreate);

        SubEntity.RemoveGameObject();
		if (!totalUndo)
		{
			if (editMode == EditMode.Create)
			{
				fsm.SetCurrentState(new StartCreatingPolygonState(fsm, PlanLayer));
			}
			else if (editMode == EditMode.Modify)
			{
				fsm.SetCurrentState(new SelectPolygonsState(fsm, PlanLayer));
			}
			else if (editMode == EditMode.SetOperation)
			{
				fsm.SetCurrentState(new SetOperationsState(fsm));
			}
		}
    }
}

public class CreateLineStringOperation : LineStringUndoOperation
{
    protected EditMode editMode;
    protected bool uncreate;

    public CreateLineStringOperation(LineStringSubEntity subEntity, PlanLayer planLayer, EditMode editMode, bool uncreate = false) : base(subEntity, planLayer)
    {
        this.editMode = editMode;
        this.uncreate = uncreate;
    }

    public bool IsCreatedManually()
    {
        return editMode == EditMode.Create;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        (SubEntity.Entity.Layer as LineStringLayer).RemoveSubEntity(SubEntity, uncreate);

        redo = new RemoveLineStringOperation(SubEntity, PlanLayer, editMode, uncreate);

        SubEntity.RemoveGameObject();
		if (!totalUndo)
		{
			if (editMode == EditMode.Create)
			{
				fsm.SetCurrentState(new StartCreatingLineStringState(fsm, PlanLayer));
			}
			else if (editMode == EditMode.Modify)
			{
				fsm.SetCurrentState(new SelectLineStringsState(fsm, PlanLayer));
			}
		}
    }
}

public class CreateEnergyLineStringOperation : CreateLineStringOperation
{
    public CreateEnergyLineStringOperation(LineStringSubEntity subEntity, PlanLayer planLayer, EditMode editMode, bool uncreate = false) : base(subEntity, planLayer, editMode, uncreate)
    {
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        //This also automatically removes references
        (SubEntity.Entity.Layer as LineStringLayer).RemoveSubEntity(SubEntity, uncreate);

        redo = new RemoveEnergyLineStringOperation(SubEntity, PlanLayer, editMode, uncreate);

        //(SubEntity as EnergyLineStringSubEntity).RemoveReferences();
        SubEntity.RemoveGameObject();
		if (!totalUndo)
		{
			if (editMode == EditMode.Create)
			{
				fsm.SetCurrentState(new StartCreatingEnergyLineStringState(fsm, PlanLayer));
			}
			else if (editMode == EditMode.Modify)
			{
				fsm.SetCurrentState(new SelectLineStringsState(fsm, PlanLayer));
			}
		}
    }
}

public class CreatePointOperation : PointUndoOperation
{
    protected bool uncreate;

    public CreatePointOperation(PointSubEntity subEntity, PlanLayer planLayer, bool uncreate = false) : base(subEntity, planLayer)
    {
        this.uncreate = uncreate;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        (SubEntity.Entity.Layer as PointLayer).RemoveSubEntity(SubEntity, uncreate);

        redo = new RemovePointOperation(SubEntity, PlanLayer, uncreate);

        SubEntity.RemoveGameObject();

		if (!totalUndo)
			fsm.SetCurrentState(new CreatePointsState(fsm, PlanLayer));
    }
}

public class CreateEnergyPointOperation : CreatePointOperation
{
    bool returnToEdit;
    public CreateEnergyPointOperation(PointSubEntity subEntity, PlanLayer planLayer, bool uncreate = false, bool returnToEdit = false) : base(subEntity, planLayer, uncreate)
    {
        this.returnToEdit = returnToEdit;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        (SubEntity.Entity.Layer as PointLayer).RemoveSubEntity(SubEntity, uncreate);

        redo = new RemoveEnergyPointOperation(SubEntity, PlanLayer, uncreate, returnToEdit);

        SubEntity.RemoveGameObject();

		if (!totalUndo)
		{
			if (returnToEdit)
				fsm.SetCurrentState(new EditEnergyPointsState(fsm, PlanLayer));
			else
				fsm.SetCurrentState(new CreateEnergyPointState(fsm, PlanLayer));
		}
    }
}

public class RemovePolygonOperation : PolygonUndoOperation
{
    EditMode editMode;
    protected bool recreate;

    public RemovePolygonOperation(PolygonSubEntity subEntity, PlanLayer planLayer, EditMode editMode, bool recreate = false) : base(subEntity, planLayer)
    {
        this.editMode = editMode;
        this.recreate = recreate;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        (SubEntity.Entity.Layer as PolygonLayer).RestoreSubEntity(SubEntity, recreate);

        SubEntity.PerformValidityCheck(editMode == EditMode.Create, editMode == EditMode.Create);

        redo = new CreatePolygonOperation(SubEntity, PlanLayer, editMode, recreate);

		if (!totalUndo)
		{
			if (editMode == EditMode.Create)
			{
				SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
				fsm.SetCurrentState(new CreatingPolygonState(fsm, SubEntity, PlanLayer));
			}
			else if (editMode == EditMode.Modify)
			{
				SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
				fsm.SetCurrentState(new SelectPolygonsState(fsm, PlanLayer));
			}
			else if (editMode == EditMode.SetOperation)
			{
				SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
				fsm.SetCurrentState(new SetOperationsState(fsm));
			}
        }
		else
			SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
	}
}

public class RemoveLineStringOperation : LineStringUndoOperation
{
    protected EditMode editMode;
    protected bool recreate;

    public RemoveLineStringOperation(LineStringSubEntity subEntity, PlanLayer planLayer, EditMode editMode, bool recreate = false) : base(subEntity, planLayer)
    {
        this.editMode = editMode;
        this.recreate = recreate;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        (SubEntity.Entity.Layer as LineStringLayer).RestoreSubEntity(SubEntity, recreate);

        redo = new CreateLineStringOperation(SubEntity, PlanLayer, editMode, recreate);

		if (!totalUndo)
		{
			if (editMode == EditMode.Create)
			{
				SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
				fsm.SetCurrentState(new CreatingLineStringState(fsm, PlanLayer, SubEntity));
			}
			else if (editMode == EditMode.Modify)
			{
				SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
				fsm.SetCurrentState(new SelectLineStringsState(fsm, PlanLayer));
			}
		}
		else
			SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
	}
}

public class RemoveEnergyLineStringOperation : RemoveLineStringOperation
{
    bool pointWasDeleted;

    public RemoveEnergyLineStringOperation(LineStringSubEntity subEntity, PlanLayer planLayer, EditMode editMode, bool recreate = false, bool pointWasDeleted = false) : base(subEntity, planLayer, editMode, recreate)
    {
        this.pointWasDeleted = pointWasDeleted;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        //This also automatically restores references
        (SubEntity.Entity.Layer as LineStringLayer).RestoreSubEntity(SubEntity, recreate);

        redo = new CreateEnergyLineStringOperation(SubEntity, PlanLayer, editMode, recreate);
		if (!totalUndo)
		{
			if (editMode == EditMode.Create)
			{
				SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
				fsm.SetCurrentState(new CreatingEnergyLineStringState(fsm, PlanLayer, SubEntity));
			}
			else if (editMode == EditMode.Modify)
			{
				if (pointWasDeleted)
					//SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.PlanReference);
					SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
				else
				{
					SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
					fsm.SetCurrentState(new SelectLineStringsState(fsm, PlanLayer));
				}
			}
		}
		else
			SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
	}
}

public class RemovePointOperation : PointUndoOperation
{
    protected bool recreate;

    public RemovePointOperation(PointSubEntity subEntity, PlanLayer planLayer, bool recreate = false) : base(subEntity, planLayer)
    {
        this.recreate = recreate;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        (SubEntity.Entity.Layer as PointLayer).RestoreSubEntity(SubEntity, recreate);

        redo = new CreatePointOperation(SubEntity, PlanLayer, recreate);

        SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform);
		if (!totalUndo)
			fsm.SetCurrentState(new EditPointsState(fsm, PlanLayer));
    }
}

public class RemoveEnergyPointOperation : RemovePointOperation
{
    bool returnToEdit;
    public RemoveEnergyPointOperation(PointSubEntity subEntity, PlanLayer planLayer, bool recreate = false, bool returnToEdit = true) : base(subEntity, planLayer, recreate)
    {
        this.returnToEdit = returnToEdit;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        (SubEntity.Entity.Layer as PointLayer).RestoreSubEntity(SubEntity, recreate);

        redo = new CreateEnergyPointOperation(SubEntity, PlanLayer, recreate, returnToEdit);

        SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform);

		//Changed target state to energy variant
		if (!totalUndo)
			fsm.SetCurrentState(new EditEnergyPointsState(fsm, PlanLayer));
    }
}

public class ModifyPolygonRemovalPlanOperation : PolygonUndoOperation
{
    bool wasScheduledForRemovalBeforeOperation;

    public ModifyPolygonRemovalPlanOperation(PolygonSubEntity subEntity, PlanLayer planLayer, bool wasScheduledForRemovalBeforeOperation) : base(subEntity, planLayer)
    {
        this.wasScheduledForRemovalBeforeOperation = wasScheduledForRemovalBeforeOperation;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        //redo = new ModifyPolygonRemovalPlanOperation(SubEntity, PlanLayer, PlanLayer.RemovedGeometry.Contains(SubEntity.GetPersistentID()));
        redo = new ModifyPolygonRemovalPlanOperation(SubEntity, PlanLayer, !wasScheduledForRemovalBeforeOperation);
        if (wasScheduledForRemovalBeforeOperation)
        {
            PlanLayer.RemovedGeometry.Add(SubEntity.GetPersistentID());
            SubEntity.RemoveDependencies();
        }
        else
        {
            PlanLayer.RemovedGeometry.Remove(SubEntity.GetPersistentID());
            SubEntity.RestoreDependencies();
        }

        SubEntity.RedrawGameObject();
		if (!totalUndo)
			fsm.SetCurrentState(new SelectPolygonsState(fsm, PlanLayer));
    }
}

public class ModifyLineStringRemovalPlanOperation : LineStringUndoOperation
{
    bool wasScheduledForRemovalBeforeOperation;

    public ModifyLineStringRemovalPlanOperation(LineStringSubEntity subEntity, PlanLayer planLayer, bool wasScheduledForRemovalBeforeOperation) : base(subEntity, planLayer)
    {
        this.wasScheduledForRemovalBeforeOperation = wasScheduledForRemovalBeforeOperation;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        //redo = new ModifyLineStringRemovalPlanOperation(SubEntity, PlanLayer, PlanLayer.RemovedGeometry.Contains(SubEntity.GetPersistentID()));
        redo = new ModifyLineStringRemovalPlanOperation(SubEntity, PlanLayer, !wasScheduledForRemovalBeforeOperation);
        if (wasScheduledForRemovalBeforeOperation)
        {
            PlanLayer.RemovedGeometry.Add(SubEntity.GetPersistentID());
            SubEntity.RemoveDependencies();
        }
        else
        {
            PlanLayer.RemovedGeometry.Remove(SubEntity.GetPersistentID());
            SubEntity.RestoreDependencies();
        }

        SubEntity.RedrawGameObject();
		if (!totalUndo)
			fsm.SetCurrentState(new SelectLineStringsState(fsm, PlanLayer));
    }
}

public class ModifyPointRemovalPlanOperation : PointUndoOperation
{
    bool wasScheduledForRemovalBeforeOperation;

    public ModifyPointRemovalPlanOperation(PointSubEntity subEntity, PlanLayer planLayer, bool wasScheduledForRemovalBeforeOperation) : base(subEntity, planLayer)
    {
        this.wasScheduledForRemovalBeforeOperation = wasScheduledForRemovalBeforeOperation;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        //redo = new ModifyPointRemovalPlanOperation(SubEntity, PlanLayer, PlanLayer.RemovedGeometry.Contains(SubEntity.GetPersistentID()));
        redo = new ModifyPointRemovalPlanOperation(SubEntity, PlanLayer, !wasScheduledForRemovalBeforeOperation);

        if (wasScheduledForRemovalBeforeOperation)
        {
            PlanLayer.RemovedGeometry.Add(SubEntity.GetPersistentID());
            SubEntity.RemoveDependencies();
        }
        else
        {
            PlanLayer.RemovedGeometry.Remove(SubEntity.GetPersistentID());
            SubEntity.RestoreDependencies();
        }

        SubEntity.RedrawGameObject();
		if (!totalUndo)
		{
			if (PlanLayer.BaseLayer.IsEnergyPointLayer())
				fsm.SetCurrentState(new EditEnergyPointsState(fsm, PlanLayer));
			else
				fsm.SetCurrentState(new EditPointsState(fsm, PlanLayer));
		}
    }
}

public class FinalizePolygonOperation : PolygonUndoOperation
{
    public FinalizePolygonOperation(PolygonSubEntity subEntity, PlanLayer planLayer) : base(subEntity, planLayer)
    {
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        SubEntity.AddPoint(Vector3.zero);

        SubEntity.PerformValidityCheck(true, true);
        //if (SubEntity is EnergyPolygonSubEntity)
        //    (SubEntity as EnergyPolygonSubEntity).UnFinalizePoly();

        SubEntity.HideRestrictionArea();
        SubEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
		if (!totalUndo)
			fsm.SetCurrentState(new CreatingPolygonState(fsm, SubEntity, PlanLayer));

        redo = new UnfinalizePolygonOperation(SubEntity, PlanLayer);
    }
}

public class FinalizeLineStringOperation : LineStringUndoOperation
{
    public FinalizeLineStringOperation(LineStringSubEntity subEntity, PlanLayer planLayer) : base(subEntity, planLayer)
    {
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        SubEntity.AddPoint(Vector3.zero);

        SubEntity.HideRestrictionArea();
        SubEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
		if (!totalUndo)
			fsm.SetCurrentState(new CreatingLineStringState(fsm, PlanLayer, SubEntity));

        redo = new UnfinalizeLineStringOperation(SubEntity, PlanLayer);
    }
}

public class FinalizeEnergyLineStringOperation : FinalizeLineStringOperation
{
	EnergyPointSubEntity point;

	public FinalizeEnergyLineStringOperation(LineStringSubEntity subEntity, PlanLayer planLayer, EnergyPointSubEntity point) : base(subEntity, planLayer)
    {
		this.point = point;
	}

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        if (SubEntity.GetPointCount() == 1) //Points are directly finalized upon creation
        {
            LineStringLayer layer = SubEntity.Entity.Layer as LineStringLayer;
            layer.RemoveSubEntity(SubEntity, true);
			if (!totalUndo)
				fsm.SetCurrentState(new StartCreatingEnergyLineStringState(fsm, PlanLayer));
        }
        else
        {
			//Remove connections between cable and point
			EnergyLineStringSubEntity cable = SubEntity as EnergyLineStringSubEntity;
			Connection connectionToRemove = null;
			foreach (Connection con in cable.connections)
			{
				if (con.point == point)
				{
					connectionToRemove = con;					
					break;
				}
			}
			cable.RemoveConnection(connectionToRemove);
			point.RemoveConnection(connectionToRemove);

			SubEntity.HideRestrictionArea();
            SubEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
			if (!totalUndo)
				fsm.SetCurrentState(new CreatingEnergyLineStringState(fsm, PlanLayer, SubEntity));
		}

        redo = new UnfinalizeEnergyLineStringOperation(SubEntity, PlanLayer, point);
    }
}

public class UnfinalizePolygonOperation : PolygonUndoOperation
{
    public UnfinalizePolygonOperation(PolygonSubEntity subEntity, PlanLayer planLayer) : base(subEntity, planLayer)
    {
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        (fsm.GetCurrentState() as CreatingPolygonState).FinalizePolygon();

        redo = new FinalizePolygonOperation(SubEntity, PlanLayer);
    }
}

public class UnfinalizeLineStringOperation : LineStringUndoOperation
{
    public UnfinalizeLineStringOperation(LineStringSubEntity subEntity, PlanLayer planLayer) : base(subEntity, planLayer)
    {
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        (fsm.GetCurrentState() as CreatingLineStringState).FinalizeLineString();

        redo = new FinalizeLineStringOperation(SubEntity, PlanLayer);
    }
}

public class UnfinalizeEnergyLineStringOperation : UnfinalizeLineStringOperation
{
	EnergyPointSubEntity point;

	public UnfinalizeEnergyLineStringOperation(LineStringSubEntity subEntity, PlanLayer planLayer, EnergyPointSubEntity point) : base(subEntity, planLayer)
    {
		this.point = point;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        if (SubEntity.GetPointCount() == 1) //Points are directly finalized upon creation
        {
            LineStringLayer layer = SubEntity.Entity.Layer as LineStringLayer;
            layer.RestoreSubEntity(SubEntity, true);
			if (!totalUndo)
				fsm.SetCurrentState(new StartCreatingEnergyLineStringState(fsm, PlanLayer));
        }
        else
        {
            (fsm.GetCurrentState() as CreatingEnergyLineStringState).FinalizeEnergyLineString(point);
        }

        redo = new FinalizeEnergyLineStringOperation(SubEntity, PlanLayer, point);
    }
}

public class ChangeConnectionOperation : UndoOperation
{
    EnergyLineStringSubEntity subEnt;
    Connection oldConn;
    Connection newCon;

    public ChangeConnectionOperation(EnergyLineStringSubEntity subEnt, Connection oldConn, Connection newCon)
    {
        this.subEnt = subEnt;
        this.oldConn = oldConn;
        this.newCon = newCon;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        subEnt.RemoveConnection(newCon);
        subEnt.AddConnection(oldConn);
        newCon.point.RemoveConnection(newCon);
        oldConn.point.AddConnection(oldConn);

        redo = new ChangeConnectionOperation(subEnt, newCon, oldConn);
    }
}

public class ReconnectCableToPoint : UndoOperation
{
    Connection oldConnection, newConnection;

    public ReconnectCableToPoint(Connection oldConnection, Connection newConnection)
    {
        this.oldConnection = oldConnection;
        this.newConnection = newConnection;
    }

    public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
    {
        //Connections have the same point but different cables
        oldConnection.point.RemoveConnection(newConnection);
        oldConnection.point.AddConnection(oldConnection);
        redo = new ReconnectCableToPoint(newConnection, oldConnection);
    }
}

public class SwitchLayerOperation : UndoOperation
{
    PlanLayer previousLayer, newLayer;

    public SwitchLayerOperation(PlanLayer previousLayer, PlanLayer newLayer)
    {
        this.previousLayer = previousLayer;
        this.newLayer = newLayer;
    }

	public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
	{
		redo = new SwitchLayerOperation(newLayer, previousLayer);
		if (!totalUndo)
		{
			PlanDetails.LayersTab.StartEditingLayer(previousLayer, true);
		}
    }
}
