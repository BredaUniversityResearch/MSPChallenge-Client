using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class LayerProbeWindow : MonoBehaviour
	{
		[SerializeField] GenericWindow window = null;
		[SerializeField] Transform contentLocation = null;
		[SerializeField] GameObject layerProbeEntryPrefab = null;

		List<LayerProbeEntry> m_entries = new List<LayerProbeEntry>();
		Vector3 m_worldSamplePos;
		Vector3 m_windowPos;

		public void ShowLayerProbeWindow(List<SubEntity> subentities, Vector3 worldSamplePosition, Vector3 windowPosition)
		{
			gameObject.SetActive(true);
			m_worldSamplePos = worldSamplePosition;
			m_windowPos = windowPosition;

			int activeEntries = 0;
			for (int i = 0; i < subentities.Count; i++)
			{
				if (i < m_entries.Count)
				{
					m_entries[i].SetToSubEntity(subentities[i]);
				}
				else
				{
					LayerProbeEntry newEntry = Instantiate(layerProbeEntryPrefab, contentLocation).GetComponent<LayerProbeEntry>();
					newEntry.Initialise(OnEntryClicked);
					newEntry.SetToSubEntity(subentities[i]);
					m_entries.Add(newEntry);
				}
				activeEntries++;
			}
			for (; activeEntries < m_entries.Count; activeEntries++)
			{
				m_entries[activeEntries].gameObject.SetActive(false);
			}

			StartCoroutine(RepositionOnFrameEnd(windowPosition));
		}

		void OnEntryClicked(SubEntity a_subEntity)
		{
			InterfaceCanvas.Instance.propertiesWindow.ShowPropertiesWindow(a_subEntity, m_worldSamplePos, m_windowPos);
		}

		IEnumerator RepositionOnFrameEnd(Vector3 position)
		{
			yield return new WaitForEndOfFrame();
			window.SetPosition(new Vector2(position.x, position.y));
		}
	}
}
