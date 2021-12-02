namespace Networking
{
	public static class MediaUrl
	{
		public static string Parse(string url)
		{
			string result;
			if (url.StartsWith("wiki://"))
			{
				result = url.Replace("wiki://", Main.MspGlobalData.wiki_base_url);
			}
			else
			{
				result = url;
			}

			return result;
		}
	}
}
