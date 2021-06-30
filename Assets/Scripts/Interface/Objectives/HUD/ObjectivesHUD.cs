//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections.Generic;
//using System;
//using DG.Tweening;

//public class ObjectivesHUD : MonoBehaviour
//{

//	public Text label;
//	public Button headerBar;
//	public Animator anim;
//	public Image arrow, handle;

//	[Header("Prefabs")]
//	public HUDObjective objectivePrefab;
//	public Transform objectiveLocation;
//	public List<HUDObjective> objectives = new List<HUDObjective>();
//	public LayoutElement spacerPrefab;

//	private void Awake()
//	{
//		SetAccent(ColorPalette.instance.accent);
//		anim.SetBool("Open", false);
//	}

//	public void TrackObjective(MonitorObjective monitorObjective, bool dir)
//	{
//		//Debug.Log(monitorObjective);
//		//Debug.Log(dir);
//		if (dir)
//		{
//			HUDObjective hudObjective = Instantiate<HUDObjective>(objectivePrefab);
//			objectives.Insert(0, hudObjective);
//			hudObjective.transform.SetParent(objectiveLocation, false);
//			hudObjective.transform.SetAsFirstSibling();

//			// Set objective stuff
//			hudObjective.CopyObjectiveDataFrom(monitorObjective);
//			hudObjective.allCountries.gameObject.SetActive((monitorObjective.country.color == Color.black));

//			UpdateSpacing();

//			hudObjective.monitorObjective = monitorObjective;
//			monitorObjective.hudObjective = hudObjective;

//			InsertObjective(hudObjective, true); // Required for the fading animation
//		}
//		else
//		{
//			for (int i = 0; i < objectives.Count; i++)
//			{
//				if (objectives[i].monitorObjective == monitorObjective)
//				{
//					UntrackObjective(objectives[i]);
//				}
//			}
//		}
//	}

//	/// <summary>
//	/// Mark objective as complete and call method to take it out of the HUD
//	/// </summary>
//	/// <param name="obj">The objective to take out</param>
//	public void CompleteObjective(HUDObjective obj)
//	{
//		objectives.Remove(obj);

//		// Color everything in accent color
//		Color accent = ColorPalette.instance.accent;
//		obj.SetAccent(accent);
//		obj.allCountries.color = accent;

//		InsertObjective(obj, false, 1f);
//		PulseHeader();
//	}

//	/// <summary>
//	/// Destroy the given objective
//	/// </summary>
//	/// <param name="obj">The objective to destroy</param>
//	/// <param name="immediate">Default is fade out. True removes instantly</param>
//	public void UntrackObjective(HUDObjective obj, bool immediate = false)
//	{
//		objectives.Remove(obj);

//		if (immediate)
//			Destroy(obj.gameObject);
//		else
//			InsertObjective(obj, false);
//	}

//	/// <summary>
//	/// Insert an objective into the HUD
//	/// </summary>
//	/// <param name="obj">The objective to insert</param>
//	/// <param name="dir">Whether to insert or take out</param>
//	/// <param name="duration">How fast the animation should take</param>
//	private void InsertObjective(HUDObjective obj, bool dir, float duration = 0.5f)
//	{
//		// Force update to get layout height
//		Canvas.ForceUpdateCanvases();
//		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)objectiveLocation.transform);

//		// Create and position the spacer
//		LayoutElement insertElement = Instantiate<LayoutElement>(spacerPrefab);
//		insertElement.transform.SetParent(objectiveLocation, false);
//		insertElement.transform.SetSiblingIndex(obj.transform.GetSiblingIndex());

//		Sequence seq = DOTween.Sequence() // Create new sequence
//			.OnComplete(() => obj.inTransition = false); // Mark object transition as done when complete

//		obj.inTransition = true;

//		// Inserting a task
//		if (dir)
//		{
//			// Init
//			obj.gameObject.SetActive(false);
//			obj.ToggleAlpha(false);
//			float firstObjectiveDuration = (objectives.Count == 1) ? 0f : duration;

//			// Sequence
//			seq.Append(insertElement.DOPreferredSize(new Vector2(0f, obj.rectTrans.sizeDelta.y), firstObjectiveDuration)); // Lerp spacer height
//			seq.AppendCallback(() => Destroy(insertElement.gameObject)); // Destroy spacer
//			seq.AppendCallback(() => obj.gameObject.SetActive(true)); // Enable objective
//			seq.AppendCallback(() => obj.ToggleLerpAlpha(true, duration)); // Fade in objective
//		}
//		// Taking out a task
//		else
//		{
//			// Init
//			//obj.gameObject.SetActive(true);
//			//obj.ToggleAlpha(true);
//			insertElement.gameObject.SetActive(false); // Disable spacer, we need to wait for objective to fade first

//			// Sequence
//			obj.ToggleLerpAlpha(false, duration); // Fade out objective
//			seq.PrependInterval(duration); // Wait for alpha to lerp
//			seq.AppendCallback(() => Destroy(obj.gameObject)); // Destroy objective
//			seq.AppendCallback(() => insertElement.gameObject.SetActive(true)); // Enable spacer
//			seq.Append(insertElement.DOPreferredSize(new Vector2(0f, obj.rectTrans.sizeDelta.y), duration)  // Lerp spacer height
//				.From()); // Lerp to from instead of to
//			seq.AppendCallback(() => Destroy(insertElement.gameObject)); // When done, destroy spacer
//		}

//		UpdateHeader();
//	}

//	/// <summary>
//	/// Toggle the HUD on/off
//	/// </summary>
//	public void ToggleHUD()
//	{
//		if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1f)
//		{
//			anim.SetBool("Open", !anim.GetBool("Open"));
//		}
//	}

//	/// <summary>
//	/// Update header text
//	/// </summary>
//	private void UpdateHeader()
//	{
//		string title = "Objectives";

//		if (objectives.Count > 0)
//		{
//			label.text = title + " (" + objectives.Count.ToString() + ")";
//		}
//		else
//		{
//			label.text = title;
//		}
//	}

//	/// <summary>
//	/// Pulses the header bar with the accent color
//	/// </summary>
//	private void PulseHeader(float duration = 1f)
//	{
//		headerBar.targetGraphic.raycastTarget = false; // Disables interaction with the button
//		anim.SetTrigger("Pulse"); // Lerps the button normal color from white to default
//		headerBar.targetGraphic.color = ColorPalette.instance.accent; // Set the graphic on the button to accent color
//		headerBar.targetGraphic.DOBlendableColor(Color.white, duration) // Lerp from accent color back to white
//			.OnComplete(() => headerBar.targetGraphic.raycastTarget = true); // When animation is done, Re-enables interaction with the button
//	}

//	/// <summary>
//	/// This updates the bottom spacing of an objective.
//	/// Parent layout spacing can't be used or it will cause layout problems when inserting an objective.
//	/// </summary>
//	private void UpdateSpacing()
//	{
//		Transform lastObjective = objectives[objectives.Count - 1].transform;
//		Transform lastTransform = objectiveLocation.GetChild(objectiveLocation.childCount - 1);

//		for (int i = 0; i < objectives.Count; i++)
//		{
//			objectives[i].layout.padding.bottom = (lastTransform == lastObjective) ? 0 : 10;
//		}
//	}

//	public void SetAccent(Color col)
//	{
//		arrow.color = col;
//		handle.color = col;
//	}
//}