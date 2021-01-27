using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

class TeamImporter : MonoBehaviour
{
    public Dictionary<int, Team> teams  { get; private set; }
    public MspGlobalData MspGlobalData        { get; private set; }

	public delegate void ImportCompleteCallback(bool success);
	public event ImportCompleteCallback OnImportComplete;

	/// <summary>
	/// Creates a teamimporter and starts importing teams.
	/// Values in the class can be null until finished is true.
	/// </summary>
	public void ImportGlobalData()
    {
		NetworkForm form = new NetworkForm();
		ServerCommunication.DoRequest<MspGlobalData>(Server.GetGlobalData(), form, HandleGlobalData);
	}

	public void HandleGlobalData(MspGlobalData data)
	{
		MspGlobalData = data;

		NetworkForm form = new NetworkForm();
        form.AddField("name", data.countries);
		ServerCommunication.DoRequest<LayerMeta>(Server.LayerMetaByName(), form, LoadEEZMeta);
	}

	public void LoadEEZMeta(LayerMeta eezMeta)
	{
		teams = new Dictionary<int, Team>();

		//Load countries from EEZ entity types
		foreach (KeyValuePair<int, EntityTypeValues> kvp in eezMeta.layer_type)
		{
			if (!teams.ContainsKey(kvp.Value.value))
			{
				teams.Add(kvp.Value.value, new Team(kvp.Value.value, Util.HexToColor(kvp.Value.polygonColor), kvp.Value.displayName));
			}
		}

		//Load manager and admin from global data
		teams.Add(1, new Team(1, Util.HexToColor(MspGlobalData.user_admin_color), MspGlobalData.user_admin_name));
		teams.Add(2, new Team(1, Util.HexToColor(MspGlobalData.user_region_manager_color), MspGlobalData.user_region_manager_name));


		if (OnImportComplete != null)
		{
			OnImportComplete.Invoke(true);
		}
	}
}

