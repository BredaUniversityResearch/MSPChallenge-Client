using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class CreditElement
{
	public string text;
	public float textSize = 30.0f;
	public TextAlignment textAlign = TextAlignment.Center;
	public FontStyle fontStyle = FontStyle.Normal;
	public Sprite image;
	public Vector2 size;
	public float marginTop = 0;
	public float marginBottom = 0;
}


[ExecuteInEditMode]
public class CreditsScroll : MonoBehaviour
{

	[Header("Insert Element In Table:")]
	public bool insert = false;
	public int insertOnLine = 0;

	[Header("Delete Element In Table:")]
	public bool delete = false;
	public int deleteOnLine = 0;

	[Header("Set Font Size For All Elements")]
	public bool setFontSize = false;
	public float fontSize = 0.0f;

	[Header("List of elements to display:")]
	[SerializeField]
	public List<CreditElement> creditElements;

	[Header("Required Variables:")]
	public Canvas canvas;
	public Image backGroundSprite;
	public Color textColor;
	public Font textFont;
	public float defaultMargin = 0.0f;
	public float defaultTextSize = 20.0f;

	public float fadeInSpeed = 1.0f;
	public float fadeOutSpeed = 1.0f;


	private GameObject elementParent;
	//private GameObject backGround;
	private List<GameObject> creditRoll = new List<GameObject>();

	private float elementHeight = 0;
	private float scrollSpeed = 50.0f;

	private bool fading = false;
	private bool fadingIn = false;

	private float fadeInCounter = 0;
	private float fadeOutCounter = 1;

	private Vector3 elementParentInitPos;

	// Use this for initialization
	void OnEnable()
	{
		if (Application.isPlaying)
		{
			elementHeight = -canvas.pixelRect.height / 2.0f;

			if (!elementParent)
			{
				elementParent = new GameObject("ElementParent");
				elementParent.transform.SetParent(this.transform, false);
				elementParentInitPos = elementParent.transform.position;
			}
			elementParent.transform.position = elementParentInitPos;

			foreach (CreditElement CE in creditElements)
			{
				if (CE.text != "")
				{
					CreateTextElement(CE);
				}
				if (CE.image != null)
				{
					CreateImageElement(CE);
				}
			}

			backGroundSprite.color = new Color(255, 255, 255, 0);
			FadeBackGround(true);
		}
	}

	// Update is called once per frame
	void Update()
	{
		if (Application.isPlaying)
		{
			if (fading)
			{
				if (fadingIn)
				{
					fadeInCounter = Mathf.Clamp(fadeInCounter + Time.deltaTime * fadeInSpeed, 0.0f, 1.0f);
					backGroundSprite.color = new Color(255f, 255f, 255f, fadeInCounter* 255f);
					if (fadeInCounter == 1.0f)
					{
						fading = false;
					}
				}
				else
				{
					fadeOutCounter = Mathf.Clamp(fadeOutCounter - Time.deltaTime * fadeOutSpeed, 0.0f, 1.0f);

					foreach (GameObject CE in creditRoll)
					{
						switch (CE.name)
						{
						case "TextElement":
							CE.GetComponent<Text>().color = new Color(0, 0, 0, fadeOutCounter);
							break;
						case "ImageElement":
							CE.GetComponent<Image>().color = new Color(255f, 255f, 255f, fadeOutCounter);
							break;
						}
					}

					backGroundSprite.color = new Color(255f, 255f, 255f, fadeOutCounter);
					if (fadeOutCounter == 0)
					{
						fading = false;
						gameObject.SetActive(false);

						foreach (GameObject go in creditRoll)
						{
							Destroy(go);
						}
						creditRoll.Clear();
					}
				}
			}
			else
			{
				elementParent.transform.position += Vector3.up * Time.deltaTime * scrollSpeed;

				if (creditRoll.Count > 0)
				{
					GameObject tLastItem = creditRoll[creditRoll.Count - 1];
					if (tLastItem.transform.position.y > canvas.transform.position.y + canvas.pixelRect.height / 2.0f + 20.0f)
					{
						FadeBackGround(false);
					}
				}
				if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
				{
					FadeBackGround(false);
				}
			}
		}
		else
		{
			if (insert)
			{
				insert = false;
				if (creditElements.Count >= insertOnLine)
				{
					creditElements.Insert(insertOnLine, new CreditElement());
				}
			}
			if (delete)
			{
				delete = false;
				if (creditElements.Count >= deleteOnLine)
				{
					creditElements.Insert(deleteOnLine, new CreditElement());
				}
			}
			if (setFontSize)
			{
				setFontSize = false;
				foreach (CreditElement CE in creditElements)
				{
					CE.textSize = fontSize;
				}
			}
		}
	}

	void CreateTextElement(CreditElement CE)
	{
		var tTextElem = new GameObject("TextElement");
		var tTextMesh = tTextElem.AddComponent<Text>();
		tTextMesh.text = CE.text;
		tTextMesh.fontSize = (int)CE.textSize;
		tTextMesh.alignment = TextAnchor.MiddleCenter;
		tTextMesh.fontStyle = CE.fontStyle;
		tTextMesh.color = textColor;
		var tTextRenderer = tTextElem.GetComponent<Renderer>();
		tTextMesh.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, canvas.GetComponent<RectTransform>().sizeDelta.x);
		tTextMesh.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 30.0f);
		tTextElem.transform.SetParent(elementParent.transform, false);
		SetPosition(CE, tTextElem.transform, tTextRenderer, true);
		tTextMesh.font = textFont;
		creditRoll.Add(tTextElem);
	}

	void CreateImageElement(CreditElement CE)
	{
		var tImageElem = new GameObject("ImageElement");
		var tImage = tImageElem.AddComponent<Image>();
		tImage.sprite = CE.image;
		var tImageRenderer = tImageElem.GetComponent<Renderer>();
		tImageElem.transform.SetParent(elementParent.transform, false);
		SetPosition(CE, tImageElem.transform, tImageRenderer, false);
		creditRoll.Add(tImageElem);
	}


	void SetPosition(CreditElement CE, Transform objTrans, Renderer renderer, bool text)
	{
		//Set Pos
		elementHeight -= CE.marginTop;
		float offset = 0;
		if (text)
		{
			offset = CE.textSize;
		}
		else
		{
			var tRectTrans = (RectTransform)objTrans;
			tRectTrans.sizeDelta = CE.size;
			offset = CE.size.y;
		}
		elementHeight -= offset / 2.0f;
		objTrans.localPosition = new Vector3(0, elementHeight, 0);
		elementHeight -= offset / 2.0f;
		elementHeight -= CE.marginBottom;
		elementHeight -= defaultMargin;
	}


	void FadeBackGround(bool fadeIn)
	{
		fading = true;
		fadingIn = fadeIn;
		fadeInCounter = 0.0f;
		fadeOutCounter = 1.0f;
	}
}
