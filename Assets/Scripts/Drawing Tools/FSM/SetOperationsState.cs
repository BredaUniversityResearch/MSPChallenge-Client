using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SetOperationsState : FSMState
{
    HashSet<PolygonEntity> subjectEntities = new HashSet<PolygonEntity>();
    HashSet<PolygonEntity> clipEntities = new HashSet<PolygonEntity>();

    public SetOperationsState(FSM fsm) : base(fsm)
    {
    }

    bool selectingBox = false;
    HashSet<PolygonEntity> currentBoxSelection = new HashSet<PolygonEntity>();

    PolygonEntity currentHover = null;

    public override void EnterState(Vector3 currentMousePosition)
    {
		base.EnterState(currentMousePosition);

		UIManager.ToolbarEnable(false, FSM.ToolbarInput.Difference, FSM.ToolbarInput.Intersect, FSM.ToolbarInput.Union);
    }

    public override void MouseMoved(Vector3 previousPosition, Vector3 currentPosition, bool cursorIsOverUI)
    {
        if (!selectingBox)
        {
            List<AbstractLayer> visibleLayers = LayerManager.GetVisibleLayersSortedByDepth();

            PolygonEntity hover = null;
            if (!cursorIsOverUI)
            {
                foreach (AbstractLayer layer in visibleLayers)
                {
                    if (layer is PolygonLayer)
                    {
                        PolygonEntity layerHover = layer.GetEntityAt(currentPosition) as PolygonEntity;
                        if (layerHover != null)
                        {
                            hover = layerHover;
                            break;
                        }
                    }
                }
            }

            if (hover != currentHover)
            {
                if (currentHover != null)
                {
                    currentHover.RedrawGameObjects(CameraManager.Instance.gameCamera, getEntityDrawMode(currentHover));
                }

                if (hover != null)
                {
                    hover.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Hover);
                }

                currentHover = hover;
            }
        }
    }

    private SubEntityDrawMode getEntityDrawMode(PolygonEntity entity)
    {
        if (subjectEntities.Contains(entity)) { return SubEntityDrawMode.SetOperationSubject; }
        if (clipEntities.Contains(entity)) { return SubEntityDrawMode.SetOperationClip; }
        return SubEntityDrawMode.Default;
    }

    public override void LeftClick(Vector3 worldPosition)
    {
        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
        {
            if (currentHover != null)
            {
                select(new HashSet<PolygonEntity>() { currentHover });
            }
            else
            {
                deselectAll();
            }
        }
        else
        {
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                if (subjectEntities.Count > 0 && clipEntities.Count == 0)
                {
                    selectNextSubject(worldPosition);
                }
                else if (subjectEntities.Count > 0 && clipEntities.Count > 0)
                {
                    selectNextClipEntity(worldPosition);
                }
            }
            else if (subjectEntities.Count > 0 && clipEntities.Count == 0)
            {
                selectNextClipEntity(worldPosition);
            }
        }
    }

    private void selectNextSubject(Vector3 position)
    {
        List<AbstractLayer> visibleLayers = LayerManager.GetVisibleLayersSortedByDepth();

        foreach (AbstractLayer layer in visibleLayers)
        {
            if (layer is PolygonLayer)
            {
                List<Entity> entities = layer.GetEntitiesAt(position);
                if (entities.Count > 0)
                {
                    int selectIndex = 0;
                    for (int i = 0; i < entities.Count; ++i)
                    {
                        if (subjectEntities.Contains(entities[i] as PolygonEntity))
                        {
                            selectIndex = (i + 1) % entities.Count;
                        }
                    }
                    selectSubjects(new HashSet<PolygonEntity> { entities[selectIndex] as PolygonEntity });
                }
            }
        }
    }

    private void selectNextClipEntity(Vector3 position)
    {
        List<AbstractLayer> visibleLayers = LayerManager.GetVisibleLayersSortedByDepth();

        foreach (AbstractLayer layer in visibleLayers)
        {
            if (layer is PolygonLayer)
            {
                List<Entity> entities = layer.GetEntitiesAt(position);
                if (entities.Count > 0)
                {
                    int selectIndex = 0;
                    if (clipEntities.Count == 0)
                    {
                        for (int i = 0; i < entities.Count; ++i)
                        {
                            if (!subjectEntities.Contains(entities[i] as PolygonEntity))
                            {
                                selectIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < entities.Count; ++i)
                        {
                            if (clipEntities.Contains(entities[i] as PolygonEntity))
                            {
                                int j = (i + 1) % entities.Count;
                                while (j != i && subjectEntities.Contains(entities[j] as PolygonEntity))
                                {
                                    j = (j + 1) % entities.Count;
                                }
                                selectIndex = j;
                            }
                        }
                    }

                    if (!subjectEntities.Contains(entities[selectIndex] as PolygonEntity))
                    {
                        selectClipEntities(new HashSet<PolygonEntity> { entities[selectIndex] as PolygonEntity });
                    }
                }
            }
        }
    }

    private bool hashSetsIntersect(HashSet<PolygonEntity> a, HashSet<PolygonEntity> b)
    {
        HashSet<PolygonEntity> tmp = new HashSet<PolygonEntity>(a);
        tmp.IntersectWith(b);
        return tmp.Count > 0;
    }

    private void select(HashSet<PolygonEntity> newSelection)
    {
        if (subjectEntities.Count == 0)
        {
            selectSubjects(newSelection);
        }
        else if (clipEntities.Count == 0 && !hashSetsIntersect(subjectEntities, newSelection))
        {
            selectClipEntities(newSelection);
        }
        else if (clipEntities.Count > 0)
        {
            deselectAll();
            selectSubjects(newSelection);
        }
        else
        {
            deselectAll();
        }
    }

    private void deselectAll()
    {
        selectSubjects(new HashSet<PolygonEntity>());
        selectClipEntities(new HashSet<PolygonEntity>());
    }

    private void selectSubjects(HashSet<PolygonEntity> newSubjects)
    {
        foreach (PolygonEntity currentSubject in subjectEntities)
        {
            if (!newSubjects.Contains(currentSubject))
            {
                currentSubject.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Default);
            }
        }

        foreach (PolygonEntity newSubject in newSubjects)
        {
            if (!subjectEntities.Contains(newSubject))
            {
                newSubject.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.SetOperationSubject);
            }
        }

        subjectEntities = newSubjects;
    }

    private void selectClipEntities(HashSet<PolygonEntity> newClipEntities)
    {
        foreach (PolygonEntity currentClipEntity in clipEntities)
        {
            if (!newClipEntities.Contains(currentClipEntity))
            {
                currentClipEntity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Default);
            }
        }

        foreach (PolygonEntity newClipEntity in newClipEntities)
        {
            if (!clipEntities.Contains(newClipEntity))
            {
                newClipEntity.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.SetOperationClip);
            }
        }

        clipEntities = newClipEntities;

        UIManager.ToolbarEnable(subjectEntities.Count > 0 && clipEntities.Count > 0, FSM.ToolbarInput.Difference);
        UIManager.ToolbarEnable(subjectEntities.Count == 1 && clipEntities.Count > 0, FSM.ToolbarInput.Intersect, FSM.ToolbarInput.Union);
    }

    public override void StartedDragging(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        selectingBox = true;
        currentBoxSelection = new HashSet<PolygonEntity>();

        BoxSelect.DrawBoxSelection(dragStartPosition, currentPosition);
    }

    public override void Dragging(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        updateBoxSelection(dragStartPosition, currentPosition);
    }

    private void updateBoxSelection(Vector3 dragStartPosition, Vector3 currentPosition)
    {
        BoxSelect.DrawBoxSelection(dragStartPosition, currentPosition);

        HashSet<PolygonEntity> selectionsInBox = new HashSet<PolygonEntity>();

        List<AbstractLayer> visibleLayers = LayerManager.GetVisibleLayersSortedByDepth();
        foreach (AbstractLayer layer in visibleLayers)
        {
            if (layer is PolygonLayer)
            {
                selectionsInBox.UnionWith((layer as PolygonLayer).GetEntitiesInBox(dragStartPosition, currentPosition));
            }
        }

        foreach (PolygonEntity selectionInBox in selectionsInBox)
        {
            if (!currentBoxSelection.Contains(selectionInBox)) { selectionInBox.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Hover); }
        }

        foreach (PolygonEntity currentlySelected in currentBoxSelection)
        {
            if (!selectionsInBox.Contains(currentlySelected)) { currentlySelected.RedrawGameObjects(CameraManager.Instance.gameCamera, getEntityDrawMode(currentlySelected)); }
        }

        currentBoxSelection = selectionsInBox;
    }

    public override void StoppedDragging(Vector3 dragStartPosition, Vector3 dragFinalPosition)
    {
        updateBoxSelection(dragStartPosition, dragFinalPosition);

        if (currentBoxSelection.Count > 0)
        {
            select(currentBoxSelection);
        }

        BoxSelect.HideBoxSelection();
        selectingBox = false;
    }

    private void performSetOperation(ClipperLib.ClipType clipType, bool removeClipEntities)
    {
        HashSet<PolygonLayer> modifiedLayers = new HashSet<PolygonLayer>();

        fsm.AddToUndoStack(new BatchUndoOperationMarker());
        foreach (PolygonEntity subjectEntity in subjectEntities)
        {
            PolygonEntity newEntity = SetOperations.BooleanP(subjectEntity, clipEntities, clipType);
            if (newEntity != null)
            {
                //foreach (PolygonSubEntity newSubEntity in newEntity.GetSubEntities())
                //{
                //    //TODO: fix for plans
                //    //fsm.AddToUndoStack(new CreatePolygonOperation(newSubEntity, UndoOperation.EditMode.SetOperation));
                //}
                newEntity.DrawGameObjects(newEntity.Layer.LayerGameObject.transform);
            }

            List<PolygonSubEntity> subjectSubEntities = subjectEntity.GetSubEntities();
            for (int i = subjectSubEntities.Count - 1; i >= 0; --i)
            {
                PolygonSubEntity subjectSubEntity = subjectSubEntities[i];
                //TODO: fix for plans
                //fsm.AddToUndoStack(new RemovePolygonOperation(subjectSubEntity, UndoOperation.EditMode.SetOperation));
                //(subjectEntity.Layer as PolygonLayer).RemovePolygonSubEntity(subjectSubEntity);
                subjectSubEntity.RemoveGameObject();

                modifiedLayers.Add(subjectEntity.Layer as PolygonLayer);
            }
        }
        subjectEntities.Clear();

        if (removeClipEntities)
        {
            foreach (PolygonEntity clipEntity in clipEntities)
            {
                List<PolygonSubEntity> clipSubEntities = clipEntity.GetSubEntities();
                for (int i = clipSubEntities.Count - 1; i >= 0; --i)
                {
                    PolygonSubEntity clipSubEntity = clipSubEntities[i];
                    //TODO: Fix for plans
                    //fsm.AddToUndoStack(new RemovePolygonOperation(clipSubEntity, UndoOperation.EditMode.SetOperation));
                    //(clipEntity.Layer as PolygonLayer).RemovePolygonSubEntity(clipSubEntity);
                    clipSubEntity.RemoveGameObject();

                    modifiedLayers.Add(clipEntity.Layer as PolygonLayer);
                }
            }
            clipEntities.Clear();
        }
        fsm.AddToUndoStack(new BatchUndoOperationMarker());

        deselectAll();

        foreach (PolygonLayer layer in modifiedLayers)
        {
            if (layer.HasEntityTypeWithInnerGlow())
            {
                layer.UpdateInnerGlowWithFirstEntityTypeSettings(true);
                layer.RedrawGameObjects(CameraManager.Instance.gameCamera, SubEntityDrawMode.Default);
            }
        }
    }

    public override void HandleToolbarInput(FSM.ToolbarInput toolbarInput)
    {
        switch (toolbarInput)
        {
            case FSM.ToolbarInput.Difference:
                if (subjectEntities.Count > 0 && clipEntities.Count > 0)
                {
                    performSetOperation(ClipperLib.ClipType.ctDifference, false);
                }
                break;
            case FSM.ToolbarInput.Union:
                if (subjectEntities.Count == 1 && clipEntities.Count > 0)
                {
                    performSetOperation(ClipperLib.ClipType.ctUnion, true);
                }
                break;
            case FSM.ToolbarInput.Intersect:
                if (subjectEntities.Count == 1 && clipEntities.Count > 0)
                {
                    performSetOperation(ClipperLib.ClipType.ctIntersection, true);
                }
                break;
        }
    }

    public override void ExitState(Vector3 currentMousePosition)
    {
        foreach (PolygonEntity entity in subjectEntities)
        {
            entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
        }

        foreach (PolygonEntity entity in clipEntities)
        {
            entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
        }

        foreach (PolygonEntity entity in currentBoxSelection)
        {
            entity.RedrawGameObjects(CameraManager.Instance.gameCamera);
        }

        BoxSelect.HideBoxSelection();
    }
}
