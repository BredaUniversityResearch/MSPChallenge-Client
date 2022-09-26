using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using System;
using System.Collections.Generic;

[AddComponentMenu("Layout/Content Size Fitter", 141)]
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class CustomContentSizeFitter : UIBehaviour, ILayoutSelfController
{
	public enum FitMode
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

	[SerializeField] protected FitMode m_HorizontalFit = FitMode.Unconstrained;

	/// <summary>
	/// The fit mode to use to determine the width.
	/// </summary>
	public FitMode horizontalFit { get { return m_HorizontalFit; } set { if (SetStruct(ref m_HorizontalFit, value)) SetDirty(); } }

	[SerializeField] protected FitMode m_VerticalFit = FitMode.Unconstrained;

	/// <summary>
	/// The fit mode to use to determine the height.
	/// </summary>
	public FitMode verticalFit { get { return m_VerticalFit; } set { if (SetStruct(ref m_VerticalFit, value)) SetDirty(); } }

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

	private void HandleSelfFittingAlongAxis(int axis)
	{
		FitMode fitting = (axis == 0 ? horizontalFit : verticalFit);
		if (fitting == FitMode.Unconstrained)
		{
			// Keep a reference to the tracked transform, but don't control its properties:
			m_Tracker.Add(this, rectTransform, DrivenTransformProperties.None);
			return;
		}

		m_Tracker.Add(this, rectTransform, (axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY));

		// Set size to min or preferred size
		if (fitting == FitMode.MinSize)
		{
			if (m_maxSize[axis] > 0.01f)
				rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, Mathf.Min(m_maxSize[axis], LayoutUtility.GetMinSize(m_Rect, axis)));
			else
				rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, LayoutUtility.GetMinSize(m_Rect, axis));
		}
		else
		{
			if (m_maxSize[axis] > 0.01f)
				rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, Mathf.Min(m_maxSize[axis], LayoutUtility.GetPreferredSize(m_Rect, axis)));
			else
				rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, LayoutUtility.GetPreferredSize(m_Rect, axis));
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

	public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
	{
		if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
		{
			return false;
		}

		currentValue = newValue;
		return true;
	}

#if UNITY_EDITOR
	protected override void OnValidate()
	{
		SetDirty();
	}

#endif
}

