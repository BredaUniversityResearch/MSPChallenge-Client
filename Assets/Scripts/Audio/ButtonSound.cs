﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	[RequireComponent(typeof(Button))]
	public class ButtonSound : MonoBehaviour, IPointerEnterHandler
	{
		public AudioSource OnClickSound;
		public AudioSource MouseEnterSound;
		public AudioSource MouseClickSound;

		void Start()
		{
			if (OnClickSound != null)
			{
				Button button = GetComponent<Button>();
				if (button != null)
				{
					button.onClick.AddListener(() => OnClickSound.Play());
				}
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (MouseEnterSound != null && GetComponent<Button>().IsInteractable())
			{
				MouseEnterSound.Play();
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (MouseClickSound != null && GetComponent<Toggle>().IsInteractable())
			{
				MouseClickSound.Play();
			}
		}
	}
}
