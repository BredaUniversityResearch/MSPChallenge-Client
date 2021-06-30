using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class RasterEntity : Entity
{
    public List<RasterSubentity> rasterSubentity;
    private Texture2D raster;

    public RasterEntity(RasterLayer layer, Texture2D raster, Vector2 offset, Vector2 scale, SubEntityObject layerObject) 
		: base(layer, layerObject)
    {
        this.raster = raster;
        rasterSubentity = new List<RasterSubentity>(1);
        rasterSubentity.Add(new RasterSubentity(this, this.raster, offset, scale));
    }

    public override void RemoveGameObjects()
    {
        foreach (RasterSubentity pse in rasterSubentity)
        {
            pse.RemoveGameObject();
        }
        rasterSubentity = null;
    }

    public override void DrawGameObjects(Transform parent, SubEntityDrawMode drawMode = SubEntityDrawMode.Default)
    {
        foreach (RasterSubentity pse in rasterSubentity)
        {
            pse.DrawGameObject(parent, drawMode);
        }
    }

    public override void RedrawGameObjects(Camera targetCamera, SubEntityDrawMode drawMode = SubEntityDrawMode.Default, bool forceScaleUpdate = false)
    {
        if (forceScaleUpdate)
        {
            foreach (RasterSubentity pse in rasterSubentity)
            {
                pse.RedrawGameObject(drawMode);
                pse.UpdateScale(targetCamera);
            }
        }
        else
            foreach (RasterSubentity pse in rasterSubentity)
                pse.RedrawGameObject(drawMode);
    }

    public void SetNewRaster(Texture2D newRaster)
    {
        foreach (RasterSubentity pse in rasterSubentity)
        {
            pse.SetNewRaster(newRaster);
        }
    }

    public void SetScale(Vector2 scale)
    {
        foreach (RasterSubentity pse in rasterSubentity)
        {
            pse.SetScale(scale);
        }
    }

    public override SubEntity GetSubEntity(int index)
    {
        if (rasterSubentity != null)
        {
            return rasterSubentity[0];
        }
        return null;
    }

    public override int GetSubEntityCount()
    {
        return rasterSubentity.Count;
    }

    public override float GetRestrictionAreaSurface()
    {
        throw new NotImplementedException();
    }

    //public override float GetInvestmentCost()
    //{
    //    throw new NotImplementedException();
    //}
}
