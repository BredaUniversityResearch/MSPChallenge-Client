using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class AbstractDistributionSlider : MonoBehaviour
{
    [SerializeField]
    protected Image outline = null;
    [SerializeField]
    protected RectTransform oldValueIndicator = null;

    [SerializeField]
    protected DistributionItem parent = null;

    [HideInInspector]
    public bool ignoreSliderCallback;
	
    public virtual float Value { get; set; }
    public virtual long ValueLong { get; set; }

    public virtual float MaxValue { get; set; }
    public virtual long MaxSliderValueLong { get; set; }

    public virtual float MinValue { get; set; }
    public virtual Vector2 AvailableRange { get; set; }

    public abstract void SetAvailableRangeLong(long min, long max);
    public abstract void SetAvailableMaximumLong(long max);
    public abstract void SetOldValue(float value);
    public abstract void SetOldValue(long value);
    public abstract bool IsChanged();
    public abstract float GetNormalizedSliderValue();
    public abstract void UpdateNewValueFill();
	
    public void MarkAsChanged(bool isChanged)
    {
        outline.gameObject.SetActive(isChanged);
    }

    public abstract void SetInteractablity(bool value);
}

