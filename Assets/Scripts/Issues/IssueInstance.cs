public abstract class IssueInstance
{
	public abstract void Destroy();
	public abstract void SetLabelVisibility(bool visibility);
	public abstract bool IsLabelVisible();
	public abstract void SetLabelInteractability(bool interactability);
	public abstract void SetLabelScale(float scale);
	public abstract void CloseIfNotClickedOn();
}

