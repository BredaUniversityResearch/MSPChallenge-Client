using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Internal;
using UnityEngine.Networking;

public class NetworkForm
{
    public List<IMultipartFormSection> Form = new List<IMultipartFormSection>();

    public NetworkForm()
    {
        //Form.Add(new MultipartFormDataSection("user", TeamManager.CurrentSessionID.ToString()));
    }

    public void AddField(string fieldName, string value)
    {
        Form.Add(new MultipartFormDataSection(fieldName, value));
    }

    public void AddField(string fieldName, int i)
    {
        Form.Add(new MultipartFormDataSection(fieldName, i.ToString()));
    }

	public void AddField<OBJECT_TYPE>(string fieldName, IEnumerable<OBJECT_TYPE> collection)
	{
		string json = JsonConvert.SerializeObject(collection);
		Form.Add(new MultipartFormDataSection(fieldName, json, "application/json"));
	}

	//public void AddField(string fieldName, JToken token)
	//{
	//	Form.Add(new MultipartFormDataSection(fieldName, token.ToObject<byte[]>()));
	//}
}