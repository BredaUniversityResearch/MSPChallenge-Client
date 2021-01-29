using UnityEngine;

public class TimeManagerWindow : MonoBehaviour {

    private static TimeManagerWindow singleton;

    public static TimeManagerWindow instance
    {
        get
        {
            if (singleton == null)
                singleton = (TimeManagerWindow)FindObjectOfType(typeof(TimeManagerWindow));
            return singleton;
        }
    }

    public GenericWindow thisGenericWindow;
    public TimeManagerEraDivision eraDivision;
    public TimeManagerTimeline timeline;
    public TimeManagerControls controls;

    private void Awake()
    {
        if (singleton == null)
            singleton = this;
        gameObject.SetActive(false);
    }

}