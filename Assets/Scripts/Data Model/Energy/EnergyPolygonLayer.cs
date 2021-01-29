using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;


public class EnergyPolygonLayer : PolygonLayer
{
    public PointLayer centerPointLayer;

    public EnergyPolygonLayer(LayerMeta layerMeta, List<SubEntityObject> layerObjects) : base(layerMeta, layerObjects)
    {
    }

    public override void Initialise()
    {
        //Create new point layer thats only used for centerpoints
        LayerMeta meta = new LayerMeta();
        meta.layer_name = "CenterPointLayer_For_" + ShortName;
        centerPointLayer = new PointLayer(meta, new List<SubEntityObject>(), this);
		centerPointLayer.EntityTypes.Add(0, new EntityType());
        centerPointLayer.editingType = EditingType.SourcePolygonPoint;
        centerPointLayer.greenEnergy = greenEnergy;
        LayerManager.AddEnergyPointLayer(centerPointLayer);
        centerPointLayer.DrawGameObject();
    }

    public override void LayerShown()
    {
        centerPointLayer.LayerGameObject.SetActive(true);
        centerPointLayer.LayerShown();
    }

    public override void LayerHidden()
    {
        centerPointLayer.LayerGameObject.SetActive(false);
        centerPointLayer.LayerHidden();
    }

    public override void UpdateScale(Camera targetCamera)
    {
        base.UpdateScale(targetCamera);
        centerPointLayer.UpdateScale(targetCamera);
    }

    public override void SetEntitiesActiveUpTo(int index, bool showRemovedInLatestPlan = true, bool showCurrentIfNotInfluencing = true)
    {
        centerPointLayer.activeEntities.Clear();

        base.SetEntitiesActiveUpTo(index, showRemovedInLatestPlan, showCurrentIfNotInfluencing);

		foreach (PolygonEntity entity in activeEntities)
            foreach (PolygonSubEntity subent in entity.GetSubEntities())
                centerPointLayer.activeEntities.Add((subent as EnergyPolygonSubEntity).sourcePoint.Entity as PointEntity);

        foreach (PointEntity ent in centerPointLayer.Entities)
            ent.RedrawGameObjects(CameraManager.Instance.gameCamera);
    }
}

