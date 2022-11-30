using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public class SearchBar : MonoBehaviour
{
    [SerializeField] Button m_clearButton;
    [SerializeField] TMP_InputField m_inputField;

    public Action<string> m_ontextChange;
    public string Text => m_inputField.text;

    void Start()
    {
        m_inputField.onValueChanged.AddListener(OnTextChange);
        m_clearButton.onClick.AddListener(ResetText);
        ResetText();
	}

    void ResetText()
	{
        m_inputField.text = "";
    }

    void OnTextChange(string a_newText)
	{
        m_clearButton.interactable = !string.IsNullOrEmpty(a_newText);
        m_ontextChange?.Invoke(a_newText);
    }
}

