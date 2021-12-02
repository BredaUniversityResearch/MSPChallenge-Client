using UnityEngine;

[CreateAssetMenu(menuName = "MSP2050/EntityInfoTextConfig")]
public class EntityInfoTextConfig : ScriptableObject
{
	[SerializeField]
	private Vector2 backgroundExtrude = Vector2.zero;
	public Vector2 BackgroundExtrude
	{
		get
		{
			return backgroundExtrude;
		}
	}

	[SerializeField]
	private Sprite backgroundSprite = null;
	public Sprite BackgroundSprite
	{
		get
		{
			return backgroundSprite;
		}
	}

	[SerializeField]
	private Font textFont = null;
	public Font TextFont
	{
		get
		{
			return textFont;
		} 
	}

	[SerializeField]
	private float backgroundScale = 2.0f;
	public float BackgroundScale
	{
		get
		{
			return backgroundScale;
		}
	}
}
