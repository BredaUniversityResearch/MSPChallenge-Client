using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TeamImporter : MonoBehaviour
	{
		public Dictionary<int, Team> teams  { get; private set; }
		public MspGlobalData MspGlobalData        { get; private set; }

		public delegate void ImportCompleteCallback(bool success);
		public event ImportCompleteCallback OnImportComplete;

		//TODO: just combine this entire thing with the TeamManager and avoid the double import

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
			teams.Add(TeamManager.GM_ID, new Team(TeamManager.GM_ID, Util.HexToColor(MspGlobalData.user_admin_color), MspGlobalData.user_admin_name));
			teams.Add(TeamManager.AM_ID, new Team(TeamManager.AM_ID, Util.HexToColor(MspGlobalData.user_region_manager_color), MspGlobalData.user_region_manager_name));


			if (OnImportComplete != null)
			{
				OnImportComplete.Invoke(true);
			}
		}
	}
}

