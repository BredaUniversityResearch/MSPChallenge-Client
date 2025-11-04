using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;
using ClipperLib;

namespace MSP2050.Scripts
{
    public class PolicyLogicSandExtraction : APolicyLogic
    {

        const string SANDDEPTH_TAG = "SandDepth";
        const string STRATIFICATION_TAG = "StratificationDepth";
        const string TIDALEXCURSION_TAG = "TidalExcursion";
        const string SANDPITS_TAG1 = "SandAndGravel";
        const string SANDPITS_TAG2 = "Extraction";
        const string PITDEPTHPROPERTY = "PitExtractionDepth";
        const string PITSLOPEPROPERTY = "PitSlope";

        static PolicyLogicSandExtraction m_instance;
        public static PolicyLogicSandExtraction Instance => m_instance;

        //Editing backups
        bool m_wasSandExtractionPlanBeforeEditing;
        PolicyPlanDataSandExtraction m_backup;

        RasterLayer m_availableSandDepthRasterLayer;
        RasterLayer m_stratificationDepthRasterLayer;
        RasterLayer m_excursionLengthRasterLayer;
        PolygonLayer m_pitLayer;
        LineStringLayer m_coastLineLayer;
		EntityPropertyMetaData m_volumeProperty;
        int m_stratificationDepthWarningId;
        int m_excursionLengthWarningId;

        public override void Initialise(APolicyData a_settings, PolicyDefinition a_definition)
        {
            base.Initialise(a_settings, a_definition);
            m_instance = this;
			m_stratificationDepthWarningId = ConstraintManager.Instance.AddNonOverlapRestrictionMessage("This sand extraction pit's size exceeds the tidal excursion length. There is a risk of stratification.");
			m_excursionLengthWarningId = ConstraintManager.Instance.AddNonOverlapRestrictionMessage("This sand extraction pit's depth exceeds the critical stratification depth. There is a risk of stratification.");

		}
        public override void PostLayerMetaInitialise()
        { 
            m_availableSandDepthRasterLayer = (RasterLayer)LayerManager.Instance.GetLayerByUniqueTags(new string[] { SANDDEPTH_TAG });
            if (m_availableSandDepthRasterLayer != null)
            {
                m_availableSandDepthRasterLayer.DrawGameObject();
                m_availableSandDepthRasterLayer.ReloadLatestRaster();
            }
            else
                Debug.LogError($"Missing sand depth raster, no layers found with tag \"{SANDDEPTH_TAG}\". Sand extraction volume estimation will break.");

                m_stratificationDepthRasterLayer = (RasterLayer)LayerManager.Instance.GetLayerByUniqueTags(new string[] { STRATIFICATION_TAG });
            if (m_stratificationDepthRasterLayer != null)
            {
                m_stratificationDepthRasterLayer.DrawGameObject();
                m_stratificationDepthRasterLayer.ReloadLatestRaster();
            }

			m_excursionLengthRasterLayer = (RasterLayer)LayerManager.Instance.GetLayerByUniqueTags(new string[] { TIDALEXCURSION_TAG });
            if (m_excursionLengthRasterLayer != null)
            {
                m_excursionLengthRasterLayer.DrawGameObject();
                m_excursionLengthRasterLayer.ReloadLatestRaster();
            }

			m_pitLayer = (PolygonLayer)LayerManager.Instance.GetLayerByUniqueTags(new string[] { SANDPITS_TAG1, SANDPITS_TAG2 });
			m_coastLineLayer = (LineStringLayer)LayerManager.Instance.GetLayerByUniqueTags(new string[] { "Line", "Bathymetry" });
            ConstraintManager.Instance.AddNonOverlapRestrictionLayers(m_stratificationDepthWarningId, m_pitLayer, m_stratificationDepthRasterLayer);
            ConstraintManager.Instance.AddNonOverlapRestrictionLayers(m_excursionLengthWarningId, m_pitLayer, m_excursionLengthRasterLayer);
            m_pitLayer.m_onSubentityMeshChange += RecalculatePitVolume;
			m_pitLayer.m_onCalculationPropertyChanged += OnPitPropertyChange;
            m_volumeProperty = new EntityPropertyMetaData("Volume", true, false, "Sand Volume", null, null, "0", false, true, false,
                TMPro.TMP_InputField.ContentType.Standard, LayerInfoPropertiesObject.ContentValidation.None, "");
			m_pitLayer.AddPropertyMetaData(m_volumeProperty);
		}

		void OnPitPropertyChange(Entity a_pit, EntityPropertyMetaData a_property)
		{
            RecalculatePitVolume((PolygonSubEntity)a_pit.GetSubEntity(0));
		}

        void RecalculatePitVolume(PolygonSubEntity a_pit)
        {
            if(a_pit.SurfaceAreaSqrKm > 100)
                a_pit.m_entity.SetPropertyMetaData(m_volumeProperty, VisualizationUtil.Instance.VisualizationSettings.ValueConversions.ConvertUnit(EstimatePitVolume(a_pit), ValueConversionCollection.UNIT_M3).FormatAsString());
            else
                a_pit.m_entity.SetPropertyMetaData(m_volumeProperty, VisualizationUtil.Instance.VisualizationSettings.ValueConversions.ConvertUnit(CalculatePitVolume(a_pit), ValueConversionCollection.UNIT_M3).FormatAsString());
        }

		float CalculatePitVolume(PolygonSubEntity a_subEntity)
        {
            float volume = 0;
            int pitDepth = 0;
            double pitSlope = 1;
            Debug.Log("Accurate");
            

            if(!int.TryParse(a_subEntity.m_entity.GetPropertyMetaData(a_subEntity.m_entity.Layer.FindPropertyMetaDataByName(PITDEPTHPROPERTY)), out pitDepth))
            {
                return 0;
            }
			double.TryParse(a_subEntity.m_entity.GetPropertyMetaData(a_subEntity.m_entity.Layer.FindPropertyMetaDataByName(PITSLOPEPROPERTY)), out pitSlope);

            pitSlope = -GeometryOperations.intConversion / Main.SCALE * pitSlope;

			//Area of the polygon to analyze
			Rect surfaceBoundingBox = a_subEntity.m_boundingBox;
            //Total area covered by the raster layer
            Rect rasterSurfaceBoundingBox = m_availableSandDepthRasterLayer.RasterBounds;

            //Relative normalized position of the bounding box of the SubEntity within the Raster bounding box.
            //Converts world coordinates to normalized[0, 1] range relative to the raster's bounds.
            float relativeXNormalized = (surfaceBoundingBox.x - rasterSurfaceBoundingBox.x) / rasterSurfaceBoundingBox.width;
            float relativeYNormalized = (surfaceBoundingBox.y - rasterSurfaceBoundingBox.y) / rasterSurfaceBoundingBox.height;

            //Gets pixel dimensions of the raster texture
            int rasterHeight = m_availableSandDepthRasterLayer.GetRasterImageHeight();
            int rasterWidth = m_availableSandDepthRasterLayer.GetRasterImageWidth();

            //Calculates the range of raster pixels that overlap with the polygon's bounding box.
            int startX = Mathf.FloorToInt(relativeXNormalized * rasterWidth);
            int startY = Mathf.FloorToInt(relativeYNormalized * rasterHeight);
            int endX = Mathf.CeilToInt((relativeXNormalized + surfaceBoundingBox.width / rasterSurfaceBoundingBox.width) * rasterWidth);
            int endY = Mathf.CeilToInt((relativeYNormalized + surfaceBoundingBox.height / rasterSurfaceBoundingBox.height) * rasterHeight);

            // Computes the area of a single pixel in square kilometers (km²).
            float pixelWidth = rasterSurfaceBoundingBox.width / rasterWidth;
            float pixelHeight = rasterSurfaceBoundingBox.height / rasterHeight;
            float areaScale = Main.SCALE * Main.SCALE;

            bool[,] pixelComplete = new bool[endX - startX, endY - startY];
            int activePixels = (endX - startX) * (endY - startY);
            List<List<ClipperLib.IntPoint>> pitBounds = new List<List<IntPoint>>() { GeometryOperations.VectorToIntPoint(a_subEntity.GetPoints()) };

			int currentDepth = 0;

            while (activePixels > 0)
            {
                currentDepth += 1;
                if (currentDepth > pitDepth)
                    break;

				//Iterates through every pixel in the calculated range.
				for (int x = startX; x < endX; x++)
                {
                    if (activePixels == 0)
                        break;

                    for (int y = startY; y < endY; y++)
                    {
                        if (pixelComplete[x- startX, y - startY])
                            continue;

                        float rasterDepth = m_availableSandDepthRasterLayer.GetConvertedRasterValueAt(x, y); 

                        //Map raster value to actual depth based on your JSON data
                        if (rasterDepth < currentDepth)
                        {
                            //Pixel needs to be excluded from bound and volume manually added
                            List<IntPoint> pixelPoints = GeometryOperations.VectorToIntPoint(new List<Vector3>()  {
                                new Vector3 (rasterSurfaceBoundingBox.x + x * pixelWidth, rasterSurfaceBoundingBox.y + y * pixelHeight),
                                new Vector3 (rasterSurfaceBoundingBox.x + x * pixelWidth, rasterSurfaceBoundingBox.y + (y + 1) * pixelHeight),
                                new Vector3 (rasterSurfaceBoundingBox.x + (x + 1) * pixelWidth, rasterSurfaceBoundingBox.y + (y + 1) * pixelHeight),
                                new Vector3 (rasterSurfaceBoundingBox.x + (x + 1) * pixelWidth, rasterSurfaceBoundingBox.y + y * pixelHeight)
                            });
                            float pixelPitOverlapArea = Util.GetPolygonOverlapArea(pitBounds, pixelPoints) * areaScale;

                            volume += pixelPitOverlapArea * (rasterDepth-currentDepth-1f); //Only add remaining difference
                            pixelComplete[x - startX, y - startY] = true;
                            activePixels--;
                            if (pixelPitOverlapArea > 0f)
								pitBounds = Util.ClipFromPolygon(pitBounds, pixelPoints);
						}
                    }
                }
                float depthVolume = Mathf.Abs(Util.GetPolygonArea(pitBounds));
				volume += depthVolume; //Add volume of remaining bounds (for 1m depth)
                pitBounds = Util.OffsetPolygon(pitBounds, pitSlope);
                if (pitBounds == null || pitBounds.Count == 0)
                {
                    break;
                }
                //else
                //{
                //    List<List<Vector3>> convertedPoints = new List<List<Vector3>>();
                //    foreach (var poly in pitBounds)
                //    {
                //        List<Vector3> points = GeometryOperations.IntPointToVector(poly);
                //        for (int i = 0; i < points.Count - 1; i++)
                //        {
                //            Debug.DrawLine(points[i], points[i + 1], Color.black, 5f);

                //        }
                //        Debug.DrawLine(points[points.Count - 1], points[0], Color.black, 5f);
                //    }
                //}

            }
			return Mathf.Max(0f, volume * areaScale / GeometryOperations.intConversion); // Now returns volume in m3
        }

        float EstimatePitVolume(PolygonSubEntity a_subEntity)
        {
            int pitDepth = 0;
            double pitSlope = 1; // in metres
			Debug.Log("Estimate");

			if (!int.TryParse(a_subEntity.m_entity.GetPropertyMetaData(a_subEntity.m_entity.Layer.FindPropertyMetaDataByName(PITDEPTHPROPERTY)), out pitDepth))
            {
                return 0;
            }
            double.TryParse(a_subEntity.m_entity.GetPropertyMetaData(a_subEntity.m_entity.Layer.FindPropertyMetaDataByName(PITSLOPEPROPERTY)), out pitSlope);

            //Area of the polygon to analyze
            Rect surfaceBoundingBox = a_subEntity.m_boundingBox;
            //Total area covered by the raster layer
            Rect rasterSurfaceBoundingBox = m_availableSandDepthRasterLayer.RasterBounds;

            //Relative normalized position of the bounding box of the SubEntity within the Raster bounding box.
            //Converts world coordinates to normalized[0, 1] range relative to the raster's bounds.
            float relativeXNormalized = (surfaceBoundingBox.x - rasterSurfaceBoundingBox.x) / rasterSurfaceBoundingBox.width;
            float relativeYNormalized = (surfaceBoundingBox.y - rasterSurfaceBoundingBox.y) / rasterSurfaceBoundingBox.height;

            //Gets pixel dimensions of the raster texture
            int rasterHeight = m_availableSandDepthRasterLayer.GetRasterImageHeight();
            int rasterWidth = m_availableSandDepthRasterLayer.GetRasterImageWidth();

            //Calculates the range of raster pixels that overlap with the polygon's bounding box.
            int startX = Mathf.FloorToInt(relativeXNormalized * rasterWidth);
            int startY = Mathf.FloorToInt(relativeYNormalized * rasterHeight);
            int endX = Mathf.CeilToInt((relativeXNormalized + surfaceBoundingBox.width / rasterSurfaceBoundingBox.width) * rasterWidth);
            int endY = Mathf.CeilToInt((relativeYNormalized + surfaceBoundingBox.height / rasterSurfaceBoundingBox.height) * rasterHeight);

            double avgDepth = 0d;

			//Iterates through every pixel in the calculated range.
			for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
					avgDepth += Mathf.Min(pitDepth, m_availableSandDepthRasterLayer.GetConvertedRasterValueAt(x, y));
                }
            }
            avgDepth /= (endX - startX) * (endY - startY);
            float volume = (float)avgDepth * a_subEntity.SurfaceAreaSqrKm * 1000000f;

            //Crude estimation of loss for pit slope
            int slopeFactor = 1;
            for(int i = 0; i < avgDepth; i++)
            {
                slopeFactor += i;
            }
            float lossFactor = slopeFactor * (float)pitSlope / 25000f;
            volume *= 1f - lossFactor;

			return Mathf.Max(0, volume);
        }

		public override void AddToPlan(Plan a_plan)
        {
            a_plan.AddPolicyData(new PolicyPlanDataSandExtraction(this));
        }

        public override void HandlePlanUpdate(APolicyData a_updateData, Plan a_plan, EPolicyUpdateStage a_stage)
        {
            if (a_stage == APolicyLogic.EPolicyUpdateStage.General)
            {
                // Update the sand extraction value in the plan
                if (a_updateData is PolicyUpdateSandExtractionPlan update)
                {
                    PolicyPlanDataSandExtraction planData = new PolicyPlanDataSandExtraction(this)
                    {
                        m_value = update.m_distanceValue
                    };

                    a_plan.SetPolicyData(planData);
                }
            }
        }

        public override void HandleGeneralUpdate(APolicyData a_data, EPolicyUpdateStage a_stage)
        {
            //No general updates needed for sand extraction
        }

        public override void RemoveFromPlan(Plan a_plan)
        {
            //Remove sand extraction policy data from the plan
            a_plan.Policies.Remove(PolicyManager.SANDEXTRACTION_POLICY_NAME);
        }

        public override void StartEditingPlan(Plan a_plan)
        {
            if (a_plan == null)
            {
                m_wasSandExtractionPlanBeforeEditing = false;
                m_backup = null;
            }
            else
            {
                if (a_plan.TryGetPolicyData<PolicyPlanDataSandExtraction>(PolicyManager.SANDEXTRACTION_POLICY_NAME, out var data))
                {
                    m_wasSandExtractionPlanBeforeEditing = true;

                    m_backup = new PolicyPlanDataSandExtraction(this)
                    {
                        m_value = data.m_value
                    };
                }
                else
                {
                    m_wasSandExtractionPlanBeforeEditing = false;
                }
                m_availableSandDepthRasterLayer.SetEntitiesActiveUpTo(a_plan);

			}
        }

        public override void StopEditingPlan(Plan a_plan)
        {
            //Clear the backup when editing stops
            m_backup = null;
        }

        public override void RestoreBackupForPlan(Plan a_plan)
        {
            if (m_wasSandExtractionPlanBeforeEditing)
            {
                //Restore the backup value
                a_plan.SetPolicyData(m_backup);
            }
            else
            {
                //Remove the policy if it wasn't part of the plan before editing
                RemoveFromPlan(a_plan);
            }
        }

        public override void SubmitChangesToPlan(Plan a_plan, BatchRequest a_batch)
        {
            if (a_plan.TryGetPolicyData<PolicyPlanDataSandExtraction>(PolicyManager.SANDEXTRACTION_POLICY_NAME, out var data))
            {
                // Create a PolicyUpdateSandExtractionPlan object with the current sand extraction value
                PolicyUpdateSandExtractionPlan update = new PolicyUpdateSandExtractionPlan()
                {
                    m_distanceValue = data.m_value,
                    policy_type = PolicyManager.SANDEXTRACTION_POLICY_NAME
                };

                // Submit the update to the server
                SetGeneralPolicyData(a_plan, update, a_batch);
            }
            else if (m_wasSandExtractionPlanBeforeEditing)
            {
                // If the plan had sand extraction data before editing but no longer does, remove the policy data
                DeleteGeneralPolicyData(a_plan, PolicyManager.SANDEXTRACTION_POLICY_NAME, a_batch);
            }
        }

        public override void GetRequiredApproval(APolicyPlanData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, Dictionary<int, List<IApprovalReason>> a_approvalReasons, ref EApprovalType a_requiredApprovalLevel, bool a_reasonOnly)
        {
            //TODO CHECK: is it possible to change restriction size for other teams, if so: should it require approval?
        }

		public override void CheckPolicyLayerIssues(Plan a_plan) 
        {
            if(a_plan.PlanLayers != null)
            {
                foreach(PlanLayer planLayer in a_plan.PlanLayers)
                {
					if(planLayer.BaseLayer == m_pitLayer)
                    { 
                        foreach(Entity pit in planLayer.GetNewGeometry())
                        {
                            PolygonSubEntity pitPoly = (PolygonSubEntity)pit.GetSubEntity(0);
							Rect bounds = pit.GetEntityBounds();

                            //Get max bounding box size, compare to max tidal excursion length
                            float maxTidalExcursionLength = GetRasterOverlapMinMax(m_excursionLengthRasterLayer, pitPoly, true);
                            float tidalExcursionLengthInKM = m_excursionLengthRasterLayer.rasterValueScale.EvaluateOutput(maxTidalExcursionLength);
                            if(tidalExcursionLengthInKM < GetSizeParallelToCoast(pitPoly))
								planLayer.issues.Add(new PlanIssueObject(ERestrictionIssueType.Warning, bounds.center.x, bounds.center.y, m_pitLayer.m_id, m_excursionLengthWarningId));

                            //Get pit depth, compare to max stratification depth
                            float maxStratDepth = GetRasterOverlapMinMax(m_stratificationDepthRasterLayer, pitPoly, true);							
							int pitDepth = 1;
                            int.TryParse(pit.GetPropertyMetaData(pit.Layer.FindPropertyMetaDataByName(PITDEPTHPROPERTY)), out pitDepth);
							float stratDepthInM = m_stratificationDepthRasterLayer.rasterValueScale.EvaluateOutput(maxStratDepth);
							if (stratDepthInM < pitDepth)
                                planLayer.issues.Add(new PlanIssueObject(ERestrictionIssueType.Warning, bounds.center.x, bounds.center.y, m_pitLayer.m_id, m_stratificationDepthWarningId));
						}
                    }
				}
            }
        }

        float GetSizeParallelToCoast(PolygonSubEntity a_pit)
        {
			Vector3 pitCenter = a_pit.m_boundingBox.center;
			float closestDistSqr = Mathf.Infinity;
			Vector2 closestLineDir = Vector3.zero;
			if (m_coastLineLayer == null || m_coastLineLayer.m_activeEntities == null || m_coastLineLayer.m_activeEntities.Count == 0)
			{
				Debug.LogError("Coast reference layer has no geometry");
                return 0f;
			}

            //Find closest line segment to pit center, use direction of that line
			foreach (LineStringEntity lsEntity in m_coastLineLayer.m_activeEntities)
			{
				List<Vector3> linePoints = lsEntity.GetSubEntity(0).GetPoints();
				for (int i = 0; i < linePoints.Count - 1; ++i)
				{
					float dist = Util.GetSquaredDistanceToLine(pitCenter, linePoints[i], linePoints[i + 1]);
					if (dist < closestDistSqr)
					{
						closestDistSqr = dist;
						closestLineDir = linePoints[i] - linePoints[i + 1];
					}
				}
			}
            closestLineDir.Normalize();
            Quaternion rotation = Quaternion.FromToRotation(closestLineDir, Vector3.right);

            //Rotate all points of pit into local space of coast, then get Width
            //Note: Commented code below can be enabled for visualization in editor
			float pitXMin = float.PositiveInfinity;
			float pitXMax = float.NegativeInfinity;
			//float pitYMin = float.PositiveInfinity;
			//float pitYMax = float.NegativeInfinity;
			foreach (Vector3 point in a_pit.GetPoints())
            {
                Vector3 rotatedPoint = rotation * point;
                if (rotatedPoint.x < pitXMin)
                    pitXMin = rotatedPoint.x;
				if (rotatedPoint.x > pitXMax)
					pitXMax = rotatedPoint.x;
				//if (rotatedPoint.y < pitYMin)
				//	pitYMin = rotatedPoint.y;
				//if (rotatedPoint.y > pitYMax)
				//	pitYMax = rotatedPoint.y;
			}

			//Quaternion inverseRot = Quaternion.Inverse(rotation);
			//Vector2 p1 = inverseRot * new Vector3(pitXMin, pitYMin);
			//Vector2 p2 = inverseRot * new Vector3(pitXMin, pitYMax);
			//Vector2 p3 = inverseRot * new Vector3(pitXMax, pitYMax);
			//Vector2 p4 = inverseRot * new Vector3(pitXMax, pitYMin);
			//Debug.DrawLine(p1, p2, Color.black, 5f);
			//Debug.DrawLine(p2, p3, Color.black, 5f);
			//Debug.DrawLine(p3, p4, Color.black, 5f);
			//Debug.DrawLine(p4, p1, Color.black, 5f);
			//Debug.Log($"Pit size parallel to coast: {pitXMax - pitXMin}");
            return pitXMax - pitXMin;
		}

        public float GetRasterOverlapMinMax(RasterLayer a_raster, PolygonSubEntity a_pit, bool a_min)
        {
            float result = a_min ? float.PositiveInfinity : float.NegativeInfinity;
			Rect surfaceBoundingBox = a_pit.m_boundingBox;
			Rect rasterSurfaceBoundingBox = a_raster.RasterBounds;

			//Relative normalized position of the bounding box of the SubEntity within the Raster bounding box.
			//Converts world coordinates to normalized[0, 1] range relative to the raster's bounds.
			float relativeXNormalized = (surfaceBoundingBox.x - rasterSurfaceBoundingBox.x) / rasterSurfaceBoundingBox.width;
			float relativeYNormalized = (surfaceBoundingBox.y - rasterSurfaceBoundingBox.y) / rasterSurfaceBoundingBox.height;

			//Gets pixel dimensions of the raster texture
			int rasterHeight = a_raster.GetRasterImageHeight();
			int rasterWidth = a_raster.GetRasterImageWidth();

			//Calculates the range of raster pixels that overlap with the polygon's bounding box.
			int startX = Mathf.FloorToInt(relativeXNormalized * rasterWidth);
			int startY = Mathf.FloorToInt(relativeYNormalized * rasterHeight);
			int endX = Mathf.CeilToInt((relativeXNormalized + surfaceBoundingBox.width / rasterSurfaceBoundingBox.width) * rasterWidth);
			int endY = Mathf.CeilToInt((relativeYNormalized + surfaceBoundingBox.height / rasterSurfaceBoundingBox.height) * rasterHeight);

			for (int x = startX; x < endX; x++)
			{
                for (int y = startY; y < endY; y++)
                {
                    float rasterValue = a_raster.GetUnscaledRasterValueAt(x, y);

                    if (a_min)
                        result = Mathf.Min(result, rasterValue);
                    else
                        result = Mathf.Max(result, rasterValue);
                }
			}
            return result;
		}

		public int GetSandExtractionSettingBeforePlan(Plan a_plan)
        {
            List<Plan> plans = PlanManager.Instance.Plans;
            int result = 0; // Default value

            // Find the index of the given plan
            int planIndex = 0;
            for (; planIndex < plans.Count; planIndex++)
                if (plans[planIndex] == a_plan)
                    break;

            // Look for the most recent influencing plan with sand extraction data
            for (int i = planIndex - 1; i >= 0; i--)
            {
                if (plans[i].InInfluencingState && plans[i].TryGetPolicyData<PolicyPlanDataSandExtraction>(PolicyManager.SANDEXTRACTION_POLICY_NAME, out var planData))
                {
                    return planData.m_value;
                }
            }

            // If no influencing plan is found, return the default value
            return result;
        }
    }
}