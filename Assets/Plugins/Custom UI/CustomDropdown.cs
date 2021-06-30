using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CustomDropdown : TMP_Dropdown
{
    public delegate void InteractabilityChangeCallback(bool newState);
    public InteractabilityChangeCallback interactabilityChangeCallback;

	protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        if (interactabilityChangeCallback != null)
            interactabilityChangeCallback(state != SelectionState.Disabled);
    }

    protected override GameObject CreateBlocker(Canvas rootCanvas)
    {
        // Create blocker GameObject.
        GameObject blocker = base.CreateBlocker(rootCanvas);

        LayoutElement layout = blocker.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;

        return blocker;
    }
}
