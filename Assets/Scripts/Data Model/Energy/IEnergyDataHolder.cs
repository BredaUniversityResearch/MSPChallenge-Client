namespace MSP2050.Scripts
{
	public interface IEnergyDataHolder
	{
		long Capacity { get; set; }
		long UsedCapacity { get; set; }
		EnergyGrid LastRunGrid { get; set; }
		EnergyGrid CurrentGrid { get; set; }
	}
}

