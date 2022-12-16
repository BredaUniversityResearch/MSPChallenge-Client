using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class TimelineButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		[HideInInspector] public int month;
		public Button button;
		public RectTransform rect;
		public LayoutElement layout;
		public Image large, multiplPlansIcon;
		public TextMeshProUGUI monthNameText; //Only used for inspect buttons

		private TimelineTrack track;

		public List<Plan> plans { get; private set; }

		void Start()
		{
			track = GetComponentInParent<TimelineTrack>();
		}

		public void SetButtonColor(Color col)
		{
			large.color = col;
			if (multiplPlansIcon != null)
			{
				multiplPlansIcon.gameObject.SetActive(false);
			}
		}

		public void SetMonth(int month, Transform parentBar)
		{
			this.month = month;
			float totalMonths = (float)SessionManager.Instance.MspGlobalData.session_end_month;
		
			rect.anchorMax = new Vector2( (float)month / totalMonths, rect.anchorMax.y);
			rect.anchorMin = new Vector2( (float)month / totalMonths, rect.anchorMax.y);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (layout.ignoreLayout)
				track.BringToFront(rect);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (layout.ignoreLayout)
				track.ResetOrder();
		}

		public void AddPlan(Plan plan)
		{
			if (plans == null)
				plans = new List<Plan>();
			plans.Add(plan);
			if (plans.Count == 1)
			{
				SetToolTip(plan.Name + ", " + Util.MonthToYearText(month));
			}
			if (plans.Count > 1)
			{
				multiplPlansIcon.gameObject.SetActive(true);
				SetToolTip("Multiple plans, " + Util.MonthToYearText(month));
			}
		}

		/// <summary>
		/// Return wether this button has been deleted.
		/// </summary>
		public bool RemovePlan(Plan plan)
		{
			plans.Remove(plan);
			if (plans.Count == 1)
			{
				multiplPlansIcon.gameObject.SetActive(false);
				SetToolTip(plans[0].Name + ", " + Util.MonthToYearText(month));
			}
			else if (plans.Count == 0)
			{
				Destroy(gameObject);
				return true;
			}
			return false;
		}

		public void SetToolTip(string text)
		{
			AddTooltip tooltip = GetComponent<AddTooltip>();
			if (tooltip == null)
				tooltip = gameObject.AddComponent<AddTooltip>();
			tooltip.text = text;
		}
	}
}