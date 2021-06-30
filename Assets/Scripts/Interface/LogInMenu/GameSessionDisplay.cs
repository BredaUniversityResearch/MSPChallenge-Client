using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

class GameSessionDisplay : MonoBehaviour
{
    public delegate void SessionSelectionCallback(GameSession newSelectedGameSession);
    public SessionSelectionCallback selectionCallback;
    public Toggle toggle = null;
    public TextMeshProUGUI nameText = null;
    public TextMeshProUGUI statusText = null;
    public Image regionImage = null;

    //public Sprite nsSprite = null;
    //public Sprite bsSprite = null;
    //public Sprite crSprite = null;

    GameSession session = null;

    void Start()
    {
        toggle.onValueChanged.AddListener(b => {
            if (b && selectionCallback != null)
                selectionCallback(session);
        });
    }

    public void SetGameSession(GameSession gameSession, ToggleGroup toggleGroup)
    {
        toggle.group = toggleGroup;
        toggle.isOn = false;
        gameObject.SetActive(true);
        session = gameSession;
        nameText.text = gameSession.name;
        if (gameSession.session_state == GameSession.SessionState.Healthy)
            statusText.text = gameSession.game_state.ToString();
        else
            statusText.text = gameSession.session_state.ToString();

		regionImage.sprite = LoginMenu.Instance.regionSettings.GetRegionInfo(gameSession.region).sprite;
        //switch (gameSession.region)
        //{
        //    case "North Sea":
        //        regionImage.sprite = nsSprite;
        //        break;
        //    case "Clyde Region":
        //        regionImage.sprite = crSprite;
        //        break;
        //    case "Baltic Sea":
        //        regionImage.sprite = bsSprite;
        //        break;
        //}
    }

    public void Disable()
    {
        if (toggle.isOn)
            toggle.isOn = false;
        gameObject.SetActive(false);
    }
}
