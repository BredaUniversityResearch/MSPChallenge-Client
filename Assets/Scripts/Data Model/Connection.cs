using UnityEngine;

public class Connection
{
    public EnergyLineStringSubEntity cable;
    public EnergyPointSubEntity point;
    public bool connectedToFirst;

    public Connection(EnergyLineStringSubEntity cable, EnergyPointSubEntity point, bool connectedToFirst)
    {
        this.cable = cable;
        this.point = point;
        this.connectedToFirst = connectedToFirst;
    }

    //public static void SubmitCreateConnection(int startID, int endID, int cableID, Vector3 startCoords)
    //{
    //    NetworkForm form = new NetworkForm();

    //    form.AddField("start", "" + startID);
    //    form.AddField("end", "" + endID);
    //    form.AddField("cable", "" + cableID);
    //    form.AddField("coords", "[" + startCoords.x + "," + startCoords.y + "]");

    //    ServerCommunication.DoRequest(Server.CreateEnergyConection(), form, null);
    //}

    public static string GetSubmissionString(string startID, string endID, string cableID, Vector3 startCoords)
    {
        return "{\"start\":" + startID +
            ",\"end\":" + endID +
            ",\"cable\":" + cableID +
            ",\"coords\":\"[" + startCoords.x.ToString() + "," + startCoords.y.ToString() + "]\"}";
    }
}

public class DirectionalConnection
{
	public EnergyLineStringSubEntity cable;
	public EnergyPointSubEntity point;

	public DirectionalConnection(EnergyLineStringSubEntity cable, EnergyPointSubEntity point)
	{
		this.point = point;
		this.cable = cable;
	}
}