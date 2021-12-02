using UnityEngine;
using UnityEngine.UI;

public class UnLoadingScreen : MonoBehaviour
{
    [SerializeField]
    private Text editionText = null;
    //[SerializeField]
    //private Image loadingScreenImage = null;

    public Image mspIcon;

    //public Sprite balticline;
    //public Sprite simcelt;
    //public Sprite northsee;


    protected void Start()
    {
        //Give Loading screen random texture from resources
        //Sprite[] tList = Resources.LoadAll<Sprite>(@"LoadingScreenImages");
        //if (tList.Length > 0)
        //{
        //    Sprite tRandSprite = tList[Random.Range(0, tList.Length)];
        //    loadingScreenImage.sprite = tRandSprite;
        //}
        //else
        //{
        //    Debug.LogError(@"Assets\Interface\Sprites\Loading Screen\Resources\LoadingScreenImages does not contain any background images.");
        //}

        if (Main.MspGlobalData != null)
        {
            SetIcon();
        }
        else
        {
            Main.OnGlobalDataLoaded += GlobalDataLoaded;
        }        
    }

    void GlobalDataLoaded()
    {
        Main.OnGlobalDataLoaded -= GlobalDataLoaded;
        SetIcon();
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    void SetIcon()
    {
        if (Main.MspGlobalData != null)
        {
			RegionInfo region = InterfaceCanvas.Instance.regionSettings.GetRegionInfo(Main.MspGlobalData.region);
			mspIcon.sprite = region.sprite;
			editionText.text = region.editionPostFix;

			//switch (Main.MspGlobalData.region)
   //         {
   //             case "balticline":
   //                 mspIcon.sprite = balticline;
   //                 editionText.text = "Baltic Sea Edition";
   //                 break;
   //             case "simcelt":
   //                 mspIcon.sprite = simcelt;
   //                 editionText.text = "Clyde Edition";
   //                 break;
   //             case "northsee":
   //                 mspIcon.sprite = northsee;
   //                 editionText.text = "North Sea Edition";
   //                 break;
   //         }
        }
    }
}

