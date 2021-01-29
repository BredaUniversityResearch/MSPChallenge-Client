using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class WarningLabel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public delegate void InspectIssueCallback();

	public Button button;
	public TextMeshProUGUI boxText;
	public Animator anim;
	private bool pointerHover;
	private InspectIssueCallback onInspectIssue = null;

	private void OnEnable()
	{
		anim.SetBool("Appear", false);
	}

	public void EnableInspectIssueButton(InspectIssueCallback callback)
	{
		onInspectIssue = callback;
	}

	public void LabelType(ERestrictionIssueType issueType)
	{
		ColorBlock issueColorBlock = button.colors;
		switch (issueType)
		{
		case ERestrictionIssueType.Warning:
			issueColorBlock.normalColor = new Color(1f, 250f / 255, 40f / 255);
			break;
		case ERestrictionIssueType.Error:
			issueColorBlock.normalColor = new Color(1f, 84f / 255, 84f / 255);
			break;
		case ERestrictionIssueType.Info:
			issueColorBlock.normalColor = new Color(123f / 255f, 216 / 255f, 246f / 255f);
			break;
		default:
			Debug.Log("Unknown restriction issue type " + issueType);
			break;
		}
		button.colors = issueColorBlock;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		pointerHover = true;
		CameraManager.Instance.canIZoom = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		pointerHover = false;
		CameraManager.Instance.canIZoom = false;
	}

	public void ToggleDetails()
	{
		anim.SetBool("Appear", !anim.GetBool("Appear"));
		this.gameObject.transform.SetAsLastSibling();
	}

	public void OnShowLayersButtonClick()
	{
		if (onInspectIssue != null)
		{
			onInspectIssue();
		}
	}

	public void CloseIfNotClickedOn()
	{
		if (gameObject.activeInHierarchy)
		{
			if (!pointerHover && anim.GetBool("Appear"))
			{
				anim.SetBool("Appear", false);
			}
		}
	}

	public void SetVisible(bool visibility)
	{
		gameObject.SetActive(visibility);
	}

	public bool IsVisible()
	{
		return gameObject.activeSelf;
	}

	public void SetScale(float scale)
	{
		gameObject.transform.localScale = new Vector3(scale, scale, 1.0f);
	}

	public void SetLabelOpenState(bool labelVisible)
	{
		anim.SetBool("Appear", labelVisible);
	}

	public void SetInteractability(bool interactability)
	{
		button.interactable = interactability;
	}
}
