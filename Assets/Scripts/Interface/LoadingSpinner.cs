using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class LoadingSpinner : MonoBehaviour
	{
		[SerializeField] private float m_spinSpeed;

		private float m_rotation;

		void Update()
		{
			m_rotation += Time.deltaTime * m_spinSpeed;
			transform.rotation = Quaternion.Euler(0f, 0f, m_rotation);
		}
	}
}
