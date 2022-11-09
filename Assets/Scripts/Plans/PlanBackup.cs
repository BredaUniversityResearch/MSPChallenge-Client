using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace MSP2050.Scripts
{
	public class PlanBackup
	{
		public int m_startTime;
		public int m_constructionStartTime;
		public string m_name;
		public string m_description;

		public List<PlanLayerBackup> m_planLayers;
		public List<PlanIssueObject> m_issues;
		public Dictionary<int, EPlanApprovalState> m_approval;

		public PlanBackup(Plan a_plan)
		{
			m_startTime = a_plan.StartTime;
			m_constructionStartTime = a_plan.ConstructionStartTime;
			m_name = a_plan.Name;
			m_description = a_plan.Description;
			m_issues = IssueManager.Instance.FindIssueDataForPlan(a_plan);

			m_approval = new Dictionary<int, EPlanApprovalState>();
			foreach (var kvp in a_plan.countryApproval)
				m_approval.Add(kvp.Key, kvp.Value);

			m_planLayers = new List<PlanLayerBackup>(a_plan.PlanLayers.Count);
			foreach (PlanLayer planlayer in a_plan.PlanLayers)
				m_planLayers.Add(new PlanLayerBackup(planlayer));
		}

		public void ResetPlanToBackup(Plan a_plan)
		{
			a_plan.StartTime = m_startTime;
			a_plan.ConstructionStartTime = m_constructionStartTime;
			a_plan.Name = m_name;
			a_plan.Description = m_description;
			a_plan.countryApproval = m_approval;

			HashSet<int> originalLayers = new HashSet<int>();
			foreach (PlanLayerBackup layerbackup in m_planLayers)
			{
				originalLayers.Add(layerbackup.m_planLayer.ID);
				if (a_plan.getPlanLayerForID(layerbackup.m_planLayer.ID) == null)
				{
					//If layer not in plan, add again
					a_plan.PlanLayers.Add(layerbackup.m_planLayer);
					if (a_plan.State != Plan.PlanState.DELETED)
						layerbackup.m_planLayer.BaseLayer.AddPlanLayer(layerbackup.m_planLayer);
					IssueManager.Instance.InitialiseIssuesForPlanLayer(layerbackup.m_planLayer);
				}
				layerbackup.ResetLayerToBackup();
			}

			//Check if remaining layers were in backup
			for (int i = 0; i < a_plan.PlanLayers.Count; i++)
			{
				if (!originalLayers.Contains(a_plan.PlanLayers[i].ID))
				{
					PlanManager.Instance.PlanLayerRemoved(a_plan, a_plan.PlanLayers[i]);
					a_plan.PlanLayers[i].BaseLayer.RemovePlanLayerAndEntities(a_plan.PlanLayers[i]);
					a_plan.PlanLayers[i].RemoveGameObjects();
					a_plan.PlanLayers.RemoveAt(i);
					i--;
				}
			}

			//Finish editing for all geometry in old state
			foreach (PlanLayerBackup planLayer in m_planLayers)
			{
				foreach (SubEntity sub in planLayer.m_newGeometry)
				{
					sub.FinishEditing();
				}
			}
			IssueManager.Instance.SetIssuesForPlan(a_plan, m_issues);
		}

		public void SubmitChanges(Plan a_plan, BatchRequest a_batch)
		{
			HashSet<int> newLayerIds = new HashSet<int>();
			HashSet<int> oldLayerIds = new HashSet<int>();
			foreach (PlanLayer planlayer in a_plan.PlanLayers)
			{
				newLayerIds.Add(planlayer.BaseLayer.ID);
			}

			foreach (PlanLayerBackup backupLayer in m_planLayers)
			{
				oldLayerIds.Add(backupLayer.m_planLayer.BaseLayer.ID);
				if (!newLayerIds.Contains(backupLayer.m_planLayer.BaseLayer.ID))
				{
					//Plan no longer contains layer, submit removal
					//Removes planlayer from plan and all geom on it
					//Removes all connections, sockets, sources and output for geom on the layer
					a_plan.SubmitRemovePlanLayer(a_plan.GetPlanLayerForLayer(backupLayer.m_planLayer.BaseLayer), a_batch);
				}
				else
				{
					//Submit no longer deleted (prev existing)
					HashSet<int> tempHS = new HashSet<int>(backupLayer.m_removedGeometry);
					tempHS.ExceptWith(backupLayer.m_planLayer.RemovedGeometry);
					foreach (int unmarkedID in tempHS)
					{
						backupLayer.m_planLayer.SubmitUnmarkForDeletion(unmarkedID, a_batch);
					}

					//Submit newly deleted (prev existing)
					tempHS = new HashSet<int>(backupLayer.m_planLayer.RemovedGeometry);
					tempHS.ExceptWith(backupLayer.m_removedGeometry);
					foreach (int markedID in tempHS)
					{
						backupLayer.m_planLayer.SubmitMarkForDeletion(markedID, a_batch);
					}

					tempHS = new HashSet<int>();
					foreach (SubEntity sub in backupLayer.m_newGeometry)
						tempHS.Add(sub.GetDatabaseID());

					HashSet<int> newAddedIDs = new HashSet<int>();
					foreach (Entity newAddedEntity in backupLayer.m_planLayer.GetNewGeometry())
					{
						newAddedIDs.Add(newAddedEntity.DatabaseID);
						if (tempHS.Contains(newAddedEntity.DatabaseID))
						{
							//Was already added previously, only submit if changed
							if (newAddedEntity.GetSubEntity(0).edited)
							{
								//These are only modified subentities that already existed on the planlayer, so just update the content
								newAddedEntity.GetSubEntity(0).SubmitUpdate(a_batch);
							}
						}
						else
						{
							//This includes both completely new geometry, as well as existing geometry that is newly modified in this plan
							newAddedEntity.GetSubEntity(0).SubmitNew(a_batch);
						}
					}

					foreach (SubEntity oldAddedSubEntity in backupLayer.m_newGeometry)
					{
						if (!newAddedIDs.Contains(oldAddedSubEntity.GetDatabaseID()))
						{
							//These are subentities that used to be added/modified in the plan, but are no longer
							oldAddedSubEntity.SubmitDelete(a_batch);
						}
					}
				}
			}
			foreach (PlanLayer planlayer in a_plan.PlanLayers)
			{
				if (!oldLayerIds.Contains(planlayer.BaseLayer.ID))
				{
					//New layer in plan, submit creation and all content
					planlayer.SubmitNewPlanLayer(a_batch);
					
					foreach (int removedID in planlayer.RemovedGeometry)
					{
						planlayer.SubmitMarkForDeletion(removedID, a_batch);
					}
					foreach (Entity newAddedEntity in planlayer.GetNewGeometry())
					{
						newAddedEntity.GetSubEntity(0).SubmitNew(a_batch);
					}
				}

				//Finish editing for all geometry in new state
				foreach (Entity entity in planlayer.GetNewGeometry())
				{
					entity.GetSubEntity(0).FinishEditing();
				}
			}
		}
	}

	public class PlanLayerBackup
	{
		public PlanLayer m_planLayer;
		public HashSet<int> m_removedGeometry;
		public List<SubEntityDataCopy> m_newGeometryData;
		public List<SubEntity> m_newGeometry;

		public PlanLayerBackup(PlanLayer a_planLayer)
		{
			m_planLayer = a_planLayer;
			m_removedGeometry = new HashSet<int>(a_planLayer.RemovedGeometry);
			m_newGeometryData = new List<SubEntityDataCopy>(a_planLayer.GetNewGeometryCount());
			m_newGeometry = new List<SubEntity>(a_planLayer.GetNewGeometryCount());
			foreach (Entity entity in a_planLayer.GetNewGeometry())
			{
				m_newGeometry.Add(entity.GetSubEntity(0));
				m_newGeometryData.Add(entity.GetSubEntity(0).GetDataCopy());
			}
		}

		public void ResetLayerToBackup()
		{
			m_planLayer.RemovedGeometry = m_removedGeometry;
			HashSet<int> originalGeometry = new HashSet<int>();
			for (int i = 0; i < m_newGeometry.Count; i++)
			{
				m_newGeometry[i].SetDataToCopy(m_newGeometryData[i]);
				m_newGeometry[i].DrawGameObject(m_newGeometry[i].Entity.Layer.LayerGameObject.transform);
				originalGeometry.Add(m_newGeometry[i].GetDatabaseID());
			}
			foreach(Entity entity in m_planLayer.GetNewGeometry())
			{
				if(!originalGeometry.Contains(entity.DatabaseID))
				{
					entity.RemoveGameObjects();
				}
			}
			m_planLayer.ClearNewGeometry();
			foreach(SubEntity sub in m_newGeometry)
			{
				m_planLayer.AddNewGeometry(sub.Entity);
			}
		}
	}
}