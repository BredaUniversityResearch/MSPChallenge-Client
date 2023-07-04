using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class TeamWindow : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_regionText;
		[SerializeField] TextMeshProUGUI m_teamText;
		[SerializeField] Image m_teamBubble;
		[SerializeField] Button m_moreInfoButton;
		[SerializeField] Transform m_teamMemberParent;
		[SerializeField] GameObject m_teamMemberPrefab;
		[SerializeField] GameObject m_loadingSpinner;

		List<TextMeshProUGUI> m_teamMemberEntries = new List<TextMeshProUGUI>();

		void Start()
		{
			m_moreInfoButton.onClick.AddListener(OnMoreInfoClicked);
			m_regionText.text = SessionManager.Instance.MspGlobalData.edition_name;
			m_teamBubble.color = SessionManager.Instance.CurrentTeam.color;
		}

		void OnEnable()
		{
			m_teamText.text = $"{SessionManager.Instance.CurrentTeam.name} Team";
			m_teamMemberParent.gameObject.SetActive(false);
			m_loadingSpinner.gameObject.SetActive(true);

			NetworkForm form = new NetworkForm();
			form.AddField("country_id", SessionManager.Instance.CurrentUserTeamID);
			ServerCommunication.Instance.DoRequest<List<UserInfo>>(Server.GetUserList(), form, OnUserListReceived);
		}

		private void OnDisable()
		{
			InterfaceCanvas.Instance.menuBarLogo.toggle.isOn = false;
		}

		void OnMoreInfoClicked()
		{
			Application.OpenURL(Path.Combine(SessionManager.Instance.MspGlobalData.team_info_base_url, SessionManager.Instance.CurrentTeam.name));
		}

		void OnUserListReceived(List<UserInfo> a_list)
		{
			m_teamText.text = $"{SessionManager.Instance.CurrentTeam.name} Team - {a_list.Count} player(s) logged in";
			m_teamMemberParent.gameObject.SetActive(true);
			m_loadingSpinner.gameObject.SetActive(false);

			int i = 0; 
			for(; i < a_list.Count; i++)
			{
				if(i < m_teamMemberEntries.Count)
				{
					m_teamMemberEntries[i].text = "- " + a_list[i].user_name;
					m_teamMemberEntries[i].gameObject.SetActive(true);
				}
				else
				{
					TextMeshProUGUI newEntry = Instantiate(m_teamMemberPrefab, m_teamMemberParent).GetComponent<TextMeshProUGUI>();
					newEntry.text = "- " + a_list[i].user_name;
					m_teamMemberEntries.Add(newEntry);
				}
			}
			for(; i < m_teamMemberEntries.Count; i++)
			{
				m_teamMemberEntries[i].gameObject.SetActive(false);
			}
		}
	}

	public struct UserInfo
	{
		public int user_id;
		public int user_country_id;
		public string user_name;
	}
}