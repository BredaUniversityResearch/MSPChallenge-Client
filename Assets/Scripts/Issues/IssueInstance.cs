namespace MSP2050.Scripts
{
	public abstract class IssueInstance
	{
		public abstract void Destroy();
		public abstract void SetLabelVisibility(bool visibility);
		public abstract bool IsLabelVisible();
		public abstract void SetLabelScale(float scale);
		public abstract void CloseIfNotClickedOn();
	}
}

