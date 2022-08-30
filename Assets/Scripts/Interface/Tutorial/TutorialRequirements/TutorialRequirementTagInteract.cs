using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialRequirementTagInteract : ATutorialRequirement
	{
		[SerializeField] private string[] m_tags;
		[SerializeField] private bool m_matchAll;
		private bool m_complete;

		public override bool EvaluateRequirement()
		{
			return m_complete;
		}

		public virtual void ActivateRequirement()
		{
			m_complete = false;
			InterfaceCanvas.Instance.RegisterInteractionListener(OnInteract);
		}

		public virtual void DeactivateRequirement()
		{
			InterfaceCanvas.Instance.UnregisterInteractionListener(OnInteract);
		}

		void OnInteract(string a_name, string[] a_tags)
		{
			if (m_complete)
				return;
			if (m_matchAll)
			{
				bool match = true;
				foreach (string tag in m_tags)
				{
					bool found = false;
					foreach (string incomingTag in a_tags)
					{
						if (string.Equals(incomingTag, tag))
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						match = false;
						break;
					}
				}

				if (match)
				{
					m_complete = true;
				}
			}
			else //Match any
			{
				foreach (string tag in m_tags)
				{
					foreach (string incomingTag in a_tags)
					{
						if (string.Equals(incomingTag, tag))
						{
							m_complete = true;
							break;
						}
					}
				}
			}
		}
	}
}
