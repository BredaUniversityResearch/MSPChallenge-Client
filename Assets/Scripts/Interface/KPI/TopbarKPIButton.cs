using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TopbarKPIButton : MonoBehaviour
	{
		[SerializeField] private RectTransform m_windowHandle = null;
		[SerializeField] private Animator m_windowAnimator = null;
		[SerializeField] private CustomToggle m_toggle = null;
    
		void Start()
		{
			StartCoroutine(SetHandlePosition());
			m_toggle.onValueChanged.AddListener(SetWindowOpen);
		}

		IEnumerator SetHandlePosition()
		{
			yield return new WaitForEndOfFrame();
			m_windowHandle.transform.position = new Vector3(transform.position.x, m_windowHandle.transform.position.y, m_windowHandle.transform.position.z);
		}

		void SetWindowOpen(bool a_open)
		{
			m_windowAnimator.SetBool("Show", a_open);
		}
	}
}
