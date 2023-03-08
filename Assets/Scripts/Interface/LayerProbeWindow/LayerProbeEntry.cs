using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LayerProbeEntry : MonoBehaviour {

		[SerializeField] TextMeshProUGUI m_layerNameText;
		[SerializeField] TextMeshProUGUI m_geomNameText;
		[SerializeField] Image m_layerIcon;
		[SerializeField] CustomButton m_barButton;

		Action<SubEntity> m_callback;
		SubEntity m_subEntity;

		private void Start()
		{
			m_barButton.onClick.AddListener(OnButtonClick);
		}

		void OnButtonClick()
		{
			m_callback.Invoke(m_subEntity);
		}

		public void Initialise(Action<SubEntity> a_callback)
		{
			m_callback = a_callback;
		}

		public void SetToSubEntity(SubEntity a_subEntity)
		{
			m_subEntity = a_subEntity;
			m_layerNameText.text = a_subEntity.m_entity.Layer.ShortName;
			string geomName = a_subEntity.m_entity.name;
			m_geomNameText.text = string.IsNullOrEmpty(geomName) ? "Unnamed" : geomName;
			m_layerIcon.sprite = LayerManager.Instance.GetSubcategoryIcon(a_subEntity.m_entity.Layer.m_subCategory);		
		}
	}
}
