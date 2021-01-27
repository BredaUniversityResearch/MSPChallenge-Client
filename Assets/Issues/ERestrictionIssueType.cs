/// <summary>
/// Restriction issue type. The order is sorted from most severe to least severe.
/// </summary>
public enum ERestrictionIssueType
{
	Error,		//Must be resolved
	Warning,	//Strongly advised to be resolved.
	Info,		//Informational issue
	None		//IDK, but the PlansMonitor required this.
};