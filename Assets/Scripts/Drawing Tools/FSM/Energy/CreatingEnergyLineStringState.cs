using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
	class CreatingEnergyLineStringState : CreatingLineStringState
	{
		public CreatingEnergyLineStringState(FSM fsm, PlanLayer planLayer, LineStringSubEntity subEntity) : base(fsm, planLayer, subEntity)
		{ }

		public override void EnterState(Vector3 currentMousePosition)
		{
			EnergyLineStringSubEntity cable = (EnergyLineStringSubEntity)subEntity;
			if (cable.connections[0].point.Entity.Layer.editingType == AbstractLayer.EditingType.SourcePolygonPoint)
			{
				//All points except sourcepoints are non-reference
				foreach (AbstractLayer layer in PolicyLogicEnergy.Instance.m_energyLayers)
				{
					if (layer.greenEnergy == cable.Entity.Layer.greenEnergy &&
					    (layer.editingType == AbstractLayer.EditingType.Socket ||
					     layer.editingType == AbstractLayer.EditingType.Transformer))
						LayerManager.Instance.AddNonReferenceLayer(layer, true);
				}
			}
			else
			{
				//All points are non-reference
				foreach (AbstractLayer layer in PolicyLogicEnergy.Instance.m_energyLayers)
				{
					if (layer.greenEnergy == cable.Entity.Layer.greenEnergy)
					{
						if (layer.editingType == AbstractLayer.EditingType.SourcePolygon)
						{
							//Get and add the centerpoint layer
							EnergyPolygonLayer polyLayer = (EnergyPolygonLayer)layer;   
							LayerManager.Instance.AddNonReferenceLayer(polyLayer.centerPointLayer, true);
						}
						else if (layer.editingType == AbstractLayer.EditingType.SourcePoint ||
						         layer.editingType == AbstractLayer.EditingType.Socket ||
						         layer.editingType == AbstractLayer.EditingType.Transformer)
						{
							LayerManager.Instance.AddNonReferenceLayer(layer, true);
						}
					}
				}
			}

			base.EnterState(currentMousePosition);
		}

		protected override bool ClickingWouldFinishDrawing(Vector3 position, out Vector3 snappingPoint, out bool drawAsInvalid)
		{
			EnergyLineStringSubEntity cable = subEntity as EnergyLineStringSubEntity;
			EnergyPointSubEntity point = PolicyLogicEnergy.Instance.GetEnergyPointAtPosition(position);
			if (point != null)
			{
				snappingPoint = point.GetPosition();
				if (point.CanConnectToEnergySubEntity(cable.connections.First().point))
				{
					drawAsInvalid = false;
					return true;
				}
				else
				{
					drawAsInvalid = true;
					return false;
				}
			}
			snappingPoint = position;
			drawAsInvalid = false;
			return false;
		}

		public override void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition)
		{
			AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);

			EnergyLineStringSubEntity cable = subEntity as EnergyLineStringSubEntity;
			EnergyPointSubEntity point = PolicyLogicEnergy.Instance.GetEnergyPointAtPosition(finalPosition);
			if (point != null)
			{
				if (point.CanConnectToEnergySubEntity(cable.connections.First().point))
				{
					//Valid energy points clicked: Finalize
					fsm.AddToUndoStack(new FinalizeEnergyLineStringOperation(subEntity, planLayer, point));
					FinalizeEnergyLineString(point);
					return;
				}
				else
				{
					//Invalid energy points clicked: do nothing
					subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreatedInvalid);
					return;
				}
			}

			//Empty space clicked: add point
			SubEntityDataCopy dataCopy = subEntity.GetDataCopy();
			subEntity.AddPoint(finalPosition);
			subEntity.RedrawGameObject(SubEntityDrawMode.BeingCreated);
			subEntity.edited = true;

			fsm.AddToUndoStack(new ModifyEnergyLineStringOperation(subEntity, planLayer, dataCopy, UndoOperation.EditMode.Create));
		}

		public void FinalizeEnergyLineString(EnergyPointSubEntity point)
		{
			//Add connections
			EnergyLineStringSubEntity cable = subEntity as EnergyLineStringSubEntity;
			subEntity.SetPointPosition(subEntity.GetPointCount() - 1, point.GetPosition());
			Connection con = new Connection(cable, point, false);
			cable.AddConnection(con);
			point.AddConnection(con);

			//Set entitytype
			List<EntityType> selectedType = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
			if (selectedType != null) { subEntity.Entity.EntityTypes = selectedType; }

			subEntity.restrictionNeedsUpdate = true;
			subEntity.UnHideRestrictionArea();
			subEntity.RedrawGameObject(SubEntityDrawMode.Default);

			subEntity = null; // set line string to null so the exit state function doesn't remove it

			fsm.TriggerGeometryComplete();
			fsm.SetCurrentState(new StartCreatingEnergyLineStringState(fsm, planLayer));
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
			fsm.AddToUndoStack(new RemoveEnergyLineStringOperation(subEntity, planLayer, UndoOperation.EditMode.Create));
			fsm.SetCurrentState(new SelectLineStringsState(fsm, planLayer));
		}

		public override void ExitState(Vector3 currentMousePosition)
		{
			AbstractLayer baseLayer = InterfaceCanvas.Instance.activePlanWindow.CurrentlyEditingBaseLayer;
			if(baseLayer == null)
				LayerManager.Instance.SetNonReferenceLayers(new HashSet<AbstractLayer>() { }, false, true);
			else
				LayerManager.Instance.SetNonReferenceLayers(new HashSet<AbstractLayer>() { baseLayer }, false, true);
			base.ExitState(currentMousePosition);
		}
	}
}

