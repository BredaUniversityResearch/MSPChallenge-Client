/// <summary>
/// Stores objective details
/// </summary>
public class ObjectiveDetails
{
	public readonly int objectiveId;
	public readonly string title; // Title of the objective
	public readonly string description; // Description of the objective
	public readonly int deadlineMonth; // Month at which this objective targets
	public readonly int appliesToCountry; // The country it applies to, -1 means it applies to all countries
	public bool completed = false;

	public ObjectiveDetails(ObjectiveObject objectiveObject)
	{
		title = objectiveObject.title;
		description = objectiveObject.description;
		deadlineMonth = objectiveObject.deadline;
		objectiveId = objectiveObject.objective_id;
		completed = objectiveObject.complete;
		appliesToCountry = objectiveObject.country_id;
	}
}