using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("Layout/Content Size Fitter", 141)]
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class CustomContentSizeFitter : UIBehaviour, ILayoutSelfController
{
	public enum EFitMode
	{
		/// <summary>
		/// Don't perform any resizing.
		/// </summary>
		Unconstrained,
		/// <summary>
		/// Resize to the minimum size of the content.
		/// </summary>
		MinSize,
		/// <summary>
		/// Resize to the preferred size of the content.
		/// </summary>
		PreferredSize
	}

	[SerializeField] protected EFitMode m_HorizontalFit = EFitMode.Unconstrained;

	/// <summary>
	/// The fit mode to use to determine the width.
	/// </summary>
	public EFitMode horizontalFit { get { return m_HorizontalFit; } set { if (SetStruct(ref m_HorizontalFit, value)) SetDirty(); } }

	[SerializeField] protected EFitMode m_VerticalFit = EFitMode.Unconstrained;

	/// <summary>
	/// The fit mode to use to determine the height.
	/// </summary>
	public EFitMode verticalFit { get { return m_VerticalFit; } set { if (SetStruct(ref m_VerticalFit, value)) SetDirty(); } }

	[SerializeField] Vector2 m_maxSize;

	[System.NonSerialized] private RectTransform m_Rect;
	private RectTransform rectTransform
	{
		get
		{
			if (m_Rect == null)
				m_Rect = GetComponent<RectTransform>();
			return m_Rect;
		}
	}

	private DrivenRectTransformTracker m_Tracker;

	protected override void OnEnable()
	{
		base.OnEnable();
		SetDirty();
	}

	protected override void OnDisable()
	{
		m_Tracker.Clear();
		LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
		base.OnDisable();
	}

	protected override void OnRectTransformDimensionsChange()
	{
		SetDirty();
	}

	private void HandleSelfFittingAlongAxis(int a_axis)
	{
		EFitMode fitting = (a_axis == 0 ? horizontalFit : verticalFit);
		if (fitting == EFitMode.Unconstrained)
		{
			// Keep a reference to the tracked transform, but don't control its properties:
			m_Tracker.Add(this, rectTransform, DrivenTransformProperties.None);
			return;
		}

		m_Tracker.Add(this, rectTransform, (a_axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY));

		// Set size to min or preferred size
		if (fitting == EFitMode.MinSize)
		{
			if (m_maxSize[a_axis] > 0.01f)
				rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)a_axis, Mathf.Min(m_maxSize[a_axis], LayoutUtility.GetMinSize(m_Rect, a_axis)));
			else
				rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)a_axis, LayoutUtility.GetMinSize(m_Rect, a_axis));
		}
		else
		{
			if (m_maxSize[a_axis] > 0.01f)
				rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)a_axis, Mathf.Min(m_maxSize[a_axis], LayoutUtility.GetPreferredSize(m_Rect, a_axis)));
			else
				rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)a_axis, LayoutUtility.GetPreferredSize(m_Rect, a_axis));
		}
	}

	/// <summary>
	/// Calculate and apply the horizontal component of the size to the RectTransform
	/// </summary>
	public virtual void SetLayoutHorizontal()
	{
		m_Tracker.Clear();
		HandleSelfFittingAlongAxis(0);
	}

	/// <summary>
	/// Calculate and apply the vertical component of the size to the RectTransform
	/// </summary>
	public virtual void SetLayoutVertical()
	{
		HandleSelfFittingAlongAxis(1);
	}

	protected void SetDirty()
	{
		if (!IsActive())
			return;

		LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
	}

	public static bool SetStruct<T>(ref T a_currentValue, T a_newValue) where T : struct
	{
		if (EqualityComparer<T>.Default.Equals(a_currentValue, a_newValue))
		{
			return false;
		}

		a_currentValue = a_newValue;
		return true;
	}

#if UNITY_EDITOR
	protected override void OnValidate()
	{
		SetDirty();
	}

#endif
}

