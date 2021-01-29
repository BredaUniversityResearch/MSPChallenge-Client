/// <summary>
/// This is what you are comparing to source to
/// ("restriction_end_layer_id" and "restriction_end_layer_type" in server)
/// Includes other things like message etc
/// </summary>
public class ConstraintTarget
{
	public readonly int constraintId;
	public readonly AbstractLayer layer;
	public readonly EntityType entityType;
	public readonly ERestrictionIssueType issueType;
	public readonly string message;
	public readonly float value;

	public ConstraintTarget(int constraintId, AbstractLayer layer, EntityType entityType, ERestrictionIssueType issueType, string message, float value)
	{
		this.constraintId = constraintId;
		this.layer = layer;
		this.entityType = entityType;
		this.issueType = issueType;
		this.message = message;
		this.value = value;
	}
}