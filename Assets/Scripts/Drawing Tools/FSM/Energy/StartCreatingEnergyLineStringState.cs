using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MSP2050.Scripts
{
	class StartCreatingEnergyLineStringState : StartCreatingLineStringState
	{
		bool validPointTooltip = false;

		public StartCreatingEnergyLineStringState(FSM fsm, PlanLayer planLayer) : base(fsm, planLayer)
		{}

		public override void EnterState(Vector3 currentMousePosition)
		{
			//All points are non-reference
			foreach (AbstractLayer layer in PolicyLogicEnergy.Instance.m_energyLayers)
			{
				if (layer.m_greenEnergy == planLayer.BaseLayer.m_greenEnergy)
				{
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
			}
			base.EnterState(currentMousePosition);
		}

		//Overriden to change cursor when not on a valid starting point
		public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
		{
			if (!cursorIsOverUI)
			{
				EnergyPointSubEntity point = PolicyLogicEnergy.Instance.GetEnergyPointAtPosition(currentPosition);
				if (point == null || !point.CanCableStartAtSubEntity(planLayer.BaseLayer.m_greenEnergy))
				{
					fsm.SetCursor(FSM.CursorType.Invalid);
					if (!showingToolTip || validPointTooltip)
					{
						TooltipManager.ForceSetToolTip("Select valid starting point");
						validPointTooltip = false;
					}
				}
				else
				{
					fsm.SetCursor(FSM.CursorType.Add);
					if (!showingToolTip || !validPointTooltip)
					{
						List<EntityType> entityTypes = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
						StringBuilder sb = new StringBuilder("Creating: " + entityTypes[0].Name);
						for (int i = 1; i < entityTypes.Count; i++)
							sb.Append("\n& " + entityTypes[i].Name);
						TooltipManager.ForceSetToolTip(sb.ToString());
						validPointTooltip = true;
					}
				}
				showingToolTip = true;
			}
			else
			{
				fsm.SetCursor(FSM.CursorType.Default);
				if (showingToolTip)
					TooltipManager.HideTooltip();
				showingToolTip = false;

			}
		}

		public override void LeftMouseButtonUp(Vector3 startPosition, Vector3 finalPosition)
		{        
			EnergyPointSubEntity point = PolicyLogicEnergy.Instance.GetEnergyPointAtPosition(finalPosition);
			if (point == null || !point.CanCableStartAtSubEntity(planLayer.BaseLayer.m_greenEnergy))
				return;

			LineStringEntity entity = baseLayer.CreateNewEnergyLineStringEntity(point.GetPosition(), new List<EntityType>() { baseLayer.m_entityTypes.GetFirstValue() }, point, planLayer);
			baseLayer.m_activeEntities.Add(entity);
			entity.EntityTypes = InterfaceCanvas.Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
			LineStringSubEntity subEntity = entity.GetSubEntity(0) as LineStringSubEntity;
			subEntity.m_edited = true;

			subEntity.DrawGameObject(entity.Layer.LayerGameObject.transform, SubEntityDrawMode.BeingCreated);
			fsm.SetCurrentState(new CreatingEnergyLineStringState(fsm, planLayer, subEntity));
			fsm.AddToUndoStack(new CreateEnergyLineStringOperation(subEntity, planLayer, UndoOperation.EditMode.Create));
 
			AudioMain.Instance.PlaySound(AudioMain.ITEM_PLACED);
		}

		public override void ExitState(Vector3 currentMousePosition)
		{
			AbstractLayer baseLayer = InterfaceCanvas.Instance.activePlanWindow.CurrentlyEditingBaseLayer;
			if (baseLayer == null)
				LayerManager.Instance.SetNonReferenceLayers(new HashSet<AbstractLayer>() { }, false, true);
			else
				LayerManager.Instance.SetNonReferenceLayers(new HashSet<AbstractLayer>() { baseLayer }, false, true);
			base.ExitState(currentMousePosition);
		}

	}
}

