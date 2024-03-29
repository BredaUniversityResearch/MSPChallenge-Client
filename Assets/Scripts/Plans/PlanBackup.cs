﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Linq;

namespace MSP2050.Scripts
{
	public class PlanBackup
	{
		public int m_startTime;
		public int m_constructionStartTime;
		public string m_name;
		public string m_description;

		public List<PlanLayerBackup> m_planLayers;
		public Dictionary<int, EPlanApprovalState> m_approval;

		public PlanBackup(Plan a_plan)
		{
			if (a_plan == null)
			{
				m_startTime = -10000;
				m_constructionStartTime = -10000;
				m_name = null;
				m_description = null;
				m_approval = new Dictionary<int, EPlanApprovalState>();
				m_planLayers = new List<PlanLayerBackup>();
			}
			else
			{
				m_startTime = a_plan.StartTime;
				m_constructionStartTime = a_plan.ConstructionStartTime;
				m_name = a_plan.Name;
				m_description = a_plan.Description;

				m_approval = new Dictionary<int, EPlanApprovalState>();
				if (a_plan.countryApproval != null)
				{
					foreach (var kvp in a_plan.countryApproval)
						m_approval.Add(kvp.Key, kvp.Value);
				}

				m_planLayers = new List<PlanLayerBackup>(a_plan.PlanLayers.Count);
				foreach (PlanLayer planlayer in a_plan.PlanLayers)
					m_planLayers.Add(new PlanLayerBackup(planlayer));
			}
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
				if (a_plan.getPlanLayerForBaseID(layerbackup.m_planLayer.BaseLayer.m_id) == null)
				{
					//If layer not in plan, add again
					a_plan.PlanLayers.Add(layerbackup.m_planLayer);
					if (a_plan.State != Plan.PlanState.DELETED)
						layerbackup.m_planLayer.BaseLayer.AddPlanLayer(layerbackup.m_planLayer);
				}
				else if(a_plan.State != Plan.PlanState.DELETED)
					layerbackup.m_planLayer.BaseLayer.UpdatePlanLayerTime(layerbackup.m_planLayer);
				layerbackup.ResetLayerToBackup();
			}

			//Check if remaining layers were in backup
			for (int i = 0; i < a_plan.PlanLayers.Count; i++)
			{
				if (!originalLayers.Contains(a_plan.PlanLayers[i].ID))
				{
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
		}

		public void SubmitChanges(Plan a_plan, BatchRequest a_batch)
		{
			HashSet<int> newLayerIds = new HashSet<int>();
			HashSet<int> oldLayerIds = new HashSet<int>();
			List<Action<BatchRequest>> postGeometryActions = new List<Action<BatchRequest>>();
			foreach (PlanLayer planlayer in a_plan.PlanLayers)
			{
				newLayerIds.Add(planlayer.BaseLayer.m_id);
			}

			//Submit changes or removal to all existing layers
			foreach (PlanLayerBackup backupLayer in m_planLayers)
			{
				oldLayerIds.Add(backupLayer.m_planLayer.BaseLayer.m_id);
				if (!newLayerIds.Contains(backupLayer.m_planLayer.BaseLayer.m_id))
				{
					//Plan no longer contains layer, submit removal
					//Removes planlayer from plan and all geom and issues on it
					//Removes all connections, sockets, sources and output for geom on the layer
					//a_plan.SubmitRemovePlanLayer(a_plan.GetPlanLayerForLayer(backupLayer.m_planLayer.BaseLayer), a_batch);
					a_plan.SubmitRemovePlanLayer(backupLayer.m_planLayer, a_batch);
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
							if (newAddedEntity.GetSubEntity(0).m_edited)
							{
								//These are only modified subentities that already existed on the planlayer, so just update the content
								Action<BatchRequest> newPostgeomAction = newAddedEntity.GetSubEntity(0).SubmitUpdate(a_batch);
								if (newPostgeomAction != null)
									postGeometryActions.Add(newPostgeomAction);
							}
						}
						else
						{
							//This includes both completely new geometry, as well as existing geometry that is newly modified in this plan							
							Action<BatchRequest> newPostgeomAction = newAddedEntity.GetSubEntity(0).SubmitNew(a_batch);
							if (newPostgeomAction != null)
								postGeometryActions.Add(newPostgeomAction);
						}
					}

					foreach (SubEntity oldAddedSubEntity in backupLayer.m_newGeometry)
					{
						if (!newAddedIDs.Contains(oldAddedSubEntity.GetDatabaseID()))
						{
							//These are subentities that used to be added/modified in the plan, but are no longer
							Action<BatchRequest> newPostgeomAction = oldAddedSubEntity.SubmitDelete(a_batch);
							if (newPostgeomAction != null)
								postGeometryActions.Add(newPostgeomAction);
						}
					}

					//Update the IDs of issues if they exist in the backup, submit delta to server
					backupLayer.UpdateIssueIDsAndSubmitChanges(a_batch);
				}
			}

			//Submit new layers
			foreach (PlanLayer planlayer in a_plan.PlanLayers)
			{
				if (!oldLayerIds.Contains(planlayer.BaseLayer.m_id))
				{
					//New layer in plan, submit creation and all content
					planlayer.SubmitNewPlanLayer(a_batch);

					foreach (int removedID in planlayer.RemovedGeometry)
					{
						planlayer.SubmitMarkForDeletion(removedID, a_batch);
					}
					foreach (Entity newAddedEntity in planlayer.GetNewGeometry())
					{
						Action<BatchRequest> newPostgeomAction = newAddedEntity.GetSubEntity(0).SubmitNew(a_batch);
						if (newPostgeomAction != null)
							postGeometryActions.Add(newPostgeomAction);
					}
					if (planlayer.issues != null)
					{
						PlanLayer localLayerRef = planlayer;
						JObject dataObject = new JObject();
						dataObject.Add("added", JToken.FromObject(planlayer.issues));
						dataObject.Add("plan", planlayer.Plan.GetDataBaseOrBatchIDReference());
						dataObject.Add("planlayer_id", planlayer.GetDataBaseOrBatchIDReference());
						dataObject.Add("removed", JToken.FromObject(new int[0]));
						a_batch.AddRequest<List<PlanIssueObject>>(Server.SendIssues(), dataObject, BatchRequest.BATCH_GROUP_ISSUES, (a) => HandleDatabaseIDResults(a, localLayerRef));
					}
				}
			}

			foreach(var action in postGeometryActions)
			{
				action.Invoke(a_batch);
			}

			//Finish editing for all geometry in new state
			foreach (PlanLayer planlayer in a_plan.PlanLayers)
			{
				foreach (Entity entity in planlayer.GetNewGeometry())
				{
					entity.GetSubEntity(0).FinishEditing();
				}
			}
		}

		private void HandleDatabaseIDResults(List<PlanIssueObject> results, PlanLayer a_layer)
		{
			a_layer.issues = new HashSet<PlanIssueObject>(results, new IssueObjectEqualityComparer());
		}

		public bool TryGetOriginalPlanLayerFor(AbstractLayer a_layer, out PlanLayer a_planLayer)
		{
			foreach(PlanLayerBackup backup in m_planLayers)
			{
				if (backup.m_planLayer.BaseLayer == a_layer)
				{
					a_planLayer = backup.m_planLayer;
					return true;
				}
			}
			a_planLayer = null;
			return false;
		}
	}

	public class PlanLayerBackup
	{
		public PlanLayer m_planLayer;
		public HashSet<int> m_removedGeometry;
		public List<SubEntityDataCopy> m_newGeometryData;
		public List<SubEntity> m_newGeometry;
		public HashSet<PlanIssueObject> m_issues;

		public PlanLayerBackup(PlanLayer a_planLayer)
		{
			m_planLayer = a_planLayer;
			m_removedGeometry = new HashSet<int>(a_planLayer.RemovedGeometry);
			m_newGeometryData = new List<SubEntityDataCopy>(a_planLayer.GetNewGeometryCount());
			m_newGeometry = new List<SubEntity>(a_planLayer.GetNewGeometryCount());
			m_issues = new HashSet<PlanIssueObject>(a_planLayer.issues, new IssueObjectEqualityComparer());
			foreach (Entity entity in a_planLayer.GetNewGeometry())
			{
				m_newGeometry.Add(entity.GetSubEntity(0));
				m_newGeometryData.Add(entity.GetSubEntity(0).GetDataCopy());
			}
		}

		public void ResetLayerToBackup()
		{
			m_planLayer.RemovedGeometry = m_removedGeometry;
			m_planLayer.issues = m_issues;
			HashSet<int> originalGeometry = new HashSet<int>();
			for (int i = 0; i < m_newGeometry.Count; i++)
			{
				m_newGeometry[i].SetDataToCopy(m_newGeometryData[i]);
				m_newGeometry[i].DrawGameObject(m_newGeometry[i].m_entity.Layer.LayerGameObject.transform);
				originalGeometry.Add(m_newGeometry[i].GetDatabaseID());
			}
			if (m_planLayer.GetNewGeometry() != null)
			{
				foreach (Entity entity in m_planLayer.GetNewGeometry())
				{
					if (!originalGeometry.Contains(entity.DatabaseID))
					{
						entity.RemoveGameObjects();
					}
				}
			}
			m_planLayer.ClearNewGeometry();
			foreach(SubEntity sub in m_newGeometry)
			{
				m_planLayer.AddNewGeometry(sub.m_entity);
				sub.ReAddToEntity();
			}
		}

		public void UpdateIssueIDsAndSubmitChanges(BatchRequest a_batch)
		{
			if (m_issues == null || m_issues.Count == 0)
			{ 
				if(m_planLayer.issues != null || m_planLayer.issues.Count > 0)
				{
					//No old issues, submit all new issues
					JObject dataObject = new JObject();
					dataObject.Add("plan", m_planLayer.Plan.GetDataBaseOrBatchIDReference());
					dataObject.Add("planlayer_id", m_planLayer.GetDataBaseOrBatchIDReference());

					Dictionary<int, PlanIssueObject> newIssues = new Dictionary<int, PlanIssueObject>();
					foreach (PlanIssueObject issue in m_planLayer.issues)
						newIssues.Add(issue.GetIssueHash(), issue);

					dataObject.Add("added", JToken.FromObject(newIssues.Values.ToArray()));
					dataObject.Add("removed", JToken.FromObject(new int[] {}));
					a_batch.AddRequest<List<PlanIssueObject>>(Server.SendIssues(), dataObject, BatchRequest.BATCH_GROUP_ISSUES, HandleDatabaseIDResults);
				}
			}
			else
			{
				List<int> removedIssueIDs = new List<int>();
				Dictionary<int, PlanIssueObject> newIssues = new Dictionary<int, PlanIssueObject>();
				foreach (PlanIssueObject issue in m_planLayer.issues)
				{
					int hash = issue.GetIssueHash();
					if(!newIssues.ContainsKey(hash))
						newIssues.Add(hash, issue);
				}

				foreach (PlanIssueObject issue in m_issues)
				{
					//Issue hash doesn't take db id into account, so we can use to check for existing
					int key = issue.GetIssueHash();
					if (newIssues.TryGetValue(key, out var newIssue))
					{
						//Update existing db id, remove from new
						newIssue.issue_database_id = issue.issue_database_id;
						newIssues.Remove(key);
					}
					else if(issue.issue_database_id == -1)
					{
						Debug.LogError("Trying to send issue removal for issue with id -1 to the server");
					}
					else
					{
						removedIssueIDs.Add(issue.issue_database_id);
					}	
				}

				JObject dataObject = new JObject();
				dataObject.Add("planlayer_id", m_planLayer.GetDataBaseOrBatchIDReference());
				dataObject.Add("plan", m_planLayer.Plan.GetDataBaseOrBatchIDReference());
				dataObject.Add("added", JToken.FromObject(newIssues.Values.ToArray()));
				dataObject.Add("removed", JToken.FromObject(removedIssueIDs));
				a_batch.AddRequest<List<PlanIssueObject>>(Server.SendIssues(), dataObject, BatchRequest.BATCH_GROUP_ISSUES, HandleDatabaseIDResults);
			}
		}

		private void HandleDatabaseIDResults(List<PlanIssueObject> a_results)
		{
			m_planLayer.issues = new HashSet<PlanIssueObject>(a_results, new IssueObjectEqualityComparer());
		}		
	}
}