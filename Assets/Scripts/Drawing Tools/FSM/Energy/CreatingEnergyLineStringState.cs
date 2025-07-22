using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
	class CreatingEnergyLineStringState : CreatingLineStringState
	{
		public CreatingEnergyLineStringState(FSM a_fsm, PlanLayer a_planLayer, LineStringSubEntity a_subEntity) : base(a_fsm, a_planLayer, a_subEntity)
		{ }

		public override void EnterState(Vector3 a_currentMousePosition)
		{
			EnergyLineStringSubEntity cable = (EnergyLineStringSubEntity)subEntity;
			if (cable.Connections[0].point.m_entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint)
			{
				//All points except sourcepoints are non-reference
				foreach (AbstractLayer layer in PolicyLogicEnergy.Instance.m_energyLayers)
				{
					if (layer.m_greenEnergy == cable.m_entity.Layer.m_greenEnergy &&
					    (layer.m_editingType == AbstractLayer.EditingType.Socket ||
					     layer.m_editingType == AbstractLayer.EditingType.Transformer))
						LayerManager.Instance.AddNonReferenceLayer(layer, true);
				}
			}
			else
			{
				//All points are non-reference
				foreach (AbstractLayer layer in PolicyLogicEnergy.Instance.m_energyLayers)
				{
					if (layer.m_greenEnergy != cable.m_entity.Layer.m_greenEnergy)
						continue;
					if (layer.m_editingType == AbstractLayer.EditingType.SourcePolygon)
					{
						//Get and add the centerpoint layer
						EnergyPolygonLayer polyLayer = (EnergyPolygonLayer)layer;   
							LayerManager.Instance.AddNonReferenceLayer(polyLayer.m_centerPointLayer, false);
							foreach (Entity entity in polyLayer.m_centerPointLayer.m_activeEntities)
								entity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Default);
					}
					else if (layer.m_editingType == AbstractLayer.EditingType.SourcePoint ||
						layer.m_editingType == AbstractLayer.EditingType.Socket ||
						layer.m_editingType == AbstractLayer.EditingType.Transformer)
					{
						LayerManager.Instance.AddNonReferenceLayer(layer, true);
					}
				}
			}

			base.EnterState(a_currentMousePosition);
		}

		protected override bool ClickingWouldFinishDrawing(Vector3 a_position, out Vector3 a_snappingPoint, out bool a_drawAsInvalid)
		{
			EnergyLineStringSubEntity cable = subEntity as EnergyLineStringSubEntity;
			EnergyPointSubEntity point = PolicyLogicEnergy.Instance.GetEnergyPointAtPosition(a_position);
			if (point != null)
			{
				a_snappingPoint = point.GetPosition();
				if (point.CanConnectToEnergySubEntity(cable.Connections.First().point))
				{
					a_drawAsInvalid = false;
					return true;
				}
				a_drawAsInvalid = true;
				return false;
			}
			a_snappingPoint = a_position;
			a_drawAsInvalid = false;
			return false;
		}

		public override void LeftMouseButtonUp(Vector3 a_startPosition, Vector3 a_finalPosition)
		{
			AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);

			EnergyLineStringSubEntity cable = subEntity as EnergyLineStringSubEntity;
			EnergyPointSubEntity point = PolicyLogicEnergy.Instance.GetEnergyPointAtPosition(a_finalPosition);
			if (point != null)
			{
				if (point.CanConnectToEnergySubEntity(cable.Connections.First().point))
				{
					//Valid energy points clicked: Finalize
					m_fsm.AddToUndoStack(new FinalizeEnergyLineStringOperation(subEntity, planLayer, point));
					FinalizeEnergyLineString(point);
					return;
				}
				//Invalid energy points clicked: do nothing
				subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreatedInvalid);
				return;
			}

			//Empty space clicked: add point
			SubEntityDataCopy dataCopy = subEntity.GetDataCopy();
			subEntity.AddPoint(a_finalPosition);
			subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
			subEntity.m_edited = true;

			m_fsm.AddToUndoStack(new ModifyEnergyLineStringOperation(subEntity, planLayer, dataCopy, UndoOperation.EditMode.Create));
		}

		public void FinalizeEnergyLineString(EnergyPointSubEntity a_point)
		{
			//Add connections
			EnergyLineStringSubEntity cable = subEntity as EnergyLineStringSubEntity;
			subEntity.SetPointPosition(subEntity.GetPointCount() - 1, a_point.GetPosition());
			Connection con = new Connection(cable, a_point, false);
			cable.AddConnection(con);
			a_point.AddConnection(con);

			//Set entitytype
			List<EntityType> selectedType = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
			if (selectedType != null) { subEntity.m_entity.EntityTypes = selectedType; }

			subEntity.m_restrictionNeedsUpdate = true;
			subEntity.UnHideRestrictionArea();
			subEntity.RedrawGameObject(SubEntityDrawMode.Default);

			a_point.WarningIfAddingToExisting(
				"Energy Grid",
				"In plan {0} you have added a cable to an energy point first created {1}, thereby changing its energy grid. If this was unintentional, you should be able to undo this action.",
				subEntity.m_entity.PlanLayer.Plan
			);

			HashSet<LineStringSubEntity> selection = new HashSet<LineStringSubEntity>() { subEntity };
			subEntity = null; // set line string to null so the exit state function doesn't remove it

			m_fsm.TriggerGeometryComplete();
			if (Input.GetKey(KeyCode.LeftShift))
				m_fsm.SetCurrentState(new StartCreatingEnergyLineStringState(m_fsm, planLayer));
			else
				m_fsm.SetCurrentState(new EditEnergyLineStringsState(m_fsm, planLayer, selection));
		}

		public override void HandleKeyboardEvents()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				Abort();
			}
		}

		public override void Abort()
		{
			m_fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(subEntity, planLayer, UndoOperation.EditMode.Create));
			m_fsm.SetCurrentState(new SelectLineStringsState(m_fsm, planLayer));
		}

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			AbstractLayer baseLayer = InterfaceCanvas.Instance.activePlanWindow.CurrentlyEditingBaseLayer;
			if(baseLayer == null)
				LayerManager.Instance.SetNonReferenceLayers(new HashSet<AbstractLayer>() { }, false, true);
			else
				LayerManager.Instance.SetNonReferenceLayers(new HashSet<AbstractLayer>() { baseLayer }, false, true);

			foreach (AbstractLayer layer in PolicyLogicEnergy.Instance.m_energyLayers)
			{
				if (layer.m_greenEnergy == planLayer.BaseLayer.m_greenEnergy && layer.m_editingType == AbstractLayer.EditingType.SourcePolygon)
				{
					foreach (Entity entity in ((EnergyPolygonLayer)layer).m_centerPointLayer.m_activeEntities)
					{
						entity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Default);
					}
				}
			}

			base.ExitState(a_currentMousePosition);
		}
	}
}
