using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class UndoOperation
	{
		public enum EditMode { Create, Modify }

		public abstract void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false);
	}

	public class BatchUndoOperationMarker : UndoOperation
	{
		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			a_redo = new BatchUndoOperationMarker();
		}
	}

	public class ConcatOperationMarker : UndoOperation
	{
		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			a_redo = new ConcatOperationMarker();
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
		protected PolygonSubEntity SubEntity { get; private set; }
		protected PlanLayer PlanLayer { get; private set; }

		protected PolygonUndoOperation(PolygonSubEntity a_subEntity, PlanLayer a_planLayer)
		{
			SubEntity = a_subEntity;
			PlanLayer = a_planLayer;
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
		protected LineStringSubEntity SubEntity { get; private set; }
		protected PlanLayer PlanLayer { get; private set; }

		protected LineStringUndoOperation(LineStringSubEntity a_subEntity, PlanLayer a_planLayer)
		{
			SubEntity = a_subEntity;
			PlanLayer = a_planLayer;
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
		protected PointSubEntity SubEntity { get; private set; }
		protected PlanLayer PlanLayer { get; private set; }

		protected PointUndoOperation(PointSubEntity a_subEntity, PlanLayer a_planLayer)
		{
			SubEntity = a_subEntity;
			PlanLayer = a_planLayer;
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
		private SubEntityDataCopy m_previousData;
		private EditMode m_editMode;

		public ModifyPolygonOperation(PolygonSubEntity a_subEntity, PlanLayer a_planLayer, SubEntityDataCopy a_previousData, EditMode a_editMode) : base(a_subEntity, a_planLayer)
		{
			m_previousData = a_previousData;
			m_editMode = a_editMode;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			SubEntityDataCopy newData = SubEntity.GetDataCopy();
			SubEntity.SetDataToCopy(m_previousData);
			SubEntity.PerformValidityCheck(m_editMode == EditMode.Create, m_editMode == EditMode.Create);
			a_redo = new ModifyPolygonOperation(SubEntity, PlanLayer, newData, m_editMode);

			if (m_editMode == EditMode.Create)
			{
				SubEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
				if(!a_totalUndo)
					a_fsm.SetCurrentState(new CreatingPolygonState(a_fsm, SubEntity, PlanLayer));
			}
			else
			{
				SubEntity.m_restrictionNeedsUpdate = true;
				SubEntity.m_entity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Default); // redraw entire entity in case of entity type change
				if (!a_totalUndo)
					a_fsm.SetCurrentState(new SelectPolygonsState(a_fsm, PlanLayer));
			}
		}
	}

	public class ModifyLineStringOperation : LineStringUndoOperation
	{
		protected SubEntityDataCopy m_previousData;
		protected EditMode m_editMode;

		public ModifyLineStringOperation(LineStringSubEntity a_subEntity, PlanLayer a_planLayer, SubEntityDataCopy a_previousData, EditMode a_editMode) : base(a_subEntity, a_planLayer)
		{
			m_previousData = a_previousData;
			m_editMode = a_editMode;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			SubEntityDataCopy newData = SubEntity.GetDataCopy();
			SubEntity.SetDataToCopy(m_previousData);
			a_redo = new ModifyLineStringOperation(SubEntity, PlanLayer, newData, m_editMode);
        
			if (m_editMode == EditMode.Create)
			{
				SubEntity.m_entity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.BeingCreated); // redraw entire entity in case of entity type change
				if (!a_totalUndo)
					a_fsm.SetCurrentState(new CreatingLineStringState(a_fsm, PlanLayer, SubEntity));
			}
			else if (m_editMode == EditMode.Modify)
			{
				SubEntity.m_restrictionNeedsUpdate = true;
				SubEntity.RedrawGameObject(SubEntityDrawMode.Default);
				if (!a_totalUndo)
					a_fsm.SetCurrentState(new SelectLineStringsState(a_fsm, PlanLayer));
			}
		}
	}

	public class ModifyEnergyLineStringOperation : ModifyLineStringOperation
	{
		public ModifyEnergyLineStringOperation(LineStringSubEntity a_subEntity, PlanLayer a_planLayer, SubEntityDataCopy a_previousData, EditMode a_editMode) : base(a_subEntity, a_planLayer, a_previousData, a_editMode)
		{}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			SubEntityDataCopy newData = SubEntity.GetDataCopy();
			SubEntity.SetDataToCopy(m_previousData);
			a_redo = new ModifyEnergyLineStringOperation(SubEntity, PlanLayer, newData, m_editMode);

			if (m_editMode == EditMode.Create)
			{
				SubEntity.m_entity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.BeingCreated); // redraw entire entity in case of entity type change
				if (!a_totalUndo)
					a_fsm.SetCurrentState(new CreatingEnergyLineStringState(a_fsm, PlanLayer, SubEntity));
			}
			else if (m_editMode == EditMode.Modify)
			{
				SubEntity.m_restrictionNeedsUpdate = true;
				//SubEntity.RedrawGameObject(SubEntityDrawMode.PlanReference);
				SubEntity.RedrawGameObject();
			}
		}

	}

	public class ModifyPointOperation : PointUndoOperation
	{
		protected SubEntityDataCopy m_previousData;

		public ModifyPointOperation(PointSubEntity a_subEntity, PlanLayer a_planLayer, SubEntityDataCopy a_previousData) : base(a_subEntity, a_planLayer)
		{
			m_previousData = a_previousData;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			SubEntityDataCopy newData = SubEntity.GetDataCopy();
			SubEntity.SetDataToCopy(m_previousData);
			a_redo = new ModifyPointOperation(SubEntity, PlanLayer, newData);

			// redraw entire entity in case of entity type change
			SubEntity.m_entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
			if (!a_totalUndo)
				a_fsm.SetCurrentState(new EditPointsState(a_fsm, PlanLayer));
		}
	}

	public class ModifyEnergyPointOperation : ModifyPointOperation
	{

		public ModifyEnergyPointOperation(PointSubEntity a_subEntity, PlanLayer a_planLayer, SubEntityDataCopy a_previousData) : base(a_subEntity, a_planLayer, a_previousData)
		{ }

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			SubEntityDataCopy newData = SubEntity.GetDataCopy();
			SubEntity.SetDataToCopy(m_previousData);
			a_redo = new ModifyEnergyPointOperation(SubEntity, PlanLayer, newData);

			// redraw entire entity in case of entity type change
			SubEntity.m_entity.RedrawGameObjects(CameraManager.Instance.gameCamera);

			//Changed target state to energy variant
			if (!a_totalUndo)
				a_fsm.SetCurrentState(new EditEnergyPointsState(a_fsm, PlanLayer));
		}
	}

	public class CreatePolygonOperation : PolygonUndoOperation
	{
		private EditMode m_editMode;
		private bool m_uncreate;

		public CreatePolygonOperation(PolygonSubEntity a_subEntity, PlanLayer a_planLayer, EditMode a_editMode, bool a_uncreate = false) : base(a_subEntity, a_planLayer)
		{
			m_editMode = a_editMode;
			m_uncreate = a_uncreate;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			(SubEntity.m_entity.Layer as PolygonLayer).RemoveSubEntity(SubEntity, m_uncreate);

			a_redo = new RemovePolygonOperation(SubEntity, PlanLayer, m_editMode, m_uncreate);

			SubEntity.RemoveGameObject();
			if (a_totalUndo)
				return;
			if (m_editMode == EditMode.Create)
			{
				a_fsm.SetCurrentState(new StartCreatingPolygonState(a_fsm, PlanLayer));
			}
			else
			{
				a_fsm.SetCurrentState(new SelectPolygonsState(a_fsm, PlanLayer));
			}
		}
	}

	public class CreateLineStringOperation : LineStringUndoOperation
	{
		protected EditMode m_editMode;
		protected bool m_uncreate;

		public CreateLineStringOperation(LineStringSubEntity a_subEntity, PlanLayer a_planLayer, EditMode a_editMode, bool a_uncreate = false) : base(a_subEntity, a_planLayer)
		{
			m_editMode = a_editMode;
			m_uncreate = a_uncreate;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			(SubEntity.m_entity.Layer as LineStringLayer).RemoveSubEntity(SubEntity, m_uncreate);

			a_redo = new RemoveLineStringOperation(SubEntity, PlanLayer, m_editMode, m_uncreate);

			SubEntity.RemoveGameObject();
			if (!a_totalUndo)
			{
				if (m_editMode == EditMode.Create)
				{
					a_fsm.SetCurrentState(new StartCreatingLineStringState(a_fsm, PlanLayer));
				}
				else if (m_editMode == EditMode.Modify)
				{
					a_fsm.SetCurrentState(new SelectLineStringsState(a_fsm, PlanLayer));
				}
			}
		}
	}

	public class CreateEnergyLineStringOperation : CreateLineStringOperation
	{
		public CreateEnergyLineStringOperation(LineStringSubEntity a_subEntity, PlanLayer a_planLayer, EditMode a_editMode, bool a_uncreate = false) : base(a_subEntity, a_planLayer, a_editMode, a_uncreate)
		{
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			//This also automatically removes references
			(SubEntity.m_entity.Layer as LineStringLayer).RemoveSubEntity(SubEntity, m_uncreate);

			a_redo = new RemoveEnergyLineStringOperation(SubEntity, PlanLayer, m_editMode, m_uncreate);

			//(SubEntity as EnergyLineStringSubEntity).RemoveReferences();
			SubEntity.RemoveGameObject();
			if (!a_totalUndo)
			{
				if (m_editMode == EditMode.Create)
				{
					a_fsm.SetCurrentState(new StartCreatingEnergyLineStringState(a_fsm, PlanLayer));
				}
				else if (m_editMode == EditMode.Modify)
				{
					a_fsm.SetCurrentState(new SelectLineStringsState(a_fsm, PlanLayer));
				}
			}
		}
	}

	public class CreatePointOperation : PointUndoOperation
	{
		protected bool m_uncreate;

		public CreatePointOperation(PointSubEntity a_subEntity, PlanLayer a_planLayer, bool a_uncreate = false) : base(a_subEntity, a_planLayer)
		{
			m_uncreate = a_uncreate;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			(SubEntity.m_entity.Layer as PointLayer).RemoveSubEntity(SubEntity, m_uncreate);

			a_redo = new RemovePointOperation(SubEntity, PlanLayer, m_uncreate);

			SubEntity.RemoveGameObject();

			if (!a_totalUndo)
				a_fsm.SetCurrentState(new CreatePointsState(a_fsm, PlanLayer));
		}
	}

	public class CreateEnergyPointOperation : CreatePointOperation
	{
		private bool m_returnToEdit;

		public CreateEnergyPointOperation(PointSubEntity a_subEntity, PlanLayer a_planLayer, bool a_uncreate = false, bool a_returnToEdit = false) : base(a_subEntity, a_planLayer, a_uncreate)
		{
			m_returnToEdit = a_returnToEdit;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			(SubEntity.m_entity.Layer as PointLayer).RemoveSubEntity(SubEntity, m_uncreate);

			a_redo = new RemoveEnergyPointOperation(SubEntity, PlanLayer, m_uncreate, m_returnToEdit);

			SubEntity.RemoveGameObject();

			if (!a_totalUndo)
			{
				if (m_returnToEdit)
					a_fsm.SetCurrentState(new EditEnergyPointsState(a_fsm, PlanLayer));
				else
					a_fsm.SetCurrentState(new CreateEnergyPointState(a_fsm, PlanLayer));
			}
		}
	}

	public class RemovePolygonOperation : PolygonUndoOperation
	{
		private EditMode m_editMode;
		private bool m_recreate;

		public RemovePolygonOperation(PolygonSubEntity a_subEntity, PlanLayer a_planLayer, EditMode a_editMode, bool a_recreate = false) : base(a_subEntity, a_planLayer)
		{
			m_editMode = a_editMode;
			m_recreate = a_recreate;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			(SubEntity.m_entity.Layer as PolygonLayer).RestoreSubEntity(SubEntity, m_recreate);

			SubEntity.PerformValidityCheck(m_editMode == EditMode.Create, m_editMode == EditMode.Create);

			a_redo = new CreatePolygonOperation(SubEntity, PlanLayer, m_editMode, m_recreate);

			if (!a_totalUndo)
			{
				if (m_editMode == EditMode.Create)
				{
					SubEntity.DrawGameObject(SubEntity.m_entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
					a_fsm.SetCurrentState(new CreatingPolygonState(a_fsm, SubEntity, PlanLayer));
				}
				else
				{
					SubEntity.DrawGameObject(SubEntity.m_entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
					a_fsm.SetCurrentState(new SelectPolygonsState(a_fsm, PlanLayer));
				}
			}
			else
				SubEntity.DrawGameObject(SubEntity.m_entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
		}
	}

	public class RemoveLineStringOperation : LineStringUndoOperation
	{
		protected EditMode m_editMode;
		protected bool m_recreate;

		public RemoveLineStringOperation(LineStringSubEntity a_subEntity, PlanLayer a_planLayer, EditMode a_editMode, bool a_recreate = false) : base(a_subEntity, a_planLayer)
		{
			m_editMode = a_editMode;
			m_recreate = a_recreate;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			(SubEntity.m_entity.Layer as LineStringLayer).RestoreSubEntity(SubEntity, m_recreate);

			a_redo = new CreateLineStringOperation(SubEntity, PlanLayer, m_editMode, m_recreate);

			if (!a_totalUndo)
			{
				if (m_editMode == EditMode.Create)
				{
					SubEntity.DrawGameObject(SubEntity.m_entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
					a_fsm.SetCurrentState(new CreatingLineStringState(a_fsm, PlanLayer, SubEntity));
				}
				else if (m_editMode == EditMode.Modify)
				{
					SubEntity.DrawGameObject(SubEntity.m_entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
					a_fsm.SetCurrentState(new SelectLineStringsState(a_fsm, PlanLayer));
				}
			}
			else
				SubEntity.DrawGameObject(SubEntity.m_entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
		}
	}

	public class RemoveEnergyLineStringOperation : RemoveLineStringOperation
	{
		private bool m_pointWasDeleted;

		public RemoveEnergyLineStringOperation(LineStringSubEntity a_subEntity, PlanLayer a_planLayer, EditMode a_editMode, bool a_recreate = false, bool a_pointWasDeleted = false) : base(a_subEntity, a_planLayer, a_editMode, a_recreate)
		{
			m_pointWasDeleted = a_pointWasDeleted;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			//This also automatically restores references
			(SubEntity.m_entity.Layer as LineStringLayer).RestoreSubEntity(SubEntity, m_recreate);

			a_redo = new CreateEnergyLineStringOperation(SubEntity, PlanLayer, m_editMode, m_recreate);
			if (!a_totalUndo)
			{
				if (m_editMode == EditMode.Create)
				{
					SubEntity.DrawGameObject(SubEntity.m_entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
					a_fsm.SetCurrentState(new CreatingEnergyLineStringState(a_fsm, PlanLayer, SubEntity));
				}
				else if (m_editMode == EditMode.Modify)
				{
					if (m_pointWasDeleted)
						//SubEntity.DrawGameObject(SubEntity.Entity.Layer.LayerGameObject.transform, SubEntityDrawMode.PlanReference);
						SubEntity.DrawGameObject(SubEntity.m_entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
					else
					{
						SubEntity.DrawGameObject(SubEntity.m_entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
						a_fsm.SetCurrentState(new SelectLineStringsState(a_fsm, PlanLayer));
					}
				}
			}
			else
				SubEntity.DrawGameObject(SubEntity.m_entity.Layer.LayerGameObject.transform, SubEntityDrawMode.Default);
		}
	}

	public class RemovePointOperation : PointUndoOperation
	{
		protected bool m_recreate;

		public RemovePointOperation(PointSubEntity a_subEntity, PlanLayer a_planLayer, bool a_recreate = false) : base(a_subEntity, a_planLayer)
		{
			m_recreate = a_recreate;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			(SubEntity.m_entity.Layer as PointLayer).RestoreSubEntity(SubEntity, m_recreate);

			a_redo = new CreatePointOperation(SubEntity, PlanLayer, m_recreate);

			SubEntity.DrawGameObject(SubEntity.m_entity.Layer.LayerGameObject.transform);
			if (!a_totalUndo)
				a_fsm.SetCurrentState(new EditPointsState(a_fsm, PlanLayer));
		}
	}

	public class RemoveEnergyPointOperation : RemovePointOperation
	{
		private bool m_returnToEdit;

		public RemoveEnergyPointOperation(PointSubEntity a_subEntity, PlanLayer a_planLayer, bool a_recreate = false, bool a_returnToEdit = true) : base(a_subEntity, a_planLayer, a_recreate)
		{ 
			m_returnToEdit = a_returnToEdit;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			(SubEntity.m_entity.Layer as PointLayer).RestoreSubEntity(SubEntity, m_recreate);

			a_redo = new CreateEnergyPointOperation(SubEntity, PlanLayer, m_recreate, m_returnToEdit);

			SubEntity.DrawGameObject(SubEntity.m_entity.Layer.LayerGameObject.transform);

			//Changed target state to energy variant
			if (!a_totalUndo)
				a_fsm.SetCurrentState(new EditEnergyPointsState(a_fsm, PlanLayer));
		}
	}

	public class ModifyPolygonRemovalPlanOperation : PolygonUndoOperation
	{
		private bool m_wasScheduledForRemovalBeforeOperation;

		public ModifyPolygonRemovalPlanOperation(PolygonSubEntity a_subEntity, PlanLayer a_planLayer, bool a_wasScheduledForRemovalBeforeOperation) : base(a_subEntity, a_planLayer)
		{
			m_wasScheduledForRemovalBeforeOperation = a_wasScheduledForRemovalBeforeOperation;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			a_redo = new ModifyPolygonRemovalPlanOperation(SubEntity, PlanLayer, !m_wasScheduledForRemovalBeforeOperation);
			if (m_wasScheduledForRemovalBeforeOperation)
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
			if (!a_totalUndo)
				a_fsm.SetCurrentState(new SelectPolygonsState(a_fsm, PlanLayer));
		}
	}

	public class ModifyLineStringRemovalPlanOperation : LineStringUndoOperation
	{
		private bool m_wasScheduledForRemovalBeforeOperation;

		public ModifyLineStringRemovalPlanOperation(LineStringSubEntity a_subEntity, PlanLayer a_planLayer, bool a_wasScheduledForRemovalBeforeOperation) : base(a_subEntity, a_planLayer)
		{
			m_wasScheduledForRemovalBeforeOperation = a_wasScheduledForRemovalBeforeOperation;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			a_redo = new ModifyLineStringRemovalPlanOperation(SubEntity, PlanLayer, !m_wasScheduledForRemovalBeforeOperation);
			if (m_wasScheduledForRemovalBeforeOperation)
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
			if (!a_totalUndo)
				a_fsm.SetCurrentState(new SelectLineStringsState(a_fsm, PlanLayer));
		}
	}

	public class ModifyPointRemovalPlanOperation : PointUndoOperation
	{
		private bool m_wasScheduledForRemovalBeforeOperation;

		public ModifyPointRemovalPlanOperation(PointSubEntity a_subEntity, PlanLayer a_planLayer, bool a_wasScheduledForRemovalBeforeOperation) : base(a_subEntity, a_planLayer)
		{
			m_wasScheduledForRemovalBeforeOperation = a_wasScheduledForRemovalBeforeOperation;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			a_redo = new ModifyPointRemovalPlanOperation(SubEntity, PlanLayer, !m_wasScheduledForRemovalBeforeOperation);

			if (m_wasScheduledForRemovalBeforeOperation)
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
			if (a_totalUndo)
				return;
			if (PlanLayer.BaseLayer.IsEnergyPointLayer())
				a_fsm.SetCurrentState(new EditEnergyPointsState(a_fsm, PlanLayer));
			else
				a_fsm.SetCurrentState(new EditPointsState(a_fsm, PlanLayer));
		}
	}

	public class FinalizePolygonOperation : PolygonUndoOperation
	{
		public FinalizePolygonOperation(PolygonSubEntity a_subEntity, PlanLayer a_planLayer) : base(a_subEntity, a_planLayer)
		{
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			SubEntity.AddPoint(Vector3.zero);

			SubEntity.PerformValidityCheck(true, true);

			SubEntity.HideRestrictionArea();
			SubEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
			if (!a_totalUndo)
				a_fsm.SetCurrentState(new CreatingPolygonState(a_fsm, SubEntity, PlanLayer));

			a_redo = new UnfinalizePolygonOperation(SubEntity, PlanLayer);
		}
	}

	public class FinalizeLineStringOperation : LineStringUndoOperation
	{
		public FinalizeLineStringOperation(LineStringSubEntity a_subEntity, PlanLayer a_planLayer) : base(a_subEntity, a_planLayer)
		{
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			SubEntity.AddPoint(Vector3.zero);

			SubEntity.HideRestrictionArea();
			SubEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
			if (!a_totalUndo)
				a_fsm.SetCurrentState(new CreatingLineStringState(a_fsm, PlanLayer, SubEntity));

			a_redo = new UnfinalizeLineStringOperation(SubEntity, PlanLayer);
		}
	}

	public class FinalizeEnergyLineStringOperation : FinalizeLineStringOperation
	{
		private EnergyPointSubEntity m_point;

		public FinalizeEnergyLineStringOperation(LineStringSubEntity a_subEntity, PlanLayer a_planLayer, EnergyPointSubEntity a_point) : base(a_subEntity, a_planLayer)
		{
			m_point = a_point;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			if (SubEntity.GetPointCount() == 1) //Points are directly finalized upon creation
			{
				LineStringLayer layer = SubEntity.m_entity.Layer as LineStringLayer;
				layer.RemoveSubEntity(SubEntity, true);
				if (!a_totalUndo)
					a_fsm.SetCurrentState(new StartCreatingEnergyLineStringState(a_fsm, PlanLayer));
			}
			else
			{
				//Remove connections between cable and point
				EnergyLineStringSubEntity cable = SubEntity as EnergyLineStringSubEntity;
				Connection connectionToRemove = null;
				foreach (Connection con in cable.Connections)
				{
					if (con.point == m_point)
					{
						connectionToRemove = con;					
						break;
					}
				}
				cable.RemoveConnection(connectionToRemove);
				m_point.RemoveConnection(connectionToRemove);

				SubEntity.HideRestrictionArea();
				SubEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
				if (!a_totalUndo)
					a_fsm.SetCurrentState(new CreatingEnergyLineStringState(a_fsm, PlanLayer, SubEntity));
			}

			a_redo = new UnfinalizeEnergyLineStringOperation(SubEntity, PlanLayer, m_point);
		}
	}

	public class UnfinalizePolygonOperation : PolygonUndoOperation
	{
		public UnfinalizePolygonOperation(PolygonSubEntity a_subEntity, PlanLayer a_planLayer) : base(a_subEntity, a_planLayer)
		{
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			(a_fsm.GetCurrentState() as CreatingPolygonState).FinalizePolygon();

			a_redo = new FinalizePolygonOperation(SubEntity, PlanLayer);
		}
	}

	public class UnfinalizeLineStringOperation : LineStringUndoOperation
	{
		public UnfinalizeLineStringOperation(LineStringSubEntity a_subEntity, PlanLayer a_planLayer) : base(a_subEntity, a_planLayer)
		{
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			(a_fsm.GetCurrentState() as CreatingLineStringState).FinalizeLineString();

			a_redo = new FinalizeLineStringOperation(SubEntity, PlanLayer);
		}
	}

	public class UnfinalizeEnergyLineStringOperation : UnfinalizeLineStringOperation
	{
		private EnergyPointSubEntity m_point;

		public UnfinalizeEnergyLineStringOperation(LineStringSubEntity a_subEntity, PlanLayer a_planLayer, EnergyPointSubEntity a_point) : base(a_subEntity, a_planLayer)
		{
			m_point = a_point;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			if (SubEntity.GetPointCount() == 1) //Points are directly finalized upon creation
			{
				LineStringLayer layer = SubEntity.m_entity.Layer as LineStringLayer;
				layer.RestoreSubEntity(SubEntity, true);
				if (!a_totalUndo)
					a_fsm.SetCurrentState(new StartCreatingEnergyLineStringState(a_fsm, PlanLayer));
			}
			else
			{
				(a_fsm.GetCurrentState() as CreatingEnergyLineStringState).FinalizeEnergyLineString(m_point);
			}

			a_redo = new FinalizeEnergyLineStringOperation(SubEntity, PlanLayer, m_point);
		}
	}

	public class ChangeConnectionOperation : UndoOperation
	{
		private EnergyLineStringSubEntity m_subEnt;
		private Connection m_oldConn;
		private Connection m_newCon;

		public ChangeConnectionOperation(EnergyLineStringSubEntity a_subEnt, Connection a_oldConn, Connection a_newCon)
		{
			m_subEnt = a_subEnt;
			m_oldConn = a_oldConn;
			m_newCon = a_newCon;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			m_subEnt.RemoveConnection(m_newCon);
			m_subEnt.AddConnection(m_oldConn);
			m_newCon.point.RemoveConnection(m_newCon);
			m_oldConn.point.AddConnection(m_oldConn);

			a_redo = new ChangeConnectionOperation(m_subEnt, m_newCon, m_oldConn);
		}
	}

	public class ReconnectCableToPoint : UndoOperation
	{
		private Connection m_oldConnection, m_newConnection;

		public ReconnectCableToPoint(Connection a_oldConnection, Connection a_newConnection)
		{
			m_oldConnection = a_oldConnection;
			m_newConnection = a_newConnection;
		}

		public override void Undo(FSM a_fsm, out UndoOperation a_redo, bool a_totalUndo = false)
		{
			//Connections have the same point but different cables
			m_oldConnection.point.RemoveConnection(m_newConnection);
			m_oldConnection.point.AddConnection(m_oldConnection);
			a_redo = new ReconnectCableToPoint(m_newConnection, m_oldConnection);
		}
	}
}
