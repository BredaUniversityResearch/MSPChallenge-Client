using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Numerics;
using static Codice.CM.WorkspaceServer.DataStore.IncomingChanges.StoreIncomingChanges.FileConflicts;
using static SoftwareRasterizerLib.PolygonRasterizer;
using UnityEngine.SocialPlatforms;

namespace MSP2050.Scripts
{
    public class PolicyLogicSandExtraction : APolicyLogic
    {

        const string SANDDEPTH_TAG = "SandDepth";
        const string SANDPITS_TAG1 = "SandAndGravel";
        const string SANDPITS_TAG2 = "Extraction";
        const int MAX_DEPTH = 12;

        static PolicyLogicSandExtraction m_instance;
        public static PolicyLogicSandExtraction Instance => m_instance;

        //Editing backups
        bool m_wasSandExtractionPlanBeforeEditing;
        PolicyPlanDataSandExtraction m_backup;

        RasterLayer m_maxDepthRasterLayer;
        EntityPropertyMetaData m_volumeProperty;

        public override void Initialise(APolicyData a_settings, PolicyDefinition a_definition)
        {
            base.Initialise(a_settings, a_definition);
            m_instance = this;
        }
        public override void PostLayerMetaInitialise()
        { 
            m_maxDepthRasterLayer = (RasterLayer)LayerManager.Instance.GetLayerByUniqueTags(new string[] { SANDDEPTH_TAG });
            PolygonLayer pitLayer = (PolygonLayer)LayerManager.Instance.GetLayerByUniqueTags(new string[] { SANDPITS_TAG1, SANDPITS_TAG2 });
   //         pitLayer.m_presetProperties.Add("Volume", (a_subent) =>
			//{
			//	PolygonSubEntity polygonEntity = (PolygonSubEntity)a_subent;
			//	return VisualizationUtil.Instance.VisualizationSettings.ValueConversions.ConvertUnit(polygonEntity.Volume, ValueConversionCollection.UNIT_M3).FormatAsString();
			//});
            pitLayer.m_onSubentityMeshChange += OnPitMeshChange;
            pitLayer.m_onCalculationPropertyChanged += OnPitPropertyChange;
            m_volumeProperty = new EntityPropertyMetaData("Volume", true, false, "Sand Volume", null, null, "0", false, true, false,
                TMPro.TMP_InputField.ContentType.Standard, LayerInfoPropertiesObject.ContentValidation.None, "");
			pitLayer.AddPropertyMetaData(m_volumeProperty);
		}

        void OnPitMeshChange(PolygonSubEntity a_pit)
        {
            a_pit.m_entity.SetPropertyMetaData(m_volumeProperty, VisualizationUtil.Instance.VisualizationSettings.ValueConversions.ConvertUnit(CalculatePitVolume(a_pit), ValueConversionCollection.UNIT_M3).FormatAsString());
		}

		void OnPitPropertyChange(Entity a_pit, EntityPropertyMetaData a_property)
		{
            a_pit.SetPropertyMetaData(m_volumeProperty, VisualizationUtil.Instance.VisualizationSettings.ValueConversions.ConvertUnit(CalculatePitVolume((PolygonSubEntity)a_pit.GetSubEntity(0)), ValueConversionCollection.UNIT_M3).FormatAsString());
		}

		public float CalculatePitVolume(PolygonSubEntity a_subEntity)
        {
            float volume = 0;
            int pitDepth = 0;
            int pitSlope = 1;

            //if (m_maxDepthRasterLayer == null)
            //    m_maxDepthRasterLayer = (RasterLayer)LayerManager.Instance.GetLayerByUniqueTags(new string[] { SANDDEPTH_TAG });

            if(!int.TryParse(a_subEntity.m_entity.GetPropertyMetaData(a_subEntity.m_entity.Layer.FindPropertyMetaDataByName("PitExtractionDepth")), out pitDepth))
            {
                return 0;
            }
			int.TryParse(a_subEntity.m_entity.GetPropertyMetaData(a_subEntity.m_entity.Layer.FindPropertyMetaDataByName("PitSlope")), out pitSlope);


			//Area of the polygon to analyze
			Rect surfaceBoundingBox = a_subEntity.m_boundingBox;
            //Total area covered by the raster layer
            Rect rasterSurfaceBoundingBox = m_maxDepthRasterLayer.RasterBounds;

            //Relative normalized position of the bounding box of the SubEntity within the Raster bounding box.
            //Converts world coordinates to normalized[0, 1] range relative to the raster's bounds.
            float relativeXNormalized = (surfaceBoundingBox.x - rasterSurfaceBoundingBox.x) / rasterSurfaceBoundingBox.width;
            float relativeYNormalized = (surfaceBoundingBox.y - rasterSurfaceBoundingBox.y) / rasterSurfaceBoundingBox.height;

            //Gets pixel dimensions of the raster texture
            int rasterHeight = m_maxDepthRasterLayer.GetRasterImageHeight();
            int rasterWidth = m_maxDepthRasterLayer.GetRasterImageWidth();

            //Calculates the range of raster pixels that overlap with the polygon's bounding box.
            int startX = Mathf.FloorToInt(relativeXNormalized * rasterWidth);
            int startY = Mathf.FloorToInt(relativeYNormalized * rasterHeight);
            int endX = Mathf.CeilToInt((relativeXNormalized + surfaceBoundingBox.width / rasterSurfaceBoundingBox.width) * rasterWidth);
            int endY = Mathf.CeilToInt((relativeYNormalized + surfaceBoundingBox.height / rasterSurfaceBoundingBox.height) * rasterHeight);

            //Extracts the polygon's vertices for point-in-polygon checks.
            List<UnityEngine.Vector3> polygonPoints = a_subEntity.GetPoints();

            // Computes the area of a single pixel in square kilometers (km²).
            float pixelWidth = rasterSurfaceBoundingBox.width / rasterWidth;
            float pixelHeight = rasterSurfaceBoundingBox.height / rasterHeight;
            float areaScale = Main.SCALE * Main.SCALE;
            float pitArea = Util.GetPolygonArea(polygonPoints) * areaScale;
			float averageDepth = 0;

			//Iterates through every pixel in the calculated range.
			for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    //Converts pixel coordinates (x,y) to world-space coordinates.
                    UnityEngine.Vector2 worldPos = m_maxDepthRasterLayer.GetWorldPositionForTextureLocation(x, y);

                    List<UnityEngine.Vector3> pixelPoints = new List<UnityEngine.Vector3> () 
                        { new UnityEngine.Vector3 (rasterSurfaceBoundingBox.x + x * pixelWidth, rasterSurfaceBoundingBox.y + y * pixelHeight),
                        new UnityEngine.Vector3 (rasterSurfaceBoundingBox.x + x * pixelWidth, rasterSurfaceBoundingBox.y + (y + 1) * pixelHeight),
                        new UnityEngine.Vector3 (rasterSurfaceBoundingBox.x + (x + 1) * pixelWidth, rasterSurfaceBoundingBox.y + (y + 1) * pixelHeight),
                        new UnityEngine.Vector3 (rasterSurfaceBoundingBox.x + (x + 1) * pixelWidth, rasterSurfaceBoundingBox.y + y * pixelHeight)};

                    //Retrieve the value of the raster at this pixel
                    float? rasterValue = m_maxDepthRasterLayer.GetRasterValueAt(worldPos);

                    if (rasterValue.HasValue)
                    {
                        //Map raster value to actual depth based on your JSON data
                        float depth = 0f;

                        switch (rasterValue.Value)
                        {
                            case 43: depth = 2f; break;    // 0-2m
                            case 85: depth = 4f; break;    // 2-4m
                            case 128: depth = 6f; break;   // 4-6m
                            case 170: depth = 8f; break;   // 6-8m
                            case 213: depth = 10f; break;  // 8-10m
                            case 255: depth = 12f; break;  // 10-12m
                            default: depth = 0f; break;   // Unknown value
                        }
                        float actualDepth = Mathf.Min(depth, pitDepth);
                        float pixelPitOverlapArea = Util.GetPolygonOverlapArea(polygonPoints, pixelPoints) * areaScale;

						volume += pixelPitOverlapArea * actualDepth;
						averageDepth += actualDepth * pixelPitOverlapArea / pitArea; //Add pre-averaged depth
					}
                }
            }
            //Correct for slopes: Depth * (slope * depth = offset) * circumfence * (correction for corner overlap)
            float perimeter = Util.GetPolygonPerimeter(polygonPoints) * Main.SCALE;
			float slopeVolume = averageDepth * averageDepth * pitSlope * perimeter * 0.333333f; // Combined: div by 2 for slope, multiply by 0.66667f for correction
			//Debug.Log($"Total volume: {volume}, slope volume: {slopeVolume}, avg depth: {averageDepth}, perimeter: {perimeter}");
			volume -= slopeVolume;
			return Mathf.Max(0f, volume); // Now returns volume in m3
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
            else if (a_plan.TryGetPolicyData<PolicyPlanDataSandExtraction>(PolicyManager.SANDEXTRACTION_POLICY_NAME, out var data))
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

        public override void OnPlanLayerRemoved(PlanLayer a_layer)
        {
            //No specific logic needed for sand extraction when a plan layer is removed
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