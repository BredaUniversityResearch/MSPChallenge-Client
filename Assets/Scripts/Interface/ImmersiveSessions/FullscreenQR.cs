using QRCoder;
using QRCoder.Unity;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class FullscreenQR : MonoBehaviour
	{
		public RawImage m_qrImage;
		[SerializeField] Button m_closeButton;

		private void Start()
		{
			m_closeButton.onClick.AddListener(Close);
		}

		void Close()
		{
			Destroy(gameObject);
		}
	}
}
