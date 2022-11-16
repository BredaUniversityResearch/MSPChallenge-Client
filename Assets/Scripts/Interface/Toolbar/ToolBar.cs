using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ToolBar : MonoBehaviour
	{
		[SerializeField] Toggle m_createToggle;
		[SerializeField] Button m_undoButton;
		[SerializeField] Button m_redoButton;
		[SerializeField] Button m_deleteButton;
		[SerializeField] Button m_recallButton;
		[SerializeField] Button m_shipDirectionButton;

		bool m_ignoreCreateToggleChange;

		private void Start()
		{
			m_createToggle.onValueChanged.AddListener(CreateToggleChanged);
			m_undoButton.onClick.AddListener(() => FSM.ToolbarButtonClicked(FSM.ToolbarInput.Undo));
			m_redoButton.onClick.AddListener(() => FSM.ToolbarButtonClicked(FSM.ToolbarInput.Redo));
			m_deleteButton.onClick.AddListener(() => FSM.ToolbarButtonClicked(FSM.ToolbarInput.Delete));
			m_recallButton.onClick.AddListener(() => FSM.ToolbarButtonClicked(FSM.ToolbarInput.Recall));
			m_shipDirectionButton.onClick.AddListener(() => FSM.ToolbarButtonClicked(FSM.ToolbarInput.ChangeDirection));
		}

		public void CreateToggleChanged(bool a_value)
		{
			if (m_ignoreCreateToggleChange)
				return;
			FSM.ToolbarButtonClicked(a_value ? FSM.ToolbarInput.Create : FSM.ToolbarInput.Edit);
		}

		public void SetCreateMode(bool a_value)
		{
			m_ignoreCreateToggleChange = true;
			m_createToggle.isOn = a_value;
			m_ignoreCreateToggleChange = false;
		}

		public void SetButtonInteractable(FSM.ToolbarInput a_state, bool a_interactable)
		{
			switch(a_state)
			{
				case FSM.ToolbarInput.Undo:
					m_undoButton.interactable = a_interactable;
					break;
				case FSM.ToolbarInput.Redo:
					m_redoButton.interactable = a_interactable;
					break;
				case FSM.ToolbarInput.Delete:
					m_deleteButton.interactable = a_interactable;
					break;
				case FSM.ToolbarInput.Recall:
					m_recallButton.interactable = a_interactable;
					break;
				case FSM.ToolbarInput.Create:
					m_createToggle.interactable = a_interactable;
					break;
				case FSM.ToolbarInput.Edit:
					m_createToggle.interactable = a_interactable;
					break;
				case FSM.ToolbarInput.ChangeDirection:
					m_shipDirectionButton.interactable = a_interactable;
					break;
			}
		}

		public void SetButtonActive(FSM.ToolbarInput a_state, bool a_active)
		{
			switch (a_state)
			{
				case FSM.ToolbarInput.Undo:
					m_undoButton.gameObject.SetActive(a_active);
					break;
				case FSM.ToolbarInput.Redo:
					m_redoButton.gameObject.SetActive(a_active);
					break;
				case FSM.ToolbarInput.Delete:
					m_deleteButton.gameObject.SetActive(a_active);
					break;
				case FSM.ToolbarInput.Recall:
					m_recallButton.gameObject.SetActive(a_active);
					break;
				case FSM.ToolbarInput.Create:
					m_createToggle.gameObject.SetActive(a_active);
					break;
				case FSM.ToolbarInput.Edit:
					m_createToggle.gameObject.SetActive(a_active);
					break;
				case FSM.ToolbarInput.ChangeDirection:
					m_shipDirectionButton.gameObject.SetActive(a_active);
					break;
			}
		}
	}
}
