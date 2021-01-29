public static class EnergyGridReceivedEvent
{
	public delegate void CallbackType();
	public static event CallbackType Event;

	public static void Invoke()
	{
		if (Event != null)
		{
			Event();
		}
	}
}
