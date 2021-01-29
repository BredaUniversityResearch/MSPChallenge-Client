using System;
using UnityEngine;

public enum EApprovalType
{
	NotDependent,
	EEZ,
	AllCountries,
	AreaManager
}

public static class ApprovalType
{
    public static EApprovalType FromDatabaseString(string data)
    {
        if (Enum.IsDefined(typeof(EApprovalType), data))
            return (EApprovalType) Enum.Parse(typeof(EApprovalType), data);
        if (Debug.isDebugBuild)
            Debug.LogWarning(string.Format("Could not convert \"{0}\" to an appropriate approval type", data));
        return EApprovalType.NotDependent;
    }

	public static string ToDatabaseString(EApprovalType type)
	{
        return type.ToString();
	}
}
