public class RestrictionObject
{
	public int id { get; set; }
	public int start_layer { get; set; }
	public string start_type { get; set; }
	public ConstraintManager.EConstraintType sort { get; set; }
	public ERestrictionIssueType type { get; set; }
	public string message { get; set; }
	public int end_layer { get; set; }
	public string end_type { get; set; }
	public float value { get; set; }
}