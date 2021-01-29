using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SetOperationsToolbarTest : MonoBehaviour
{
    public Main Main;

    private bool visible = false;

    public void EnableButtons()
    {
        visible = !visible;
        UIManager.ToolbarVisibility(visible, FSM.ToolbarInput.Difference, FSM.ToolbarInput.Intersect, FSM.ToolbarInput.Union);
    }

    public void DoIntersection()
    {
        FSM.ToolbarButtonClicked(FSM.ToolbarInput.Intersect);
        //if (LayerManager.SelectedLayers().Count < 2)
        //{
        //    Debug.Log("Not enough layers enabled!");
        //    return;
        //}

        //List<Layer> layers = LayerManager.SelectedLayers();

        //List<List<Vector3>> polys = SetOperations.Boolean(layers[0] as PolygonLayer, layers[1] as PolygonLayer, ClipperLib.ClipType.ctIntersection);
    }

    public void DoUnion()
    {
        FSM.ToolbarButtonClicked(FSM.ToolbarInput.Union);
        //if (LayerManager.SelectedLayers().Count < 2)
        //{
        //    Debug.Log("Not enough layers enabled!");
        //    return;
        //}

        //List<Layer> layers = LayerManager.SelectedLayers();

        //List<List<Vector3>> polys = SetOperations.Boolean(layers[0] as PolygonLayer, layers[1] as PolygonLayer, ClipperLib.ClipType.ctUnion);
    }

    public void DoDifference()
    {
        FSM.ToolbarButtonClicked(FSM.ToolbarInput.Difference);
        //if (LayerManager.SelectedLayers().Count < 2)
        //{
        //    Debug.Log("Not enough layers enabled!");
        //    return;
        //}

        //List<Layer> layers = LayerManager.SelectedLayers();

        //List<List<Vector3>> polys = SetOperations.Boolean(layers[0] as PolygonLayer, layers[1] as PolygonLayer, ClipperLib.ClipType.ctDifference);
    }
}
