using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_LayerSelect : AP_PopoutWindow
	{
		[SerializeField] GameObject m_catgoryPrefab;
		[SerializeField] GameObject m_subcatgoryPrefab;
		[SerializeField] GameObject m_layerPrefab;
		[SerializeField] Transform m_contentContainer;
		[SerializeField] Button m_confirmButton;
		[SerializeField] Button m_cancelButton;

		bool m_initialised;
		bool m_changed;
		bool m_ignoreCallback;
		HashSet<AbstractLayer> m_currentLayers;
		Dictionary<AbstractLayer, AP_LayerSelectLayer> m_layerObjects;
		Dictionary<string, AP_LayerSelectSubcategory> m_subcategoryObjects;
		Dictionary<string, AP_LayerSelectCategory> m_categoryObjects;

		void Initialise()
		{
			m_initialised = true;

			m_confirmButton.onClick.AddListener(OnAccept);
			m_cancelButton.onClick.AddListener(TryClose);

			m_layerObjects = new Dictionary<AbstractLayer, AP_LayerSelectLayer>();
			m_subcategoryObjects = new Dictionary<string, AP_LayerSelectSubcategory>();
			m_categoryObjects = new Dictionary<string, AP_LayerSelectCategory>();

			foreach(AbstractLayer layer in LayerManager.Instance.GetAllLayers())
			{
				if (!layer.Editable)
					continue;

				AP_LayerSelectSubcategory subcategory;
				if (!m_subcategoryObjects.TryGetValue(layer.SubCategory, out subcategory))
				{
					AP_LayerSelectCategory category;
					if (!m_categoryObjects.TryGetValue(layer.Category, out category))
					{
						category = Instantiate(m_catgoryPrefab, m_contentContainer).GetComponent<AP_LayerSelectCategory>();
						category.Initialise(LayerManager.Instance.MakeCategoryDisplayString(layer.Category));
						m_categoryObjects.Add(layer.Category, category);
					}
					subcategory = Instantiate(m_subcatgoryPrefab, m_contentContainer).GetComponent<AP_LayerSelectSubcategory>();
					subcategory.transform.SetSiblingIndex(category.transform.GetSiblingIndex() + 1);
					subcategory.Initialise(LayerManager.Instance.MakeCategoryDisplayString(layer.SubCategory), LayerManager.Instance.GetSubcategoryIcon(layer.SubCategory));
					m_subcategoryObjects.Add(layer.SubCategory, subcategory);
				}

				AP_LayerSelectLayer layerObj = Instantiate(m_layerPrefab, subcategory.ContentContainer).GetComponent<AP_LayerSelectLayer>();
				layerObj.Initialise(layer, OnLayerToggleChanged);
				m_layerObjects.Add(layer, layerObj);
			}

			foreach(var kvp in m_layerObjects)
			{
				kvp.Value.LoadDependencies(this);
			}
		}

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			if (!m_initialised)
				Initialise();

			base.OpenToContent(a_content, a_toggle, a_APWindow);

			m_changed = false;
			m_ignoreCallback = true;
			foreach(var kvp in m_layerObjects)
			{
				kvp.Value.ResetValue();
			}

			m_currentLayers = new HashSet<AbstractLayer>();
			foreach(PlanLayer pl in a_content.PlanLayers)
			{
				m_currentLayers.Add(pl.BaseLayer);
				m_layerObjects[pl.BaseLayer].SetValue(true);
			}
			m_ignoreCallback = false;
		}

		void OnLayerToggleChanged(AbstractLayer a_layer, bool a_value)
		{
			if (m_ignoreCallback)
				return;

			m_changed = true;
			if (a_value)
			{
				m_currentLayers.Add(a_layer);
			}
			else
			{
				m_currentLayers.Remove(a_layer);
			}
		}

		void OnAccept()
		{
			m_contentToggle.ForceClose(true); //applies content
		}

		public override void ApplyContent()
		{
			HashSet<AbstractLayer> originalLayers = new HashSet<AbstractLayer>();
			foreach (PlanLayer pl in m_plan.PlanLayers)
			{
				originalLayers.Add(pl.BaseLayer);
				//m_layerObjects[pl.BaseLayer].SetValue(true);
			}

			HashSet<AbstractLayer> added = new HashSet<AbstractLayer>(m_currentLayers);
			added.ExceptWith(originalLayers);
			foreach (AbstractLayer addedLayer in added)
			{
				//For added layers, check if plan previously contained this layer, if so: readd old planlayer to maintain ID, otherwise create new
				if(m_APWindow.PlanBackup.TryGetOriginalPlanLayerFor(addedLayer, out PlanLayer existingPlanLayer))
				{
					m_plan.PlanLayers.Add(existingPlanLayer);
					existingPlanLayer.ClearContent();
					existingPlanLayer.DrawGameObjects();
					addedLayer.AddPlanLayer(existingPlanLayer);
					addedLayer.SetEntitiesActiveUpTo(m_plan);
				}
				else
				{
					m_plan.AddNewPlanLayerFor(addedLayer);
					addedLayer.SetEntitiesActiveUpTo(m_plan);
				}
			}
			HashSet<AbstractLayer> removed = new HashSet<AbstractLayer>(originalLayers);
			removed.ExceptWith(m_currentLayers);

			bool seperatelyRemoveGreenCables = PolicyLogicEnergy.Instance.m_energyCableLayerGreen != null && !removed.Contains(PolicyLogicEnergy.Instance.m_energyCableLayerGreen);
			bool seperatelyRemoveGreyCables = PolicyLogicEnergy.Instance.m_energyCableLayerGrey != null && !removed.Contains(PolicyLogicEnergy.Instance.m_energyCableLayerGrey);
			Dictionary<int, List<EnergyLineStringSubEntity>> network = null;
			if (seperatelyRemoveGreenCables)
				network = PolicyLogicEnergy.Instance.m_energyCableLayerGreen.GetNodeConnectionsForPlan(m_plan);
			if (seperatelyRemoveGreyCables)
				network = PolicyLogicEnergy.Instance.m_energyCableLayerGrey.GetNodeConnectionsForPlan(m_plan, network);

			foreach (AbstractLayer removedLayer in removed)
			{
				PlanLayer removedPlanLayer = m_plan.GetPlanLayerForLayer(removedLayer);
				removedLayer.RemovePlanLayerAndEntities(removedPlanLayer);

				//Remove attached cables, if the cable layers were not already being removed
				if (removedLayer.IsEnergyLayer() && (seperatelyRemoveGreenCables || seperatelyRemoveGreyCables))
				{
					foreach (Entity entity in removedPlanLayer.GetNewGeometry())
					{
						SubEntity subEnt = entity.GetSubEntity(0);
						if (network.ContainsKey(subEnt.GetDatabaseID()))
						{
							foreach (EnergyLineStringSubEntity cable in network[subEnt.GetDatabaseID()])
							{
								cable.Entity.PlanLayer.RemoveNewGeometry(cable.Entity);
								cable.RemoveGameObject();
							}
						}
					}
				}

				removedPlanLayer.RemoveGameObjects();
				m_plan.PlanLayers.Remove(removedPlanLayer);
			}

			//Update energy policy data
			bool hadEnergyLayers = PolicyLogicEnergy.Instance.m_energyCableLayerGreen != null && originalLayers.Contains(PolicyLogicEnergy.Instance.m_energyCableLayerGreen) ||
				PolicyLogicEnergy.Instance.m_energyCableLayerGrey != null && originalLayers.Contains(PolicyLogicEnergy.Instance.m_energyCableLayerGrey);
			bool hasEnergyLayers = PolicyLogicEnergy.Instance.m_energyCableLayerGreen != null && m_currentLayers.Contains(PolicyLogicEnergy.Instance.m_energyCableLayerGreen) ||
				PolicyLogicEnergy.Instance.m_energyCableLayerGrey != null && m_currentLayers.Contains(PolicyLogicEnergy.Instance.m_energyCableLayerGrey);
			if(hadEnergyLayers && !hasEnergyLayers)
			{ 
				if(m_plan.TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var data) && !data.altersEnergyDistribution)
				{
					//Energy layers removed and no energy policy selected, remove from plan
					PolicyLogicEnergy.Instance.RemoveFromPlan(m_plan);
				}
			}
			else if(!hadEnergyLayers && hasEnergyLayers)
			{
				if (!m_plan.TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var data))
				{
					//Energy layers added and no energy policy selected, add to pan
					PolicyLogicEnergy.Instance.AddToPlan(m_plan, false);
				}
			}
			ConstraintManager.Instance.CheckConstraints(m_plan, out var unavailableTypeNames);
			LayerManager.Instance.UpdateVisibleLayersToPlan(m_plan);
			m_APWindow.RefreshContent();
		}

		public override bool MayClose()
		{
			if(m_changed)
			{
				DialogBoxManager.instance.ConfirmationWindow("Discard layer changes?", "Are you sure you want to discard any changes made to what layers are in the plan?", null, m_contentToggle.ForceClose);
				return false;
			}
			return true;
		}

		public AP_LayerSelectLayer GetLayerObjectForLayer(AbstractLayer a_layer)
		{
			return m_layerObjects[a_layer];
		}
	}
}
