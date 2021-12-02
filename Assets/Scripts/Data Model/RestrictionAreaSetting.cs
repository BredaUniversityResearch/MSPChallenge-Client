
public class RestrictionAreaSetting
{
	public int teamId { get; private set; }
	public float restrictionSize { get; private set; }

	public RestrictionAreaSetting(int teamId, float restrictionSize)
	{
		this.teamId = teamId;
		this.restrictionSize = restrictionSize;
	}
};