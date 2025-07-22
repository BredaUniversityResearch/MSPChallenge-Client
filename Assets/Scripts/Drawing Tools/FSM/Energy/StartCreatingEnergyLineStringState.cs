using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MSP2050.Scripts
{
	class StartCreatingEnergyLineStringState : StartCreatingLineStringState
	{
		private bool m_validPointTooltip = false;

		public StartCreatingEnergyLineStringState(FSM a_fsm, PlanLayer a_planLayer) : base(a_fsm, a_planLayer)
		{}

		public override void EnterState(Vector3 a_currentMousePosition)
		{
			//All points are non-reference
			foreach (AbstractLayer layer in PolicyLogicEnergy.Instance.m_energyLayers)
			{
				if (layer.m_greenEnergy != m_planLayer.BaseLayer.m_greenEnergy)
					continue;
				if (layer.m_editingType == AbstractLayer.EditingType.SourcePolygon)
				{
					//Get and add the centerpoint layer
					EnergyPolygonLayer polyLayer = (EnergyPolygonLayer)layer;
					LayerManager.Instance.AddNonReferenceLayer(polyLayer.m_centerPointLayer, false); //Redrawing a centerpoint layer doesn't work, so manually redraw active entities
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
			base.EnterState(a_currentMousePosition);
		}

		//Overriden to change cursor when not on a valid starting point
		public override void MouseMoved(Vector3 a_previousPosition, Vector3 a_currentPosition, bool a_cursorIsOverUI)
		{
			if (!a_cursorIsOverUI)
			{
				EnergyPointSubEntity point = PolicyLogicEnergy.Instance.GetEnergyPointAtPosition(a_currentPosition);
				if (point == null || !point.CanCableStartAtSubEntity(m_planLayer.BaseLayer.m_greenEnergy))
				{
					m_fsm.SetCursor(FSM.CursorType.Invalid);
					if (!m_showingToolTip || m_validPointTooltip)
					{
						TooltipManager.ForceSetToolTip("Select valid starting point");
						m_validPointTooltip = false;
					}
				}
				else
				{
					m_fsm.SetCursor(FSM.CursorType.Add);
					if (!m_showingToolTip || !m_validPointTooltip)
					{
						List<EntityType> entityTypes = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
						StringBuilder sb = new StringBuilder("Creating: " + entityTypes[0].Name);
						for (int i = 1; i < entityTypes.Count; i++)
							sb.Append("\n& " + entityTypes[i].Name);
						TooltipManager.ForceSetToolTip(sb.ToString());
						m_validPointTooltip = true;
					}
				}
				m_showingToolTip = true;
			}
			else
			{
				m_fsm.SetCursor(FSM.CursorType.Default);
				if (m_showingToolTip)
					TooltipManager.HideTooltip();
				m_showingToolTip = false;

			}
		}

		public override void LeftMouseButtonUp(Vector3 a_startPosition, Vector3 a_finalPosition)
		{
			EnergyPointSubEntity point = PolicyLogicEnergy.Instance.GetEnergyPointAtPosition(a_finalPosition);
			if (point == null || !point.CanCableStartAtSubEntity(m_planLayer.BaseLayer.m_greenEnergy))
				return;

			LineStringEntity entity = m_baseLayer.CreateNewEnergyLineStringEntity(point.GetPosition(), new List<EntityType>() { m_baseLayer.m_entityTypes.GetFirstValue() }, point, m_planLayer);
			m_baseLayer.m_activeEntities.Add(entity);
			entity.EntityTypes = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
			LineStringSubEntity subEntity = entity.GetSubEntity(0) as LineStringSubEntity;
			subEntity.m_edited = true;

			subEntity.DrawGameObject(entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
			m_fsm.SetCurrentState(new CreatingEnergyLineStringState(m_fsm, m_planLayer, subEntity));
			m_fsm.AddToUndoStack(new CreateEnergyLineStringOperation(subEntity, m_planLayer, UndoOperation.EditMode.Create));

			AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);
			point.WarningIfAddingToExisting(
				"Energy Grid",
				"In plan {0} you have added a cable to an energy point first created {1}, thereby changing its energy grid. If this was unintentional, you should be able to undo this action.",
				subEntity.m_entity.PlanLayer.Plan
			);
		}

		public override void ExitState(Vector3 a_currentMousePosition)
		{
			AbstractLayer baseLayer = InterfaceCanvas.Instance.activePlanWindow.CurrentlyEditingBaseLayer;
			if (baseLayer == null)
				LayerManager.Instance.SetNonReferenceLayers(new HashSet<AbstractLayer>() { }, false, true);
			else
				LayerManager.Instance.SetNonReferenceLayers(new HashSet<AbstractLayer>() { baseLayer }, false, true);

			foreach (AbstractLayer layer in PolicyLogicEnergy.Instance.m_energyLayers)
			{
				if (layer.m_greenEnergy == m_planLayer.BaseLayer.m_greenEnergy && layer.m_editingType == AbstractLayer.EditingType.SourcePolygon)
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
