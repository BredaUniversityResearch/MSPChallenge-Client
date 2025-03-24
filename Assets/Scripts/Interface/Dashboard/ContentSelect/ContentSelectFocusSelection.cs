using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace MSP2050.Scripts
{
	public class ContentSelectFocusSelection : MonoBehaviour
	{
		[SerializeField] Button m_nextButton;
		[SerializeField] Button m_previousButton;
		[SerializeField] TextMeshProUGUI m_currentFocusText;

		int m_currentIndex;
		string[] m_focusNames;
		Action<int> m_changeCallback;

		public int CurrentIndex => m_currentIndex;
		public string CurrentOption => m_focusNames[m_currentIndex];


		private void Start()
		{
			m_nextButton.onClick.AddListener(OnNextButton);
			m_previousButton.onClick.AddListener(OnPrevButton);
		}

		public void Initialise(string[] a_focusNames, Action<int> a_changeCallback, int a_selectedFocus = 0)
		{
			m_currentIndex = a_selectedFocus;
			m_focusNames = a_focusNames; 
			m_changeCallback = a_changeCallback;
			m_currentFocusText.text = m_focusNames[m_currentIndex];
		}

		void OnNextButton()
		{
			m_currentIndex++;
			if (m_currentIndex == m_focusNames.Length)
				m_currentIndex = 0;
			m_currentFocusText.text = m_focusNames[m_currentIndex];
			m_changeCallback.Invoke(m_currentIndex);
		}

		void OnPrevButton()
		{
			m_currentIndex--;
			if (m_currentIndex == -1)
				m_currentIndex = m_focusNames.Length - 1;
			m_currentFocusText.text = m_focusNames[m_currentIndex];
			m_changeCallback.Invoke(m_currentIndex);
		}
	}
}