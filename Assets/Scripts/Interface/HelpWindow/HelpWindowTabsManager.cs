using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HelpWindowTabsManager : MonoBehaviour
{
    [SerializeField] float m_fadeTime;
    [SerializeField] CanvasGroup[] m_subGroups;
    [SerializeField] Button[] m_buttons;
    [SerializeField] Button m_returnButton;

    CanvasGroup m_current;

    private void Awake()
    {
        m_current = m_subGroups[0];
        for(int i = 0; i < m_buttons.Length; i++)
        {
            int index = i;
            m_buttons[i].onClick.AddListener( () =>
            {
                FadeTo(m_subGroups[index]);
                m_returnButton.gameObject.SetActive(index != 0);

            });
        }
    }

    void FadeTo(CanvasGroup a_target)
    {
        StartCoroutine(FadeFromTo(a_target));
    }

    IEnumerator FadeFromTo(CanvasGroup a_to)
    {
        float timepassed = 0;
        m_current.interactable = false;
        a_to.interactable = false;
        a_to.gameObject.SetActive(true);

        while(timepassed < m_fadeTime)
        {
            yield return 0;
            timepassed += Time.deltaTime;
            float t = timepassed / m_fadeTime;
            m_current.alpha = 1f - t;
            a_to.alpha = t;
        }
        a_to.interactable = true;
        a_to.alpha = 1f;
        m_current.gameObject.SetActive(false);
        m_current = a_to;
    }

}
