using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Numerics;

namespace MSP2050.Scripts
{
    public class PolicyLogicSandExtraction : APolicyLogic
    {

        const string LAYER_TAG = "SandDepth";
        const int MAX_DEPTH = 12;

        static PolicyLogicSandExtraction m_instance;
        public static PolicyLogicSandExtraction Instance => m_instance;

		//Editing backups
		bool m_wasSandExtractionPlanBeforeEditing;
		PolicyPlanDataSandExtraction m_backup;

        RasterLayer m_maxDepthRasterLayer;

        public override void Initialise(APolicyData a_settings, PolicyDefinition a_definition)
        {
            base.Initialise(a_settings, a_definition);
            m_instance = this;
            m_maxDepthRasterLayer = (RasterLayer)LayerManager.Instance.GetLayerByUniqueTag(LAYER_TAG);
        }

		public float CalculatePitVolume(PolygonSubEntity a_subEntity)
		{
			float volume = 0;

			Rect surfaceBoundingBox = a_subEntity.m_boundingBox;
			Rect rasterSurfaceBoundingBox = m_maxDepthRasterLayer.RasterBounds;

			// Relative normalized position of the bounding box of the SubEntity within the Raster bounding box.
			float relativeXNormalized = (surfaceBoundingBox.x - rasterSurfaceBoundingBox.x) / rasterSurfaceBoundingBox.width;
			float relativeYNormalized = (surfaceBoundingBox.y - rasterSurfaceBoundingBox.y) / rasterSurfaceBoundingBox.height;

			// Height and Width of the texture of the Raster Layer
			int rasterHeight = m_maxDepthRasterLayer.GetRasterImageHeight();
			int rasterWidth = m_maxDepthRasterLayer.GetRasterImageWidth();

			// Calculate the pixel range in the raster that corresponds to the bounding box of the PolygonSubEntity
			int startX = Mathf.FloorToInt(relativeXNormalized * rasterWidth);
			int startY = Mathf.FloorToInt(relativeYNormalized * rasterHeight);
			int endX = Mathf.CeilToInt((relativeXNormalized + surfaceBoundingBox.width / rasterSurfaceBoundingBox.width) * rasterWidth + 1);
			int endY = Mathf.CeilToInt((relativeYNormalized + surfaceBoundingBox.height / rasterSurfaceBoundingBox.height) * rasterHeight + 1);

			// Convert PolygonSubEntity to a list of Vector3 points
			List<UnityEngine.Vector3> polygonPoints = a_subEntity.GetPoints();

			// Iterate over the pixels in the calculated range
			for (int x = startX; x < endX; x++)
			{
				for (int y = startY; y < endY; y++)
				{
					// Convert pixel coordinates to world coordinates
					UnityEngine.Vector2 worldPos = m_maxDepthRasterLayer.GetWorldPositionForTextureLocation(x, y);

					// Check if the world position is inside the PolygonSubEntity
					List<UnityEngine.Vector3> worldPosList = new List<UnityEngine.Vector3> { new UnityEngine.Vector3(worldPos.x, worldPos.y, 0) };
					if (Util.GetPolygonOverlap(polygonPoints, worldPosList).Count > 0)
					{
						// Retrieve the value of the raster at this pixel
						float? rasterValue = m_maxDepthRasterLayer.GetRasterValueAt(worldPos);

						// If the raster value is valid, add it to the volume
						if (rasterValue.HasValue)
						{
							volume += rasterValue.Value * MAX_DEPTH;
						}
					}
				}
			}

			return volume;
		}
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